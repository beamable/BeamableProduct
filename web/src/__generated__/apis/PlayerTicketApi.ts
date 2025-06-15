import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { TicketQueryResponse } from '@/__generated__/schemas/TicketQueryResponse';

export class PlayerTicketApi {
  constructor(
    private readonly requester: HttpRequester
  ) {
  }
  
  /**
   * @param {string} playerId - Player Id
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<TicketQueryResponse>>} A promise containing the HttpResponse of TicketQueryResponse
   */
  async getPlayerMatchmakingTicketsByPlayerId(playerId: string, gamertag?: string): Promise<HttpResponse<TicketQueryResponse>> {
    let endpoint = "/api/players/{playerId}/matchmaking/tickets".replace("{playerId}", encodeURIComponent(playerId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<TicketQueryResponse>({
      url: endpoint,
      method: HttpMethod.GET,
      headers
    });
  }
}
