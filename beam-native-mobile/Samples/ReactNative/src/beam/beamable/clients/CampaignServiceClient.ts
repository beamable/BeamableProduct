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
  
  async forwardFunnelToSlack(params: Types.ForwardFunnelToSlackRequestArgs): Promise<Types.WebhookResult> {
    return this.request({
      endpoint: "ForwardFunnelToSlack",
      payload: params,
      withAuth: true
    });
  }
  
  async listRegisteredPlayers(): Promise<Types.RegisteredPlayerList> {
    return this.request({
      endpoint: "ListRegisteredPlayers",
      withAuth: true
    });
  }
  
  async sendCampaignPushToPlayer(params: Types.SendCampaignPushToPlayerRequestArgs): Promise<Types.AdminSendResult> {
    return this.request({
      endpoint: "SendCampaignPushToPlayer",
      payload: params,
      withAuth: true
    });
  }
  
  async launchCampaign(params: Types.LaunchCampaignRequestArgs): Promise<Types.LaunchResult> {
    return this.request({
      endpoint: "LaunchCampaign",
      payload: params,
      withAuth: true
    });
  }
  
  async estimateAudience(params: Types.EstimateAudienceRequestArgs): Promise<Types.AudienceEstimate> {
    return this.request({
      endpoint: "EstimateAudience",
      payload: params,
      withAuth: true
    });
  }
  
  async checkFcmConfig(): Promise<Types.FcmConfigStatus> {
    return this.request({
      endpoint: "CheckFcmConfig",
      withAuth: true
    });
  }
}
