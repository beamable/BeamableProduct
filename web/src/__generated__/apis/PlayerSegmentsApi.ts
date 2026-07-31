/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { playerIdPlaceholder } from '@/__generated__/apis/constants';
import { realmIdPlaceholder } from '@/__generated__/apis/constants';
import type { ApiRealmsPlayersSegmentsGetPlayerSegmentsResponse } from '@/__generated__/schemas/ApiRealmsPlayersSegmentsGetPlayerSegmentsResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { SegmentTransitionInfoCursorPagedResult } from '@/__generated__/schemas/SegmentTransitionInfoCursorPagedResult';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param playerId - The `playerId` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function realmsGetPlayersSegments(requester: HttpRequester, playerId: bigint | string, realmId: string, customerId?: string, gamertag?: string): Promise<HttpResponse<ApiRealmsPlayersSegmentsGetPlayerSegmentsResponse>> {
  let endpoint = "/api/realms/{realmId}/players/{playerId}/segments".replace(playerIdPlaceholder, endpointEncoder(playerId)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
  // Make the API request
  return makeApiRequest<ApiRealmsPlayersSegmentsGetPlayerSegmentsResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      customerId
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
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param cursor - The `cursor` parameter to include in the API request.
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function realmsGetPlayersSegmentsTransitions(requester: HttpRequester, playerId: bigint | string, realmId: string, cursor?: string, customerId?: string, gamertag?: string): Promise<HttpResponse<SegmentTransitionInfoCursorPagedResult>> {
  let endpoint = "/api/realms/{realmId}/players/{playerId}/segments/transitions".replace(playerIdPlaceholder, endpointEncoder(playerId)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
  // Make the API request
  return makeApiRequest<SegmentTransitionInfoCursorPagedResult>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      cursor,
      customerId
    },
    g: gamertag,
    w: true
  });
}
