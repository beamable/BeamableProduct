/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { playerIdPlaceholder } from '@/__generated__/apis/constants';
import { POST } from '@/constants';
import type { GetStatsResponse } from '@/__generated__/schemas/GetStatsResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { PlayerStatsActorCommonResponse } from '@/__generated__/schemas/PlayerStatsActorCommonResponse';
import type { SetStatsRequest } from '@/__generated__/schemas/SetStatsRequest';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param playerId - The `playerId` parameter to include in the API request.
 * @param domain - The `domain` parameter to include in the API request.
 * @param keys - The `keys` parameter to include in the API request.
 * @param visibility - The `visibility` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * @param gamertag - Set the request timeout in seconds. Defaults to 10 seconds.
 * 
 */
export async function playersGetStatsByPlayerId(requester: HttpRequester, playerId: string, domain?: string, keys?: string[], visibility?: unknown, gamertag?: string, timeout?: string): Promise<HttpResponse<GetStatsResponse>> {
  let endpoint = "/api/players/{playerId}/stats".replace(playerIdPlaceholder, endpointEncoder(playerId));
  
  // Make the API request
  return makeApiRequest<GetStatsResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      domain,
      keys,
      visibility
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
 * @param payload - The `SetStatsRequest` instance to use for the API request
 * @param playerId - The `playerId` parameter to include in the API request.
 * @param domain - The `domain` parameter to include in the API request.
 * @param visibility - The `visibility` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * @param gamertag - Set the request timeout in seconds. Defaults to 10 seconds.
 * 
 */
export async function playersPostStatsByPlayerId(requester: HttpRequester, playerId: string, payload: SetStatsRequest, domain?: string, visibility?: unknown, gamertag?: string, timeout?: string): Promise<HttpResponse<PlayerStatsActorCommonResponse>> {
  let endpoint = "/api/players/{playerId}/stats".replace(playerIdPlaceholder, endpointEncoder(playerId));
  
  // Make the API request
  return makeApiRequest<PlayerStatsActorCommonResponse, SetStatsRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    q: {
      domain,
      visibility
    },
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
 * @param domain - The `domain` parameter to include in the API request.
 * @param keys - The `keys` parameter to include in the API request.
 * @param visibility - The `visibility` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * @param gamertag - Set the request timeout in seconds. Defaults to 10 seconds.
 * 
 */
export async function playersDeleteStatsByPlayerId(requester: HttpRequester, playerId: string, domain?: string, keys?: string[], visibility?: unknown, gamertag?: string, timeout?: string): Promise<HttpResponse<PlayerStatsActorCommonResponse>> {
  let endpoint = "/api/players/{playerId}/stats".replace(playerIdPlaceholder, endpointEncoder(playerId));
  
  // Make the API request
  return makeApiRequest<PlayerStatsActorCommonResponse>({
    r: requester,
    e: endpoint,
    m: DELETE,
    q: {
      domain,
      keys,
      visibility
    },
    g: gamertag,
    w: true
  });
}
