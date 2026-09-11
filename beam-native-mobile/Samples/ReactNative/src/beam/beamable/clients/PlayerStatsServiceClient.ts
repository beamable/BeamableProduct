/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { BeamMicroServiceClient, type BeamBase } from '@beamable/sdk';
import type * as Types from './types';

declare module '@beamable/sdk' {
  interface BeamBase {
    /**
     * Access the PlayerStatsService microservice.
     * @remarks Before accessing this property, register it first via the `use` method.
     * @example
     * ```ts
     * // client-side:
     * beam.use(PlayerStatsServiceClient);
     * beam.playerStatsServiceClient.serviceName;
     * // server-side:
     * beamServer.use(PlayerStatsServiceClient);
     * beamServer.playerStatsServiceClient.serviceName;
     * ```
     */
    playerStatsServiceClient: PlayerStatsServiceClient;
  }
}

export class PlayerStatsServiceClient extends BeamMicroServiceClient {
  constructor(
    beam: BeamBase
  ) {
    super(beam);
  }
  
  get serviceName(): string {
    return "PlayerStatsService";
  }
  
  async getMyStats(): Promise<Types.StatListResponse> {
    return this.request({
      endpoint: "GetMyStats",
      withAuth: true
    });
  }
  
  async setMyStat(params: Types.SetMyStatRequestArgs): Promise<Types.StatResult> {
    return this.request({
      endpoint: "SetMyStat",
      payload: params,
      withAuth: true
    });
  }
  
  async addToMyStat(params: Types.AddToMyStatRequestArgs): Promise<Types.StatResult> {
    return this.request({
      endpoint: "AddToMyStat",
      payload: params,
      withAuth: true
    });
  }
  
  async deleteMyStat(params: Types.DeleteMyStatRequestArgs): Promise<Types.StatResult> {
    return this.request({
      endpoint: "DeleteMyStat",
      payload: params,
      withAuth: true
    });
  }
  
  async createPlayersWithStat(params: Types.CreatePlayersWithStatRequestArgs): Promise<Types.BulkCreateResult> {
    return this.request({
      endpoint: "CreatePlayersWithStat",
      payload: params,
      withAuth: true
    });
  }
  
  async getPlayerStats(params: Types.GetPlayerStatsRequestArgs): Promise<Types.StatListResponse> {
    return this.request({
      endpoint: "GetPlayerStats",
      payload: params,
      withAuth: true
    });
  }
  
  async setPlayerStat(params: Types.SetPlayerStatRequestArgs): Promise<Types.StatResult> {
    return this.request({
      endpoint: "SetPlayerStat",
      payload: params,
      withAuth: true
    });
  }
}
