import { BeamRequester } from '@/network/http/BeamRequester';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { BeamEnvironmentConfig } from '@/configs/BeamEnvironmentConfig';
import { HEADERS } from '@/constants';
import packageJson from '../../package.json';
import { BeamEnvironment } from '@/core/BeamEnvironmentRegistry';
import { BeamBaseConfig } from '@/configs/BeamBaseConfig';
import { BeamError } from '@/constants/Errors';
import { ApiService, type ApiServiceCtor } from '@/services/types/ApiService';
import {
  BeamServerServiceType,
  BeamServiceType,
  RefreshableServiceMap,
} from '@/core/types';
import type { Refreshable } from '@/services';
import {
  BeamMicroServiceClient,
  BeamMicroServiceClientCtor,
} from '@/core/BeamMicroServiceClient';

export interface BeamEnvVars {
  /** The secret key for signing requests. */
  BEAM_REALM_SECRET: string;
  /** The routing key for microservice requests. */
  BEAM_ROUTING_KEY: string;
}

/** The base class for Beam SDK client and server instances. */
export abstract class BeamBase {
  /** The HTTP requester instance used by the Beam SDK. */
  readonly requester: HttpRequester;

  /** The Beamable Customer ID. */
  cid: string;
  /** The Beamable Project ID. */
  pid: string;

  protected envConfig: BeamEnvironmentConfig;
  protected defaultHeaders: Record<string, string>;
  protected clientServices = {} as BeamServiceType;
  protected serverServices = {} as BeamServerServiceType;
  protected refreshable = {} as Record<
    keyof RefreshableServiceMap,
    Refreshable<unknown>
  >;
  protected isInitialized = false;

  private static _env: BeamEnvVars = {
    BEAM_REALM_SECRET: '',
    BEAM_ROUTING_KEY: '',
  };

  /**
   * Environment variables that can be set to configure the Beam SDK.
   * @remarks
   * These values **must** be supplied at runtime via real environment variables
   * (e.g. `process.env.REALM_SECRET`), and **must not** be committed directly into your source code.
   * @example
   * ```ts
   * BeamBase.env.REALM_SECRET = process.env.REALM_SECRET;
   * const beam = await Beam.init({ ... });
   * ```
   */
  static get env(): BeamEnvVars {
    return this._env;
  }

  /**
   * Dynamically adds a service to the Beam SDK instance.
   * @param Service The service class to add.
   * @example
   * ```ts
   * // client-side:
   * beam.use(StatsService);
   * await beam.stats.get({...});
   * // server-side:
   * beamServer.use(StatsService);
   * await beamServer.stats.get({...});
   * ```
   */
  abstract use<T extends ApiService>(Service: ApiServiceCtor<T>): this;

  /**
   * Dynamically adds a microservice client to the Beam SDK instance.
   * @param Client The microservice client class to add.
   * @example
   * ```ts
   * // client-side:
   * beam.use(MyMicroServiceClient);
   * beam.myMicroServiceClient.serviceName;
   * // server-side:
   * beamServer.use(MyMicroServiceClient);
   * beamServer.myMicroServiceClient.serviceName;
   * ```
   */
  abstract use<T extends BeamMicroServiceClient>(
    Client: BeamMicroServiceClientCtor<T>,
  ): this;

  protected constructor(config: BeamBaseConfig) {
    this.cid = config.cid;
    this.pid = config.pid;
    this.envConfig = BeamEnvironment.get(config.environment ?? 'prod');
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

  protected isApiService(ctor: any): ctor is ApiServiceCtor<ApiService> {
    return ctor.prototype instanceof ApiService;
  }

  protected isMicroServiceClient(
    ctor: any,
  ): ctor is BeamMicroServiceClientCtor<BeamMicroServiceClient> {
    return ctor.prototype instanceof BeamMicroServiceClient;
  }

  protected throwServiceUnavailable(
    serviceName: string,
    isServer?: boolean,
  ): never {
    if (!this.isInitialized)
      throw new BeamError(
        `Call \`${isServer ? 'BeamServer.init({...})' : 'await Beam.init({...})'}\` to initialize the Beam ${isServer ? 'server' : 'client'} SDK.`,
      );

    serviceName = serviceName.charAt(0).toUpperCase() + serviceName.slice(1);
    throw new BeamError(
      `Call \`${isServer ? 'beamServer' : 'beam'}.use(${serviceName}Service)\` to enable the ${serviceName} service.`,
    );
  }
}
