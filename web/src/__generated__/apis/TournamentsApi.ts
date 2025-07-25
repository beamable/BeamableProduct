/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/__generated__/apis/constants';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import type { AdminGetPlayerStatusResponse } from '@/__generated__/schemas/AdminGetPlayerStatusResponse';
import type { AdminPlayerStatus } from '@/__generated__/schemas/AdminPlayerStatus';
import type { EmptyResponse } from '@/__generated__/schemas/EmptyResponse';
import type { GetChampionsResponse } from '@/__generated__/schemas/GetChampionsResponse';
import type { GetGroupsResponse } from '@/__generated__/schemas/GetGroupsResponse';
import type { GetGroupStatusResponse } from '@/__generated__/schemas/GetGroupStatusResponse';
import type { GetPlayerStatusResponse } from '@/__generated__/schemas/GetPlayerStatusResponse';
import type { GetStandingsResponse } from '@/__generated__/schemas/GetStandingsResponse';
import type { GetStatusForGroupsRequest } from '@/__generated__/schemas/GetStatusForGroupsRequest';
import type { GetStatusForGroupsResponse } from '@/__generated__/schemas/GetStatusForGroupsResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { JoinRequest } from '@/__generated__/schemas/JoinRequest';
import type { PlayerStatus } from '@/__generated__/schemas/PlayerStatus';
import type { RewardsRequest } from '@/__generated__/schemas/RewardsRequest';
import type { RewardsResponse } from '@/__generated__/schemas/RewardsResponse';
import type { ScoreRequest } from '@/__generated__/schemas/ScoreRequest';
import type { TournamentClientView } from '@/__generated__/schemas/TournamentClientView';
import type { TournamentQueryResponse } from '@/__generated__/schemas/TournamentQueryResponse';
import type { UpdatePlayerStatusRequest } from '@/__generated__/schemas/UpdatePlayerStatusRequest';

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `GetStatusForGroupsRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function tournamentsPostSearchGroupsBasic(requester: HttpRequester, payload: GetStatusForGroupsRequest, gamertag?: string): Promise<HttpResponse<GetStatusForGroupsResponse>> {
  let endpoint = "/basic/tournaments/search/groups";
  
  // Make the API request
  return makeApiRequest<GetStatusForGroupsResponse, GetStatusForGroupsRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param contentId - The `contentId` parameter to include in the API request.
 * @param cycle - The `cycle` parameter to include in the API request.
 * @param isRunning - The `isRunning` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function tournamentsGetBasic(requester: HttpRequester, contentId?: string, cycle?: number, isRunning?: boolean, gamertag?: string): Promise<HttpResponse<TournamentQueryResponse>> {
  let endpoint = "/basic/tournaments/";
  
  // Make the API request
  return makeApiRequest<TournamentQueryResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      contentId,
      cycle,
      isRunning
    },
    g: gamertag
  });
}

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `JoinRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function tournamentsPostBasic(requester: HttpRequester, payload: JoinRequest, gamertag?: string): Promise<HttpResponse<PlayerStatus>> {
  let endpoint = "/basic/tournaments/";
  
  // Make the API request
  return makeApiRequest<PlayerStatus, JoinRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param contentId - The `contentId` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function tournamentsGetMeGroupBasic(requester: HttpRequester, contentId?: string, gamertag?: string): Promise<HttpResponse<GetGroupStatusResponse>> {
  let endpoint = "/basic/tournaments/me/group";
  
  // Make the API request
  return makeApiRequest<GetGroupStatusResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      contentId
    },
    g: gamertag,
    w: true
  });
}

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param contentId - The `contentId` parameter to include in the API request.
 * @param tournamentId - The `tournamentId` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function tournamentsGetRewardsBasic(requester: HttpRequester, contentId?: string, tournamentId?: string, gamertag?: string): Promise<HttpResponse<RewardsResponse>> {
  let endpoint = "/basic/tournaments/rewards";
  
  // Make the API request
  return makeApiRequest<RewardsResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      contentId,
      tournamentId
    },
    g: gamertag,
    w: true
  });
}

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `RewardsRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function tournamentsPostRewardsBasic(requester: HttpRequester, payload: RewardsRequest, gamertag?: string): Promise<HttpResponse<RewardsResponse>> {
  let endpoint = "/basic/tournaments/rewards";
  
  // Make the API request
  return makeApiRequest<RewardsResponse, RewardsRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param tournamentId - The `tournamentId` parameter to include in the API request.
 * @param contentId - The `contentId` parameter to include in the API request.
 * @param cycle - The `cycle` parameter to include in the API request.
 * @param focus - The `focus` parameter to include in the API request.
 * @param from - The `from` parameter to include in the API request.
 * @param max - The `max` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function tournamentsGetGlobalBasic(requester: HttpRequester, tournamentId: string, contentId?: string, cycle?: number, focus?: bigint | string, from?: number, max?: number, gamertag?: string): Promise<HttpResponse<GetStandingsResponse>> {
  let endpoint = "/basic/tournaments/global";
  
  // Make the API request
  return makeApiRequest<GetStandingsResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      tournamentId,
      contentId,
      cycle,
      focus,
      from,
      max
    },
    g: gamertag,
    w: true
  });
}

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param tournamentId - The `tournamentId` parameter to include in the API request.
 * @param contentId - The `contentId` parameter to include in the API request.
 * @param cycle - The `cycle` parameter to include in the API request.
 * @param focus - The `focus` parameter to include in the API request.
 * @param from - The `from` parameter to include in the API request.
 * @param max - The `max` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function tournamentsGetStandingsGroupBasic(requester: HttpRequester, tournamentId: string, contentId?: string, cycle?: number, focus?: bigint | string, from?: number, max?: number, gamertag?: string): Promise<HttpResponse<GetStandingsResponse>> {
  let endpoint = "/basic/tournaments/standings/group";
  
  // Make the API request
  return makeApiRequest<GetStandingsResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      tournamentId,
      contentId,
      cycle,
      focus,
      from,
      max
    },
    g: gamertag,
    w: true
  });
}

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param tournamentId - The `tournamentId` parameter to include in the API request.
 * @param contentId - The `contentId` parameter to include in the API request.
 * @param cycle - The `cycle` parameter to include in the API request.
 * @param focus - The `focus` parameter to include in the API request.
 * @param from - The `from` parameter to include in the API request.
 * @param max - The `max` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function tournamentsGetStandingsBasic(requester: HttpRequester, tournamentId: string, contentId?: string, cycle?: number, focus?: bigint | string, from?: number, max?: number, gamertag?: string): Promise<HttpResponse<GetStandingsResponse>> {
  let endpoint = "/basic/tournaments/standings";
  
  // Make the API request
  return makeApiRequest<GetStandingsResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      tournamentId,
      contentId,
      cycle,
      focus,
      from,
      max
    },
    g: gamertag,
    w: true
  });
}

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param playerId - The `playerId` parameter to include in the API request.
 * @param contentId - The `contentId` parameter to include in the API request.
 * @param hasUnclaimedRewards - The `hasUnclaimedRewards` parameter to include in the API request.
 * @param tournamentId - The `tournamentId` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function tournamentsGetAdminPlayerBasic(requester: HttpRequester, playerId: bigint | string, contentId?: string, hasUnclaimedRewards?: boolean, tournamentId?: string, gamertag?: string): Promise<HttpResponse<AdminGetPlayerStatusResponse>> {
  let endpoint = "/basic/tournaments/admin/player";
  
  // Make the API request
  return makeApiRequest<AdminGetPlayerStatusResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      playerId,
      contentId,
      hasUnclaimedRewards,
      tournamentId
    },
    g: gamertag,
    w: true
  });
}

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `UpdatePlayerStatusRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function tournamentsPutAdminPlayerBasic(requester: HttpRequester, payload: UpdatePlayerStatusRequest, gamertag?: string): Promise<HttpResponse<AdminPlayerStatus>> {
  let endpoint = "/basic/tournaments/admin/player";
  
  // Make the API request
  return makeApiRequest<AdminPlayerStatus, UpdatePlayerStatusRequest>({
    r: requester,
    e: endpoint,
    m: PUT,
    p: payload,
    g: gamertag,
    w: true
  });
}

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param contentId - The `contentId` parameter to include in the API request.
 * @param hasUnclaimedRewards - The `hasUnclaimedRewards` parameter to include in the API request.
 * @param tournamentId - The `tournamentId` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function tournamentsGetMeBasic(requester: HttpRequester, contentId?: string, hasUnclaimedRewards?: boolean, tournamentId?: string, gamertag?: string): Promise<HttpResponse<GetPlayerStatusResponse>> {
  let endpoint = "/basic/tournaments/me";
  
  // Make the API request
  return makeApiRequest<GetPlayerStatusResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      contentId,
      hasUnclaimedRewards,
      tournamentId
    },
    g: gamertag,
    w: true
  });
}

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param cycles - The `cycles` parameter to include in the API request.
 * @param tournamentId - The `tournamentId` parameter to include in the API request.
 * @param contentId - The `contentId` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function tournamentsGetChampionsBasic(requester: HttpRequester, cycles: number, tournamentId: string, contentId?: string, gamertag?: string): Promise<HttpResponse<GetChampionsResponse>> {
  let endpoint = "/basic/tournaments/champions";
  
  // Make the API request
  return makeApiRequest<GetChampionsResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      cycles,
      tournamentId,
      contentId
    },
    g: gamertag,
    w: true
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `ScoreRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function tournamentsPostScoreBasic(requester: HttpRequester, payload: ScoreRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/basic/tournaments/score";
  
  // Make the API request
  return makeApiRequest<EmptyResponse, ScoreRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag
  });
}

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param tournamentId - The `tournamentId` parameter to include in the API request.
 * @param contentId - The `contentId` parameter to include in the API request.
 * @param cycle - The `cycle` parameter to include in the API request.
 * @param focus - The `focus` parameter to include in the API request.
 * @param from - The `from` parameter to include in the API request.
 * @param max - The `max` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function tournamentsGetGroupsBasic(requester: HttpRequester, tournamentId: string, contentId?: string, cycle?: number, focus?: bigint | string, from?: number, max?: number, gamertag?: string): Promise<HttpResponse<GetGroupsResponse>> {
  let endpoint = "/basic/tournaments/groups";
  
  // Make the API request
  return makeApiRequest<GetGroupsResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      tournamentId,
      contentId,
      cycle,
      focus,
      from,
      max
    },
    g: gamertag,
    w: true
  });
}

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function tournamentsGetByObjectId(requester: HttpRequester, objectId: bigint | string, gamertag?: string): Promise<HttpResponse<TournamentClientView>> {
  let endpoint = "/object/tournaments/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<TournamentClientView>({
    r: requester,
    e: endpoint,
    m: GET,
    g: gamertag,
    w: true
  });
}
