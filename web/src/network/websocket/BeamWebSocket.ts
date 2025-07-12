import { BeamWebSocketError } from '@/constants/Errors';
import { wait } from '@/utils/wait';
import {
  promiseWithResolvers,
  PromiseWithResolversPolyfill,
} from '@/utils/promiseWithResolvers';
import { BeamApi } from '@/core/BeamApi';

interface BeamWebSocketConnect {
  api: BeamApi;
  cid: string;
  pid: string;
  refreshToken: string;
}

export class BeamWebSocket {
  private socket?: WebSocket;
  private api?: BeamApi;
  private url?: string;
  private cid?: string;
  private pid?: string;
  private refreshToken?: string;
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
    socket.onopen = () => {
      this.reconnectAttempts = 0;
      this.connectPromiseWithResolvers?.resolve();
    };

    // Web socket error event handler
    socket.onerror = async (e) => {
      console.error('WebSocket error:', e);
      if (
        socket.readyState === WebSocket.OPEN ||
        socket.readyState === WebSocket.CONNECTING
      ) {
        // If the socket is still open or connecting, we can try to reconnect
        socket.close();
      } else {
        this.connectPromiseWithResolvers?.reject(
          new BeamWebSocketError('WebSocket error occurred'),
        );
      }
    };

    // Web socket close event handler
    this.socket.onclose = async (e) => {
      // if explicitly called disconnect(), don't reconnect
      if (this.isDisconnecting) return;

      console.warn('WebSocket closed:', e.code, e.reason);
      if (this.reconnectAttempts < this.maxRetries) {
        await this.reconnect();
      } else {
        this.connectPromiseWithResolvers?.reject(
          new BeamWebSocketError(
            'Maximum web socket reconnect attempts reached',
          ),
        );
      }
    };
  }

  private async setWebSocketUrl(): Promise<void> {
    if (!this.api) {
      throw new BeamWebSocketError('API instance is not set');
    }

    if (this.url) return; // URL already set

    const realmConfigResponse = await this.api.realms.getRealmsClientDefaults();
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
    if (!this.api) return null;

    try {
      // Fetch the access token for the new connection
      const accessTokenResponse = await this.api.auth.postAuthRefreshTokenV2({
        customerId: this.cid,
        realmId: this.pid,
        refreshToken: this.refreshToken,
      });
      return accessTokenResponse.body.accessToken ?? null;
    } catch (error) {
      return null;
    }
  }

  get rawSocket() {
    return this.socket;
  }

  /**
   * Opens a WebSocket connection to the Beamable server.
   *
   * @param {BeamWebSocketConnect} param - The connection parameters.
   * @returns {Promise<void>}
   */
  async connect(param: BeamWebSocketConnect): Promise<void> {
    this.api = param.api;
    this.cid = param.cid;
    this.pid = param.pid;
    this.refreshToken = param.refreshToken;
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
