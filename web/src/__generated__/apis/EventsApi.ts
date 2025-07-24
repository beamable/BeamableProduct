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
import type { EventApplyRequest } from '@/__generated__/schemas/EventApplyRequest';
import type { EventContentResponse } from '@/__generated__/schemas/EventContentResponse';
import type { EventObjectData } from '@/__generated__/schemas/EventObjectData';
import type { EventPhaseEndRequest } from '@/__generated__/schemas/EventPhaseEndRequest';
import type { EventQueryResponse } from '@/__generated__/schemas/EventQueryResponse';
import type { EventsInDateRangeResponse } from '@/__generated__/schemas/EventsInDateRangeResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { PingRsp } from '@/__generated__/schemas/PingRsp';
import type { SetContentRequest } from '@/__generated__/schemas/SetContentRequest';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function eventsGetContentBasic(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<EventContentResponse>> {
  let endpoint = "/basic/events/content";
  
  // Make the API request
  return makeApiRequest<EventContentResponse>({
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
 * @param from - The `from` parameter to include in the API request.
 * @param limit - The `limit` parameter to include in the API request.
 * @param query - The `query` parameter to include in the API request.
 * @param to - The `to` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function eventsGetCalendarBasic(requester: HttpRequester, from?: string, limit?: number, query?: string, to?: string, gamertag?: string): Promise<HttpResponse<EventsInDateRangeResponse>> {
  let endpoint = "/basic/events/calendar";
  
  // Make the API request
  return makeApiRequest<EventsInDateRangeResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      from,
      limit,
      query,
      to
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
 * @param payload - The `EventApplyRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function eventsPostApplyContentBasic(requester: HttpRequester, payload: EventApplyRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/basic/events/applyContent";
  
  // Make the API request
  return makeApiRequest<CommonResponse, EventApplyRequest>({
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
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function eventsGetRunningBasic(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<EventQueryResponse>> {
  let endpoint = "/basic/events/running";
  
  // Make the API request
  return makeApiRequest<EventQueryResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    g: gamertag
  });
}

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `EventPhaseEndRequest` instance to use for the API request
 * @param objectId - Format: events.event_content_id.event_started_timestamp
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function eventsPutEndPhaseByObjectId(requester: HttpRequester, objectId: string, payload: EventPhaseEndRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/object/events/{objectId}/endPhase".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<CommonResponse, EventPhaseEndRequest>({
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
 * @param objectId - Format: events.event_content_id.event_started_timestamp
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function eventsGetByObjectId(requester: HttpRequester, objectId: string, gamertag?: string): Promise<HttpResponse<EventObjectData>> {
  let endpoint = "/object/events/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<EventObjectData>({
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
 * @param objectId - Format: events.event_content_id.event_started_timestamp
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function eventsGetPingByObjectId(requester: HttpRequester, objectId: string, gamertag?: string): Promise<HttpResponse<PingRsp>> {
  let endpoint = "/object/events/{objectId}/ping".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<PingRsp>({
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
 * @param payload - The `SetContentRequest` instance to use for the API request
 * @param objectId - Format: events.event_content_id.event_started_timestamp
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function eventsPutContentByObjectId(requester: HttpRequester, objectId: string, payload: SetContentRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/object/events/{objectId}/content".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<CommonResponse, SetContentRequest>({
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
 * @param objectId - Format: events.event_content_id.event_started_timestamp
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function eventsDeleteContentByObjectId(requester: HttpRequester, objectId: string, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/object/events/{objectId}/content".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
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
 * @param objectId - Format: events.event_content_id.event_started_timestamp
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function eventsPutRefreshByObjectId(requester: HttpRequester, objectId: string, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/object/events/{objectId}/refresh".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<CommonResponse>({
    r: requester,
    e: endpoint,
    m: PUT,
    g: gamertag,
    w: true
  });
}
