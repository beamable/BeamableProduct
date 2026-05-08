/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { namePlaceholder } from '@/__generated__/apis/constants';
import { PATCH } from '@/constants';
import { PUT } from '@/constants';
import type { AnalyticEventView } from '@/__generated__/schemas/AnalyticEventView';
import type { BatchUpsertAnalyticsEventsRequest } from '@/__generated__/schemas/BatchUpsertAnalyticsEventsRequest';
import type { BatchUpsertAnalyticsEventsResponse } from '@/__generated__/schemas/BatchUpsertAnalyticsEventsResponse';
import type { EmptyMessage } from '@/__generated__/schemas/EmptyMessage';
import type { GetAnalyticEventSchemasResponse } from '@/__generated__/schemas/GetAnalyticEventSchemasResponse';
import type { GetAnalyticEventsResponse } from '@/__generated__/schemas/GetAnalyticEventsResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { PatchAnalyticsEventRequest } from '@/__generated__/schemas/PatchAnalyticsEventRequest';
import type { UpsertAnalyticsEventRequest } from '@/__generated__/schemas/UpsertAnalyticsEventRequest';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function analyticsGetEventSchemas(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<GetAnalyticEventsResponse>> {
  let endpoint = "/api/analytics/event/schemas";
  
  // Make the API request
  return makeApiRequest<GetAnalyticEventsResponse>({
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
export async function analyticsGetEventSchemasUrls(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<GetAnalyticEventSchemasResponse>> {
  let endpoint = "/api/analytics/event/schemas:urls";
  
  // Make the API request
  return makeApiRequest<GetAnalyticEventSchemasResponse>({
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
 * @param name - The `name` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function analyticsGetEventSchemasByName(requester: HttpRequester, name: string, gamertag?: string): Promise<HttpResponse<AnalyticEventView>> {
  let endpoint = "/api/analytics/event/schemas/{name}".replace(namePlaceholder, endpointEncoder(name));
  
  // Make the API request
  return makeApiRequest<AnalyticEventView>({
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
 * @param payload - The `UpsertAnalyticsEventRequest` instance to use for the API request
 * @param name - The `name` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function analyticsPutEventSchemasByName(requester: HttpRequester, name: string, payload: UpsertAnalyticsEventRequest, gamertag?: string): Promise<HttpResponse<AnalyticEventView>> {
  let endpoint = "/api/analytics/event/schemas/{name}".replace(namePlaceholder, endpointEncoder(name));
  
  // Make the API request
  return makeApiRequest<AnalyticEventView, UpsertAnalyticsEventRequest>({
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
 * @param payload - The `PatchAnalyticsEventRequest` instance to use for the API request
 * @param name - The `name` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function analyticsPatchEventSchemasByName(requester: HttpRequester, name: string, payload: PatchAnalyticsEventRequest, gamertag?: string): Promise<HttpResponse<EmptyMessage>> {
  let endpoint = "/api/analytics/event/schemas/{name}".replace(namePlaceholder, endpointEncoder(name));
  
  // Make the API request
  return makeApiRequest<EmptyMessage, PatchAnalyticsEventRequest>({
    r: requester,
    e: endpoint,
    m: PATCH,
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
 * @param name - The `name` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function analyticsDeleteEventSchemasByName(requester: HttpRequester, name: string, gamertag?: string): Promise<HttpResponse<EmptyMessage>> {
  let endpoint = "/api/analytics/event/schemas/{name}".replace(namePlaceholder, endpointEncoder(name));
  
  // Make the API request
  return makeApiRequest<EmptyMessage>({
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
 * @param payload - The `BatchUpsertAnalyticsEventsRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function analyticsPutEventSchemasBatch(requester: HttpRequester, payload: BatchUpsertAnalyticsEventsRequest, gamertag?: string): Promise<HttpResponse<BatchUpsertAnalyticsEventsResponse>> {
  let endpoint = "/api/analytics/event/schemas:batch";
  
  // Make the API request
  return makeApiRequest<BatchUpsertAnalyticsEventsResponse, BatchUpsertAnalyticsEventsRequest>({
    r: requester,
    e: endpoint,
    m: PUT,
    p: payload,
    g: gamertag,
    w: true
  });
}
