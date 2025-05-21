import { HttpRequester } from '@/http/types/HttpRequester';
import { BeamConfig } from '@/configs/BeamConfig';
import {
  BeamEnvironmentConfig,
} from '@/configs/BeamEnvironmentConfig';
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
  private readonly envConfig: BeamEnvironmentConfig;
  private tokenStorage: TokenStorage;
  private requester: HttpRequester;

  constructor(config: BeamConfig) {
    this.cid = config.cid;
    this.pid = config.pid;
    this.alias = config.alias;
    this.envConfig = BeamEnvironment.get(config.environmentName);
    this.tokenStorage = isBrowserEnv()
      ? new BrowserTokenStorage()
      : new MemoryTokenStorage();
    this.requester =
      config.requester ??
      new FetchRequester({
        baseUrl: this.envConfig.apiUrl,
        defaultHeaders: {
          Accept: 'application/json',
          'Content-Type': 'application/json',
          'X-BEAM-SCOPE': `${this.cid}.${this.pid}`,
        },
        authProvider: async () => (await this.tokenStorage.getToken()) ?? '',
      });
  }

  /**
   * Returns a concise, human-readable summary of this Beam instance’s core configuration.
   *
   * @returns A string of the form
   *   `Beam(config: cid=<cid>, pid=<pid>, alias=<alias>)`
   */
  toString(): string {
    const { cid, pid, alias } = this;
    return `Beam(config: cid=${cid}, pid=${pid}, alias=${alias})`;
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
