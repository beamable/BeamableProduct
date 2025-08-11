import { BeamBase } from '@/core/BeamBase';
import { makeMicroServiceRequest } from '@/utils/makeMicroServiceRequest';

export interface BeamMicroServiceClientRequestProps<TReq = any> {
  endpoint: string;
  payload?: TReq;
  withAuth: boolean;
}

export type BeamMicroServiceClientCtor<T> = new (beam: BeamBase) => T;

export abstract class BeamMicroServiceClient {
  private readonly beam: BeamBase;

  protected constructor(beam: BeamBase) {
    this.beam = beam;
  }

  public abstract get serviceName(): string;

  protected async request<TRes = any, TReq = any>(
    props: BeamMicroServiceClientRequestProps<TReq>,
  ): Promise<TRes> {
    const { endpoint, payload, withAuth } = props;
    return await makeMicroServiceRequest<TRes, TReq>({
      beam: this.beam,
      serviceName: this.serviceName,
      endpoint,
      payload,
      withAuth,
    });
  }
}
