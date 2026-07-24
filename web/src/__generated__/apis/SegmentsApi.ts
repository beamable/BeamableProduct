/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { customerIdPlaceholder } from '@/__generated__/apis/constants';
import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { playerIdPlaceholder } from '@/__generated__/apis/constants';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import { realmIdPlaceholder } from '@/__generated__/apis/constants';
import { segmentIdPlaceholder } from '@/__generated__/apis/constants';
import type { ApiCustomersRealmsSegmentsAttributesGetSegmentsResponse } from '@/__generated__/schemas/ApiCustomersRealmsSegmentsAttributesGetSegmentsResponse';
import type { AudienceRequest } from '@/__generated__/schemas/AudienceRequest';
import type { BulkMembershipResponse } from '@/__generated__/schemas/BulkMembershipResponse';
import type { CreateSegmentRequest } from '@/__generated__/schemas/CreateSegmentRequest';
import type { DuplicateSegmentRequest } from '@/__generated__/schemas/DuplicateSegmentRequest';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { ListMutationRequest } from '@/__generated__/schemas/ListMutationRequest';
import type { ListMutationResult } from '@/__generated__/schemas/ListMutationResult';
import type { MembershipCheckResponse } from '@/__generated__/schemas/MembershipCheckResponse';
import type { MembershipQueryRequest } from '@/__generated__/schemas/MembershipQueryRequest';
import type { MembershipQueryResponse } from '@/__generated__/schemas/MembershipQueryResponse';
import type { PreviewSegmentRequest } from '@/__generated__/schemas/PreviewSegmentRequest';
import type { RealmReconcileResult } from '@/__generated__/schemas/RealmReconcileResult';
import type { SegmentAuditInfoCursorPagedResult } from '@/__generated__/schemas/SegmentAuditInfoCursorPagedResult';
import type { SegmentClearResponse } from '@/__generated__/schemas/SegmentClearResponse';
import type { SegmentCountResponse } from '@/__generated__/schemas/SegmentCountResponse';
import type { SegmentMemberInfoCursorPagedResult } from '@/__generated__/schemas/SegmentMemberInfoCursorPagedResult';
import type { SegmentReconcileResult } from '@/__generated__/schemas/SegmentReconcileResult';
import type { SegmentResponse } from '@/__generated__/schemas/SegmentResponse';
import type { SegmentResponseCursorPagedResult } from '@/__generated__/schemas/SegmentResponseCursorPagedResult';
import type { SegmentState } from '@/__generated__/schemas/enums/SegmentState';
import type { SegmentTransitionInfoCursorPagedResult } from '@/__generated__/schemas/SegmentTransitionInfoCursorPagedResult';
import type { SetSegmentStateRequest } from '@/__generated__/schemas/SetSegmentStateRequest';
import type { UpdateSegmentRequest } from '@/__generated__/schemas/UpdateSegmentRequest';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `CreateSegmentRequest` instance to use for the API request
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostRealmsSegments(requester: HttpRequester, customerId: string, realmId: string, payload: CreateSegmentRequest, gamertag?: string): Promise<HttpResponse<SegmentResponse>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/segments".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
  // Make the API request
  return makeApiRequest<SegmentResponse, CreateSegmentRequest>({
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
 * @param cursor - The `cursor` parameter to include in the API request.
 * @param state - The `state` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetRealmsSegments(requester: HttpRequester, customerId: string, realmId: string, cursor?: string, state?: SegmentState, gamertag?: string): Promise<HttpResponse<SegmentResponseCursorPagedResult>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/segments".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
  // Make the API request
  return makeApiRequest<SegmentResponseCursorPagedResult>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      cursor,
      state
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
 * @param payload - The `DuplicateSegmentRequest` instance to use for the API request
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param segmentId - The `segmentId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostRealmsSegmentsDuplicate(requester: HttpRequester, customerId: string, realmId: string, segmentId: string, payload: DuplicateSegmentRequest, gamertag?: string): Promise<HttpResponse<SegmentResponse>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/segments/{segmentId}/duplicate".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId)).replace(segmentIdPlaceholder, endpointEncoder(segmentId));
  
  // Make the API request
  return makeApiRequest<SegmentResponse, DuplicateSegmentRequest>({
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
 * @param payload - The `PreviewSegmentRequest` instance to use for the API request
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostRealmsSegmentsPreview(requester: HttpRequester, customerId: string, realmId: string, payload: PreviewSegmentRequest, gamertag?: string): Promise<HttpResponse<BulkMembershipResponse>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/segments/preview".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
  // Make the API request
  return makeApiRequest<BulkMembershipResponse, PreviewSegmentRequest>({
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
 * @param payload - The `AudienceRequest` instance to use for the API request
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostRealmsSegmentsAudience(requester: HttpRequester, customerId: string, realmId: string, payload: AudienceRequest, gamertag?: string): Promise<HttpResponse<BulkMembershipResponse>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/segments/audience".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
  // Make the API request
  return makeApiRequest<BulkMembershipResponse, AudienceRequest>({
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
 * @param payload - The `MembershipQueryRequest` instance to use for the API request
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostRealmsSegmentsMemberships(requester: HttpRequester, customerId: string, realmId: string, payload: MembershipQueryRequest, gamertag?: string): Promise<HttpResponse<MembershipQueryResponse>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/segments/memberships".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
  // Make the API request
  return makeApiRequest<MembershipQueryResponse, MembershipQueryRequest>({
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
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostRealmsSegmentsReconcile(requester: HttpRequester, customerId: string, realmId: string, gamertag?: string): Promise<HttpResponse<RealmReconcileResult>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/segments/reconcile".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
  // Make the API request
  return makeApiRequest<RealmReconcileResult>({
    r: requester,
    e: endpoint,
    m: POST,
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
 * @param segmentId - The `segmentId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostRealmsSegmentsReconcileByCustomerIdAndRealmIdAndSegmentId(requester: HttpRequester, customerId: string, realmId: string, segmentId: string, gamertag?: string): Promise<HttpResponse<SegmentReconcileResult>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/segments/{segmentId}/reconcile".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId)).replace(segmentIdPlaceholder, endpointEncoder(segmentId));
  
  // Make the API request
  return makeApiRequest<SegmentReconcileResult>({
    r: requester,
    e: endpoint,
    m: POST,
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
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetRealmsSegmentsAttributes(requester: HttpRequester, customerId: string, realmId: string, gamertag?: string): Promise<HttpResponse<ApiCustomersRealmsSegmentsAttributesGetSegmentsResponse>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/segments/attributes".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
  // Make the API request
  return makeApiRequest<ApiCustomersRealmsSegmentsAttributesGetSegmentsResponse>({
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
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param segmentId - The `segmentId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetRealmsSegmentsCount(requester: HttpRequester, customerId: string, realmId: string, segmentId: string, gamertag?: string): Promise<HttpResponse<SegmentCountResponse>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/segments/{segmentId}/count".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId)).replace(segmentIdPlaceholder, endpointEncoder(segmentId));
  
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
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param segmentId - The `segmentId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetRealmsSegmentsByCustomerIdAndRealmIdAndSegmentId(requester: HttpRequester, customerId: string, realmId: string, segmentId: string, gamertag?: string): Promise<HttpResponse<SegmentResponse>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/segments/{segmentId}".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId)).replace(segmentIdPlaceholder, endpointEncoder(segmentId));
  
  // Make the API request
  return makeApiRequest<SegmentResponse>({
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
 * @param payload - The `UpdateSegmentRequest` instance to use for the API request
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param segmentId - The `segmentId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPutRealmsSegments(requester: HttpRequester, customerId: string, realmId: string, segmentId: string, payload: UpdateSegmentRequest, gamertag?: string): Promise<HttpResponse<SegmentResponse>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/segments/{segmentId}".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId)).replace(segmentIdPlaceholder, endpointEncoder(segmentId));
  
  // Make the API request
  return makeApiRequest<SegmentResponse, UpdateSegmentRequest>({
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
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param segmentId - The `segmentId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersDeleteRealmsSegments(requester: HttpRequester, customerId: string, realmId: string, segmentId: string, gamertag?: string): Promise<HttpResponse<SegmentResponse>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/segments/{segmentId}".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId)).replace(segmentIdPlaceholder, endpointEncoder(segmentId));
  
  // Make the API request
  return makeApiRequest<SegmentResponse>({
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
 * @param payload - The `SetSegmentStateRequest` instance to use for the API request
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param segmentId - The `segmentId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPutRealmsSegmentsState(requester: HttpRequester, customerId: string, realmId: string, segmentId: string, payload: SetSegmentStateRequest, gamertag?: string): Promise<HttpResponse<SegmentResponse>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/segments/{segmentId}/state".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId)).replace(segmentIdPlaceholder, endpointEncoder(segmentId));
  
  // Make the API request
  return makeApiRequest<SegmentResponse, SetSegmentStateRequest>({
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
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param segmentId - The `segmentId` parameter to include in the API request.
 * @param cursor - The `cursor` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetRealmsSegmentsMembers(requester: HttpRequester, customerId: string, realmId: string, segmentId: string, cursor?: string, gamertag?: string): Promise<HttpResponse<SegmentMemberInfoCursorPagedResult>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/segments/{segmentId}/members".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId)).replace(segmentIdPlaceholder, endpointEncoder(segmentId));
  
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
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param segmentId - The `segmentId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersDeleteRealmsSegmentsMembers(requester: HttpRequester, customerId: string, realmId: string, segmentId: string, gamertag?: string): Promise<HttpResponse<SegmentClearResponse>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/segments/{segmentId}/members".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId)).replace(segmentIdPlaceholder, endpointEncoder(segmentId));
  
  // Make the API request
  return makeApiRequest<SegmentClearResponse>({
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
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param playerId - The `playerId` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param segmentId - The `segmentId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetRealmsSegmentsMembersByCustomerIdAndRealmIdAndSegmentIdAndPlayerId(requester: HttpRequester, customerId: string, playerId: bigint | string, realmId: string, segmentId: string, gamertag?: string): Promise<HttpResponse<MembershipCheckResponse>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/segments/{segmentId}/members/{playerId}".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(playerIdPlaceholder, endpointEncoder(playerId)).replace(realmIdPlaceholder, endpointEncoder(realmId)).replace(segmentIdPlaceholder, endpointEncoder(segmentId));
  
  // Make the API request
  return makeApiRequest<MembershipCheckResponse>({
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
 * @param payload - The `ListMutationRequest` instance to use for the API request
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param segmentId - The `segmentId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostRealmsSegmentsIncludesAdd(requester: HttpRequester, customerId: string, realmId: string, segmentId: string, payload: ListMutationRequest, gamertag?: string): Promise<HttpResponse<ListMutationResult>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/segments/{segmentId}/includes/add".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId)).replace(segmentIdPlaceholder, endpointEncoder(segmentId));
  
  // Make the API request
  return makeApiRequest<ListMutationResult, ListMutationRequest>({
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
 * @param payload - The `ListMutationRequest` instance to use for the API request
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param segmentId - The `segmentId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostRealmsSegmentsIncludesRemove(requester: HttpRequester, customerId: string, realmId: string, segmentId: string, payload: ListMutationRequest, gamertag?: string): Promise<HttpResponse<ListMutationResult>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/segments/{segmentId}/includes/remove".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId)).replace(segmentIdPlaceholder, endpointEncoder(segmentId));
  
  // Make the API request
  return makeApiRequest<ListMutationResult, ListMutationRequest>({
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
 * @param payload - The `ListMutationRequest` instance to use for the API request
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param segmentId - The `segmentId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostRealmsSegmentsExcludesAdd(requester: HttpRequester, customerId: string, realmId: string, segmentId: string, payload: ListMutationRequest, gamertag?: string): Promise<HttpResponse<ListMutationResult>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/segments/{segmentId}/excludes/add".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId)).replace(segmentIdPlaceholder, endpointEncoder(segmentId));
  
  // Make the API request
  return makeApiRequest<ListMutationResult, ListMutationRequest>({
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
 * @param payload - The `ListMutationRequest` instance to use for the API request
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param segmentId - The `segmentId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostRealmsSegmentsExcludesRemove(requester: HttpRequester, customerId: string, realmId: string, segmentId: string, payload: ListMutationRequest, gamertag?: string): Promise<HttpResponse<ListMutationResult>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/segments/{segmentId}/excludes/remove".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId)).replace(segmentIdPlaceholder, endpointEncoder(segmentId));
  
  // Make the API request
  return makeApiRequest<ListMutationResult, ListMutationRequest>({
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
 * @param segmentId - The `segmentId` parameter to include in the API request.
 * @param cursor - The `cursor` parameter to include in the API request.
 * @param playerId - The `playerId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetRealmsSegmentsTransitions(requester: HttpRequester, customerId: string, realmId: string, segmentId: string, cursor?: string, playerId?: bigint | string, gamertag?: string): Promise<HttpResponse<SegmentTransitionInfoCursorPagedResult>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/segments/{segmentId}/transitions".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId)).replace(segmentIdPlaceholder, endpointEncoder(segmentId));
  
  // Make the API request
  return makeApiRequest<SegmentTransitionInfoCursorPagedResult>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      cursor,
      playerId
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
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param AccountId - The `AccountId` parameter to include in the API request.
 * @param SegmentId - The `SegmentId` parameter to include in the API request.
 * @param cursor - The `cursor` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetRealmsSegmentsAuditLogs(requester: HttpRequester, customerId: string, realmId: string, AccountId?: string, SegmentId?: string, cursor?: string, gamertag?: string): Promise<HttpResponse<SegmentAuditInfoCursorPagedResult>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/segments/audit-logs".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
  // Make the API request
  return makeApiRequest<SegmentAuditInfoCursorPagedResult>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      AccountId,
      SegmentId,
      cursor
    },
    g: gamertag,
    w: true
  });
}
