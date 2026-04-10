/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { playerIdPlaceholder } from '@/__generated__/apis/constants';
import type { ApiPlayersPartiesDeletePlayerPartyResponse } from '@/__generated__/schemas/ApiPlayersPartiesDeletePlayerPartyResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { Party } from '@/__generated__/schemas/Party';
import type { PartyInvitesForPlayerResponse } from '@/__generated__/schemas/PartyInvitesForPlayerResponse';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param playerId - Player Id
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function playersGetPartiesByPlayerId(requester: HttpRequester, playerId: string, gamertag?: string): Promise<HttpResponse<Party>> {
  let endpoint = "/api/players/{playerId}/parties".replace(playerIdPlaceholder, endpointEncoder(playerId));
  
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
 * @param playerId - Player Id
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function playersDeletePartiesByPlayerId(requester: HttpRequester, playerId: string, gamertag?: string): Promise<HttpResponse<ApiPlayersPartiesDeletePlayerPartyResponse>> {
  let endpoint = "/api/players/{playerId}/parties".replace(playerIdPlaceholder, endpointEncoder(playerId));
  
  // Make the API request
  return makeApiRequest<ApiPlayersPartiesDeletePlayerPartyResponse>({
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
 * @param playerId - PlayerId
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function playersGetPartiesInvitesByPlayerId(requester: HttpRequester, playerId: string, gamertag?: string): Promise<HttpResponse<PartyInvitesForPlayerResponse>> {
  let endpoint = "/api/players/{playerId}/parties/invites".replace(playerIdPlaceholder, endpointEncoder(playerId));
  
  // Make the API request
  return makeApiRequest<PartyInvitesForPlayerResponse>({
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
 * @param playerId - PlayerId
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function playersGetPartyInvitesByPlayerId(requester: HttpRequester, playerId: string, gamertag?: string): Promise<HttpResponse<PartyInvitesForPlayerResponse>> {
  let endpoint = "/api/players/{playerId}/party/invites".replace(playerIdPlaceholder, endpointEncoder(playerId));
  
  // Make the API request
  return makeApiRequest<PartyInvitesForPlayerResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    g: gamertag,
    w: true
  });
}
