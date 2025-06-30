import { CalendarClaimRequest } from '@/__generated__/schemas/CalendarClaimRequest';
import { CalendarQueryResponse } from '@/__generated__/schemas/CalendarQueryResponse';
import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpResponse } from '@/network/http/types/HttpResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/constants';
import { POST } from '@/constants';

export class CalendarsApi {
  constructor(
    private readonly r: HttpRequester
  ) {
  }
  
  /**
   * @param {CalendarClaimRequest} payload - The `CalendarClaimRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postCalendarClaimByObjectId(objectId: bigint | string, payload: CalendarClaimRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/object/calendars/{objectId}/claim".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CommonResponse, CalendarClaimRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CalendarQueryResponse>>} A promise containing the HttpResponse of CalendarQueryResponse
   */
  async getCalendarByObjectId(objectId: bigint | string, gamertag?: string): Promise<HttpResponse<CalendarQueryResponse>> {
    let e = "/object/calendars/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CalendarQueryResponse>({
      r: this.r,
      e,
      m: GET,
      g: gamertag
    });
  }
}
