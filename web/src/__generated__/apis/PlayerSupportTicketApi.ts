/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { playerIdPlaceholder } from '@/__generated__/apis/constants';
import { POST } from '@/constants';
import { ticketIdPlaceholder } from '@/__generated__/apis/constants';
import type { CreateTicketRequest } from '@/__generated__/schemas/CreateTicketRequest';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { PlayerSupportTicketView } from '@/__generated__/schemas/PlayerSupportTicketView';
import type { PlayerTicketListResponse } from '@/__generated__/schemas/PlayerTicketListResponse';
import type { TicketStatus } from '@/__generated__/schemas/enums/TicketStatus';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param playerId - The `playerId` parameter to include in the API request.
 * @param cursor - The `cursor` parameter to include in the API request.
 * @param limit - The `limit` parameter to include in the API request.
 * @param status - The `status` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function playersGetSupportTicketsByPlayerId(requester: HttpRequester, playerId: string, cursor?: string, limit?: number, status?: TicketStatus[], gamertag?: string): Promise<HttpResponse<PlayerTicketListResponse>> {
  let endpoint = "/api/players/{playerId}/support/tickets".replace(playerIdPlaceholder, endpointEncoder(playerId));
  
  // Make the API request
  return makeApiRequest<PlayerTicketListResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      cursor,
      limit,
      status
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
 * @param payload - The `CreateTicketRequest` instance to use for the API request
 * @param playerId - The `playerId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function playersPostSupportTicketsByPlayerId(requester: HttpRequester, playerId: string, payload: CreateTicketRequest, gamertag?: string): Promise<HttpResponse<PlayerSupportTicketView>> {
  let endpoint = "/api/players/{playerId}/support/tickets".replace(playerIdPlaceholder, endpointEncoder(playerId));
  
  // Make the API request
  return makeApiRequest<PlayerSupportTicketView, CreateTicketRequest>({
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
 * @param playerId - The `playerId` parameter to include in the API request.
 * @param ticketId - The `ticketId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function playersGetSupportTickets(requester: HttpRequester, playerId: string, ticketId: string, gamertag?: string): Promise<HttpResponse<PlayerSupportTicketView>> {
  let endpoint = "/api/players/{playerId}/support/tickets/{ticketId}".replace(playerIdPlaceholder, endpointEncoder(playerId)).replace(ticketIdPlaceholder, endpointEncoder(ticketId));
  
  // Make the API request
  return makeApiRequest<PlayerSupportTicketView>({
    r: requester,
    e: endpoint,
    m: GET,
    g: gamertag,
    w: true
  });
}
