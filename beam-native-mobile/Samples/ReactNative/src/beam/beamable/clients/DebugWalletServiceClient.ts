/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { BeamMicroServiceClient, type BeamBase } from '@beamable/sdk';
import type * as Types from './types';

declare module '@beamable/sdk' {
  interface BeamBase {
    /**
     * Access the DebugWalletService microservice.
     * @remarks Before accessing this property, register it first via the `use` method.
     * @example
     * ```ts
     * // client-side:
     * beam.use(DebugWalletServiceClient);
     * beam.debugWalletServiceClient.serviceName;
     * // server-side:
     * beamServer.use(DebugWalletServiceClient);
     * beamServer.debugWalletServiceClient.serviceName;
     * ```
     */
    debugWalletServiceClient: DebugWalletServiceClient;
  }
}

export class DebugWalletServiceClient extends BeamMicroServiceClient {
  constructor(
    beam: BeamBase
  ) {
    super(beam);
  }
  
  get serviceName(): string {
    return "DebugWalletService";
  }
  
  async addCurrency(params: Types.AddCurrencyRequestArgs): Promise<Types.CurrencyGrantResult> {
    return this.request({
      endpoint: "AddCurrency",
      payload: params,
      withAuth: true
    });
  }
}
