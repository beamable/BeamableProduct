import { ApiPlayersPresencePutPlayerPresenceResponse } from '@/__generated__/schemas/ApiPlayersPresencePutPlayerPresenceResponse';
import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { OnlineStatus } from '@/__generated__/schemas/OnlineStatus';
import { SetPresenceStatusRequest } from '@/__generated__/schemas/SetPresenceStatusRequest';

export class PlayerApi {
  constructor(
    private readonly requester: HttpRequester
  ) {
  }
  
  /**
   * @param {string} playerId - The `playerId` parameter to include in the API request.
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<ApiPlayersPresencePutPlayerPresenceResponse>>} A promise containing the HttpResponse of ApiPlayersPresencePutPlayerPresenceResponse
   */
  async putPlayerPresenceByPlayerId(playerId: string, gamertag?: string): Promise<HttpResponse<ApiPlayersPresencePutPlayerPresenceResponse>> {
    let endpoint = "/api/players/{playerId}/presence".replace("{playerId}", encodeURIComponent(playerId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<ApiPlayersPresencePutPlayerPresenceResponse>({
      url: endpoint,
      method: HttpMethod.PUT,
      headers
    });
  }
  
  /**
   * @param {string} playerId - The `playerId` parameter to include in the API request.
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<OnlineStatus>>} A promise containing the HttpResponse of OnlineStatus
   */
  async getPlayerPresenceByPlayerId(playerId: string, gamertag?: string): Promise<HttpResponse<OnlineStatus>> {
    let endpoint = "/api/players/{playerId}/presence".replace("{playerId}", encodeURIComponent(playerId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<OnlineStatus>({
      url: endpoint,
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {SetPresenceStatusRequest} payload - The `SetPresenceStatusRequest` instance to use for the API request
   * @param {string} playerId - The `playerId` parameter to include in the API request.
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<OnlineStatus>>} A promise containing the HttpResponse of OnlineStatus
   */
  async putPlayerPresenceStatusByPlayerId(playerId: string, payload: SetPresenceStatusRequest, gamertag?: string): Promise<HttpResponse<OnlineStatus>> {
    let endpoint = "/api/players/{playerId}/presence/status".replace("{playerId}", encodeURIComponent(playerId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<OnlineStatus, SetPresenceStatusRequest>({
      url: endpoint,
      method: HttpMethod.PUT,
      headers,
      body: payload
    });
  }
}
