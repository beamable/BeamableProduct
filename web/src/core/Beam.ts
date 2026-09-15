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
  type BeamClientContext,
  type BeamClientContextData,
  type BeamServiceType,
  type RefreshableServiceMap,
  type Subscription,
  type ClientSubscriptionMap,
  NOTIFICATION_CONTEXTS,
} from '@/core/types';
import { BeamJsonUtils } from '@/utils/BeamJsonUtils';
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
      // Load any persisted tokens before the synchronous `isExpired` read below.
      // No-op for the synchronous browser/Node storages; the React Native
      // (AsyncStorage) storage loads its tokens here. Optional-chained so
      // custom token storages that predate this hook keep working.
      await this.tokenStorage.hydrate?.();

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

      // Realtime is opt-out via `config.realtime.enabled = false`. Skipping it
      // keeps the SDK usable as a pure API client when there's no player to
      // sustain a realtime session (e.g. an admin/portal context, which would
      // otherwise fail the socket handshake with "no player id"). Callers can
      // connect later with `connectRealtime()` once a player exists.
      const realtimeEnabled = this.beamConfig.realtime?.enabled ?? true;

      await Promise.all([
        this.clientServices.account.current(),
        realtimeEnabled ? this.setupRealtimeConnection() : Promise.resolve(),
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
      apiUrl: this.envConfig.apiUrl,
    });
  }

  /**
   * Establishes the realtime (websocket) connection to Beamable server-events.
   *
   * Use this when the client was initialized with `realtime: { enabled: false }`
   * — for example after creating a player via `beam.auth.loginAsGuest()`, at
   * which point the account has a player id on the realm and the realtime
   * session can be sustained.
   *
   * @example
   * ```ts
   * const beam = await Beam.init({ cid, pid, realtime: { enabled: false } });
   * await beam.auth.loginAsGuest();
   * await beam.connectRealtime();
   * ```
   * @throws {BeamWebSocketError} If no refresh token is available.
   */
  async connectRealtime(): Promise<void> {
    await this.setupRealtimeConnection();
  }

  /**
   * Closes the realtime (websocket) connection, if one is open. Safe to call
   * when no connection exists (no-op).
   */
  disconnectRealtime(): void {
    this.ws.disconnect();
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
  on<K extends BeamClientContext>(
    context: K,
    handler: (data: BeamClientContextData<K>) => void,
  ) {
    this.checkIfInitAndSupportedContext(context);
    const abortController = new AbortController();
    const isRefreshable = this.isRefreshableContext(context);
    const listener = async (e: MessageEvent) => {
      const eventData = JSON.parse(e.data) as {
        context: string;
        messageFull: string;
      };
      // ignore the message if the context does not match
      if (eventData.context !== context) return;

      // A notification context carries its data directly: the payload *is* the event, so it goes
      // straight to the handler. parseSocketMessage is deliberately not used here — it extracts the
      // payload by taking the first key that is not `scopes`/`delay`, which is right for a refresh
      // envelope and returns nonsense for an object that is itself the data.
      if (!isRefreshable) {
        handler(
          JSON.parse(
            eventData.messageFull,
            BeamJsonUtils.reviver,
          ) as BeamClientContextData<K>,
        );
        return;
      }

      await this.dispatchRefresh(
        context as keyof RefreshableServiceMap,
        eventData.messageFull,
        abortController.signal,
        handler as (data: unknown) => void,
      );
    };

    // Registered on the socket wrapper, not on `rawSocket`: `reconnect()` replaces the underlying
    // WebSocket, and a listener bound to the old instance would silently stop firing.
    this.ws.addListener(listener);
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
  off<K extends BeamClientContext>(
    context: K,
    handler?: (data: BeamClientContextData<K>) => void,
  ) {
    this.checkIfInitAndSupportedContext(context);
    const subs = this.subscriptions[context];
    if (!subs) return;

    if (!handler) {
      // if no handler is supplied, remove them all
      subs.forEach(({ listener, abortController }) => {
        this.ws.removeListener(listener);
        abortController?.abort();
      });
      delete this.subscriptions[context];
      return;
    }

    const index = subs.findIndex((s) => s.handler === handler);
    if (index === -1) return;

    const { listener, abortController } = subs[index];
    this.ws.removeListener(listener);
    abortController?.abort();
    subs.splice(index, 1);
    if (subs.length === 0) delete this.subscriptions[context];
  }

  /**
   * The refreshable half of `on`: parse the envelope, honour the server-provided delay, re-fetch the
   * service data, hand it over.
   *
   * Its own generic method so `R` is a single key inside it. Reading the registry through the whole
   * `keyof RefreshableServiceMap` union instead makes `refresh` an intersection of every service's
   * parameter type, which nothing satisfies.
   */
  private async dispatchRefresh<R extends keyof RefreshableServiceMap>(
    context: R,
    messageFull: string,
    signal: AbortSignal,
    handler: (data: unknown) => void,
  ): Promise<void> {
    const payload = parseSocketMessage<R>(messageFull);

    if ('delay' in payload) {
      try {
        await wait(payload.delay, signal);
      } catch {
        return; // aborted
      }
    }

    handler(await this.refreshableRegistry[context].refresh(payload.data));
  }

  /** True when `context` names a refreshable service rather than a pass-through notification. */
  private isRefreshableContext(
    context: BeamClientContext,
  ): context is keyof RefreshableServiceMap {
    return context in this.refreshableRegistry;
  }

  private checkIfInitAndSupportedContext(context: BeamClientContext) {
    if (!this.isInitialized) {
      throw new BeamError(
        `Call \`await Beam.init({...})\` to initialize the Beam client SDK.`,
      );
    }

    if (
      !this.isRefreshableContext(context) &&
      !NOTIFICATION_CONTEXTS.includes(context)
    ) {
      throw new BeamError(
        `Context "${context}" is not supported. Available contexts: ${[
          ...Object.keys(this.refreshableRegistry),
          ...NOTIFICATION_CONTEXTS,
        ].join(', ')}`,
      );
    }
  }
}

// Declaration‑merge interface that exposes all the client‑side services injected at runtime by the ClientServicesMixin.
// Each property corresponds to a key in ServiceMap, so you get typed access to beam.account, beam.auth, etc.
export interface Beam extends BeamServiceType {}
