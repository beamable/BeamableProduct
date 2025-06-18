import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { EventClaimRequest } from '@/__generated__/schemas/EventClaimRequest';
import { EventClaimResponse } from '@/__generated__/schemas/EventClaimResponse';
import { EventPlayerView } from '@/__generated__/schemas/EventPlayerView';
import { EventScoreRequest } from '@/__generated__/schemas/EventScoreRequest';
import { GET } from '@/constants';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/constants';
import { POST } from '@/constants';
import { PUT } from '@/constants';

export class EventPlayersApi {
  constructor(
    private readonly r: HttpRequester
  ) {
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EventPlayerView>>} A promise containing the HttpResponse of EventPlayerView
   */
  async getEventPlayersByObjectId(objectId: bigint | string, gamertag?: string): Promise<HttpResponse<EventPlayerView>> {
    let e = "/object/event-players/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<EventPlayerView>({
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
   * @param {EventClaimRequest} payload - The `EventClaimRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EventClaimResponse>>} A promise containing the HttpResponse of EventClaimResponse
   */
  async postEventPlayersClaimByObjectId(objectId: bigint | string, payload: EventClaimRequest, gamertag?: string): Promise<HttpResponse<EventClaimResponse>> {
    let e = "/object/event-players/{objectId}/claim".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<EventClaimResponse, EventClaimRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {EventScoreRequest} payload - The `EventScoreRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putEventPlayersScoreByObjectId(objectId: bigint | string, payload: EventScoreRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/object/event-players/{objectId}/score".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CommonResponse, EventScoreRequest>({
      r: this.r,
      e,
      m: PUT,
      p: payload,
      g: gamertag
    });
  }
}
