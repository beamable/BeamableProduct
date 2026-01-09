import { BeamServerWebSocketError } from '@/constants/Errors';
import { wait } from '@/utils/wait';
import {
  promiseWithResolvers,
  PromiseWithResolversPolyfill,
} from '@/utils/promiseWithResolvers';
import { createHash } from '@/utils/createHash';
import { BeamBase } from '@/core/BeamBase';
import { ServerEventType } from '@/core/types/ServerEventType';
import { isBrowserEnv } from '@/utils/isBrowserEnv';

interface BeamServerWebSocketConnectParams {
  apiUrl: string;
  cid: string;
  pid: string;
  accessToken?: string;
  eventWhitelist?: ServerEventType[];
}

export interface Message<T = any> {
  id: number;
  body: T;
}

export interface NotificationMessage<T = any> extends Message<T> {
  path: string;
}

interface StatusMessage<T = any> extends Message<T> {
  status: number;
}

interface NonceResponseBody {
  nonce: string;
}

export class BeamServerWebSocket {
  private NONCE_REQUEST_ID = -1;
  private AUTH_REQUEST_ID = -2;
  private PROVIDER_REQUEST_ID = -3;
  private SUCCESS_STATUS = 200;
  private wsUrl = '';
  private apiUrl = '';
  private cid = '';
  private pid = '';
  private accessToken?: string;
  private eventWhitelist?: ServerEventType[];
  private socket?: WebSocket;
  private connectPromiseWithResolvers?: PromiseWithResolversPolyfill;
  private isDisconnecting = false;
  private isReconnecting = false;
  private reconnectAttempts = 0;
  private maxRetries = 3;

  private async initWebSocket(): Promise<void> {
    this.setWebSocketUrl(this.apiUrl);
    // Create a new WebSocket connection
    const socket = new WebSocket(this.wsUrl);
    this.socket = socket;

    // Web socket open event handler
    socket.onopen = () => this.handleOpen();
    // Web socket message event handler
    socket.onmessage = (event) => this.handleSocketMessage(event);
    // Web socket error event handler
    socket.onerror = (event) => this.handleError(event);
    // Web socket close event handler
    socket.onclose = (event) => this.handleClose(event);
  }

  private handleOpen() {
    this.reconnectAttempts = 0;
    this.connectPromiseWithResolvers?.resolve();
    if (isBrowserEnv()) {
      if (!this.accessToken) {
        this.disconnect();
        throw new BeamServerWebSocketError('No access token provided');
      }
      this.authWithAccessToken(this.accessToken);
      return;
    }

    this.requestNonce();
  }

  private async handleError(e: Event) {
    if (
      this.socket?.readyState === WebSocket.OPEN ||
      this.socket?.readyState === WebSocket.CONNECTING
    ) {
      // If the socket is still open or connecting, we can try to reconnect
      this.socket.close();
    } else {
      this.connectPromiseWithResolvers?.reject(
        new BeamServerWebSocketError('WebSocket error occurred', { cause: e }),
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
        new BeamServerWebSocketError(
          'Maximum web socket reconnect attempts reached',
          { cause: e },
        ),
      );
    }
  }

  private handleSocketMessage(event: MessageEvent) {
    const message = JSON.parse((event as MessageEvent).data) as Message;
    switch (message.id) {
      case this.NONCE_REQUEST_ID:
        this.handleNonceMessage(message);
        break;
      case this.AUTH_REQUEST_ID:
        this.handleAuthStatus(message as StatusMessage);
        break;
      case this.PROVIDER_REQUEST_ID:
        this.handleProviderStatus(message as StatusMessage);
        break;
      default:
        this.handleNotification(message);
        break;
    }
  }

  private handleNonceMessage(message: Message) {
    this.authWithSecretAndNonce(
      BeamBase.env.BEAM_REALM_SECRET,
      (message.body as NonceResponseBody).nonce,
    );
  }

  private handleAuthStatus(message: StatusMessage) {
    this.handleStatusMessage(message, this.registerForEvents.bind(this));
  }

  private handleProviderStatus(message: StatusMessage) {
    this.handleStatusMessage(message, () => {
      // No-op on success; keep connection open
    });
  }

  private handleStatusMessage(message: StatusMessage, onSuccess: () => void) {
    if (message.status === this.SUCCESS_STATUS) {
      onSuccess();
    } else {
      this.disconnect();
    }
  }

  private handleNotification(message: Message) {
    this.acknowledgeMessage(message.id);
  }

  private setWebSocketUrl(apiUrl: string) {
    if (!apiUrl) throw new BeamServerWebSocketError('No apiUrl provided');

    if (this.wsUrl) return; // URL already set

    this.wsUrl = apiUrl
      .replace('localhost', 'host.docker.internal')
      .replace('http', 'ws')
      .replace('https', 'wss')
      .concat('/socket');
  }

  private requestNonce() {
    const nonceRequest = JSON.stringify({
      id: this.NONCE_REQUEST_ID,
      method: 'get',
      path: 'gateway/nonce',
    });
    this.sendMessage(nonceRequest);
  }

  private authWithSecretAndNonce(secret: string, nonce: string) {
    const signature = this.generateSignature(secret, nonce);
    const authRequest = JSON.stringify({
      id: this.AUTH_REQUEST_ID,
      method: 'post',
      path: 'gateway/auth',
      body: {
        cid: this.cid,
        pid: this.pid,
        signature,
      },
    });
    this.sendMessage(authRequest);
  }

  private authWithAccessToken(token: string) {
    const authRequest = JSON.stringify({
      id: this.AUTH_REQUEST_ID,
      method: 'post',
      path: 'gateway/auth',
      body: {
        cid: this.cid,
        pid: this.pid,
        token,
      },
    });
    this.sendMessage(authRequest);
  }

  private registerForEvents() {
    const providerRequest = JSON.stringify({
      id: this.PROVIDER_REQUEST_ID,
      method: 'post',
      path: 'gateway/provider',
      body:
        (this.eventWhitelist ?? []).length > 0
          ? { type: 'event', evtWhitelist: this.eventWhitelist }
          : { type: 'event' },
    });
    this.sendMessage(providerRequest);
  }

  private acknowledgeMessage(messageId: number) {
    const acknowledgementRequest = JSON.stringify({
      id: messageId,
      status: this.SUCCESS_STATUS,
    });
    this.sendMessage(acknowledgementRequest);
  }

  private generateSignature(secret: string, nonce: string) {
    const data = `${secret}${nonce}`;
    // hash using md5 to base64 string
    return createHash('md5').update(data).digest('base64');
  }

  private sendMessage(message: string) {
    if (this.socket?.readyState !== WebSocket.OPEN) {
      throw new BeamServerWebSocketError('WebSocket is not open');
    }
    this.socket.send(message);
  }

  get rawSocket() {
    return this.socket;
  }

  /**
   * Opens a WebSocket connection to the Beamable server.
   *
   * @param {BeamServerWebSocketConnectParams} params - The connection parameters.
   * @returns {Promise<void>}
   */
  async connect(params: BeamServerWebSocketConnectParams): Promise<void> {
    this.cid = params.cid;
    this.pid = params.pid;
    this.apiUrl = params.apiUrl;
    this.accessToken = params.accessToken;
    this.eventWhitelist = params.eventWhitelist;
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
