import { BeamWebSocketError } from '@/constants/Errors';
import { wait } from '@/utils/wait';
import {
  promiseWithResolvers,
  PromiseWithResolversPolyfill,
} from '@/utils/promiseWithResolvers';
import {
  authPostTokensRefreshToken,
  realmsGetClientDefaultsBasic,
} from '@/__generated__/apis';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { getUserDeviceAndPlatform } from '@/utils/getUserDeviceAndPlatform';

interface BeamWebSocketConnectParams {
  requester: HttpRequester;
  cid: string;
  pid: string;
  refreshToken: string;
  /** The API base URL the SDK is pointed at (used to retarget a loopback socket host). */
  apiUrl: string;
}

export class BeamWebSocket {
  private url = '';
  private apiUrl = '';
  private cid = '';
  private pid = '';
  private refreshToken = '';
  private socket?: WebSocket;
  private requester?: HttpRequester;
  private connectPromiseWithResolvers?: PromiseWithResolversPolyfill;
  private isDisconnecting = false;
  private isReconnecting = false;
  private reconnectAttempts = 0;
  private maxRetries = 3;
  /**
   * Message listeners owned by this wrapper rather than by a `WebSocket` instance.
   *
   * `reconnect()` builds a brand-new `WebSocket` and reassigns `this.socket`, so a listener
   * attached directly to `rawSocket` is bound to the discarded object and silently stops firing
   * after the first reconnect. Keeping them here and re-attaching in `initWebSocket` is what makes
   * a subscription survive a dropped connection.
   */
  private messageListeners = new Set<(event: MessageEvent) => void>();

  private async initWebSocket(): Promise<void> {
    let accessToken: string | null = null;
    try {
      const [token] = await Promise.all([
        this.getAccessToken(),
        this.setWebSocketUrl(),
      ]);
      accessToken = token;
    } catch (error) {
      if (error instanceof BeamWebSocketError) {
        return this.connectPromiseWithResolvers?.reject(error);
      }
    }

    if (!accessToken) {
      return this.connectPromiseWithResolvers?.reject(
        new BeamWebSocketError(
          'Failed to obtain access token for WebSocket connection',
        ),
      );
    }

    // Create a new WebSocket connection
    const socket = new WebSocket(
      `${this.url}/connect?access_token=${accessToken}&send-session-start=true`,
    );
    this.socket = socket;

    // Web socket open event handler
    socket.onopen = () => this.handleOpen();
    // Web socket message event handler — dispatches to the wrapper's own listener list, which is
    // why it is re-attached here (the single socket-creation point) on every reconnect.
    socket.onmessage = (event) => this.handleMessage(event);
    // Web socket error event handler
    socket.onerror = (event) => this.handleError(event);
    // Web socket close event handler
    socket.onclose = (event) => this.handleClose(event);
  }

  /**
   * Fan one frame out to every registered listener.
   *
   * Each listener is invoked in its own try/catch. That is load-bearing, not defensive dressing:
   * before this list existed each subscription had its own `addEventListener` and the DOM isolated
   * their failures, so one throwing handler could not starve the others. Iterating ourselves
   * removes that isolation, and without the guard a single bad handler would abort the loop and
   * silently drop every listener registered after it.
   */
  private handleMessage(event: MessageEvent): void {
    for (const listener of [...this.messageListeners]) {
      try {
        listener(event);
      } catch (e) {
        console.warn('A websocket message listener threw:', e);
      }
    }
  }

  /**
   * Registers a listener for every message frame on this socket.
   *
   * Prefer this over `rawSocket.addEventListener`: listeners registered here are re-attached to the
   * new underlying socket on reconnect, and are isolated from each other's failures.
   *
   * @param listener Called with each raw `MessageEvent`. Filtering is the caller's job.
   * @returns {void}
   */
  addListener(listener: (event: MessageEvent) => void): void {
    this.messageListeners.add(listener);
  }

  /**
   * Removes a previously registered message listener. Passing a function that was never added, or
   * was already removed, is a no-op.
   * @returns {void}
   */
  removeListener(listener: (event: MessageEvent) => void): void {
    this.messageListeners.delete(listener);
  }

  private handleOpen() {
    this.reconnectAttempts = 0;
    // The server's `&send-session-start=true` query param tells the gateway to
    // expect a `session-start` frame as the very first message on this socket.
    // Browsers can't set request headers on the WebSocket upgrade, so we send
    // the device info as a frame instead. If the frame is missing or malformed,
    // the server will close the connection.
    this.sendSessionStartFrame();
    this.connectPromiseWithResolvers?.resolve();
  }

  private sendSessionStartFrame(): void {
    if (!this.socket) return;
    try {
      this.socket.send(JSON.stringify(buildSessionStartFrame()));
    } catch (e) {
      console.warn('Failed to send session-start frame:', e);
    }
  }

  private handleError(e: Event) {
    if (
      this.socket?.readyState === WebSocket.OPEN ||
      this.socket?.readyState === WebSocket.CONNECTING
    ) {
      // If the socket is still open or connecting, we can try to reconnect
      this.socket.close();
    } else {
      this.connectPromiseWithResolvers?.reject(
        new BeamWebSocketError('WebSocket error occurred', { cause: e }),
      );
    }
  }

  private async handleClose(e: CloseEvent) {
    // if explicitly called disconnect(), don't reconnect
    if (this.isDisconnecting) return;

    console.warn('WebSocket closed:', e.code, e.reason);
    if (this.reconnectAttempts < this.maxRetries) {
      await this.reconnect();
    } else {
      this.connectPromiseWithResolvers?.reject(
        new BeamWebSocketError(
          'Maximum web socket reconnect attempts reached',
          { cause: e },
        ),
      );
    }
  }

  private async setWebSocketUrl(): Promise<void> {
    if (!this.requester) throw new BeamWebSocketError('No requester provided');

    if (this.url) return; // URL already set

    const realmConfigResponse = await realmsGetClientDefaultsBasic(
      this.requester,
    );
    const realmConfig = realmConfigResponse.body;
    if (realmConfig.websocketConfig.provider === 'pubnub') {
      // Web SDK does not support pubnub
      throw new BeamWebSocketError(
        'Unsupported websocket provider. Configure your Realm in portal to include: namespace=notification, key=publisher, value=beamable.',
      );
    }

    const url = realmConfig.websocketConfig.uri;
    if (!url) throw new BeamWebSocketError('No websocket URL found');

    this.url = this.retargetLoopbackHost(url);
  }

  /**
   * A realm's client-defaults can advertise a loopback websocket host (e.g. a local stack
   * returns `ws://localhost:8080`). On a phone/emulator `localhost` resolves to the device
   * itself, so the socket can never connect (close 1006) even though REST works — REST uses
   * the configured {@link apiUrl}, while this socket URI is taken verbatim from the server.
   * When the advertised host is loopback but the SDK is talking to a real host, retarget the
   * socket at that same host. Mirrors BeamServerWebSocket's localhost rewrite.
   */
  private retargetLoopbackHost(wsUri: string): string {
    const loopback = new Set(['localhost', '127.0.0.1']);
    try {
      const ws = new URL(wsUri);
      if (!loopback.has(ws.hostname) || !this.apiUrl) return wsUri;

      const api = new URL(this.apiUrl);
      if (loopback.has(api.hostname)) return wsUri; // nothing better to point at

      ws.hostname = api.hostname;
      // Preserve the socket's own scheme/port; drop the trailing slash URL adds so the
      // downstream `${url}/connect` template doesn't produce a double slash.
      return ws.toString().replace(/\/$/, '');
    } catch {
      return wsUri; // non-URL value — leave it untouched
    }
  }

  private async getAccessToken(): Promise<string | null> {
    if (!this.requester) return null;

    try {
      // Fetch the access token for the new connection
      const accessTokenResponse = await authPostTokensRefreshToken(
        this.requester,
        {
          customerId: this.cid,
          realmId: this.pid,
          refreshToken: this.refreshToken,
        },
      );
      return accessTokenResponse.body.accessToken ?? null;
    } catch {
      return null;
    }
  }

  get rawSocket() {
    return this.socket;
  }

  /**
   * Opens a WebSocket connection to the Beamable server.
   *
   * @param {BeamWebSocketConnectParams} params - The connection parameters.
   * @returns {Promise<void>}
   */
  async connect(params: BeamWebSocketConnectParams): Promise<void> {
    this.requester = params.requester;
    this.cid = params.cid;
    this.pid = params.pid;
    this.refreshToken = params.refreshToken;
    this.apiUrl = params.apiUrl;
    this.isDisconnecting = false;
    this.reconnectAttempts = 0;
    this.connectPromiseWithResolvers = promiseWithResolvers();
    await this.initWebSocket();
    return this.connectPromiseWithResolvers.promise;
  }

  private async reconnect(): Promise<void> {
    if (this.isReconnecting) return;

    this.isReconnecting = true;
    this.reconnectAttempts++;
    const jitter = Math.random() * 500; // up to 0.5 seconds of randomness
    const delay = 2 ** this.reconnectAttempts * 1000 + jitter; // exponential backoff
    await wait(delay); // pause before the next attempt

    if (this.isDisconnecting) return;

    await this.initWebSocket();
    this.isReconnecting = false;
  }

  /**
   * Closes the WebSocket connection.
   * @returns {void}
   */
  disconnect(): void {
    if (!this.socket) return;
    this.isDisconnecting = true;
    // detach handlers before closing
    this.socket.onopen =
      this.socket.onmessage =
      this.socket.onerror =
      this.socket.onclose =
        null;
    this.socket.close(1000, 'Client disconnected');
    this.socket = undefined;
  }

  /**
   * Disposes the WebSocket connection.
   * @returns {void}
   */
  dispose(): void {
    this.disconnect();
  }
}

interface SessionStartFrame {
  type: 'session-start';
  device: {
    platform: string;
    model: string;
    locale?: string;
    'language.code'?: string;
    'language.context'?: string;
  };
}

/**
 * Build the `session-start` frame payload sent as the first WebSocket message.
 * The shape mirrors the server-side `SessionDeviceInfo` record (camelCase
 * JSON via `JsonSerializerDefaults.Web`).
 */
function buildSessionStartFrame(): SessionStartFrame {
  const { deviceType, platform } = getUserDeviceAndPlatform();
  const browserLocale =
    typeof navigator !== 'undefined' ? navigator.language : undefined;

  const frame: SessionStartFrame = {
    type: 'session-start',
    device: {
      platform,
      model: deviceType,
    },
  };

  if (browserLocale) {
    frame.device.locale = browserLocale.toLowerCase();
    frame.device['language.code'] = browserLocale;
    frame.device['language.context'] = 'IETF';
  }

  return frame;
}
