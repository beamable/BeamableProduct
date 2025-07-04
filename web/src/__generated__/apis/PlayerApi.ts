import { ApiPlayersPresencePutPlayerPresenceResponse } from '@/__generated__/schemas/ApiPlayersPresencePutPlayerPresenceResponse';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpResponse } from '@/network/http/types/HttpResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/constants';
import { OnlineStatus } from '@/__generated__/schemas/OnlineStatus';
import { PUT } from '@/constants';
import { SetPresenceStatusRequest } from '@/__generated__/schemas/SetPresenceStatusRequest';

export class PlayerApi {
  constructor(
    private readonly r: HttpRequester
  ) {
  }
  
  /**
   * @param {string} playerId - The `playerId` parameter to include in the API request.
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<ApiPlayersPresencePutPlayerPresenceResponse>>} A promise containing the HttpResponse of ApiPlayersPresencePutPlayerPresenceResponse
   */
  async putPlayerPresenceByPlayerId(playerId: string, gamertag?: string): Promise<HttpResponse<ApiPlayersPresencePutPlayerPresenceResponse>> {
    let e = "/api/players/{playerId}/presence".replace(objectIdPlaceholder, endpointEncoder(playerId));
    
    // Make the API request
    return makeApiRequest<ApiPlayersPresencePutPlayerPresenceResponse>({
      r: this.r,
      e,
      m: PUT,
      g: gamertag
    });
  }
  
  /**
   * @param {string} playerId - The `playerId` parameter to include in the API request.
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<OnlineStatus>>} A promise containing the HttpResponse of OnlineStatus
   */
  async getPlayerPresenceByPlayerId(playerId: string, gamertag?: string): Promise<HttpResponse<OnlineStatus>> {
    let e = "/api/players/{playerId}/presence".replace(objectIdPlaceholder, endpointEncoder(playerId));
    
    // Make the API request
    return makeApiRequest<OnlineStatus>({
      r: this.r,
      e,
      m: GET,
      g: gamertag
    });
  }
  
  /**
   * @param {SetPresenceStatusRequest} payload - The `SetPresenceStatusRequest` instance to use for the API request
   * @param {string} playerId - The `playerId` parameter to include in the API request.
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<OnlineStatus>>} A promise containing the HttpResponse of OnlineStatus
   */
  async putPlayerPresenceStatusByPlayerId(playerId: string, payload: SetPresenceStatusRequest, gamertag?: string): Promise<HttpResponse<OnlineStatus>> {
    let e = "/api/players/{playerId}/presence/status".replace(objectIdPlaceholder, endpointEncoder(playerId));
    
    // Make the API request
    return makeApiRequest<OnlineStatus, SetPresenceStatusRequest>({
      r: this.r,
      e,
      m: PUT,
      p: payload,
      g: gamertag
    });
  }
}
