import { BeamRequester } from '@/network/http/BeamRequester';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { BeamEnvironmentConfig } from '@/configs/BeamEnvironmentConfig';
import { HEADERS } from '@/constants';
import packageJson from '../../package.json';
import { BeamEnvironment } from '@/core/BeamEnvironmentRegistry';
import { BeamBaseConfig } from '@/configs/BeamBaseConfig';

/** The base class for Beam SDK client and server instances. */
export abstract class BeamBase {
  public readonly requester: HttpRequester;

  protected cid: string;
  protected pid: string;
  protected envConfig: BeamEnvironmentConfig;
  protected defaultHeaders: Record<string, string>;

  protected constructor(config: BeamBaseConfig) {
    this.cid = config.cid;
    this.pid = config.pid;
    this.envConfig = BeamEnvironment.get(config.environment ?? 'Prod');
    this.defaultHeaders = {
      [HEADERS.ACCEPT]: 'application/json',
      [HEADERS.CONTENT_TYPE]: 'application/json',
      [HEADERS.BEAM_SCOPE]: `${this.cid}.${this.pid}`,
      [HEADERS.BEAM_SDK_VERSION]: packageJson.version,
    };
    this.addOptionalDefaultHeader(HEADERS.GAME_VERSION, config.gameVersion);
    this.requester = this.createBeamRequester(config);
    this.requester.baseUrl = this.envConfig.apiUrl;
    this.requester.defaultHeaders = this.defaultHeaders;
  }

  protected abstract createBeamRequester(config: BeamBaseConfig): BeamRequester;

  protected addOptionalDefaultHeader(key: string, value?: string): void {
    if (value) {
      this.defaultHeaders[key] = value;
    }
  }
}
