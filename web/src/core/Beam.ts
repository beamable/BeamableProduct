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
import { generateTag } from '@/utils/generateTag';
import packageJson from '../../package.json';

export class Beam {
  /**
   * A namespace of generated API service clients.
   * Use `beam.api.<serviceName>` to access specific clients.
   */
  public readonly api: BeamApi;
  public tokenStorage: TokenStorage;
  private readonly cid: string;
  private readonly pid: string;
  private readonly defaultHeaders: Record<string, string>;
  private readonly requester: HttpRequester;
  private envConfig: BeamEnvironmentConfig;

  constructor(config: BeamConfig) {
    const env = config.environment;
    const tag = generateTag();

    this.cid = config.cid;
    this.pid = config.pid;
    this.envConfig = BeamEnvironment.get(env ?? 'Prod');
    this.tokenStorage =
      config.tokenStorage ??
      (isBrowserEnv() ? new BrowserTokenStorage(tag) : new NodeTokenStorage());

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
