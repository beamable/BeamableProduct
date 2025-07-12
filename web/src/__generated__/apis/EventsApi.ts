import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { EventApplyRequest } from '@/__generated__/schemas/EventApplyRequest';
import { EventContentResponse } from '@/__generated__/schemas/EventContentResponse';
import { EventObjectData } from '@/__generated__/schemas/EventObjectData';
import { EventPhaseEndRequest } from '@/__generated__/schemas/EventPhaseEndRequest';
import { EventQueryResponse } from '@/__generated__/schemas/EventQueryResponse';
import { EventsInDateRangeResponse } from '@/__generated__/schemas/EventsInDateRangeResponse';
import { GET } from '@/constants';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpResponse } from '@/network/http/types/HttpResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/__generated__/apis/constants';
import { PingRsp } from '@/__generated__/schemas/PingRsp';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import { SetContentRequest } from '@/__generated__/schemas/SetContentRequest';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getEventsContent(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<EventContentResponse>> {
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
export async function getEventsCalendar(requester: HttpRequester, from?: string, limit?: number, query?: string, to?: string, gamertag?: string): Promise<HttpResponse<EventsInDateRangeResponse>> {
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
export async function postEventApplyContent(requester: HttpRequester, payload: EventApplyRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
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
export async function getEventsRunning(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<EventQueryResponse>> {
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
export async function putEventEndPhaseByObjectId(requester: HttpRequester, objectId: string, payload: EventPhaseEndRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
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
export async function getEventByObjectId(requester: HttpRequester, objectId: string, gamertag?: string): Promise<HttpResponse<EventObjectData>> {
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
export async function getEventPingByObjectId(requester: HttpRequester, objectId: string, gamertag?: string): Promise<HttpResponse<PingRsp>> {
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
export async function putEventContentByObjectId(requester: HttpRequester, objectId: string, payload: SetContentRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
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
export async function deleteEventContentByObjectId(requester: HttpRequester, objectId: string, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
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
export async function putEventRefreshByObjectId(requester: HttpRequester, objectId: string, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
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
