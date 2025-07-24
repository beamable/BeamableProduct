/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { idPlaceholder } from '@/__generated__/apis/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import type { ApiMatchmakingTicketsDeleteTicketResponse } from '@/__generated__/schemas/ApiMatchmakingTicketsDeleteTicketResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { Match } from '@/__generated__/schemas/Match';
import type { Ticket } from '@/__generated__/schemas/Ticket';
import type { TicketQueryResponse } from '@/__generated__/schemas/TicketQueryResponse';
import type { TicketReservationRequest } from '@/__generated__/schemas/TicketReservationRequest';
import type { TicketReservationResponse } from '@/__generated__/schemas/TicketReservationResponse';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param id - Match ID
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function matchmakingGetMatchesById(requester: HttpRequester, id: string, gamertag?: string): Promise<HttpResponse<Match>> {
  let endpoint = "/api/matchmaking/matches/{id}".replace(idPlaceholder, endpointEncoder(id));
  
  // Make the API request
  return makeApiRequest<Match>({
    r: requester,
    e: endpoint,
    m: GET,
    g: gamertag,
    w: true
  });
}

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param IncludeInactive - The `IncludeInactive` parameter to include in the API request.
 * @param Limit - The `Limit` parameter to include in the API request.
 * @param Players - The `Players` parameter to include in the API request.
 * @param Skip - The `Skip` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function matchmakingGetTickets(requester: HttpRequester, IncludeInactive?: boolean, Limit?: number, Players?: string[], Skip?: number, gamertag?: string): Promise<HttpResponse<TicketQueryResponse>> {
  let endpoint = "/api/matchmaking/tickets";
  
  // Make the API request
  return makeApiRequest<TicketQueryResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      IncludeInactive,
      Limit,
      Players,
      Skip
    },
    g: gamertag,
    w: true
  });
}

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `TicketReservationRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function matchmakingPostTickets(requester: HttpRequester, payload: TicketReservationRequest, gamertag?: string): Promise<HttpResponse<TicketReservationResponse>> {
  let endpoint = "/api/matchmaking/tickets";
  
  // Make the API request
  return makeApiRequest<TicketReservationResponse, TicketReservationRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param id - Ticket ID
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function matchmakingGetTicketsById(requester: HttpRequester, id: string, gamertag?: string): Promise<HttpResponse<Ticket>> {
  let endpoint = "/api/matchmaking/tickets/{id}".replace(idPlaceholder, endpointEncoder(id));
  
  // Make the API request
  return makeApiRequest<Ticket>({
    r: requester,
    e: endpoint,
    m: GET,
    g: gamertag,
    w: true
  });
}

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param id - The `id` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function matchmakingDeleteTicketsById(requester: HttpRequester, id: string, gamertag?: string): Promise<HttpResponse<ApiMatchmakingTicketsDeleteTicketResponse>> {
  let endpoint = "/api/matchmaking/tickets/{id}".replace(idPlaceholder, endpointEncoder(id));
  
  // Make the API request
  return makeApiRequest<ApiMatchmakingTicketsDeleteTicketResponse>({
    r: requester,
    e: endpoint,
    m: DELETE,
    g: gamertag,
    w: true
  });
}
