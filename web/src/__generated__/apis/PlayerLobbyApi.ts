import { ApiPlayersLobbiesDeletePlayerLobbyResponse } from '@/__generated__/schemas/ApiPlayersLobbiesDeletePlayerLobbyResponse';
import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { Lobby } from '@/__generated__/schemas/Lobby';

export class PlayerLobbyApi {
  constructor(
    private readonly requester: HttpRequester
  ) {
  }
  
  /**
   * @param {string} playerId - Player Id
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Lobby>>} A promise containing the HttpResponse of Lobby
   */
  async getPlayerLobbiesByPlayerId(playerId: string, gamertag?: string): Promise<HttpResponse<Lobby>> {
    let endpoint = "/api/players/{playerId}/lobbies";
    endpoint = endpoint.replace("{playerId}", encodeURIComponent(playerId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<Lobby>({
      url: endpoint,
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {string} playerId - Player Id
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<ApiPlayersLobbiesDeletePlayerLobbyResponse>>} A promise containing the HttpResponse of ApiPlayersLobbiesDeletePlayerLobbyResponse
   */
  async deletePlayerLobbiesByPlayerId(playerId: string, gamertag?: string): Promise<HttpResponse<ApiPlayersLobbiesDeletePlayerLobbyResponse>> {
    let endpoint = "/api/players/{playerId}/lobbies";
    endpoint = endpoint.replace("{playerId}", encodeURIComponent(playerId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<ApiPlayersLobbiesDeletePlayerLobbyResponse>({
      url: endpoint,
      method: HttpMethod.DELETE,
      headers
    });
  }
}
