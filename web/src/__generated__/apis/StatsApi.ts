/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { idPlaceholder } from '@/__generated__/apis/constants';
import { itemTypePlaceholder } from '@/__generated__/apis/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/__generated__/apis/constants';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import type { BatchGetRequest } from '@/__generated__/schemas/BatchGetRequest';
import type { BatchGetStatsResponse } from '@/__generated__/schemas/BatchGetStatsResponse';
import type { BatchReadStatsResponse } from '@/__generated__/schemas/BatchReadStatsResponse';
import type { BatchSetStatsRequest } from '@/__generated__/schemas/BatchSetStatsRequest';
import type { BatchWriteRequest } from '@/__generated__/schemas/BatchWriteRequest';
import type { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import type { EmptyResponse } from '@/__generated__/schemas/EmptyResponse';
import type { GetStatsResponse } from '@/__generated__/schemas/GetStatsResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { SearchExtendedRequest } from '@/__generated__/schemas/SearchExtendedRequest';
import type { SearchExtendedResponse } from '@/__generated__/schemas/SearchExtendedResponse';
import type { SetStatsRequest } from '@/__generated__/schemas/SetStatsRequest';
import type { StatRequest } from '@/__generated__/schemas/StatRequest';
import type { StatsActorCommonResponse } from '@/__generated__/schemas/StatsActorCommonResponse';
import type { StatsActorStatsSearchRequest } from '@/__generated__/schemas/StatsActorStatsSearchRequest';
import type { StatsActorStatsSearchResponse } from '@/__generated__/schemas/StatsActorStatsSearchResponse';
import type { StatsBasicStatsSearchRequest } from '@/__generated__/schemas/StatsBasicStatsSearchRequest';
import type { StatsBasicStatsSearchResponse } from '@/__generated__/schemas/StatsBasicStatsSearchResponse';
import type { StatsResponse } from '@/__generated__/schemas/StatsResponse';
import type { StatsSearchExtendedRequest } from '@/__generated__/schemas/StatsSearchExtendedRequest';
import type { StatsSearchExtendedResponse } from '@/__generated__/schemas/StatsSearchExtendedResponse';
import type { StatsSubscribeRequest } from '@/__generated__/schemas/StatsSubscribeRequest';
import type { StatsUnsubscribeRequest } from '@/__generated__/schemas/StatsUnsubscribeRequest';
import type { StatsVisibility } from '@/__generated__/schemas/enums/StatsVisibility';
import type { StatUpdateRequest } from '@/__generated__/schemas/StatUpdateRequest';
import type { StatUpdateRequestStringListFormat } from '@/__generated__/schemas/StatUpdateRequestStringListFormat';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param id - The `id` parameter to include in the API request.
 * @param itemType - The `itemType` parameter to include in the API request.
 * @param domain - The `domain` parameter to include in the API request.
 * @param keys - The `keys` parameter to include in the API request.
 * @param visibility - The `visibility` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * @param gamertag - Set the request timeout in seconds. Defaults to 10 seconds.
 * 
 */
export async function statsGet(requester: HttpRequester, id: string, itemType: string, domain?: string, keys?: string[], visibility?: StatsVisibility, gamertag?: string, timeout?: string): Promise<HttpResponse<GetStatsResponse>> {
  let endpoint = "/api/stats/{itemType}/{id}".replace(idPlaceholder, endpointEncoder(id)).replace(itemTypePlaceholder, endpointEncoder(itemType));
  
  // Make the API request
  return makeApiRequest<GetStatsResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      domain,
      keys,
      visibility
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
 * @param payload - The `SetStatsRequest` instance to use for the API request
 * @param id - The `id` parameter to include in the API request.
 * @param itemType - The `itemType` parameter to include in the API request.
 * @param domain - The `domain` parameter to include in the API request.
 * @param visibility - The `visibility` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * @param gamertag - Set the request timeout in seconds. Defaults to 10 seconds.
 * 
 */
export async function statsPost(requester: HttpRequester, id: string, itemType: string, payload: SetStatsRequest, domain?: string, visibility?: StatsVisibility, gamertag?: string, timeout?: string): Promise<HttpResponse<StatsActorCommonResponse>> {
  let endpoint = "/api/stats/{itemType}/{id}".replace(idPlaceholder, endpointEncoder(id)).replace(itemTypePlaceholder, endpointEncoder(itemType));
  
  // Make the API request
  return makeApiRequest<StatsActorCommonResponse, SetStatsRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    q: {
      domain,
      visibility
    },
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
 * @param id - The `id` parameter to include in the API request.
 * @param itemType - The `itemType` parameter to include in the API request.
 * @param domain - The `domain` parameter to include in the API request.
 * @param keys - The `keys` parameter to include in the API request.
 * @param visibility - The `visibility` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * @param gamertag - Set the request timeout in seconds. Defaults to 10 seconds.
 * 
 */
export async function statsDelete(requester: HttpRequester, id: string, itemType: string, domain?: string, keys?: string[], visibility?: StatsVisibility, gamertag?: string, timeout?: string): Promise<HttpResponse<StatsActorCommonResponse>> {
  let endpoint = "/api/stats/{itemType}/{id}".replace(idPlaceholder, endpointEncoder(id)).replace(itemTypePlaceholder, endpointEncoder(itemType));
  
  // Make the API request
  return makeApiRequest<StatsActorCommonResponse>({
    r: requester,
    e: endpoint,
    m: DELETE,
    q: {
      domain,
      keys,
      visibility
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
 * @param payload - The `StatsSubscribeRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * @param gamertag - Set the request timeout in seconds. Defaults to 10 seconds.
 * 
 */
export async function statsPutSubscription(requester: HttpRequester, payload: StatsSubscribeRequest, gamertag?: string, timeout?: string): Promise<HttpResponse<StatsActorCommonResponse>> {
  let endpoint = "/api/stats/subscription";
  
  // Make the API request
  return makeApiRequest<StatsActorCommonResponse, StatsSubscribeRequest>({
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
 * @param payload - The `StatsUnsubscribeRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * @param gamertag - Set the request timeout in seconds. Defaults to 10 seconds.
 * 
 */
export async function statsDeleteSubscription(requester: HttpRequester, payload: StatsUnsubscribeRequest, gamertag?: string, timeout?: string): Promise<HttpResponse<StatsActorCommonResponse>> {
  let endpoint = "/api/stats/subscription";
  
  // Make the API request
  return makeApiRequest<StatsActorCommonResponse, StatsUnsubscribeRequest>({
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
 * @param payload - The `BatchGetRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * @param gamertag - Set the request timeout in seconds. Defaults to 10 seconds.
 * 
 */
export async function statsPostBatch(requester: HttpRequester, payload: BatchGetRequest, gamertag?: string, timeout?: string): Promise<HttpResponse<BatchGetStatsResponse>> {
  let endpoint = "/api/stats/batch";
  
  // Make the API request
  return makeApiRequest<BatchGetStatsResponse, BatchGetRequest>({
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
 * @param payload - The `BatchWriteRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * @param gamertag - Set the request timeout in seconds. Defaults to 10 seconds.
 * 
 */
export async function statsPutBatch(requester: HttpRequester, payload: BatchWriteRequest, gamertag?: string, timeout?: string): Promise<HttpResponse<StatsActorCommonResponse>> {
  let endpoint = "/api/stats/batch";
  
  // Make the API request
  return makeApiRequest<StatsActorCommonResponse, BatchWriteRequest>({
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
 * @param payload - The `StatsActorStatsSearchRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * @param gamertag - Set the request timeout in seconds. Defaults to 10 seconds.
 * 
 */
export async function statsPostQuery(requester: HttpRequester, payload: StatsActorStatsSearchRequest, gamertag?: string, timeout?: string): Promise<HttpResponse<StatsActorStatsSearchResponse>> {
  let endpoint = "/api/stats/query";
  
  // Make the API request
  return makeApiRequest<StatsActorStatsSearchResponse, StatsActorStatsSearchRequest>({
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
 * @param payload - The `StatsSearchExtendedRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * @param gamertag - Set the request timeout in seconds. Defaults to 10 seconds.
 * 
 */
export async function statsPostQueryExtended(requester: HttpRequester, payload: StatsSearchExtendedRequest, gamertag?: string, timeout?: string): Promise<HttpResponse<StatsSearchExtendedResponse>> {
  let endpoint = "/api/stats/query/extended";
  
  // Make the API request
  return makeApiRequest<StatsSearchExtendedResponse, StatsSearchExtendedRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `StatsSubscribeRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function statsPutSubscribeBasic(requester: HttpRequester, payload: StatsSubscribeRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/basic/stats/subscribe";
  
  // Make the API request
  return makeApiRequest<CommonResponse, StatsSubscribeRequest>({
    r: requester,
    e: endpoint,
    m: PUT,
    p: payload,
    g: gamertag
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `StatsUnsubscribeRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function statsDeleteSubscribeBasic(requester: HttpRequester, payload: StatsUnsubscribeRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/basic/stats/subscribe";
  
  // Make the API request
  return makeApiRequest<CommonResponse, StatsUnsubscribeRequest>({
    r: requester,
    e: endpoint,
    m: DELETE,
    p: payload,
    g: gamertag
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param objectIds - The `objectIds` parameter to include in the API request.
 * @param format - The `format` parameter to include in the API request.
 * @param stats - The `stats` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function statsGetClientBatchBasic(requester: HttpRequester, objectIds: string, format?: string, stats?: string, gamertag?: string): Promise<HttpResponse<BatchReadStatsResponse>> {
  let endpoint = "/basic/stats/client/batch";
  
  // Make the API request
  return makeApiRequest<BatchReadStatsResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      objectIds,
      format,
      stats
    },
    g: gamertag
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `BatchSetStatsRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function statsPostBatchBasic(requester: HttpRequester, payload: BatchSetStatsRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/basic/stats/batch";
  
  // Make the API request
  return makeApiRequest<EmptyResponse, BatchSetStatsRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `StatsBasicStatsSearchRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function statsPostSearchBasic(requester: HttpRequester, payload: StatsBasicStatsSearchRequest, gamertag?: string): Promise<HttpResponse<StatsBasicStatsSearchResponse>> {
  let endpoint = "/basic/stats/search";
  
  // Make the API request
  return makeApiRequest<StatsBasicStatsSearchResponse, StatsBasicStatsSearchRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `SearchExtendedRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function statsPostSearchExtendedBasic(requester: HttpRequester, payload: SearchExtendedRequest, gamertag?: string): Promise<HttpResponse<SearchExtendedResponse>> {
  let endpoint = "/basic/stats/search/extended";
  
  // Make the API request
  return makeApiRequest<SearchExtendedResponse, SearchExtendedRequest>({
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
 * @param payload - The `StatUpdateRequestStringListFormat` instance to use for the API request
 * @param objectId - Format: domain.visibility.type.gamerTag
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function statsPostClientStringlistByObjectId(requester: HttpRequester, objectId: string, payload: StatUpdateRequestStringListFormat, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/object/stats/{objectId}/client/stringlist".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<EmptyResponse, StatUpdateRequestStringListFormat>({
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
 * @param objectId - Format: domain.visibility.type.gamerTag
 * @param stats - The `stats` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function statsGetByObjectId(requester: HttpRequester, objectId: string, stats?: string, gamertag?: string): Promise<HttpResponse<StatsResponse>> {
  let endpoint = "/object/stats/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<StatsResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      stats
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
 * @param payload - The `StatUpdateRequest` instance to use for the API request
 * @param objectId - Format: domain.visibility.type.gamerTag
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function statsPostByObjectId(requester: HttpRequester, objectId: string, payload: StatUpdateRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/object/stats/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<EmptyResponse, StatUpdateRequest>({
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
 * @param payload - The `StatRequest` instance to use for the API request
 * @param objectId - Format: domain.visibility.type.gamerTag
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function statsDeleteByObjectId(requester: HttpRequester, objectId: string, payload: StatRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/object/stats/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<EmptyResponse, StatRequest>({
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
 * @param objectId - Format: domain.visibility.type.gamerTag
 * @param stats - The `stats` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function statsGetClientByObjectId(requester: HttpRequester, objectId: string, stats?: string, gamertag?: string): Promise<HttpResponse<StatsResponse>> {
  let endpoint = "/object/stats/{objectId}/client".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<StatsResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      stats
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
 * @param payload - The `StatUpdateRequest` instance to use for the API request
 * @param objectId - Format: domain.visibility.type.gamerTag
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function statsPostClientByObjectId(requester: HttpRequester, objectId: string, payload: StatUpdateRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/object/stats/{objectId}/client".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<EmptyResponse, StatUpdateRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}
