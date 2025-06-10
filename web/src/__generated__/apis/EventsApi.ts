import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { EventApplyRequest } from '@/__generated__/schemas/EventApplyRequest';
import { EventContentResponse } from '@/__generated__/schemas/EventContentResponse';
import { EventObjectData } from '@/__generated__/schemas/EventObjectData';
import { EventPhaseEndRequest } from '@/__generated__/schemas/EventPhaseEndRequest';
import { EventQueryResponse } from '@/__generated__/schemas/EventQueryResponse';
import { EventsInDateRangeResponse } from '@/__generated__/schemas/EventsInDateRangeResponse';
import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { makeQueryString } from '@/utils/makeQueryString';
import { PingRsp } from '@/__generated__/schemas/PingRsp';
import { SetContentRequest } from '@/__generated__/schemas/SetContentRequest';

export class EventsApi {
  constructor(
    private readonly requester: HttpRequester
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
    let endpoint = "/basic/events/content";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EventContentResponse>({
      url: endpoint,
      method: HttpMethod.GET,
      headers,
      withAuth: true
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
    let endpoint = "/basic/events/calendar";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      from,
      limit,
      query,
      to
    });
    
    // Make the API request
    return this.requester.request<EventsInDateRangeResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers,
      withAuth: true
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
    let endpoint = "/basic/events/applyContent";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, EventApplyRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EventQueryResponse>>} A promise containing the HttpResponse of EventQueryResponse
   */
  async getEventsRunning(gamertag?: string): Promise<HttpResponse<EventQueryResponse>> {
    let endpoint = "/basic/events/running";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EventQueryResponse>({
      url: endpoint,
      method: HttpMethod.GET,
      headers
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
    let endpoint = "/object/events/{objectId}/endPhase";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, EventPhaseEndRequest>({
      url: endpoint,
      method: HttpMethod.PUT,
      headers,
      body: payload,
      withAuth: true
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
    let endpoint = "/object/events/{objectId}/";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EventObjectData>({
      url: endpoint,
      method: HttpMethod.GET,
      headers,
      withAuth: true
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
    let endpoint = "/object/events/{objectId}/ping";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PingRsp>({
      url: endpoint,
      method: HttpMethod.GET,
      headers,
      withAuth: true
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
    let endpoint = "/object/events/{objectId}/content";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, SetContentRequest>({
      url: endpoint,
      method: HttpMethod.PUT,
      headers,
      body: payload,
      withAuth: true
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
    let endpoint = "/object/events/{objectId}/content";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse>({
      url: endpoint,
      method: HttpMethod.DELETE,
      headers,
      withAuth: true
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
    let endpoint = "/object/events/{objectId}/refresh";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse>({
      url: endpoint,
      method: HttpMethod.PUT,
      headers,
      withAuth: true
    });
  }
}
