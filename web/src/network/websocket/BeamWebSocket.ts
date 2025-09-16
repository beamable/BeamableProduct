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

interface BeamWebSocketConnectParams {
  requester: HttpRequester;
  cid: string;
  pid: string;
  refreshToken: string;
}

export class BeamWebSocket {
  private url = '';
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
    // Web socket error event handler
    socket.onerror = (event) => this.handleError(event);
    // Web socket close event handler
    socket.onclose = (event) => this.handleClose(event);
  }

  private handleOpen() {
    this.reconnectAttempts = 0;
    this.connectPromiseWithResolvers?.resolve();
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

    this.url = url;
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
