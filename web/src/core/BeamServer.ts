import type {
  BeamServerConfig,
  ServerEventsConfig,
} from '@/configs/BeamServerConfig';
import { BaseRequester } from '@/network/http/BaseRequester';
import { BeamRequester } from '@/network/http/BeamRequester';
import { BeamBase, type BeamEnvVars } from '@/core/BeamBase';
import { HEADERS } from '@/constants';
import { ApiService, type ApiServiceCtor } from '@/services/types/ApiService';
import { ServerServicesMixin } from '@/core/mixins';
import type {
  BeamServerServiceType,
  ServerSubscriptionMap,
  Subscription,
} from '@/core/types';
import {
  BeamMicroServiceClient,
  type BeamMicroServiceClientCtor,
} from '@/core/BeamMicroServiceClient';
import {
  BeamServerWebSocket,
  Message,
  NotificationMessage,
} from '@/network/websocket/BeamServerWebSocket';
import { ServerEventType } from '@/core/types/ServerEventType';
import { BeamError } from '@/constants/Errors';
import { isBrowserEnv } from '@/utils/isBrowserEnv';

/** The main class for interacting with the Beam Server SDK. */
export class BeamServer extends ServerServicesMixin(BeamBase) {
  private serverEventsConfig?: ServerEventsConfig;
  private ws: BeamServerWebSocket;
  private subscriptions: Partial<ServerSubscriptionMap> = {};

  /** Initialize a new Beam server instance. */
  static async init(config: BeamServerConfig) {
    const beamServer = new this(config);
    beamServer.serverEventsConfig = config.serverEvents;
    if (beamServer.isServerEventEnabled) await beamServer.connect();
    beamServer.isInitialized = true;
    config.services?.(beamServer);
    return beamServer;
  }

  protected constructor(config: BeamServerConfig) {
    super(config);
    this.addOptionalDefaultHeader(HEADERS.UA, config.engine);
    this.addOptionalDefaultHeader(HEADERS.UA_VERSION, config.engineVersion);
    this.ws = new BeamServerWebSocket();
  }

  protected createBeamRequester(config: BeamServerConfig): BeamRequester {
    return new BeamRequester({
      inner: config.requester ?? new BaseRequester(),
      tokenStorage: this.tokenStorage,
      useSignedRequest: !isBrowserEnv() && (config.useSignedRequest ?? false),
      pid: this.pid,
    });
  }

  static get env(): BeamEnvVars {
    return BeamBase.env;
  }

  use<T extends ApiServiceCtor<any> | BeamMicroServiceClientCtor<any>>(
    ctors: readonly T[],
  ): this;
  use<T extends ApiServiceCtor<any> | BeamMicroServiceClientCtor<any>>(
    ctor: T,
  ): this;
  use(ctorOrCtors: any): this {
    const ctors = Array.isArray(ctorOrCtors) ? ctorOrCtors : [ctorOrCtors];

    if (this.isApiService(ctors[0])) {
      ctors.forEach((c) => this.registerApiService(c));
      return this;
    }

    if (this.isMicroServiceClient(ctors[0])) {
      ctors.forEach((c) => this.registerMicroClient(c));
      return this;
    }

    return this;
  }

  /** Registers an API service with the BeamServer instance. */
  private registerApiService<T extends ApiService>(Ctor: ApiServiceCtor<T>) {
    const svc = new Ctor({ beam: this });

    (this.serverServices as any)[svc.serviceName] = (userId: string) => {
      svc.userId = userId;
      return svc;
    };
  }

  /** Registers a microservice client with the BeamServer instance. */
  private registerMicroClient<T extends BeamMicroServiceClient>(
    Ctor: BeamMicroServiceClientCtor<T>,
  ) {
    const client = new Ctor(this);
    const serviceName = client.serviceName;

    const identifier =
      serviceName.charAt(0).toLowerCase() + serviceName.slice(1);
    const clientName = `${identifier}Client`;

    (this as any)[clientName] = client;
  }

  /** Connects the server SDK to the Beamable gateway to listen for server-events. This method is called automatically during `BeamServer.init()` if `enableServerEvents` is set to `true`. */
  private async connect() {
    const tokenData = await this.tokenStorage.getTokenData();
    const accessToken = tokenData.accessToken ?? undefined;
    await this.ws.connect({
      apiUrl: this.envConfig.apiUrl,
      cid: this.cid,
      pid: this.pid,
      eventWhitelist: this.serverEventsConfig?.eventWhitelist,
      accessToken,
    });
  }

  /**
   * Subscribes to a server-event and listens for messages.
   * @template {ServerEventType} K
   * @param eventType The server-event to subscribe to, e.g., 'content.manifest'.
   * @param handler The callback to process the data when a message is received.
   * @example
   * ```ts
   * const handler = (data) => {
   *   console.log('Content manifest received:', data);
   * };
   * beamServer.on('content.manifest', handler);
   * ```
   */
  on<K extends ServerEventType>(eventType: K, handler: (data: any) => void) {
    this.checkIfInit();
    const listener = (e: MessageEvent) => {
      const message = JSON.parse(e.data) as Message;
      if (!('path' in message)) return;

      const notificationMessage = message as NotificationMessage;
      const notificationEventType = notificationMessage.path.split('/')[1];
      if (notificationEventType === eventType) {
        handler(notificationMessage.body);
      }
    };

    this.ws.rawSocket?.addEventListener('message', listener);
    const subs: Subscription[] = this.subscriptions[eventType] ?? [];
    subs.push({ handler, listener });
    this.subscriptions[eventType] = subs;
  }

  /**
   * Unsubscribes from a specific server-event or removes all subscriptions if no handler is provided.
   * @template {ServerEventType} K
   * @param eventType The server-event to unsubscribe from, e.g., 'content.manifest'.
   * @param handler The callback to remove. If not provided, all handlers for the server-event are removed.
   * @example
   * ```ts
   * beamServer.off('content.manifest', handler);
   * // or to remove all handlers for the server-event
   * beamServer.off('content.manifest');
   * ```
   */
  off<K extends ServerEventType>(eventType: K, handler?: (data: any) => void) {
    this.checkIfInit();
    const subs = this.subscriptions[eventType];
    if (!subs) return;

    if (!handler) {
      // if no handler is supplied, remove them all
      subs.forEach(({ listener }) => {
        this.ws.rawSocket?.removeEventListener('message', listener);
      });
      delete this.subscriptions[eventType];
      return;
    }

    const index = subs.findIndex((s) => s.handler === handler);
    if (index === -1) return;

    const { listener } = subs[index];
    this.ws.rawSocket?.removeEventListener('message', listener);
    subs.splice(index, 1);
    if (subs.length === 0) delete this.subscriptions[eventType];
  }

  private checkIfInit() {
    if (!this.isInitialized) {
      throw new BeamError(
        `Call \`await BeamServer.init({...})\` to initialize the Beam server SDK.`,
      );
    }
  }

  private get isServerEventEnabled() {
    return this.serverEventsConfig?.enabled ?? false;
  }
}

// Declaration‑merge interface that exposes all the server‑side services injected at runtime by the ServerServicesMixin.
// Each property corresponds to a key in ServiceMap, so you get typed access to beamServer.account(userId), beam.auth(userId), etc.
export interface BeamServer extends BeamServerServiceType {}
