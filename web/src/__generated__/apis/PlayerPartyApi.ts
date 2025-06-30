import { ApiPlayersPartiesDeletePlayerPartyResponse } from '@/__generated__/schemas/ApiPlayersPartiesDeletePlayerPartyResponse';
import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpResponse } from '@/network/http/types/HttpResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/constants';
import { Party } from '@/__generated__/schemas/Party';
import { PartyInvitesForPlayerResponse } from '@/__generated__/schemas/PartyInvitesForPlayerResponse';

export class PlayerPartyApi {
  constructor(
    private readonly r: HttpRequester
  ) {
  }
  
  /**
   * @param {string} playerId - Player Id
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Party>>} A promise containing the HttpResponse of Party
   */
  async getPlayerPartiesByPlayerId(playerId: string, gamertag?: string): Promise<HttpResponse<Party>> {
    let e = "/api/players/{playerId}/parties".replace(objectIdPlaceholder, endpointEncoder(playerId));
    
    // Make the API request
    return makeApiRequest<Party>({
      r: this.r,
      e,
      m: GET,
      g: gamertag
    });
  }
  
  /**
   * @param {string} playerId - Player Id
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<ApiPlayersPartiesDeletePlayerPartyResponse>>} A promise containing the HttpResponse of ApiPlayersPartiesDeletePlayerPartyResponse
   */
  async deletePlayerPartiesByPlayerId(playerId: string, gamertag?: string): Promise<HttpResponse<ApiPlayersPartiesDeletePlayerPartyResponse>> {
    let e = "/api/players/{playerId}/parties".replace(objectIdPlaceholder, endpointEncoder(playerId));
    
    // Make the API request
    return makeApiRequest<ApiPlayersPartiesDeletePlayerPartyResponse>({
      r: this.r,
      e,
      m: DELETE,
      g: gamertag
    });
  }
  
  /**
   * @param {string} playerId - PlayerId
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<PartyInvitesForPlayerResponse>>} A promise containing the HttpResponse of PartyInvitesForPlayerResponse
   */
  async getPlayerPartiesInvitesByPlayerId(playerId: string, gamertag?: string): Promise<HttpResponse<PartyInvitesForPlayerResponse>> {
    let e = "/api/players/{playerId}/parties/invites".replace(objectIdPlaceholder, endpointEncoder(playerId));
    
    // Make the API request
    return makeApiRequest<PartyInvitesForPlayerResponse>({
      r: this.r,
      e,
      m: GET,
      g: gamertag
    });
  }
  
  /**
   * @param {string} playerId - PlayerId
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<PartyInvitesForPlayerResponse>>} A promise containing the HttpResponse of PartyInvitesForPlayerResponse
   */
  async getPlayerPartyInvitesByPlayerId(playerId: string, gamertag?: string): Promise<HttpResponse<PartyInvitesForPlayerResponse>> {
    let e = "/api/players/{playerId}/party/invites".replace(objectIdPlaceholder, endpointEncoder(playerId));
    
    // Make the API request
    return makeApiRequest<PartyInvitesForPlayerResponse>({
      r: this.r,
      e,
      m: GET,
      g: gamertag
    });
  }
}
