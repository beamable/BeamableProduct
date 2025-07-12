import { BatchReadStatsResponse } from '@/__generated__/schemas/BatchReadStatsResponse';
import { BatchSetStatsRequest } from '@/__generated__/schemas/BatchSetStatsRequest';
import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { DELETE } from '@/constants';
import { EmptyResponse } from '@/__generated__/schemas/EmptyResponse';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpResponse } from '@/network/http/types/HttpResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/__generated__/apis/constants';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import { SearchExtendedRequest } from '@/__generated__/schemas/SearchExtendedRequest';
import { SearchExtendedResponse } from '@/__generated__/schemas/SearchExtendedResponse';
import { StatRequest } from '@/__generated__/schemas/StatRequest';
import { StatsResponse } from '@/__generated__/schemas/StatsResponse';
import { StatsSearchRequest } from '@/__generated__/schemas/StatsSearchRequest';
import { StatsSearchResponse } from '@/__generated__/schemas/StatsSearchResponse';
import { StatsSubscribeRequest } from '@/__generated__/schemas/StatsSubscribeRequest';
import { StatsUnsubscribeRequest } from '@/__generated__/schemas/StatsUnsubscribeRequest';
import { StatUpdateRequest } from '@/__generated__/schemas/StatUpdateRequest';
import { StatUpdateRequestStringListFormat } from '@/__generated__/schemas/StatUpdateRequestStringListFormat';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `StatsSubscribeRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function putStatSubscribe(requester: HttpRequester, payload: StatsSubscribeRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/basic/stats/subscribe";
  
  // Make the API request
  return makeApiRequest<CommonResponse, StatsSubscribeRequest>({
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
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function deleteStatSubscribe(requester: HttpRequester, payload: StatsUnsubscribeRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/basic/stats/subscribe";
  
  // Make the API request
  return makeApiRequest<CommonResponse, StatsUnsubscribeRequest>({
    r: requester,
    e: endpoint,
    m: DELETE,
    p: payload,
    g: gamertag,
    w: true
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
export async function getStatsClientBatch(requester: HttpRequester, objectIds: string, format?: string, stats?: string, gamertag?: string): Promise<HttpResponse<BatchReadStatsResponse>> {
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
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `BatchSetStatsRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postStatBatch(requester: HttpRequester, payload: BatchSetStatsRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/basic/stats/batch";
  
  // Make the API request
  return makeApiRequest<EmptyResponse, BatchSetStatsRequest>({
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
 * @param payload - The `StatsSearchRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postStatSearch(requester: HttpRequester, payload: StatsSearchRequest, gamertag?: string): Promise<HttpResponse<StatsSearchResponse>> {
  let endpoint = "/basic/stats/search";
  
  // Make the API request
  return makeApiRequest<StatsSearchResponse, StatsSearchRequest>({
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
 * @param payload - The `SearchExtendedRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postStatSearchExtended(requester: HttpRequester, payload: SearchExtendedRequest, gamertag?: string): Promise<HttpResponse<SearchExtendedResponse>> {
  let endpoint = "/basic/stats/search/extended";
  
  // Make the API request
  return makeApiRequest<SearchExtendedResponse, SearchExtendedRequest>({
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
 * @param payload - The `StatUpdateRequestStringListFormat` instance to use for the API request
 * @param objectId - Format: domain.visibility.type.gamerTag
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postStatClientStringlistByObjectId(requester: HttpRequester, objectId: string, payload: StatUpdateRequestStringListFormat, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
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
export async function getStatByObjectId(requester: HttpRequester, objectId: string, stats?: string, gamertag?: string): Promise<HttpResponse<StatsResponse>> {
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
export async function postStatByObjectId(requester: HttpRequester, objectId: string, payload: StatUpdateRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
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
export async function deleteStatByObjectId(requester: HttpRequester, objectId: string, payload: StatRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
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
export async function getStatClientByObjectId(requester: HttpRequester, objectId: string, stats?: string, gamertag?: string): Promise<HttpResponse<StatsResponse>> {
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
export async function postStatClientByObjectId(requester: HttpRequester, objectId: string, payload: StatUpdateRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
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
