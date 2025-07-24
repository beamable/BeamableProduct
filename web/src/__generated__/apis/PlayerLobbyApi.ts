/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { playerIdPlaceholder } from '@/__generated__/apis/constants';
import type { ApiPlayersLobbiesDeletePlayerLobbyResponse } from '@/__generated__/schemas/ApiPlayersLobbiesDeletePlayerLobbyResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { Lobby } from '@/__generated__/schemas/Lobby';

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
export async function playersGetLobbiesByPlayerId(requester: HttpRequester, playerId: string, gamertag?: string): Promise<HttpResponse<Lobby>> {
  let endpoint = "/api/players/{playerId}/lobbies".replace(playerIdPlaceholder, endpointEncoder(playerId));
  
  // Make the API request
  return makeApiRequest<Lobby>({
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
export async function playersDeleteLobbiesByPlayerId(requester: HttpRequester, playerId: string, gamertag?: string): Promise<HttpResponse<ApiPlayersLobbiesDeletePlayerLobbyResponse>> {
  let endpoint = "/api/players/{playerId}/lobbies".replace(playerIdPlaceholder, endpointEncoder(playerId));
  
  // Make the API request
  return makeApiRequest<ApiPlayersLobbiesDeletePlayerLobbyResponse>({
    r: requester,
    e: endpoint,
    m: DELETE,
    g: gamertag,
    w: true
  });
}
