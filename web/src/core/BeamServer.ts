import { BeamServerConfig } from '@/configs/BeamServerConfig';
import { BaseRequester } from '@/network/http/BaseRequester';
import { BeamRequester } from '@/network/http/BeamRequester';
import { BeamBase, type BeamEnvVars } from '@/core/BeamBase';
import { HEADERS } from '@/constants';
import { ApiService, type ApiServiceCtor } from '@/services/types/ApiService';
import { BeamConfig } from '@/configs/BeamConfig';
import { ServerServicesMixin } from '@/core/mixins';
import { BeamServerServiceType } from '@/core/types';
import {
  BeamMicroServiceClient,
  type BeamMicroServiceClientCtor,
} from '@/core/BeamMicroServiceClient';

/** The main class for interacting with the Beam Server SDK. */
export class BeamServer extends ServerServicesMixin(BeamBase) {
  /** Initialize a new Beam server instance. */
  static init(config: BeamConfig): BeamServer {
    const beamServer = new this(config);
    beamServer.isInitialized = true;
    return beamServer;
  }

  protected constructor(config: BeamServerConfig) {
    super(config);
    this.addOptionalDefaultHeader(HEADERS.UA, config.engine);
    this.addOptionalDefaultHeader(HEADERS.UA_VERSION, config.engineVersion);
  }

  protected createBeamRequester(config: BeamServerConfig): BeamRequester {
    const baseRequester = config.requester ?? new BaseRequester();
    return new BeamRequester({
      inner: baseRequester,
      useSignedRequest: true,
      pid: this.pid,
    });
  }

  static get env(): BeamEnvVars {
    return BeamBase.env;
  }

  use<T extends ApiService>(Service: ApiServiceCtor<T>): this;
  use<T extends BeamMicroServiceClient>(
    Client: BeamMicroServiceClientCtor<T>,
  ): this;
  use(Ctor: any): this {
    if (this.isApiService(Ctor)) {
      const svc = new Ctor({ beam: this });
      (this.serverServices as any)[svc.serviceName] = (userId: string) => {
        svc.userId = userId;
        return svc;
      };
    } else if (this.isMicroServiceClient(Ctor)) {
      const client = new Ctor(this);
      const serviceName = client.serviceName;
      const serviceNameIdentifier =
        serviceName.charAt(0).toLowerCase() + serviceName.slice(1);
      const clientName = `${serviceNameIdentifier}Client`;
      (this as any)[clientName] = client;
    }

    return this;
  }
}

// Declaration‑merge interface that exposes all the server‑side services injected at runtime by the ServerServicesMixin.
// Each property corresponds to a key in ServiceMap, so you get typed access to beamServer.account(userId), beam.auth(userId), etc.
export interface BeamServer extends BeamServerServiceType {}
