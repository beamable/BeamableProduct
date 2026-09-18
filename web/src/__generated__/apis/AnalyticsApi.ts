/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { endpointEncoder } from '@/utils/endpointEncoder';
import { eventPlaceholder } from '@/__generated__/apis/constants';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import type { AnalyticsObservedEventsResponse } from '@/__generated__/schemas/AnalyticsObservedEventsResponse';
import type { AnalyticsQueryRequest } from '@/__generated__/schemas/AnalyticsQueryRequest';
import type { AnalyticsSchemaListResponse } from '@/__generated__/schemas/AnalyticsSchemaListResponse';
import type { AnalyticsSchemaResponse } from '@/__generated__/schemas/AnalyticsSchemaResponse';
import type { ApiAnalyticsQueryPostAnalyticsResponse } from '@/__generated__/schemas/ApiAnalyticsQueryPostAnalyticsResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function analyticsGetEvents(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<AnalyticsObservedEventsResponse>> {
  let endpoint = "/api/analytics/events";
  
  // Make the API request
  return makeApiRequest<AnalyticsObservedEventsResponse>({
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
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function analyticsGetSchemas(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<AnalyticsSchemaListResponse>> {
  let endpoint = "/api/analytics/schemas";
  
  // Make the API request
  return makeApiRequest<AnalyticsSchemaListResponse>({
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
 * @param event - The `event` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function analyticsGetSchemasByEvent(requester: HttpRequester, event: string, gamertag?: string): Promise<HttpResponse<AnalyticsSchemaResponse>> {
  let endpoint = "/api/analytics/schemas/{event}".replace(eventPlaceholder, endpointEncoder(event));
  
  // Make the API request
  return makeApiRequest<AnalyticsSchemaResponse>({
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
 * @param payload - The `AnalyticsQueryRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function analyticsPostQuery(requester: HttpRequester, payload: AnalyticsQueryRequest, gamertag?: string): Promise<HttpResponse<ApiAnalyticsQueryPostAnalyticsResponse>> {
  let endpoint = "/api/analytics/query";
  
  // Make the API request
  return makeApiRequest<ApiAnalyticsQueryPostAnalyticsResponse, AnalyticsQueryRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}
