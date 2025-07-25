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
import { PUT } from '@/constants';
import type { ApiPartiesInviteDeletePartyResponse } from '@/__generated__/schemas/ApiPartiesInviteDeletePartyResponse';
import type { ApiPartiesInvitePostPartyResponse } from '@/__generated__/schemas/ApiPartiesInvitePostPartyResponse';
import type { ApiPartiesMembersDeletePartyResponse } from '@/__generated__/schemas/ApiPartiesMembersDeletePartyResponse';
import type { CancelInviteToParty } from '@/__generated__/schemas/CancelInviteToParty';
import type { CreateParty } from '@/__generated__/schemas/CreateParty';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { InviteToParty } from '@/__generated__/schemas/InviteToParty';
import type { LeaveParty } from '@/__generated__/schemas/LeaveParty';
import type { Party } from '@/__generated__/schemas/Party';
import type { PromoteNewLeader } from '@/__generated__/schemas/PromoteNewLeader';
import type { UpdateParty } from '@/__generated__/schemas/UpdateParty';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `CreateParty` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function partiesPost(requester: HttpRequester, payload: CreateParty, gamertag?: string): Promise<HttpResponse<Party>> {
  let endpoint = "/api/parties";
  
  // Make the API request
  return makeApiRequest<Party, CreateParty>({
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
 * @param payload - The `UpdateParty` instance to use for the API request
 * @param id - Id of the party
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function partiesPutMetadataById(requester: HttpRequester, id: string, payload: UpdateParty, gamertag?: string): Promise<HttpResponse<Party>> {
  let endpoint = "/api/parties/{id}/metadata".replace(idPlaceholder, endpointEncoder(id));
  
  // Make the API request
  return makeApiRequest<Party, UpdateParty>({
    r: requester,
    e: endpoint,
    m: PUT,
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
 * @param id - Id of the party
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function partiesGetById(requester: HttpRequester, id: string, gamertag?: string): Promise<HttpResponse<Party>> {
  let endpoint = "/api/parties/{id}".replace(idPlaceholder, endpointEncoder(id));
  
  // Make the API request
  return makeApiRequest<Party>({
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
 * @param id - Id of the party
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function partiesPutById(requester: HttpRequester, id: string, gamertag?: string): Promise<HttpResponse<Party>> {
  let endpoint = "/api/parties/{id}".replace(idPlaceholder, endpointEncoder(id));
  
  // Make the API request
  return makeApiRequest<Party>({
    r: requester,
    e: endpoint,
    m: PUT,
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
 * @param payload - The `PromoteNewLeader` instance to use for the API request
 * @param id - Id of the party
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function partiesPutPromoteById(requester: HttpRequester, id: string, payload: PromoteNewLeader, gamertag?: string): Promise<HttpResponse<Party>> {
  let endpoint = "/api/parties/{id}/promote".replace(idPlaceholder, endpointEncoder(id));
  
  // Make the API request
  return makeApiRequest<Party, PromoteNewLeader>({
    r: requester,
    e: endpoint,
    m: PUT,
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
 * @param payload - The `InviteToParty` instance to use for the API request
 * @param id - Id of the party
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function partiesPostInviteById(requester: HttpRequester, id: string, payload: InviteToParty, gamertag?: string): Promise<HttpResponse<ApiPartiesInvitePostPartyResponse>> {
  let endpoint = "/api/parties/{id}/invite".replace(idPlaceholder, endpointEncoder(id));
  
  // Make the API request
  return makeApiRequest<ApiPartiesInvitePostPartyResponse, InviteToParty>({
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
 * @param payload - The `CancelInviteToParty` instance to use for the API request
 * @param id - Id of the party
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function partiesDeleteInviteById(requester: HttpRequester, id: string, payload: CancelInviteToParty, gamertag?: string): Promise<HttpResponse<ApiPartiesInviteDeletePartyResponse>> {
  let endpoint = "/api/parties/{id}/invite".replace(idPlaceholder, endpointEncoder(id));
  
  // Make the API request
  return makeApiRequest<ApiPartiesInviteDeletePartyResponse, CancelInviteToParty>({
    r: requester,
    e: endpoint,
    m: DELETE,
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
 * @param payload - The `LeaveParty` instance to use for the API request
 * @param id - Id of the party
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function partiesDeleteMembersById(requester: HttpRequester, id: string, payload: LeaveParty, gamertag?: string): Promise<HttpResponse<ApiPartiesMembersDeletePartyResponse>> {
  let endpoint = "/api/parties/{id}/members".replace(idPlaceholder, endpointEncoder(id));
  
  // Make the API request
  return makeApiRequest<ApiPartiesMembersDeletePartyResponse, LeaveParty>({
    r: requester,
    e: endpoint,
    m: DELETE,
    p: payload,
    g: gamertag,
    w: true
  });
}
