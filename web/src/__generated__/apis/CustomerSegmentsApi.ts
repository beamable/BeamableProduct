/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { customerIdPlaceholder } from '@/__generated__/apis/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { playerIdPlaceholder } from '@/__generated__/apis/constants';
import { segmentIdPlaceholder } from '@/__generated__/apis/constants';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { MembershipCheckResponse } from '@/__generated__/schemas/MembershipCheckResponse';
import type { SegmentCountResponse } from '@/__generated__/schemas/SegmentCountResponse';
import type { SegmentMemberInfoCursorPagedResult } from '@/__generated__/schemas/SegmentMemberInfoCursorPagedResult';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param segmentId - The `segmentId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetSegmentsCount(requester: HttpRequester, customerId: string, segmentId: string, gamertag?: string): Promise<HttpResponse<SegmentCountResponse>> {
  let endpoint = "/api/customers/{customerId}/segments/{segmentId}/count".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(segmentIdPlaceholder, endpointEncoder(segmentId));
  
  // Make the API request
  return makeApiRequest<SegmentCountResponse>({
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
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param segmentId - The `segmentId` parameter to include in the API request.
 * @param cursor - The `cursor` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetSegmentsMembers(requester: HttpRequester, customerId: string, segmentId: string, cursor?: string, gamertag?: string): Promise<HttpResponse<SegmentMemberInfoCursorPagedResult>> {
  let endpoint = "/api/customers/{customerId}/segments/{segmentId}/members".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(segmentIdPlaceholder, endpointEncoder(segmentId));
  
  // Make the API request
  return makeApiRequest<SegmentMemberInfoCursorPagedResult>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      cursor
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
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param playerId - The `playerId` parameter to include in the API request.
 * @param segmentId - The `segmentId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetSegmentsMembersByCustomerIdAndSegmentIdAndPlayerId(requester: HttpRequester, customerId: string, playerId: bigint | string, segmentId: string, gamertag?: string): Promise<HttpResponse<MembershipCheckResponse>> {
  let endpoint = "/api/customers/{customerId}/segments/{segmentId}/members/{playerId}".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(playerIdPlaceholder, endpointEncoder(playerId)).replace(segmentIdPlaceholder, endpointEncoder(segmentId));
  
  // Make the API request
  return makeApiRequest<MembershipCheckResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    g: gamertag,
    w: true
  });
}
