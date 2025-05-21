import { HttpRequester } from '@/http/types/HttpRequester';
import { BeamConfig } from '@/configs/BeamConfig';
import { BeamEnvironmentConfig } from '@/configs/BeamEnvironmentConfig';
import { FetchRequester } from '@/http/FetchRequester';
import { isBrowserEnv } from '@/utils/isBrowserEnv';
import { BrowserTokenStorage } from '@/platform/browser/BrowserTokenStorage';
import { TokenStorage } from '@/platform/types/TokenStorage';
import { MemoryTokenStorage } from '@/platform/node/MemoryTokenStorage';
import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequest } from '@/http/types/HttpRequest';
import { BeamEnvironment } from '@/core/BeamEnvironmentRegistry';

export class Beam {
  private readonly cid: string;
  private readonly pid: string;
  private readonly alias: string;
  private readonly realm: string;
  private envConfig: BeamEnvironmentConfig;
  private tokenStorage: TokenStorage;
  private requester: HttpRequester;

  constructor(config: BeamConfig) {
    const env = config.environment;
    this.cid = config.cid;
    this.pid = config.pid;
    this.alias = config.alias;
    this.realm = config.realm;
    this.envConfig = BeamEnvironment.get(env);
    this.tokenStorage = isBrowserEnv()
      ? new BrowserTokenStorage()
      : new MemoryTokenStorage();

    const defaultHeaders = {
      Accept: 'application/json',
      'Content-Type': 'application/json',
      'X-BEAM-SCOPE': `${this.cid}.${this.pid}`,
    };

    const tokenProvider = async () =>
      (await this.tokenStorage.getToken()) ?? '';

    const customRequester = config.requester;
    if (customRequester) {
      customRequester.setBaseUrl(this.envConfig.apiUrl);
      customRequester.setTokenProvider(tokenProvider);
      Object.entries(defaultHeaders).forEach(([key, value]) => {
        customRequester.setDefaultHeader(key, value);
      });
    }

    this.requester =
      customRequester ??
      new FetchRequester({
        baseUrl: this.envConfig.apiUrl,
        defaultHeaders,
        tokenProvider,
      });
  }

  /**
   * Returns a concise, human-readable summary of this Beam instance’s core configuration.
   * @returns A string of the form `Beam(config: cid=<cid>, pid=<pid>, alias=<alias>, realm=<realm>)`
   */
  toString(): string {
    const { cid, pid, alias, realm } = this;
    return `Beam(config: cid=${cid}, pid=${pid}, alias=${alias}, realm=${realm})`;
  }

  // Tuna login sample demo for Chris
  async login(username: string, password: string) {
    // will be replaced with schema gen
    type TokenResponse = {
      access_token: string;
      expires_in: number;
      refresh_token: string;
      token_type: string;
    };

    const req: HttpRequest = {
      url: '/basic/auth/token',
      method: HttpMethod.POST,
      body: JSON.stringify({
        grant_type: 'password',
        customerScoped: false,
        username,
        password,
      }),
    };

    const res = await this.requester.request<TokenResponse>(req);
    console.log(res.body);
  }
}
