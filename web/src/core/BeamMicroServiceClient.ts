import { BeamBase } from '@/core/BeamBase';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { makeMicroServiceRequest } from '@/utils/makeMicroServiceRequest';

export interface BeamMicroServiceClientRequestProps<TReq = any> {
  endpoint: string;
  payload?: TReq;
  withAuth: boolean;
}

/**
 * The minimal surface a microservice client needs from the SDK that hosts it,
 * so the same generated clients work under both a realm-scoped {@link BeamBase}
 * (`Beam`/`BeamServer`) and a zone-scoped `BeamZoneSdk`.
 */
export interface BeamMicroServiceHost {
  /** Beamable Customer ID. */
  cid: string;
  /** The HTTP requester the request rides on (carries auth/scope headers). */
  requester: HttpRequester;
  /**
   * The routing scope segment that follows the cid in the `/basic/{cid}.{scope}.micro_...`
   * path: the realm `pid` for a realm SDK, the zone `zid` for a zone SDK.
   */
  readonly microServiceScope: string;
}

export type BeamMicroServiceClientCtor<T> = new (beam: BeamBase) => T;

export abstract class BeamMicroServiceClient {
  private readonly host: BeamMicroServiceHost;

  protected constructor(host: BeamMicroServiceHost) {
    this.host = host;
  }

  public abstract get serviceName(): string;

  protected async request<TRes = any, TReq = any>(
    props: BeamMicroServiceClientRequestProps<TReq>,
  ): Promise<TRes> {
    const { endpoint, payload, withAuth } = props;
    return await makeMicroServiceRequest<TRes, TReq>({
      host: this.host,
      serviceName: this.serviceName,
      endpoint,
      payload,
      withAuth,
    });
  }
}
