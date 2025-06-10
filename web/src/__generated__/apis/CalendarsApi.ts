import { CalendarClaimRequest } from '@/__generated__/schemas/CalendarClaimRequest';
import { CalendarQueryResponse } from '@/__generated__/schemas/CalendarQueryResponse';
import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';

export class CalendarsApi {
  constructor(
    private readonly requester: HttpRequester
  ) {
  }
  
  /**
   * @param {CalendarClaimRequest} payload - The `CalendarClaimRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postCalendarClaimByObjectId(objectId: bigint | string, payload: CalendarClaimRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/object/calendars/{objectId}/claim";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, CalendarClaimRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CalendarQueryResponse>>} A promise containing the HttpResponse of CalendarQueryResponse
   */
  async getCalendarByObjectId(objectId: bigint | string, gamertag?: string): Promise<HttpResponse<CalendarQueryResponse>> {
    let endpoint = "/object/calendars/{objectId}/";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CalendarQueryResponse>({
      url: endpoint,
      method: HttpMethod.GET,
      headers
    });
  }
}
