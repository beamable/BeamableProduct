/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { playerIdPlaceholder } from '@/__generated__/apis/constants';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { PlayerSessionActorSessionClientHistoryResponse } from '@/__generated__/schemas/PlayerSessionActorSessionClientHistoryResponse';
import type { PlayerSessionActorSessionHistoryResponse } from '@/__generated__/schemas/PlayerSessionActorSessionHistoryResponse';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param playerId - The `playerId` parameter to include in the API request.
 * @param month - The `month` parameter to include in the API request.
 * @param year - The `year` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * @param gamertag - Set the request timeout in seconds. Defaults to 10 seconds.
 * 
 */
export async function playersGetSessionsByPlayerId(requester: HttpRequester, playerId: string, month?: number, year?: number, gamertag?: string, timeout?: string): Promise<HttpResponse<PlayerSessionActorSessionHistoryResponse>> {
  let endpoint = "/api/players/{playerId}/sessions".replace(playerIdPlaceholder, endpointEncoder(playerId));
  
  // Make the API request
  return makeApiRequest<PlayerSessionActorSessionHistoryResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      month,
      year
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
 * @param playerId - The `playerId` parameter to include in the API request.
 * @param month - The `month` parameter to include in the API request.
 * @param year - The `year` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * @param gamertag - Set the request timeout in seconds. Defaults to 10 seconds.
 * 
 */
export async function playersGetSessionsClientByPlayerId(requester: HttpRequester, playerId: string, month?: number, year?: number, gamertag?: string, timeout?: string): Promise<HttpResponse<PlayerSessionActorSessionClientHistoryResponse>> {
  let endpoint = "/api/players/{playerId}/sessions/client".replace(playerIdPlaceholder, endpointEncoder(playerId));
  
  // Make the API request
  return makeApiRequest<PlayerSessionActorSessionClientHistoryResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      month,
      year
    },
    g: gamertag,
    w: true
  });
}
