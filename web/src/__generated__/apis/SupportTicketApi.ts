/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { customerIdPlaceholder } from '@/__generated__/apis/constants';
import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { PATCH } from '@/constants';
import { POST } from '@/constants';
import { realmIdPlaceholder } from '@/__generated__/apis/constants';
import { ticketIdPlaceholder } from '@/__generated__/apis/constants';
import type { AddCommentRequest } from '@/__generated__/schemas/AddCommentRequest';
import type { ApiCustomersRealmsSupportTicketsDeleteSupportTicketResponse } from '@/__generated__/schemas/ApiCustomersRealmsSupportTicketsDeleteSupportTicketResponse';
import type { AppendHistoryRequest } from '@/__generated__/schemas/AppendHistoryRequest';
import type { CreateTicketRequest } from '@/__generated__/schemas/CreateTicketRequest';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { SupportTicket } from '@/__generated__/schemas/SupportTicket';
import type { TicketComment } from '@/__generated__/schemas/TicketComment';
import type { TicketEvent } from '@/__generated__/schemas/TicketEvent';
import type { TicketListResponse } from '@/__generated__/schemas/TicketListResponse';
import type { TicketPriority } from '@/__generated__/schemas/enums/TicketPriority';
import type { TicketStatus } from '@/__generated__/schemas/enums/TicketStatus';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param assigneeId - The `assigneeId` parameter to include in the API request.
 * @param cursor - The `cursor` parameter to include in the API request.
 * @param limit - The `limit` parameter to include in the API request.
 * @param priority - The `priority` parameter to include in the API request.
 * @param search - The `search` parameter to include in the API request.
 * @param status - The `status` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetRealmsSupportTickets(requester: HttpRequester, customerId: string, realmId: string, assigneeId?: string, cursor?: string, limit?: number, priority?: TicketPriority[], search?: string, status?: TicketStatus[], gamertag?: string): Promise<HttpResponse<TicketListResponse>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/support/tickets".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
  // Make the API request
  return makeApiRequest<TicketListResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      assigneeId,
      cursor,
      limit,
      priority,
      search,
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
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostRealmsSupportTickets(requester: HttpRequester, customerId: string, realmId: string, payload: CreateTicketRequest, gamertag?: string): Promise<HttpResponse<SupportTicket>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/support/tickets".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
  // Make the API request
  return makeApiRequest<SupportTicket, CreateTicketRequest>({
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
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param ticketId - The `ticketId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetRealmsSupportTicketsByTicketId(requester: HttpRequester, customerId: string, realmId: string, ticketId: string, gamertag?: string): Promise<HttpResponse<SupportTicket>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/support/tickets/{ticketId}".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId)).replace(ticketIdPlaceholder, endpointEncoder(ticketId));
  
  // Make the API request
  return makeApiRequest<SupportTicket>({
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
 * @param payload - The `any` instance to use for the API request
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param ticketId - The `ticketId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPatchRealmsSupportTicketsByTicketId(requester: HttpRequester, customerId: string, realmId: string, ticketId: string, payload: any, gamertag?: string): Promise<HttpResponse<SupportTicket>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/support/tickets/{ticketId}".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId)).replace(ticketIdPlaceholder, endpointEncoder(ticketId));
  
  // Make the API request
  return makeApiRequest<SupportTicket, any>({
    r: requester,
    e: endpoint,
    m: PATCH,
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
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param ticketId - The `ticketId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersDeleteRealmsSupportTicketsByTicketId(requester: HttpRequester, customerId: string, realmId: string, ticketId: string, gamertag?: string): Promise<HttpResponse<ApiCustomersRealmsSupportTicketsDeleteSupportTicketResponse>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/support/tickets/{ticketId}".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId)).replace(ticketIdPlaceholder, endpointEncoder(ticketId));
  
  // Make the API request
  return makeApiRequest<ApiCustomersRealmsSupportTicketsDeleteSupportTicketResponse>({
    r: requester,
    e: endpoint,
    m: DELETE,
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
 * @param payload - The `AddCommentRequest` instance to use for the API request
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param ticketId - The `ticketId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostRealmsSupportTicketsComments(requester: HttpRequester, customerId: string, realmId: string, ticketId: string, payload: AddCommentRequest, gamertag?: string): Promise<HttpResponse<TicketComment>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/support/tickets/{ticketId}/comments".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId)).replace(ticketIdPlaceholder, endpointEncoder(ticketId));
  
  // Make the API request
  return makeApiRequest<TicketComment, AddCommentRequest>({
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
 * @param payload - The `AppendHistoryRequest` instance to use for the API request
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param ticketId - The `ticketId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostRealmsSupportTicketsHistory(requester: HttpRequester, customerId: string, realmId: string, ticketId: string, payload: AppendHistoryRequest, gamertag?: string): Promise<HttpResponse<TicketEvent>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/support/tickets/{ticketId}/history".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId)).replace(ticketIdPlaceholder, endpointEncoder(ticketId));
  
  // Make the API request
  return makeApiRequest<TicketEvent, AppendHistoryRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}
