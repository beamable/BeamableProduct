import type { BeamConfig } from '@/configs/BeamConfig';
import { BaseRequester } from '@/network/http/BaseRequester';
import { BeamRequester } from '@/network/http/BeamRequester';
import { AccountService } from '@/services/AccountService';
import { AuthService } from '@/services/AuthService';
import { readConfig, saveConfig } from '@/defaults';
import { parseSocketMessage, saveToken } from '@/core/BeamUtils';
import type { TokenResponse } from '@/__generated__/schemas';
import { PlayerService } from '@/services/PlayerService';
import { BeamWebSocket } from '@/network/websocket/BeamWebSocket';
import { BeamError, BeamWebSocketError } from '@/constants/Errors';
import {
  REFRESHABLE_SERVICES,
  type BeamServiceType,
  type RefreshableServiceMap,
  type Subscription,
  type ClientSubscriptionMap,
} from '@/core/types';
import { wait } from '@/utils/wait';
import { HEADERS } from '@/constants';
import { BeamBase, type BeamEnvVars } from '@/core/BeamBase';
import { ApiService, type ApiServiceCtor } from '@/services/types/ApiService';
import { ClientServicesMixin } from '@/core/mixins';
import {
  BeamMicroServiceClient,
  type BeamMicroServiceClientCtor,
} from '@/core/BeamMicroServiceClient';
import { ContentService } from '@/services/ContentService';
import { type RefreshableService } from '@/services';

/** The main class for interacting with the Beam Client SDK. */
export class Beam extends ClientServicesMixin(BeamBase) {
  /**
   * A namespace of player-related services.
   * Use `beam.player.<method>` to access player-specific operations.
   */
  player: PlayerService;

  private readonly beamConfig: BeamConfig;
  private ws: BeamWebSocket;
  private subscriptions: Partial<ClientSubscriptionMap> = {};

  /** Initialize a new Beam client instance. */
  static async init(config: BeamConfig) {
    const beam = new this(config);
    await beam.connect();
    beam.isInitialized = true;
    const noop = () => {};
    beam.on('content.refresh', noop); // listen for content refresh; cache update happens inside the listener via refreshableRegistry
    config.services?.(beam);
    return beam;
  }

  protected constructor(config: BeamConfig) {
    super(config);
    this.beamConfig = config;
    this.addOptionalDefaultHeader(HEADERS.UA, config.gameEngine);
    this.addOptionalDefaultHeader(HEADERS.UA_VERSION, config.gameEngineVersion);
    this.ws = new BeamWebSocket();
    this.player = new PlayerService();
    this.use(AuthService);
    this.use(AccountService);
    this.use(ContentService);
  }

  protected createBeamRequester(config: BeamConfig): BeamRequester {
    return new BeamRequester({
      inner: config.requester ?? new BaseRequester(),
      tokenStorage: this.tokenStorage,
      useSignedRequest: false,
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

  /** Registers an API service with the Beam instance. */
  private registerApiService<T extends ApiService>(Ctor: ApiServiceCtor<T>) {
    const svc = new Ctor({ beam: this, getPlayer: () => this.player });
    const svcName = svc.serviceName;

    (this.clientServices as any)[svcName] = svc;

    if (REFRESHABLE_SERVICES.includes(svcName)) {
      const refreshKey = `${svcName}.refresh` as keyof RefreshableServiceMap;
      this.refreshableRegistry[refreshKey] =
        svc as unknown as RefreshableService<any>;
    }
  }

  /** Registers a microservice client with the Beam instance. */
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

  /** Connects the client SDK to the Beamable platform. This method is called automatically during `Beam.init()`. */
  private async connect(): Promise<void> {
    try {
      const savedConfig = await readConfig();
      // If the saved config cid does not match the current one, clear the token storage
      if (this.cid !== savedConfig.cid) this.tokenStorage.clear();
      // If the cid or pid has changed, save the new configuration
      if (this.cid !== savedConfig.cid || this.pid !== savedConfig.pid)
        await saveConfig({ cid: this.cid, pid: this.pid });

      let tokenResponse: TokenResponse | undefined;
      const tokenData = await this.tokenStorage.getTokenData();
      const accessToken = tokenData.accessToken;
      if (!accessToken) {
        // If no access token exists, login as a guest
        tokenResponse = await this.clientServices.auth.loginAsGuest();
      } else if (this.tokenStorage.isExpired) {
        // If the access token is expired, try to refresh it using the refresh token
        // If no refresh token exists, sign in as a guest
        const refreshToken = tokenData.refreshToken;
        tokenResponse = refreshToken
          ? await this.clientServices.auth.refreshAuthToken({ refreshToken })
          : await this.clientServices.auth.loginAsGuest();
      }

      if (tokenResponse) await saveToken(this.tokenStorage, tokenResponse);

      await Promise.all([
        this.clientServices.account.current(),
        this.setupRealtimeConnection(),
        this.clientServices.content.syncContentManifests({
          ids: Array.from(
            new Set(['global', ...(this.beamConfig.contentNamespaces ?? [])]),
          ),
        }),
      ]);
    } finally {
      this.clientServices = {} as BeamServiceType; // clear the services added during initialization
    }
  }

  private async setupRealtimeConnection() {
    const { refreshToken } = await this.tokenStorage.getTokenData();
    if (!refreshToken) throw new BeamWebSocketError('No refresh token found');

    await this.ws.connect({
      requester: this.requester,
      cid: this.cid,
      pid: this.pid,
      refreshToken,
    });
  }

  /**
   * Refreshes the current Beam SDK instance with a new token response.
   * This method re-initializes the SDK with the provided token,
   * updates the internal state, and re-establishes necessary connections.
   * @param tokenResponse The new token response to use for refreshing the SDK.
   * @example
   * ```ts
   * const newToken = await beam.auth.loginWithEmail({ email, password });
   * await beam.refresh(newToken);
   * ```
   */
  async refresh(tokenResponse?: TokenResponse) {
    if (tokenResponse) {
      await saveToken(this.tokenStorage, tokenResponse);
    }

    const cachedClientServices = this.clientServices;
    const cachedRefreshableRegistry = this.refreshableRegistry;
    const beam = await Beam.init(this.beamConfig);
    beam.clientServices = cachedClientServices;
    beam.refreshableRegistry = cachedRefreshableRegistry;
    Object.assign(this, beam);
  }

  /**
   * Subscribes to a specific context and listens for messages.
   * @template {keyof RefreshableServiceMap} K
   * @param context The context to subscribe to, e.g., 'inventory.refresh'.
   * @param handler The callback to process the data when a message is received.
   * @example
   * ```ts
   * const handler = (data) => {
   *   console.log('New inventory data:', data);
   * }
   * beam.use(InventoryService);
   * beam.on('inventory.refresh', handler);
   * ```
   */
  on<K extends keyof RefreshableServiceMap>(
    context: K,
    handler: (data: RefreshableServiceMap[K]['data']) => void,
  ) {
    this.checkIfInitAndSupportedContext(context);
    const abortController = new AbortController();
    const listener = async (e: MessageEvent) => {
      const eventData = JSON.parse(e.data) as {
        context: string;
        messageFull: string;
      };
      // ignore the message if the context does not match
      if (eventData.context !== context) return;

      // parse the messageFull as the expected type
      const payload = parseSocketMessage<K>(eventData.messageFull);

      if ('delay' in payload) {
        try {
          await wait(payload.delay, abortController.signal);
        } catch {
          return; // aborted
        }
      }

      const data = await this.refreshableRegistry[context].refresh(
        payload.data,
      );
      handler(data);
    };

    this.ws.rawSocket?.addEventListener('message', listener);
    const subs: Subscription[] = this.subscriptions[context] ?? [];
    subs.push({ handler, listener, abortController });
    this.subscriptions[context] = subs;
  }

  /**
   * Unsubscribes from a specific context or removes all subscriptions if no handler is provided.
   * @template {keyof RefreshableServiceMap} K
   * @param context The context to unsubscribe from, e.g., 'inventory.refresh'.
   * @param handler The callback to remove. If not provided, all handlers for the context are removed.
   * @example
   * ```ts
   * beam.off('inventory.refresh', handler);
   * // or to remove all handlers for the context
   * beam.off('inventory.refresh');
   * ```
   */
  off<K extends keyof RefreshableServiceMap>(
    context: K,
    handler?: (data: RefreshableServiceMap[K]['data']) => void,
  ) {
    this.checkIfInitAndSupportedContext(context);
    const subs = this.subscriptions[context];
    if (!subs) return;

    if (!handler) {
      // if no handler is supplied, remove them all
      subs.forEach(({ listener, abortController }) => {
        this.ws.rawSocket?.removeEventListener('message', listener);
        abortController?.abort();
      });
      delete this.subscriptions[context];
      return;
    }

    const index = subs.findIndex((s) => s.handler === handler);
    if (index === -1) return;

    const { listener, abortController } = subs[index];
    this.ws.rawSocket?.removeEventListener('message', listener);
    abortController?.abort();
    subs.splice(index, 1);
    if (subs.length === 0) delete this.subscriptions[context];
  }

  private checkIfInitAndSupportedContext(context: keyof RefreshableServiceMap) {
    if (!this.isInitialized) {
      throw new BeamError(
        `Call \`await Beam.init({...})\` to initialize the Beam client SDK.`,
      );
    }

    if (!this.refreshableRegistry[context]) {
      throw new BeamError(
        `Context "${context}" is not supported. Available contexts: ${Object.keys(
          this.refreshableRegistry,
        ).join(', ')}`,
      );
    }
  }
}

// Declaration‑merge interface that exposes all the client‑side services injected at runtime by the ClientServicesMixin.
// Each property corresponds to a key in ServiceMap, so you get typed access to beam.account, beam.auth, etc.
export interface Beam extends BeamServiceType {}
