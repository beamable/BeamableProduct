/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { BeamMicroServiceClient, type BeamBase } from '@beamable/sdk';
import type * as Types from './types';

declare module '@beamable/sdk' {
  interface BeamBase {
    /**
     * Access the SampleService microservice.
     * @remarks Before accessing this property, register it first via the `use` method.
     * @example
     * ```ts
     * // client-side:
     * beam.use(SampleServiceClient);
     * beam.sampleServiceClient.serviceName;
     * // server-side:
     * beamServer.use(SampleServiceClient);
     * beamServer.sampleServiceClient.serviceName;
     * ```
     */
    sampleServiceClient: SampleServiceClient;
  }
}

export class SampleServiceClient extends BeamMicroServiceClient {
  constructor(
    beam: BeamBase
  ) {
    super(beam);
  }
  
  get serviceName(): string {
    return "SampleService";
  }
  
  async add(params: Types.AddRequestArgs): Promise<number> {
    return this.request({
      endpoint: "Add",
      payload: params,
      withAuth: true
    });
  }
  
  async greet(params: Types.GreetRequestArgs): Promise<string> {
    return this.request({
      endpoint: "Greet",
      payload: params,
      withAuth: true
    });
  }
  
  async whoAmI(): Promise<Types.WhoAmIResult> {
    return this.request({
      endpoint: "WhoAmI",
      withAuth: true
    });
  }
  
  async visit(): Promise<Types.VisitResult> {
    return this.request({
      endpoint: "Visit",
      withAuth: true
    });
  }
}
