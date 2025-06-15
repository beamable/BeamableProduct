import { ApiPlayersPartiesDeletePlayerPartyResponse } from '@/__generated__/schemas/ApiPlayersPartiesDeletePlayerPartyResponse';
import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { Party } from '@/__generated__/schemas/Party';
import { PartyInvitesForPlayerResponse } from '@/__generated__/schemas/PartyInvitesForPlayerResponse';

export class PlayerPartyApi {
  constructor(
    private readonly requester: HttpRequester
  ) {
  }
  
  /**
   * @param {string} playerId - Player Id
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Party>>} A promise containing the HttpResponse of Party
   */
  async getPlayerPartiesByPlayerId(playerId: string, gamertag?: string): Promise<HttpResponse<Party>> {
    let endpoint = "/api/players/{playerId}/parties".replace("{playerId}", encodeURIComponent(playerId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<Party>({
      url: endpoint,
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {string} playerId - Player Id
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<ApiPlayersPartiesDeletePlayerPartyResponse>>} A promise containing the HttpResponse of ApiPlayersPartiesDeletePlayerPartyResponse
   */
  async deletePlayerPartiesByPlayerId(playerId: string, gamertag?: string): Promise<HttpResponse<ApiPlayersPartiesDeletePlayerPartyResponse>> {
    let endpoint = "/api/players/{playerId}/parties".replace("{playerId}", encodeURIComponent(playerId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<ApiPlayersPartiesDeletePlayerPartyResponse>({
      url: endpoint,
      method: HttpMethod.DELETE,
      headers
    });
  }
  
  /**
   * @param {string} playerId - PlayerId
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<PartyInvitesForPlayerResponse>>} A promise containing the HttpResponse of PartyInvitesForPlayerResponse
   */
  async getPlayerPartiesInvitesByPlayerId(playerId: string, gamertag?: string): Promise<HttpResponse<PartyInvitesForPlayerResponse>> {
    let endpoint = "/api/players/{playerId}/parties/invites".replace("{playerId}", encodeURIComponent(playerId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PartyInvitesForPlayerResponse>({
      url: endpoint,
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {string} playerId - PlayerId
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<PartyInvitesForPlayerResponse>>} A promise containing the HttpResponse of PartyInvitesForPlayerResponse
   */
  async getPlayerPartyInvitesByPlayerId(playerId: string, gamertag?: string): Promise<HttpResponse<PartyInvitesForPlayerResponse>> {
    let endpoint = "/api/players/{playerId}/party/invites".replace("{playerId}", encodeURIComponent(playerId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PartyInvitesForPlayerResponse>({
      url: endpoint,
      method: HttpMethod.GET,
      headers
    });
  }
}
