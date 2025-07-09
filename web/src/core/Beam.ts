import { BeamConfig } from '@/configs/BeamConfig';
import { BaseRequester } from '@/network/http/BaseRequester';
import { BeamRequester } from '@/network/http/BeamRequester';
import { TokenStorage } from '@/platform/types/TokenStorage';
import { BeamApi } from '@/core/BeamApi';
import { BeamService } from '@/core/BeamService';
import { AccountService } from '@/services/AccountService';
import { AuthService } from '@/services/AuthService';
import { defaultTokenStorage, readConfig, saveConfig } from '@/defaults';
import { saveToken } from '@/core/BeamUtils';
import { TokenResponse } from '@/__generated__/schemas';
import { PlayerService } from '@/services/PlayerService';
import { BeamWebSocket } from '@/network/websocket/BeamWebSocket';
import { BeamError, BeamWebSocketError } from '@/constants/Errors';
import { AnnouncementsService } from '@/services/AnnouncementsService';
import { Refreshable } from '@/services/types/Refreshable';
import { ContextMap, Subscription, SubscriptionMap } from '@/core/types';
import { wait } from '@/utils/wait';
import { HEADERS } from '@/constants';
import { BeamBase } from '@/core/BeamBase';
import { StatsService } from '@/services/StatsService';
import { LeaderboardsService } from '@/services/LeaderboardsService';

/** The main class for interacting with the Beam Client SDK. */
export class Beam extends BeamBase {
  public readonly api: BeamApi;

  /**
   * A namespace of player-related services.
   * Use `beam.player.<method>` to access player-specific operations.
   */
  public readonly player: PlayerService;

  /**
   * The token storage instance used by the SDK.
   * Defaults to `BrowserTokenStorage` in browser environments and `NodeTokenStorage` in Node.js environments.
   * Can be overridden via the `tokenStorage` option in the `BeamConfig`.
   */
  public tokenStorage: TokenStorage;

  private static localTokenStorage: TokenStorage;
  private readonly refreshable: Record<keyof ContextMap, Refreshable<unknown>>;
  private ws: BeamWebSocket;
  private subscriptions: Partial<SubscriptionMap> = {};
  private readyPromise?: Promise<void>;
  private isReadyPromise: boolean = false;

  constructor(config: BeamConfig) {
    Beam.localTokenStorage =
      config.tokenStorage ?? defaultTokenStorage(config.instanceTag);
    super(config);
    this.tokenStorage = Beam.localTokenStorage;
    this.addOptionalDefaultHeader(HEADERS.UA, config.gameEngine);
    this.addOptionalDefaultHeader(HEADERS.UA_VERSION, config.gameEngineVersion);
    this.ws = new BeamWebSocket();
    this.api = new BeamApi(this.requester);
    this.player = new PlayerService();
    BeamService.attachServices(this);
    this.refreshable = {
      'announcements.refresh': this.announcements,
    };
  }

  /** Initializes the Beam SDK instance. Later calls return the same initialization promise. */
  async ready(): Promise<void> {
    if (!this.readyPromise) this.readyPromise = this.init();
    return this.readyPromise;
  }

  /** Returns whether the Beam SDK instance is ready. */
  get isReady(): boolean {
    return this.isReadyPromise;
  }

  /**
   * Subscribes to a specific context and listens for messages.
   * @template {keyof ContextMap} K
   * @template {ContextMap[K]['data']} T
   * @param context The context to subscribe to, e.g., 'inventory.refresh'.
   * @param handler The callback to process the data when a message is received.
   * @example
   * ```ts
   * beam.on('inventory.refresh', (data) => {
   *   console.log('New inventory data:', data);
   * });
   * ```
   */
  on<K extends keyof ContextMap, T extends ContextMap[K]['data']>(
    context: K,
    handler: (data: T) => void,
  ) {
    this.checkIfReadyAndSupportedContext(context, 'subscribing');
    const abortController = new AbortController();
    const listener = async (e: MessageEvent) => {
      const eventData = JSON.parse(e.data) as {
        context: string;
        messageFull: string;
      };
      // ignore the message if the context does not match
      if (eventData.context !== context) return;

      // parse the messageFull as the expected type
      const payload = JSON.parse(eventData.messageFull) as Omit<
        ContextMap[K],
        'data'
      >;

      if ('delay' in payload) {
        try {
          await wait(payload.delay, abortController.signal);
        } catch {
          return; // aborted
        }
      }

      const data = (await this.refreshable[context].refresh()) as T;
      handler(data);
    };

    this.ws.rawSocket?.addEventListener('message', listener);
    const subs: Subscription[] = this.subscriptions[context] ?? [];
    subs.push({ handler, listener, abortController });
    this.subscriptions[context] = subs;
  }

  /**
   * Unsubscribes from a specific context or removes all subscriptions if no handler is provided.
   * @template {keyof ContextMap} K
   * @param context The context to unsubscribe from, e.g., 'inventory.refresh'.
   * @param handler The callback to remove. If not provided, all handlers for the context are removed.
   * @example
   * ```ts
   * beam.off('inventory.refresh', myHandler);
   * // or to remove all handlers for the context
   * beam.off('inventory.refresh');
   * ```
   */
  off<K extends keyof ContextMap>(
    context: K,
    handler?: (data: ContextMap[K]['data']) => void,
  ) {
    this.checkIfReadyAndSupportedContext(context, 'unsubscribing');
    const subs = this.subscriptions[context];
    if (!subs) return;

    if (!handler) {
      // if no handler is supplied, remove them all
      subs.forEach(({ listener, abortController }) => {
        this.ws.rawSocket?.removeEventListener('message', listener);
        abortController.abort();
      });
      delete this.subscriptions[context];
      return;
    }

    const index = subs.findIndex((s) => s.handler === handler);
    if (index === -1) return;

    const { listener, abortController } = subs[index];
    this.ws.rawSocket?.removeEventListener('message', listener);
    abortController.abort();
    subs.splice(index, 1);
    if (subs.length === 0) delete this.subscriptions[context];
  }

  private async init(): Promise<void> {
    return new Promise(async (resolve, reject) => {
      try {
        const savedConfig = await readConfig();
        if (this.cid !== savedConfig.cid || this.pid !== savedConfig.pid) {
          this.tokenStorage.clear(); // TODO: consider namespacing by pid
          await saveConfig({ cid: this.cid, pid: this.pid });
        }

        const accessToken = await this.tokenStorage.getAccessToken();
        if (accessToken === null) {
          // If no access token exists, sign in as a guest and save the tokens
          const tokenResponse = await this.auth.signInAsGuest();
          await saveToken(this.tokenStorage, tokenResponse);
        } else if (this.tokenStorage.isExpired) {
          // If the access token is expired, try to refresh it using the refresh token
          // If no refresh token exists, sign in as a guest and save the tokens
          let tokenResponse: TokenResponse;
          const refreshToken = await this.tokenStorage.getRefreshToken();

          if (!refreshToken) tokenResponse = await this.auth.signInAsGuest();
          else
            tokenResponse = await this.auth.refreshAuthToken({ refreshToken });

          await saveToken(this.tokenStorage, tokenResponse);
        }

        // If we have a valid access token, fetch the current player account and set it
        this.player.account = await this.account.current();

        await this.setupRealtimeConnection();
        this.isReadyPromise = true;
        resolve();
      } catch (error) {
        this.isReadyPromise = false;
        this.readyPromise = undefined;
        reject(error);
      }
    });
  }

  protected createBeamRequester(config: BeamConfig): BeamRequester {
    const baseRequester = config.requester ?? new BaseRequester();
    return new BeamRequester({
      inner: baseRequester,
      tokenStorage: Beam.localTokenStorage,
      pid: this.pid,
    });
  }

  private async setupRealtimeConnection() {
    const refreshToken = await this.tokenStorage.getRefreshToken();
    if (!refreshToken) throw new BeamWebSocketError('No refresh token found');

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

    await this.ws.connect({
      api: this.api,
      cid: this.cid,
      pid: this.pid,
      refreshToken,
      url,
    });
  }

  private checkIfReadyAndSupportedContext(
    context: keyof ContextMap,
    messageType: string,
  ) {
    if (!this.isReadyPromise) {
      throw new BeamError(
        `Beam SDK is not ready. Please call \`await beam.ready()\` before ${messageType}.`,
      );
    }

    if (!this.refreshable[context]) {
      throw new BeamError(
        `Context "${context}" is not supported. Available contexts: ${Object.keys(
          this.refreshable,
        ).join(', ')}`,
      );
    }
  }
}

export interface Beam {
  /** High-level account helper built on top of `beam.api.accounts.*` endpoints. */
  account: AccountService;
  /** High-level announcement helper built on top of `beam.api.announcements.*` endpoints. */
  announcements: AnnouncementsService;
  /** High-level auth helper built on top of `beam.api.auth.*` endpoints. */
  auth: AuthService;
  /** High-level leaderboards helper built on top of `beam.api.leaderboards.*` endpoints. */
  leaderboards: LeaderboardsService;
  /** High-level stats helper built on top of `beam.api.stats.*` endpoints. */
  stats: StatsService;
}
