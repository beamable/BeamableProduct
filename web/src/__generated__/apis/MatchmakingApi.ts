import { ApiMatchmakingTicketsDeleteTicketResponse } from '@/__generated__/schemas/ApiMatchmakingTicketsDeleteTicketResponse';
import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpResponse } from '@/network/http/types/HttpResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { Match } from '@/__generated__/schemas/Match';
import { objectIdPlaceholder } from '@/constants';
import { POST } from '@/constants';
import { Ticket } from '@/__generated__/schemas/Ticket';
import { TicketQueryResponse } from '@/__generated__/schemas/TicketQueryResponse';
import { TicketReservationRequest } from '@/__generated__/schemas/TicketReservationRequest';
import { TicketReservationResponse } from '@/__generated__/schemas/TicketReservationResponse';

export class MatchmakingApi {
  constructor(
    private readonly r: HttpRequester
  ) {
  }
  
  /**
   * @param {string} id - Match ID
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Match>>} A promise containing the HttpResponse of Match
   */
  async getMatchmakingMatchesById(id: string, gamertag?: string): Promise<HttpResponse<Match>> {
    let e = "/api/matchmaking/matches/{id}".replace(objectIdPlaceholder, endpointEncoder(id));
    
    // Make the API request
    return makeApiRequest<Match>({
      r: this.r,
      e,
      m: GET,
      g: gamertag
    });
  }
  
  /**
   * @param {boolean} IncludeInactive - The `IncludeInactive` parameter to include in the API request.
   * @param {number} Limit - The `Limit` parameter to include in the API request.
   * @param {string[]} Players - The `Players` parameter to include in the API request.
   * @param {number} Skip - The `Skip` parameter to include in the API request.
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<TicketQueryResponse>>} A promise containing the HttpResponse of TicketQueryResponse
   */
  async getMatchmakingTickets(IncludeInactive?: boolean, Limit?: number, Players?: string[], Skip?: number, gamertag?: string): Promise<HttpResponse<TicketQueryResponse>> {
    let e = "/api/matchmaking/tickets";
    
    // Make the API request
    return makeApiRequest<TicketQueryResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        IncludeInactive,
        Limit,
        Players,
        Skip
      },
      g: gamertag
    });
  }
  
  /**
   * @param {TicketReservationRequest} payload - The `TicketReservationRequest` instance to use for the API request
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<TicketReservationResponse>>} A promise containing the HttpResponse of TicketReservationResponse
   */
  async postMatchmakingTickets(payload: TicketReservationRequest, gamertag?: string): Promise<HttpResponse<TicketReservationResponse>> {
    let e = "/api/matchmaking/tickets";
    
    // Make the API request
    return makeApiRequest<TicketReservationResponse, TicketReservationRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @param {string} id - Ticket ID
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Ticket>>} A promise containing the HttpResponse of Ticket
   */
  async getMatchmakingTicketsById(id: string, gamertag?: string): Promise<HttpResponse<Ticket>> {
    let e = "/api/matchmaking/tickets/{id}".replace(objectIdPlaceholder, endpointEncoder(id));
    
    // Make the API request
    return makeApiRequest<Ticket>({
      r: this.r,
      e,
      m: GET,
      g: gamertag
    });
  }
  
  /**
   * @param {string} id - The `id` parameter to include in the API request.
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<ApiMatchmakingTicketsDeleteTicketResponse>>} A promise containing the HttpResponse of ApiMatchmakingTicketsDeleteTicketResponse
   */
  async deleteMatchmakingTicketsById(id: string, gamertag?: string): Promise<HttpResponse<ApiMatchmakingTicketsDeleteTicketResponse>> {
    let e = "/api/matchmaking/tickets/{id}".replace(objectIdPlaceholder, endpointEncoder(id));
    
    // Make the API request
    return makeApiRequest<ApiMatchmakingTicketsDeleteTicketResponse>({
      r: this.r,
      e,
      m: DELETE,
      g: gamertag
    });
  }
}
