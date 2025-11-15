import { BeamRequester } from '@/network/http/BeamRequester';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { BeamEnvironmentConfig } from '@/configs/BeamEnvironmentConfig';
import { HEADERS } from '@/constants';
import packageJson from '../../package.json';
import { BeamEnvironment } from '@/core/BeamEnvironmentRegistry';
import { BeamBaseConfig } from '@/configs/BeamBaseConfig';
import { BeamError } from '@/constants/Errors';
import { ApiService, type ApiServiceCtor } from '@/services/types/ApiService';
import type {
  BeamServerServiceType,
  BeamServiceType,
  RefreshableRegistry,
} from '@/core/types';
import {
  BeamMicroServiceClient,
  BeamMicroServiceClientCtor,
} from '@/core/BeamMicroServiceClient';
import { TokenStorage } from '@/platform/types/TokenStorage';
import { defaultTokenStorage } from '@/defaults';

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
  /**
   * The token storage instance used by the client SDK.
   * Defaults to `BrowserTokenStorage` in browser environments and `NodeTokenStorage` in Node.js environments.
   * Can be overridden via the `tokenStorage` option in the `BeamConfig`.
   */
  tokenStorage: TokenStorage;

  protected envConfig: BeamEnvironmentConfig;
  protected defaultHeaders: Record<string, string>;
  protected clientServices = {} as BeamServiceType;
  protected serverServices = {} as BeamServerServiceType;
  protected refreshableRegistry = {} as RefreshableRegistry;
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
   * Dynamically adds multiple api services or microservice clients to the Beam SDK.
   * @param ctors - An array of constructors for the api service or microservice client.
   * @returns The current instance of BeamBase.
   * @example
   * ```ts
   * const beam = await Beam.init({ ... });
   * beam.use([LeadboardService, StatsService]);
   * ```
   * or
   * ```ts
   * const beam = await Beam.init({ ... });
   * beam.use([MyMicroserviceClient, MyOtherMicroserviceClient]);
   * ```
   */
  abstract use<T extends ApiServiceCtor<any> | BeamMicroServiceClientCtor<any>>(
    ctors: readonly T[],
  ): this;

  /**
   * Dynamically adds a single api service or microservice client to the Beam SDK.
   * @param ctor - The constructor for the api service or microservice client.
   * @returns The current instance of BeamBase.
   * @example
   * ```ts
   * const beam = await Beam.init({ ... });
   * beam.use(StatsService);
   * ```
   * or
   * ```ts
   * const beam = await Beam.init({ ... });
   * beam.use(MyMicroserviceClient);
   * ```
   */
  abstract use<T extends ApiServiceCtor<any> | BeamMicroServiceClientCtor<any>>(
    ctor: T,
  ): this;

  protected constructor(config: BeamBaseConfig) {
    this.cid = config.cid;
    this.pid = config.pid;
    this.envConfig = BeamEnvironment.get(this.getConfigEnvironment(config));
    this.tokenStorage =
      config.tokenStorage ??
      defaultTokenStorage({ pid: config.pid, tag: config.instanceTag });
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

  protected getConfigEnvironment(config: BeamBaseConfig) {
    return config.environment ?? 'prod'; // default to prod if not provided
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
