import { HttpRequester } from '@/http/types/HttpRequester';
import { BeamConfig } from '@/configs/BeamConfig';
import {
  BeamEnvironment,
  BeamEnvironmentConfig,
} from '@/configs/BeamEnvironmentConfig';
import { FetchRequester } from '@/http/FetchRequester';
import { isBrowserEnv } from '@/utils/isBrowserEnv';
import { BrowserTokenStorage } from '@/platform/browser/BrowserTokenStorage';
import { TokenStorage } from '@/platform/types/TokenStorage';
import { MemoryTokenStorage } from '@/platform/node/MemoryTokenStorage';

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
    this.envConfig = BeamEnvironment[config.environment];
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
}
