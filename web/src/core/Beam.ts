import { HttpRequester } from '@/http/types/HttpRequester';
import { BeamConfig } from '@/configs/BeamConfig';
import { BeamEnvironmentConfig } from '@/configs/BeamEnvironmentConfig';
import { BaseRequester } from '@/http/BaseRequester';
import { BeamRequester } from '@/http/BeamRequester';
import { isBrowserEnv } from '@/utils/isBrowserEnv';
import { TokenStorage } from '@/platform/types/TokenStorage';
import { BrowserTokenStorage } from '@/platform/BrowserTokenStorage';
import { NodeTokenStorage } from '@/platform/NodeTokenStorage';
import { BeamEnvironment } from '@/core/BeamEnvironmentRegistry';
import { BeamApi } from '@/core/BeamApi';
import packageJson from '../../package.json';
import { BeamService } from '@/core/BeamService';
import { AccountService } from '@/services/AccountService';
import { AuthService } from '@/services/AuthService';
import { BeamUtils } from '@/core/BeamUtils';
import { TokenResponse } from '@/__generated__/schemas';
import { PlayerService } from '@/services/PlayerService';

/** The main class for interacting with the Beam SDK. */
export class Beam {
  /**
   * A namespace of generated API service clients.
   * Use `beam.api.<serviceName>` to access specific clients.
   */
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

  private readonly cid: string;
  private readonly pid: string;
  private readonly defaultHeaders: Record<string, string>;
  private readonly requester: HttpRequester;
  private envConfig: BeamEnvironmentConfig;

  constructor(config: BeamConfig) {
    const env = config.environment;
    this.cid = config.cid;
    this.pid = config.pid;
    this.envConfig = BeamEnvironment.get(env ?? 'Prod');
    this.tokenStorage =
      config.tokenStorage ??
      (isBrowserEnv()
        ? new BrowserTokenStorage(config.instanceTag)
        : new NodeTokenStorage());

    this.defaultHeaders = {
      Accept: 'application/json',
      'Content-Type': 'application/json',
      'X-BEAM-SCOPE': `${this.cid}.${this.pid}`,
      'X-KS-BEAM-SDK-VERSION': packageJson.version,
    };
    this.addOptionalDefaultHeader('X-KS-GAME-VERSION', config.gameVersion);
    this.addOptionalDefaultHeader('X-KS-USER-AGENT', config.gameEngine);
    this.addOptionalDefaultHeader(
      'X-KS-USER-AGENT-VERSION',
      config.gameEngineVersion,
    );

    this.requester = this.createBeamRequester(config);
    this.api = new BeamApi(this.requester);
    this.player = new PlayerService();
    BeamService.attachServices(this);
  }

  /**
   * Initializes the Beam SDK instance.
   * @returns {Promise<void>}
   */
  async ready() {
    const accessToken = await this.tokenStorage.getAccessToken();

    if (accessToken === null) {
      // If no access token exists, sign in as a guest and save the tokens
      const tokenResponse = await this.auth.signInAsGuest();
      await BeamUtils.saveToken(this.tokenStorage, tokenResponse);
    } else if (this.tokenStorage.isExpired) {
      // If the access token is expired, try to refresh it using the refresh token
      // If no refresh token exists, sign in as a guest and save the tokens
      let tokenResponse: TokenResponse;
      const refreshToken = await this.tokenStorage.getRefreshToken();

      if (!refreshToken) tokenResponse = await this.auth.signInAsGuest();
      else tokenResponse = await this.auth.refreshAuthToken(refreshToken);

      await BeamUtils.saveToken(this.tokenStorage, tokenResponse);
    }

    // If we have a valid access token, fetch the current player account and set it
    this.player.account = await this.account.getCurrentPlayer();
  }

  /**
   * Returns a concise, human-readable summary of this Beam instance’s core configuration.
   * @returns {string} A string of the form `Beam(config: cid=<cid>, pid=<pid>)`
   */
  toString(): string {
    const { cid, pid } = this;
    return `Beam(config: cid=${cid}, pid=${pid})`;
  }

  private createBeamRequester(config: BeamConfig): BeamRequester {
    const tokenProvider = async () =>
      (await this.tokenStorage.getAccessToken()) ?? '';

    const customRequester = config.requester;
    if (customRequester) {
      customRequester.setBaseUrl(this.envConfig.apiUrl);
      customRequester.setTokenProvider(tokenProvider);
      Object.entries(this.defaultHeaders).forEach(([key, value]) => {
        customRequester.setDefaultHeader(key, value);
      });
    }

    const baseRequester =
      customRequester ??
      new BaseRequester({
        baseUrl: this.envConfig.apiUrl,
        defaultHeaders: this.defaultHeaders,
        tokenProvider,
      });

    return new BeamRequester({
      inner: baseRequester,
      tokenStorage: this.tokenStorage,
      cid: this.cid,
      pid: this.pid,
    });
  }

  private addOptionalDefaultHeader(key: string, value?: string): void {
    if (value) {
      this.defaultHeaders[key] = value;
    }
  }
}

export interface Beam {
  /** High-level account helper built on top of beam.api.accounts.* endpoints */
  account: AccountService;
  /** High-level auth helper built on top of beam.api.auth.* endpoints */
  auth: AuthService;
}
