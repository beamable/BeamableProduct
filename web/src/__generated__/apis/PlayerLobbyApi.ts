import { ApiPlayersLobbiesDeletePlayerLobbyResponse } from '@/__generated__/schemas/ApiPlayersLobbiesDeletePlayerLobbyResponse';
import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpResponse } from '@/network/http/types/HttpResponse';
import { Lobby } from '@/__generated__/schemas/Lobby';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { playerIdPlaceholder } from '@/__generated__/apis/constants';

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
export async function getPlayerLobbiesByPlayerId(requester: HttpRequester, playerId: string, gamertag?: string): Promise<HttpResponse<Lobby>> {
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
export async function deletePlayerLobbiesByPlayerId(requester: HttpRequester, playerId: string, gamertag?: string): Promise<HttpResponse<ApiPlayersLobbiesDeletePlayerLobbyResponse>> {
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
