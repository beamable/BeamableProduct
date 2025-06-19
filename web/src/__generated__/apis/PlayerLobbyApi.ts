import { ApiPlayersLobbiesDeletePlayerLobbyResponse } from '@/__generated__/schemas/ApiPlayersLobbiesDeletePlayerLobbyResponse';
import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { Lobby } from '@/__generated__/schemas/Lobby';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/constants';

export class PlayerLobbyApi {
  constructor(
    private readonly r: HttpRequester
  ) {
  }
  
  /**
   * @param {string} playerId - Player Id
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Lobby>>} A promise containing the HttpResponse of Lobby
   */
  async getPlayerLobbiesByPlayerId(playerId: string, gamertag?: string): Promise<HttpResponse<Lobby>> {
    let e = "/api/players/{playerId}/lobbies".replace(objectIdPlaceholder, endpointEncoder(playerId));
    
    // Make the API request
    return makeApiRequest<Lobby>({
      r: this.r,
      e,
      m: GET,
      g: gamertag
    });
  }
  
  /**
   * @param {string} playerId - Player Id
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<ApiPlayersLobbiesDeletePlayerLobbyResponse>>} A promise containing the HttpResponse of ApiPlayersLobbiesDeletePlayerLobbyResponse
   */
  async deletePlayerLobbiesByPlayerId(playerId: string, gamertag?: string): Promise<HttpResponse<ApiPlayersLobbiesDeletePlayerLobbyResponse>> {
    let e = "/api/players/{playerId}/lobbies".replace(objectIdPlaceholder, endpointEncoder(playerId));
    
    // Make the API request
    return makeApiRequest<ApiPlayersLobbiesDeletePlayerLobbyResponse>({
      r: this.r,
      e,
      m: DELETE,
      g: gamertag
    });
  }
}
