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
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/constants';
import { PingRsp } from '@/__generated__/schemas/PingRsp';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import { SetContentRequest } from '@/__generated__/schemas/SetContentRequest';

export class EventsApi {
  constructor(
    private readonly r: HttpRequester
  ) {
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EventContentResponse>>} A promise containing the HttpResponse of EventContentResponse
   */
  async getEventsContent(gamertag?: string): Promise<HttpResponse<EventContentResponse>> {
    let e = "/basic/events/content";
    
    // Make the API request
    return makeApiRequest<EventContentResponse>({
      r: this.r,
      e,
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
   * @param {string} from - The `from` parameter to include in the API request.
   * @param {number} limit - The `limit` parameter to include in the API request.
   * @param {string} query - The `query` parameter to include in the API request.
   * @param {string} to - The `to` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EventsInDateRangeResponse>>} A promise containing the HttpResponse of EventsInDateRangeResponse
   */
  async getEventsCalendar(from?: string, limit?: number, query?: string, to?: string, gamertag?: string): Promise<HttpResponse<EventsInDateRangeResponse>> {
    let e = "/basic/events/calendar";
    
    // Make the API request
    return makeApiRequest<EventsInDateRangeResponse>({
      r: this.r,
      e,
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
   * @param {EventApplyRequest} payload - The `EventApplyRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postEventApplyContent(payload: EventApplyRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/basic/events/applyContent";
    
    // Make the API request
    return makeApiRequest<CommonResponse, EventApplyRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EventQueryResponse>>} A promise containing the HttpResponse of EventQueryResponse
   */
  async getEventsRunning(gamertag?: string): Promise<HttpResponse<EventQueryResponse>> {
    let e = "/basic/events/running";
    
    // Make the API request
    return makeApiRequest<EventQueryResponse>({
      r: this.r,
      e,
      m: GET,
      g: gamertag
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {EventPhaseEndRequest} payload - The `EventPhaseEndRequest` instance to use for the API request
   * @param {string} objectId - Format: events.event_content_id.event_started_timestamp
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putEventEndPhaseByObjectId(objectId: string, payload: EventPhaseEndRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/object/events/{objectId}/endPhase".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CommonResponse, EventPhaseEndRequest>({
      r: this.r,
      e,
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
   * @param {string} objectId - Format: events.event_content_id.event_started_timestamp
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EventObjectData>>} A promise containing the HttpResponse of EventObjectData
   */
  async getEventByObjectId(objectId: string, gamertag?: string): Promise<HttpResponse<EventObjectData>> {
    let e = "/object/events/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<EventObjectData>({
      r: this.r,
      e,
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
   * @param {string} objectId - Format: events.event_content_id.event_started_timestamp
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PingRsp>>} A promise containing the HttpResponse of PingRsp
   */
  async getEventPingByObjectId(objectId: string, gamertag?: string): Promise<HttpResponse<PingRsp>> {
    let e = "/object/events/{objectId}/ping".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<PingRsp>({
      r: this.r,
      e,
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
   * @param {SetContentRequest} payload - The `SetContentRequest` instance to use for the API request
   * @param {string} objectId - Format: events.event_content_id.event_started_timestamp
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putEventContentByObjectId(objectId: string, payload: SetContentRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/object/events/{objectId}/content".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CommonResponse, SetContentRequest>({
      r: this.r,
      e,
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
   * @param {string} objectId - Format: events.event_content_id.event_started_timestamp
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async deleteEventContentByObjectId(objectId: string, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/object/events/{objectId}/content".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CommonResponse>({
      r: this.r,
      e,
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
   * @param {string} objectId - Format: events.event_content_id.event_started_timestamp
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putEventRefreshByObjectId(objectId: string, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/object/events/{objectId}/refresh".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CommonResponse>({
      r: this.r,
      e,
      m: PUT,
      g: gamertag,
      w: true
    });
  }
}
