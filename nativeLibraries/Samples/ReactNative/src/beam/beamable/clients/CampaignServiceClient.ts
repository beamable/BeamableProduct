/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { BeamMicroServiceClient, type BeamBase } from '@beamable/sdk';
import type * as Types from './types';

declare module '@beamable/sdk' {
  interface BeamBase {
    /**
     * Access the CampaignService microservice.
     * @remarks Before accessing this property, register it first via the `use` method.
     * @example
     * ```ts
     * // client-side:
     * beam.use(CampaignServiceClient);
     * beam.campaignServiceClient.serviceName;
     * // server-side:
     * beamServer.use(CampaignServiceClient);
     * beamServer.campaignServiceClient.serviceName;
     * ```
     */
    campaignServiceClient: CampaignServiceClient;
  }
}

export class CampaignServiceClient extends BeamMicroServiceClient {
  constructor(
    beam: BeamBase
  ) {
    super(beam);
  }

  get serviceName(): string {
    return "CampaignService";
  }

  async registerDeviceToken(params: Types.RegisterDeviceTokenRequestArgs): Promise<Types.RegisterResult> {
    return this.request({
      endpoint: "RegisterDeviceToken",
      payload: params,
      withAuth: true
    });
  }

  async unregisterDeviceToken(params: Types.UnregisterDeviceTokenRequestArgs): Promise<Types.UnregisterResult> {
    return this.request({
      endpoint: "UnregisterDeviceToken",
      payload: params,
      withAuth: true
    });
  }

  async listMyDevices(): Promise<Types.DeviceList> {
    return this.request({
      endpoint: "ListMyDevices",
      withAuth: true
    });
  }
}
