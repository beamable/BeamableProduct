import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpResponse } from '@/network/http/types/HttpResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/constants';
import { TicketQueryResponse } from '@/__generated__/schemas/TicketQueryResponse';

export class PlayerTicketApi {
  constructor(
    private readonly r: HttpRequester
  ) {
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {string} playerId - Player Id
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<TicketQueryResponse>>} A promise containing the HttpResponse of TicketQueryResponse
   */
  async getPlayerMatchmakingTicketsByPlayerId(playerId: string, gamertag?: string): Promise<HttpResponse<TicketQueryResponse>> {
    let e = "/api/players/{playerId}/matchmaking/tickets".replace(objectIdPlaceholder, endpointEncoder(playerId));
    
    // Make the API request
    return makeApiRequest<TicketQueryResponse>({
      r: this.r,
      e,
      m: GET,
      g: gamertag,
      w: true
    });
  }
}
