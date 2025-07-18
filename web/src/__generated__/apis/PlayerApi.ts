import { ApiPlayersPresencePutPlayerPresenceResponse } from '@/__generated__/schemas/ApiPlayersPresencePutPlayerPresenceResponse';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpResponse } from '@/network/http/types/HttpResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { OnlineStatus } from '@/__generated__/schemas/OnlineStatus';
import { playerIdPlaceholder } from '@/__generated__/apis/constants';
import { PUT } from '@/constants';
import { SetPresenceStatusRequest } from '@/__generated__/schemas/SetPresenceStatusRequest';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param playerId - The `playerId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function playersPutPresenceByPlayerId(requester: HttpRequester, playerId: string, gamertag?: string): Promise<HttpResponse<ApiPlayersPresencePutPlayerPresenceResponse>> {
  let endpoint = "/api/players/{playerId}/presence".replace(playerIdPlaceholder, endpointEncoder(playerId));
  
  // Make the API request
  return makeApiRequest<ApiPlayersPresencePutPlayerPresenceResponse>({
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
 * @param playerId - The `playerId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function playersGetPresenceByPlayerId(requester: HttpRequester, playerId: string, gamertag?: string): Promise<HttpResponse<OnlineStatus>> {
  let endpoint = "/api/players/{playerId}/presence".replace(playerIdPlaceholder, endpointEncoder(playerId));
  
  // Make the API request
  return makeApiRequest<OnlineStatus>({
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
 * @param payload - The `SetPresenceStatusRequest` instance to use for the API request
 * @param playerId - The `playerId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function playersPutPresenceStatusByPlayerId(requester: HttpRequester, playerId: string, payload: SetPresenceStatusRequest, gamertag?: string): Promise<HttpResponse<OnlineStatus>> {
  let endpoint = "/api/players/{playerId}/presence/status".replace(playerIdPlaceholder, endpointEncoder(playerId));
  
  // Make the API request
  return makeApiRequest<OnlineStatus, SetPresenceStatusRequest>({
    r: requester,
    e: endpoint,
    m: PUT,
    p: payload,
    g: gamertag,
    w: true
  });
}
