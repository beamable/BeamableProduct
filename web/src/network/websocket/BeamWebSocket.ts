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
  /**
   * How long `connect()` waits for the socket to open before rejecting, in milliseconds.
   * `0` disables the timeout.
   * @default 15000
   */
  connectTimeoutMs?: number;
}

/** Default time `connect()` waits for the realtime socket to open. */
export const DEFAULT_REALTIME_CONNECT_TIMEOUT_MS = 15000;

/** How long the diagnostic HTTP request made after a failed handshake may take. */
const HANDSHAKE_PROBE_TIMEOUT_MS = 3000;

export class BeamWebSocket {
  private url = '';
  private apiUrl = '';
  private cid = '';
  private pid = '';
  private refreshToken = '';
  private socket?: WebSocket;
  private requester?: HttpRequester;
  private connectPromiseWithResolvers?: PromiseWithResolversPolyfill;
  private connectSettled = true;
  private connectTimer?: ReturnType<typeof setTimeout>;
  // Incremented by every connect()/disconnect() so work started by an older
  // connection attempt can tell it has been superseded and bail out.
  private generation = 0;
  private isDisconnecting = false;
  private isReconnecting = false;
  private reconnectAttempts = 0;
  private maxRetries = 3;

  private async initWebSocket(): Promise<void> {
    const generation = this.generation;
    let accessToken: string | null = null;
    try {
      const [token] = await Promise.all([
        this.getAccessToken(),
        this.setWebSocketUrl(),
      ]);
      accessToken = token;
    } catch (error) {
      if (generation !== this.generation) return;
      return this.failConnect(
        error instanceof BeamWebSocketError
          ? error
          : new BeamWebSocketError(
              'Failed to prepare the realtime WebSocket connection',
              { cause: error },
            ),
      );
    }

    // disconnect() or a newer connect() happened while we were fetching the token
    if (generation !== this.generation || this.isDisconnecting) return;

    if (!accessToken) {
      return this.failConnect(
        new BeamWebSocketError(
          'Failed to obtain access token for WebSocket connection',
        ),
      );
    }

    // Create a new WebSocket connection
    const connectUrl = `${this.url}/connect?access_token=${accessToken}&send-session-start=true`;
    const socket = new WebSocket(connectUrl);
    this.socket = socket;

    // Events from a socket that has since been replaced are ignored, so a
    // stale close can't trigger a second reconnect loop.
    socket.onopen = () => {
      if (this.socket === socket) this.handleOpen();
    };
    socket.onerror = (event) => {
      if (this.socket === socket) this.handleError(event, connectUrl);
    };
    socket.onclose = (event) => {
      if (this.socket === socket) this.handleClose(event);
    };
  }

  private handleOpen() {
    this.reconnectAttempts = 0;
    // The server's `&send-session-start=true` query param tells the gateway to
    // expect a `session-start` frame as the very first message on this socket.
    // Browsers can't set request headers on the WebSocket upgrade, so we send
    // the device info as a frame instead. If the frame is missing or malformed,
    // the server will close the connection.
    this.sendSessionStartFrame();
    this.settleConnect();
  }

  private sendSessionStartFrame(): void {
    if (!this.socket) return;
    try {
      this.socket.send(JSON.stringify(buildSessionStartFrame()));
    } catch (e) {
      console.warn('Failed to send session-start frame:', e);
    }
  }

  private handleError(e: Event, connectUrl: string) {
    if (
      this.socket?.readyState === WebSocket.OPEN ||
      this.socket?.readyState === WebSocket.CONNECTING
    ) {
      // If the socket is still open or connecting, we can try to reconnect
      this.socket.close();
      return;
    }

    // Once connect() has settled, the close handler owns reconnection.
    if (this.connectSettled) return;

    // The handshake failed before the socket ever opened. Browsers don't
    // expose the HTTP status of a failed upgrade, so ask the server directly
    // to turn a generic "error" into something actionable.
    const generation = this.generation;
    void this.describeHandshakeFailure(connectUrl, e).then((error) => {
      if (generation === this.generation) this.failConnect(error);
    });
  }

  private async handleClose(e: CloseEvent) {
    // if explicitly called disconnect(), don't reconnect
    if (this.isDisconnecting) return;

    console.warn('WebSocket closed:', e.code, e.reason);
    if (this.reconnectAttempts < this.maxRetries) {
      await this.reconnect();
    } else {
      this.failConnect(
        new BeamWebSocketError(
          `Maximum web socket reconnect attempts reached (${this.maxRetries}) for ${this.describeHost()}; last close code ${e.code}${e.reason ? ` (${e.reason})` : ''}`,
          { cause: e, context: { code: e.code, reason: e.reason } },
        ),
      );
    }
  }

  /** Resolves the pending connect() promise, if any. */
  private settleConnect() {
    if (this.connectSettled) return;
    this.connectSettled = true;
    this.clearConnectTimer();
    this.connectPromiseWithResolvers?.resolve();
  }

  /**
   * Rejects the pending connect() promise. When the initial connection fails
   * the socket is torn down so it doesn't keep retrying in the background.
   * After connect() has resolved, this is a no-op.
   */
  private failConnect(error: BeamWebSocketError) {
    if (this.connectSettled) return;
    this.connectSettled = true;
    this.clearConnectTimer();
    this.teardown();
    this.connectPromiseWithResolvers?.reject(error);
  }

  private clearConnectTimer() {
    if (this.connectTimer === undefined) return;
    clearTimeout(this.connectTimer);
    this.connectTimer = undefined;
  }

  private startConnectTimer(timeoutMs: number) {
    this.clearConnectTimer();
    if (!(timeoutMs > 0)) return;
    this.connectTimer = setTimeout(() => {
      this.connectTimer = undefined;
      this.failConnect(
        new BeamWebSocketError(
          `Realtime connection to ${this.describeHost()} did not open within ${timeoutMs} ms. ` +
            'WebSockets may be blocked by a proxy, firewall or TLS-intercepting gateway (plain HTTP requests can still work). ' +
            'Raise `realtime.connectTimeoutMs`, or set `realtime.enabled: false` to initialize without realtime.',
          { context: { url: this.url, timeoutMs } },
        ),
      );
    }, timeoutMs);
  }

  private describeHost(): string {
    try {
      return this.url ? new URL(this.url).host : 'the realtime server';
    } catch {
      return this.url;
    }
  }

  /**
   * Builds the error for a handshake that failed before the socket opened,
   * including the HTTP status the server returns for the same URL when it
   * can be read.
   */
  private async describeHandshakeFailure(
    connectUrl: string,
    cause: Event,
  ): Promise<BeamWebSocketError> {
    const host = this.describeHost();
    const status = await probeHttpStatus(connectUrl);
    let message: string;
    if (status === 401 || status === 403) {
      message = `WebSocket handshake with ${host} was rejected (HTTP ${status}): the realtime access token was not accepted. Sign in again or check that the token belongs to this realm.`;
    } else if (status !== undefined) {
      message = `WebSocket handshake with ${host} failed (HTTP ${status} from the handshake URL). If other requests succeed, WebSockets may be blocked by a proxy or firewall.`;
    } else {
      message = `WebSocket handshake with ${host} failed. WebSockets may be blocked by a proxy or firewall, or the server rejected the connection.`;
    }
    return new BeamWebSocketError(message, {
      cause,
      context: { url: this.url, status },
    });
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
    } catch (error) {
      const status = readHttpStatus(error);
      throw new BeamWebSocketError(
        `Failed to obtain access token for WebSocket connection${status ? ` (HTTP ${status} from the refresh-token exchange)` : ''}`,
        { cause: error, context: { status } },
      );
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
    // Abandon any previous socket and its pending retries.
    this.teardown();
    this.generation++;
    this.requester = params.requester;
    this.cid = params.cid;
    this.pid = params.pid;
    this.refreshToken = params.refreshToken;
    this.apiUrl = params.apiUrl;
    this.isDisconnecting = false;
    this.isReconnecting = false;
    this.reconnectAttempts = 0;
    const connectPromise = promiseWithResolvers();
    this.connectPromiseWithResolvers = connectPromise;
    this.connectSettled = false;
    this.startConnectTimer(
      params.connectTimeoutMs ?? DEFAULT_REALTIME_CONNECT_TIMEOUT_MS,
    );
    await this.initWebSocket();
    return connectPromise.promise;
  }

  private async reconnect(): Promise<void> {
    if (this.isReconnecting) return;

    const generation = this.generation;
    this.isReconnecting = true;
    try {
      this.reconnectAttempts++;
      const jitter = Math.random() * 500; // up to 0.5 seconds of randomness
      const delay = 2 ** this.reconnectAttempts * 1000 + jitter; // exponential backoff
      await wait(delay); // pause before the next attempt

      if (this.isDisconnecting || generation !== this.generation) return;

      await this.initWebSocket();
    } finally {
      // Always clear the flag, including on the early returns above, so a
      // later close is never silently ignored.
      if (generation === this.generation) this.isReconnecting = false;
    }
  }

  /** Detaches handlers from the current socket and closes it. */
  private teardown(): void {
    this.isDisconnecting = true;
    this.generation++;
    this.isReconnecting = false;
    const socket = this.socket;
    this.socket = undefined;
    if (!socket) return;
    socket.onopen = socket.onmessage = socket.onerror = socket.onclose = null;
    socket.close(1000, 'Client disconnected');
  }

  /**
   * Closes the WebSocket connection.
   * @returns {void}
   */
  disconnect(): void {
    this.teardown();
    this.failConnect(
      new BeamWebSocketError('WebSocket disconnected before it opened'),
    );
  }

  /**
   * Disposes the WebSocket connection.
   * @returns {void}
   */
  dispose(): void {
    this.disconnect();
  }
}

/** Reads an HTTP status from an error thrown by the Beam requester, if present. */
function readHttpStatus(error: unknown): number | undefined {
  const status = (error as { context?: { response?: { status?: unknown } } })
    ?.context?.response?.status;
  return typeof status === 'number' ? status : undefined;
}

/**
 * Makes a plain HTTP GET to a WebSocket URL and returns the response status,
 * or `undefined` if the request can't be made or read (no fetch, CORS,
 * network failure or timeout).
 */
async function probeHttpStatus(wsUrl: string): Promise<number | undefined> {
  if (typeof fetch !== 'function') return undefined;
  const httpUrl = wsUrl.replace(/^ws(s?):/i, 'http$1:');
  if (!/^https?:/i.test(httpUrl)) return undefined;
  const controller =
    typeof AbortController === 'function' ? new AbortController() : undefined;
  const timer = setTimeout(
    () => controller?.abort(),
    HANDSHAKE_PROBE_TIMEOUT_MS,
  );
  try {
    const response = await fetch(httpUrl, {
      method: 'GET',
      signal: controller?.signal,
    });
    return response.status;
  } catch {
    return undefined;
  } finally {
    clearTimeout(timer);
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
