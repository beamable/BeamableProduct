import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { EventClaimRequest } from '@/__generated__/schemas/EventClaimRequest';
import { EventClaimResponse } from '@/__generated__/schemas/EventClaimResponse';
import { EventPlayerView } from '@/__generated__/schemas/EventPlayerView';
import { EventScoreRequest } from '@/__generated__/schemas/EventScoreRequest';
import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';

export class EventPlayersApi {
  constructor(
    private readonly requester: HttpRequester
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
    let endpoint = "/object/event-players/{objectId}/".replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EventPlayerView>({
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
   * @param {EventClaimRequest} payload - The `EventClaimRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EventClaimResponse>>} A promise containing the HttpResponse of EventClaimResponse
   */
  async postEventPlayersClaimByObjectId(objectId: bigint | string, payload: EventClaimRequest, gamertag?: string): Promise<HttpResponse<EventClaimResponse>> {
    let endpoint = "/object/event-players/{objectId}/claim".replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EventClaimResponse, EventClaimRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {EventScoreRequest} payload - The `EventScoreRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putEventPlayersScoreByObjectId(objectId: bigint | string, payload: EventScoreRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/object/event-players/{objectId}/score".replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, EventScoreRequest>({
      url: endpoint,
      method: HttpMethod.PUT,
      headers,
      body: payload
    });
  }
}
