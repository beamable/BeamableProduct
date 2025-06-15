import { ApiMatchmakingTicketsDeleteTicketResponse } from '@/__generated__/schemas/ApiMatchmakingTicketsDeleteTicketResponse';
import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { makeQueryString } from '@/utils/makeQueryString';
import { Match } from '@/__generated__/schemas/Match';
import { Ticket } from '@/__generated__/schemas/Ticket';
import { TicketQueryResponse } from '@/__generated__/schemas/TicketQueryResponse';
import { TicketReservationRequest } from '@/__generated__/schemas/TicketReservationRequest';
import { TicketReservationResponse } from '@/__generated__/schemas/TicketReservationResponse';

export class MatchmakingApi {
  constructor(
    private readonly requester: HttpRequester
  ) {
  }
  
  /**
   * @param {string} id - Match ID
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Match>>} A promise containing the HttpResponse of Match
   */
  async getMatchmakingMatchesById(id: string, gamertag?: string): Promise<HttpResponse<Match>> {
    let endpoint = "/api/matchmaking/matches/{id}".replace("{id}", encodeURIComponent(id.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<Match>({
      url: endpoint,
      method: HttpMethod.GET,
      headers
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
    let endpoint = "/api/matchmaking/tickets";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      IncludeInactive,
      Limit,
      Players,
      Skip
    });
    
    // Make the API request
    return this.requester.request<TicketQueryResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {TicketReservationRequest} payload - The `TicketReservationRequest` instance to use for the API request
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<TicketReservationResponse>>} A promise containing the HttpResponse of TicketReservationResponse
   */
  async postMatchmakingTickets(payload: TicketReservationRequest, gamertag?: string): Promise<HttpResponse<TicketReservationResponse>> {
    let endpoint = "/api/matchmaking/tickets";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<TicketReservationResponse, TicketReservationRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {string} id - Ticket ID
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Ticket>>} A promise containing the HttpResponse of Ticket
   */
  async getMatchmakingTicketsById(id: string, gamertag?: string): Promise<HttpResponse<Ticket>> {
    let endpoint = "/api/matchmaking/tickets/{id}".replace("{id}", encodeURIComponent(id.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<Ticket>({
      url: endpoint,
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {string} id - The `id` parameter to include in the API request.
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<ApiMatchmakingTicketsDeleteTicketResponse>>} A promise containing the HttpResponse of ApiMatchmakingTicketsDeleteTicketResponse
   */
  async deleteMatchmakingTicketsById(id: string, gamertag?: string): Promise<HttpResponse<ApiMatchmakingTicketsDeleteTicketResponse>> {
    let endpoint = "/api/matchmaking/tickets/{id}".replace("{id}", encodeURIComponent(id.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<ApiMatchmakingTicketsDeleteTicketResponse>({
      url: endpoint,
      method: HttpMethod.DELETE,
      headers
    });
  }
}
