/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/__generated__/apis/constants';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import type { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { LeaderboardAddRequest } from '@/__generated__/schemas/LeaderboardAddRequest';
import type { LeaderboardAssignmentInfo } from '@/__generated__/schemas/LeaderboardAssignmentInfo';
import type { LeaderboardCreateRequest } from '@/__generated__/schemas/LeaderboardCreateRequest';
import type { LeaderboardDetails } from '@/__generated__/schemas/LeaderboardDetails';
import type { LeaderboardListResponse } from '@/__generated__/schemas/LeaderboardListResponse';
import type { LeaderboardMembershipResponse } from '@/__generated__/schemas/LeaderboardMembershipResponse';
import type { LeaderboardPartitionInfo } from '@/__generated__/schemas/LeaderboardPartitionInfo';
import type { LeaderboardRemoveCacheEntryRequest } from '@/__generated__/schemas/LeaderboardRemoveCacheEntryRequest';
import type { LeaderboardRemoveEntryRequest } from '@/__generated__/schemas/LeaderboardRemoveEntryRequest';
import type { LeaderboardSwapRequest } from '@/__generated__/schemas/LeaderboardSwapRequest';
import type { LeaderboardUidResponse } from '@/__generated__/schemas/LeaderboardUidResponse';
import type { LeaderBoardViewResponse } from '@/__generated__/schemas/LeaderBoardViewResponse';
import type { ListLeaderBoardViewResponse } from '@/__generated__/schemas/ListLeaderBoardViewResponse';
import type { MatchMakingMatchesPvpResponse } from '@/__generated__/schemas/MatchMakingMatchesPvpResponse';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param includePartitions - The `includePartitions` parameter to include in the API request.
 * @param limit - The `limit` parameter to include in the API request.
 * @param prefix - The `prefix` parameter to include in the API request.
 * @param skip - The `skip` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function leaderboardsGetListBasic(requester: HttpRequester, includePartitions?: boolean, limit?: number, prefix?: string, skip?: number, gamertag?: string): Promise<HttpResponse<LeaderboardListResponse>> {
  let endpoint = "/basic/leaderboards/list";
  
  // Make the API request
  return makeApiRequest<LeaderboardListResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      includePartitions,
      limit,
      prefix,
      skip
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
 * @param dbid - The `dbid` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function leaderboardsGetPlayerBasic(requester: HttpRequester, dbid: bigint | string, gamertag?: string): Promise<HttpResponse<ListLeaderBoardViewResponse>> {
  let endpoint = "/basic/leaderboards/player";
  
  // Make the API request
  return makeApiRequest<ListLeaderBoardViewResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      dbid
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
 * @param boardId - The `boardId` parameter to include in the API request.
 * @param joinBoard - The `joinBoard` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function leaderboardsGetAssignmentBasic(requester: HttpRequester, boardId: string, joinBoard?: boolean, gamertag?: string): Promise<HttpResponse<LeaderboardAssignmentInfo>> {
  let endpoint = "/basic/leaderboards/assignment";
  
  // Make the API request
  return makeApiRequest<LeaderboardAssignmentInfo>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      boardId,
      joinBoard
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
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function leaderboardsGetUidBasic(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<LeaderboardUidResponse>> {
  let endpoint = "/basic/leaderboards/uid";
  
  // Make the API request
  return makeApiRequest<LeaderboardUidResponse>({
    r: requester,
    e: endpoint,
    m: GET,
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
 * @param objectId - Gamertag of the player.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function leaderboardsDeleteEntriesByObjectId(requester: HttpRequester, objectId: string, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/object/leaderboards/{objectId}/entries".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<CommonResponse>({
    r: requester,
    e: endpoint,
    m: DELETE,
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
 * @param objectId - Gamertag of the player.
 * @param playerId - The `playerId` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function leaderboardsGetMembershipByObjectId(requester: HttpRequester, objectId: string, playerId: bigint | string, gamertag?: string): Promise<HttpResponse<LeaderboardMembershipResponse>> {
  let endpoint = "/object/leaderboards/{objectId}/membership".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<LeaderboardMembershipResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      playerId
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
 * @param objectId - Gamertag of the player.
 * @param ids - The `ids` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function leaderboardsGetRanksByObjectId(requester: HttpRequester, objectId: string, ids: string, gamertag?: string): Promise<HttpResponse<LeaderBoardViewResponse>> {
  let endpoint = "/object/leaderboards/{objectId}/ranks".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<LeaderBoardViewResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      ids
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
 * @param objectId - Gamertag of the player.
 * @param playerId - The `playerId` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function leaderboardsGetPartitionByObjectId(requester: HttpRequester, objectId: string, playerId: bigint | string, gamertag?: string): Promise<HttpResponse<LeaderboardPartitionInfo>> {
  let endpoint = "/object/leaderboards/{objectId}/partition".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<LeaderboardPartitionInfo>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      playerId
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
 * @param objectId - Gamertag of the player.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function leaderboardsGetFriendsByObjectId(requester: HttpRequester, objectId: string, gamertag?: string): Promise<HttpResponse<LeaderBoardViewResponse>> {
  let endpoint = "/object/leaderboards/{objectId}/friends".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<LeaderBoardViewResponse>({
    r: requester,
    e: endpoint,
    m: GET,
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
 * @param payload - The `LeaderboardCreateRequest` instance to use for the API request
 * @param objectId - Gamertag of the player.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function leaderboardsPostByObjectId(requester: HttpRequester, objectId: string, payload: LeaderboardCreateRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/object/leaderboards/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<CommonResponse, LeaderboardCreateRequest>({
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
 * @param objectId - Gamertag of the player.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function leaderboardsDeleteByObjectId(requester: HttpRequester, objectId: string, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/object/leaderboards/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<CommonResponse>({
    r: requester,
    e: endpoint,
    m: DELETE,
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
 * @param objectId - Gamertag of the player.
 * @param poolSize - The `poolSize` parameter to include in the API request.
 * @param windowSize - The `windowSize` parameter to include in the API request.
 * @param windows - The `windows` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function leaderboardsGetMatchesByObjectId(requester: HttpRequester, objectId: string, poolSize: number, windowSize: number, windows: number, gamertag?: string): Promise<HttpResponse<MatchMakingMatchesPvpResponse>> {
  let endpoint = "/object/leaderboards/{objectId}/matches".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<MatchMakingMatchesPvpResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      poolSize,
      windowSize,
      windows
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
 * @param objectId - Gamertag of the player.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function leaderboardsGetAssignmentByObjectId(requester: HttpRequester, objectId: string, gamertag?: string): Promise<HttpResponse<LeaderboardAssignmentInfo>> {
  let endpoint = "/object/leaderboards/{objectId}/assignment".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<LeaderboardAssignmentInfo>({
    r: requester,
    e: endpoint,
    m: GET,
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
 * @param payload - The `LeaderboardRemoveCacheEntryRequest` instance to use for the API request
 * @param objectId - Gamertag of the player.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function leaderboardsDeleteAssignmentByObjectId(requester: HttpRequester, objectId: string, payload: LeaderboardRemoveCacheEntryRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/object/leaderboards/{objectId}/assignment".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<CommonResponse, LeaderboardRemoveCacheEntryRequest>({
    r: requester,
    e: endpoint,
    m: DELETE,
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
 * @param payload - The `LeaderboardAddRequest` instance to use for the API request
 * @param objectId - Gamertag of the player.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function leaderboardsPutEntryByObjectId(requester: HttpRequester, objectId: string, payload: LeaderboardAddRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/object/leaderboards/{objectId}/entry".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<CommonResponse, LeaderboardAddRequest>({
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
 * @param payload - The `LeaderboardRemoveEntryRequest` instance to use for the API request
 * @param objectId - Gamertag of the player.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function leaderboardsDeleteEntryByObjectId(requester: HttpRequester, objectId: string, payload: LeaderboardRemoveEntryRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/object/leaderboards/{objectId}/entry".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<CommonResponse, LeaderboardRemoveEntryRequest>({
    r: requester,
    e: endpoint,
    m: DELETE,
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
 * @param objectId - Gamertag of the player.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function leaderboardsPutFreezeByObjectId(requester: HttpRequester, objectId: string, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/object/leaderboards/{objectId}/freeze".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<CommonResponse>({
    r: requester,
    e: endpoint,
    m: PUT,
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
 * @param objectId - Gamertag of the player.
 * @param from - The `from` parameter to include in the API request.
 * @param max - The `max` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function leaderboardsGetDetailsByObjectId(requester: HttpRequester, objectId: string, from?: number, max?: number, gamertag?: string): Promise<HttpResponse<LeaderboardDetails>> {
  let endpoint = "/object/leaderboards/{objectId}/details".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<LeaderboardDetails>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
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
 * @param objectId - Gamertag of the player.
 * @param focus - The `focus` parameter to include in the API request.
 * @param friends - The `friends` parameter to include in the API request.
 * @param from - The `from` parameter to include in the API request.
 * @param guild - The `guild` parameter to include in the API request.
 * @param max - The `max` parameter to include in the API request.
 * @param outlier - The `outlier` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function leaderboardsGetViewByObjectId(requester: HttpRequester, objectId: string, focus?: bigint | string, friends?: boolean, from?: number, guild?: boolean, max?: number, outlier?: bigint | string, gamertag?: string): Promise<HttpResponse<LeaderBoardViewResponse>> {
  let endpoint = "/object/leaderboards/{objectId}/view".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<LeaderBoardViewResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      focus,
      friends,
      from,
      guild,
      max,
      outlier
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
 * @param payload - The `LeaderboardSwapRequest` instance to use for the API request
 * @param objectId - Gamertag of the player.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function leaderboardsPutSwapByObjectId(requester: HttpRequester, objectId: string, payload: LeaderboardSwapRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/object/leaderboards/{objectId}/swap".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<CommonResponse, LeaderboardSwapRequest>({
    r: requester,
    e: endpoint,
    m: PUT,
    p: payload,
    g: gamertag,
    w: true
  });
}
