/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { accountIdPlaceholder } from '@/__generated__/apis/constants';
import { bundleNamePlaceholder } from '@/__generated__/apis/constants';
import { customerIdPlaceholder } from '@/__generated__/apis/constants';
import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { nsPlaceholder } from '@/__generated__/apis/constants';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import { realmIdPlaceholder } from '@/__generated__/apis/constants';
import { segmentIdPlaceholder } from '@/__generated__/apis/constants';
import { zoneIdPlaceholder } from '@/__generated__/apis/constants';
import type { ApiBeamoZonesForcedBundlesDeleteZonesResponse } from '@/__generated__/schemas/ApiBeamoZonesForcedBundlesDeleteZonesResponse';
import type { ApiBeamoZonesForcedBundlesPutZonesResponse } from '@/__generated__/schemas/ApiBeamoZonesForcedBundlesPutZonesResponse';
import type { ApiCustomersZonesSegmentsAttributesGetZonesResponse } from '@/__generated__/schemas/ApiCustomersZonesSegmentsAttributesGetZonesResponse';
import type { ApiCustomersZonesSegmentsIconGetZonesResponse } from '@/__generated__/schemas/ApiCustomersZonesSegmentsIconGetZonesResponse';
import type { ApiCustomersZonesSegmentsJobsGetZonesResponse } from '@/__generated__/schemas/ApiCustomersZonesSegmentsJobsGetZonesResponse';
import type { CreateSegmentRequest } from '@/__generated__/schemas/CreateSegmentRequest';
import type { CreateZoneRequest } from '@/__generated__/schemas/CreateZoneRequest';
import type { EmptyMessage } from '@/__generated__/schemas/EmptyMessage';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { ListForcedBundlesResponse } from '@/__generated__/schemas/ListForcedBundlesResponse';
import type { MembershipQueryResponse } from '@/__generated__/schemas/MembershipQueryResponse';
import type { PromoteZoneRequest } from '@/__generated__/schemas/PromoteZoneRequest';
import type { PromoteZoneResponse } from '@/__generated__/schemas/PromoteZoneResponse';
import type { RenameZoneRequest } from '@/__generated__/schemas/RenameZoneRequest';
import type { SegmentAuditInfoCursorPagedResult } from '@/__generated__/schemas/SegmentAuditInfoCursorPagedResult';
import type { SegmentCountsRequest } from '@/__generated__/schemas/SegmentCountsRequest';
import type { SegmentCountsResponse } from '@/__generated__/schemas/SegmentCountsResponse';
import type { SegmentFlowRequest } from '@/__generated__/schemas/SegmentFlowRequest';
import type { SegmentHistoryResult } from '@/__generated__/schemas/SegmentHistoryResult';
import type { SegmentJobsRequest } from '@/__generated__/schemas/SegmentJobsRequest';
import type { SegmentJobsResponse } from '@/__generated__/schemas/SegmentJobsResponse';
import type { SegmentMemberCursorPagedResult } from '@/__generated__/schemas/SegmentMemberCursorPagedResult';
import type { SegmentMembershipQueryRequest } from '@/__generated__/schemas/SegmentMembershipQueryRequest';
import type { SegmentOverridesRequest } from '@/__generated__/schemas/SegmentOverridesRequest';
import type { SegmentOverridesResponse } from '@/__generated__/schemas/SegmentOverridesResponse';
import type { SegmentPlayerHistoryRequest } from '@/__generated__/schemas/SegmentPlayerHistoryRequest';
import type { SegmentPreviewRequest } from '@/__generated__/schemas/SegmentPreviewRequest';
import type { SegmentPreviewResponse } from '@/__generated__/schemas/SegmentPreviewResponse';
import type { SegmentPropertiesRequest } from '@/__generated__/schemas/SegmentPropertiesRequest';
import type { SegmentPropertiesResponse } from '@/__generated__/schemas/SegmentPropertiesResponse';
import type { SegmentResponse } from '@/__generated__/schemas/SegmentResponse';
import type { SegmentResponseCursorPagedResult } from '@/__generated__/schemas/SegmentResponseCursorPagedResult';
import type { SegmentResultsResponse } from '@/__generated__/schemas/SegmentResultsResponse';
import type { SegmentSetsRequest } from '@/__generated__/schemas/SegmentSetsRequest';
import type { SegmentSetsResponse } from '@/__generated__/schemas/SegmentSetsResponse';
import type { SegmentState } from '@/__generated__/schemas/enums/SegmentState';
import type { SegmentTagsResponse } from '@/__generated__/schemas/SegmentTagsResponse';
import type { SetForcedBundleRequest } from '@/__generated__/schemas/SetForcedBundleRequest';
import type { SetRealmZoneRequest } from '@/__generated__/schemas/SetRealmZoneRequest';
import type { SetSegmentIconRequest } from '@/__generated__/schemas/SetSegmentIconRequest';
import type { SetSegmentStatesRequest } from '@/__generated__/schemas/SetSegmentStatesRequest';
import type { SetZoneRoleRequest } from '@/__generated__/schemas/SetZoneRoleRequest';
import type { UpdateSegmentRequest } from '@/__generated__/schemas/UpdateSegmentRequest';
import type { ZonesResponse } from '@/__generated__/schemas/ZonesResponse';
import type { ZoneStatsCatalogResponse } from '@/__generated__/schemas/ZoneStatsCatalogResponse';
import type { ZoneStatsRequest } from '@/__generated__/schemas/ZoneStatsRequest';
import type { ZoneStatsResponse } from '@/__generated__/schemas/ZoneStatsResponse';
import type { ZoneView } from '@/__generated__/schemas/ZoneView';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param zoneId - The `zoneId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoGetZonesForcedBundles(requester: HttpRequester, customerId: string, zoneId: string, gamertag?: string): Promise<HttpResponse<ListForcedBundlesResponse>> {
  let endpoint = "/api/beamo/zones/{customerId}/{zoneId}/forced-bundles".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
  // Make the API request
  return makeApiRequest<ListForcedBundlesResponse>({
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
 * @param payload - The `SetForcedBundleRequest` instance to use for the API request
 * @param bundleName - The `bundleName` parameter to include in the API request.
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param ns - The `ns` parameter to include in the API request.
 * @param zoneId - The `zoneId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoPutZonesForcedBundles(requester: HttpRequester, bundleName: string, customerId: string, ns: string, zoneId: string, payload: SetForcedBundleRequest, gamertag?: string): Promise<HttpResponse<ApiBeamoZonesForcedBundlesPutZonesResponse>> {
  let endpoint = "/api/beamo/zones/{customerId}/{zoneId}/forced-bundles/{ns}/{bundleName}".replace(bundleNamePlaceholder, endpointEncoder(bundleName)).replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(nsPlaceholder, endpointEncoder(ns)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
  // Make the API request
  return makeApiRequest<ApiBeamoZonesForcedBundlesPutZonesResponse, SetForcedBundleRequest>({
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
 * @param bundleName - The `bundleName` parameter to include in the API request.
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param ns - The `ns` parameter to include in the API request.
 * @param zoneId - The `zoneId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoDeleteZonesForcedBundles(requester: HttpRequester, bundleName: string, customerId: string, ns: string, zoneId: string, gamertag?: string): Promise<HttpResponse<ApiBeamoZonesForcedBundlesDeleteZonesResponse>> {
  let endpoint = "/api/beamo/zones/{customerId}/{zoneId}/forced-bundles/{ns}/{bundleName}".replace(bundleNamePlaceholder, endpointEncoder(bundleName)).replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(nsPlaceholder, endpointEncoder(ns)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
  // Make the API request
  return makeApiRequest<ApiBeamoZonesForcedBundlesDeleteZonesResponse>({
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
 * @param customerId - ID of the customer.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetZonesByCustomerId(requester: HttpRequester, customerId: string, gamertag?: string): Promise<HttpResponse<ZonesResponse>> {
  let endpoint = "/api/customers/{customerId}/zones".replace(customerIdPlaceholder, endpointEncoder(customerId));
  
  // Make the API request
  return makeApiRequest<ZonesResponse>({
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
 * @param payload - The `CreateZoneRequest` instance to use for the API request
 * @param customerId - ID of the customer to create the zone under.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostZonesByCustomerId(requester: HttpRequester, customerId: string, payload: CreateZoneRequest, gamertag?: string): Promise<HttpResponse<ZoneView>> {
  let endpoint = "/api/customers/{customerId}/zones".replace(customerIdPlaceholder, endpointEncoder(customerId));
  
  // Make the API request
  return makeApiRequest<ZoneView, CreateZoneRequest>({
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
 * @param customerId - ID of the customer.
 * @param zoneId - ID of the zone to retrieve.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetZones(requester: HttpRequester, customerId: string, zoneId: string, gamertag?: string): Promise<HttpResponse<ZoneView>> {
  let endpoint = "/api/customers/{customerId}/zones/{zoneId}".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
  // Make the API request
  return makeApiRequest<ZoneView>({
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
 * @param customerId - ID of the customer that owns the zone.
 * @param zoneId - ID of the zone to archive.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersDeleteZones(requester: HttpRequester, customerId: string, zoneId: string, gamertag?: string): Promise<HttpResponse<EmptyMessage>> {
  let endpoint = "/api/customers/{customerId}/zones/{zoneId}".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
  // Make the API request
  return makeApiRequest<EmptyMessage>({
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
 * @param payload - The `RenameZoneRequest` instance to use for the API request
 * @param customerId - ID of the customer that owns the zone.
 * @param zoneId - ID of the zone to rename.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPutZonesRename(requester: HttpRequester, customerId: string, zoneId: string, payload: RenameZoneRequest, gamertag?: string): Promise<HttpResponse<EmptyMessage>> {
  let endpoint = "/api/customers/{customerId}/zones/{zoneId}/rename".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
  // Make the API request
  return makeApiRequest<EmptyMessage, RenameZoneRequest>({
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
 * @param payload - The `SetZoneRoleRequest` instance to use for the API request
 * @param accountId - ID of the account to grant the role to.
 * @param customerId - ID of the customer that owns the zone.
 * @param zoneId - ID of the zone the role is scoped to.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPutZonesRoles(requester: HttpRequester, accountId: string, customerId: string, zoneId: string, payload: SetZoneRoleRequest, gamertag?: string): Promise<HttpResponse<EmptyMessage>> {
  let endpoint = "/api/customers/{customerId}/zones/{zoneId}/roles/{accountId}".replace(accountIdPlaceholder, endpointEncoder(accountId)).replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
  // Make the API request
  return makeApiRequest<EmptyMessage, SetZoneRoleRequest>({
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
 * @param accountId - ID of the account to remove the role from.
 * @param customerId - ID of the customer that owns the zone.
 * @param zoneId - ID of the zone the role is scoped to.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersDeleteZonesRoles(requester: HttpRequester, accountId: string, customerId: string, zoneId: string, gamertag?: string): Promise<HttpResponse<EmptyMessage>> {
  let endpoint = "/api/customers/{customerId}/zones/{zoneId}/roles/{accountId}".replace(accountIdPlaceholder, endpointEncoder(accountId)).replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
  // Make the API request
  return makeApiRequest<EmptyMessage>({
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
 * @param payload - The `SetRealmZoneRequest` instance to use for the API request
 * @param customerId - ID of the customer.
 * @param realmId - ID of the realm to bind.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPutRealmsZone(requester: HttpRequester, customerId: string, realmId: string, payload: SetRealmZoneRequest, gamertag?: string): Promise<HttpResponse<EmptyMessage>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/zone".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
  // Make the API request
  return makeApiRequest<EmptyMessage, SetRealmZoneRequest>({
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
 * @param payload - The `PromoteZoneRequest` instance to use for the API request
 * @param customerId - ID of the customer.
 * @param zoneId - ID of the zone to promote into.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostZonesPromotion(requester: HttpRequester, customerId: string, zoneId: string, payload: PromoteZoneRequest, gamertag?: string): Promise<HttpResponse<PromoteZoneResponse>> {
  let endpoint = "/api/customers/{customerId}/zones/{zoneId}/promotion".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
  // Make the API request
  return makeApiRequest<PromoteZoneResponse, PromoteZoneRequest>({
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
 * @param customerId - ID of the customer.
 * @param zoneId - ID of the destination zone.
 * @param sourceZoneId - ID of the source zone.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetZonesPromotion(requester: HttpRequester, customerId: string, zoneId: string, sourceZoneId?: string, gamertag?: string): Promise<HttpResponse<PromoteZoneResponse>> {
  let endpoint = "/api/customers/{customerId}/zones/{zoneId}/promotion".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
  // Make the API request
  return makeApiRequest<PromoteZoneResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      sourceZoneId
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
 * @param zoneId - The `zoneId` parameter to include in the API request.
 * @param cursor - The `cursor` parameter to include in the API request.
 * @param ids - The `ids` parameter to include in the API request.
 * @param include - The `include` parameter to include in the API request.
 * @param limit - The `limit` parameter to include in the API request.
 * @param managedBy - The `managedBy` parameter to include in the API request.
 * @param state - The `state` parameter to include in the API request.
 * @param tags - The `tags` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetZonesSegments(requester: HttpRequester, customerId: string, zoneId: string, cursor?: string, ids?: string[], include?: string, limit?: number, managedBy?: string, state?: SegmentState, tags?: string[], gamertag?: string): Promise<HttpResponse<SegmentResponseCursorPagedResult>> {
  let endpoint = "/api/customers/{customerId}/zones/{zoneId}/segments".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
  // Make the API request
  return makeApiRequest<SegmentResponseCursorPagedResult>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      cursor,
      ids,
      include,
      limit,
      managedBy,
      state,
      tags
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
 * @param payload - The `CreateSegmentRequest` instance to use for the API request
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param zoneId - The `zoneId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostZonesSegments(requester: HttpRequester, customerId: string, zoneId: string, payload: CreateSegmentRequest, gamertag?: string): Promise<HttpResponse<SegmentResponse>> {
  let endpoint = "/api/customers/{customerId}/zones/{zoneId}/segments".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
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
 * @param payload - The `UpdateSegmentRequest` instance to use for the API request
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param segmentId - The `segmentId` parameter to include in the API request.
 * @param zoneId - The `zoneId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPutZonesSegments(requester: HttpRequester, customerId: string, segmentId: string, zoneId: string, payload: UpdateSegmentRequest, gamertag?: string): Promise<HttpResponse<SegmentResponse>> {
  let endpoint = "/api/customers/{customerId}/zones/{zoneId}/segments/{segmentId}".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(segmentIdPlaceholder, endpointEncoder(segmentId)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
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
 * @param payload - The `SetSegmentStatesRequest` instance to use for the API request
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param zoneId - The `zoneId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostZonesSegmentsStates(requester: HttpRequester, customerId: string, zoneId: string, payload: SetSegmentStatesRequest, gamertag?: string): Promise<HttpResponse<SegmentResultsResponse>> {
  let endpoint = "/api/customers/{customerId}/zones/{zoneId}/segments/states".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
  // Make the API request
  return makeApiRequest<SegmentResultsResponse, SetSegmentStatesRequest>({
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
 * @param payload - The `SegmentOverridesRequest` instance to use for the API request
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param zoneId - The `zoneId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostZonesSegmentsOverrides(requester: HttpRequester, customerId: string, zoneId: string, payload: SegmentOverridesRequest, gamertag?: string): Promise<HttpResponse<SegmentOverridesResponse>> {
  let endpoint = "/api/customers/{customerId}/zones/{zoneId}/segments/overrides".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
  // Make the API request
  return makeApiRequest<SegmentOverridesResponse, SegmentOverridesRequest>({
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
 * @param payload - The `SetSegmentIconRequest` instance to use for the API request
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param segmentId - The `segmentId` parameter to include in the API request.
 * @param zoneId - The `zoneId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPutZonesSegmentsIcon(requester: HttpRequester, customerId: string, segmentId: string, zoneId: string, payload: SetSegmentIconRequest, gamertag?: string): Promise<HttpResponse<SegmentResponse>> {
  let endpoint = "/api/customers/{customerId}/zones/{zoneId}/segments/{segmentId}/icon".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(segmentIdPlaceholder, endpointEncoder(segmentId)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
  // Make the API request
  return makeApiRequest<SegmentResponse, SetSegmentIconRequest>({
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
 * @param segmentId - The `segmentId` parameter to include in the API request.
 * @param zoneId - The `zoneId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersDeleteZonesSegmentsIcon(requester: HttpRequester, customerId: string, segmentId: string, zoneId: string, gamertag?: string): Promise<HttpResponse<SegmentResponse>> {
  let endpoint = "/api/customers/{customerId}/zones/{zoneId}/segments/{segmentId}/icon".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(segmentIdPlaceholder, endpointEncoder(segmentId)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
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
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param segmentId - The `segmentId` parameter to include in the API request.
 * @param zoneId - The `zoneId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetZonesSegmentsIcon(requester: HttpRequester, customerId: string, segmentId: string, zoneId: string, gamertag?: string): Promise<HttpResponse<ApiCustomersZonesSegmentsIconGetZonesResponse>> {
  let endpoint = "/api/customers/{customerId}/zones/{zoneId}/segments/{segmentId}/icon".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(segmentIdPlaceholder, endpointEncoder(segmentId)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
  // Make the API request
  return makeApiRequest<ApiCustomersZonesSegmentsIconGetZonesResponse>({
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
 * @param zoneId - The `zoneId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetZonesSegmentsTags(requester: HttpRequester, customerId: string, zoneId: string, gamertag?: string): Promise<HttpResponse<SegmentTagsResponse>> {
  let endpoint = "/api/customers/{customerId}/zones/{zoneId}/segments/tags".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
  // Make the API request
  return makeApiRequest<SegmentTagsResponse>({
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
 * @param zoneId - The `zoneId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostZonesSegmentsRelease(requester: HttpRequester, customerId: string, segmentId: string, zoneId: string, gamertag?: string): Promise<HttpResponse<SegmentResponse>> {
  let endpoint = "/api/customers/{customerId}/zones/{zoneId}/segments/{segmentId}/release".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(segmentIdPlaceholder, endpointEncoder(segmentId)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
  // Make the API request
  return makeApiRequest<SegmentResponse>({
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
 * @param payload - The `SegmentPropertiesRequest` instance to use for the API request
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param zoneId - The `zoneId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostZonesSegmentsProperties(requester: HttpRequester, customerId: string, zoneId: string, payload: SegmentPropertiesRequest, gamertag?: string): Promise<HttpResponse<SegmentPropertiesResponse>> {
  let endpoint = "/api/customers/{customerId}/zones/{zoneId}/segments/properties".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
  // Make the API request
  return makeApiRequest<SegmentPropertiesResponse, SegmentPropertiesRequest>({
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
 * @param payload - The `SegmentSetsRequest` instance to use for the API request
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param zoneId - The `zoneId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostZonesSegmentsAggregates(requester: HttpRequester, customerId: string, zoneId: string, payload: SegmentSetsRequest, gamertag?: string): Promise<HttpResponse<SegmentSetsResponse>> {
  let endpoint = "/api/customers/{customerId}/zones/{zoneId}/segments/aggregates".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
  // Make the API request
  return makeApiRequest<SegmentSetsResponse, SegmentSetsRequest>({
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
 * @param payload - The `SegmentFlowRequest` instance to use for the API request
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param zoneId - The `zoneId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostZonesSegmentsHistoryFlow(requester: HttpRequester, customerId: string, zoneId: string, payload: SegmentFlowRequest, gamertag?: string): Promise<HttpResponse<SegmentHistoryResult>> {
  let endpoint = "/api/customers/{customerId}/zones/{zoneId}/segments/history/flow".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
  // Make the API request
  return makeApiRequest<SegmentHistoryResult, SegmentFlowRequest>({
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
 * @param payload - The `SegmentPlayerHistoryRequest` instance to use for the API request
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param zoneId - The `zoneId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostZonesSegmentsHistoryPlayers(requester: HttpRequester, customerId: string, zoneId: string, payload: SegmentPlayerHistoryRequest, gamertag?: string): Promise<HttpResponse<SegmentHistoryResult>> {
  let endpoint = "/api/customers/{customerId}/zones/{zoneId}/segments/history/players".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
  // Make the API request
  return makeApiRequest<SegmentHistoryResult, SegmentPlayerHistoryRequest>({
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
 * @param payload - The `SegmentPreviewRequest` instance to use for the API request
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param zoneId - The `zoneId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostZonesSegmentsPreview(requester: HttpRequester, customerId: string, zoneId: string, payload: SegmentPreviewRequest, gamertag?: string): Promise<HttpResponse<SegmentPreviewResponse>> {
  let endpoint = "/api/customers/{customerId}/zones/{zoneId}/segments/preview".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
  // Make the API request
  return makeApiRequest<SegmentPreviewResponse, SegmentPreviewRequest>({
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
 * @param zoneId - The `zoneId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetZonesSegmentsStats(requester: HttpRequester, customerId: string, zoneId: string, gamertag?: string): Promise<HttpResponse<ZoneStatsCatalogResponse>> {
  let endpoint = "/api/customers/{customerId}/zones/{zoneId}/segments/stats".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
  // Make the API request
  return makeApiRequest<ZoneStatsCatalogResponse>({
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
 * @param payload - The `ZoneStatsRequest` instance to use for the API request
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param zoneId - The `zoneId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostZonesSegmentsStats(requester: HttpRequester, customerId: string, zoneId: string, payload: ZoneStatsRequest, gamertag?: string): Promise<HttpResponse<ZoneStatsResponse>> {
  let endpoint = "/api/customers/{customerId}/zones/{zoneId}/segments/stats".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
  // Make the API request
  return makeApiRequest<ZoneStatsResponse, ZoneStatsRequest>({
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
 * @param payload - The `SegmentJobsRequest` instance to use for the API request
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param zoneId - The `zoneId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostZonesSegmentsJobs(requester: HttpRequester, customerId: string, zoneId: string, payload: SegmentJobsRequest, gamertag?: string): Promise<HttpResponse<SegmentJobsResponse>> {
  let endpoint = "/api/customers/{customerId}/zones/{zoneId}/segments/jobs".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
  // Make the API request
  return makeApiRequest<SegmentJobsResponse, SegmentJobsRequest>({
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
 * @param zoneId - The `zoneId` parameter to include in the API request.
 * @param limit - The `limit` parameter to include in the API request.
 * @param segmentId - The `segmentId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetZonesSegmentsJobs(requester: HttpRequester, customerId: string, zoneId: string, limit?: number, segmentId?: string, gamertag?: string): Promise<HttpResponse<ApiCustomersZonesSegmentsJobsGetZonesResponse>> {
  let endpoint = "/api/customers/{customerId}/zones/{zoneId}/segments/jobs".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
  // Make the API request
  return makeApiRequest<ApiCustomersZonesSegmentsJobsGetZonesResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      limit,
      segmentId
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
 * @param zoneId - The `zoneId` parameter to include in the API request.
 * @param custom - The `custom` parameter to include in the API request.
 * @param domain - The `domain` parameter to include in the API request.
 * @param namespace - The `namespace` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param refresh - The `refresh` parameter to include in the API request.
 * @param visibility - The `visibility` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetZonesSegmentsAttributes(requester: HttpRequester, customerId: string, zoneId: string, custom?: boolean, domain?: string, namespace?: string[], realmId?: string, refresh?: boolean, visibility?: string, gamertag?: string): Promise<HttpResponse<ApiCustomersZonesSegmentsAttributesGetZonesResponse>> {
  let endpoint = "/api/customers/{customerId}/zones/{zoneId}/segments/attributes".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
  // Make the API request
  return makeApiRequest<ApiCustomersZonesSegmentsAttributesGetZonesResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      custom,
      domain,
      namespace,
      realmId,
      refresh,
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
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param zoneId - The `zoneId` parameter to include in the API request.
 * @param AccountId - The `AccountId` parameter to include in the API request.
 * @param SegmentId - The `SegmentId` parameter to include in the API request.
 * @param cursor - The `cursor` parameter to include in the API request.
 * @param limit - The `limit` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetZonesSegmentsAuditLogs(requester: HttpRequester, customerId: string, zoneId: string, AccountId?: string, SegmentId?: string, cursor?: string, limit?: number, gamertag?: string): Promise<HttpResponse<SegmentAuditInfoCursorPagedResult>> {
  let endpoint = "/api/customers/{customerId}/zones/{zoneId}/segments/audit-logs".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
  // Make the API request
  return makeApiRequest<SegmentAuditInfoCursorPagedResult>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      AccountId,
      SegmentId,
      cursor,
      limit
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
 * @param payload - The `SegmentMembershipQueryRequest` instance to use for the API request
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param zoneId - The `zoneId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostZonesSegmentsMemberships(requester: HttpRequester, customerId: string, zoneId: string, payload: SegmentMembershipQueryRequest, gamertag?: string): Promise<HttpResponse<MembershipQueryResponse>> {
  let endpoint = "/api/customers/{customerId}/zones/{zoneId}/segments/memberships".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
  // Make the API request
  return makeApiRequest<MembershipQueryResponse, SegmentMembershipQueryRequest>({
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
 * @param payload - The `SegmentCountsRequest` instance to use for the API request
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param zoneId - The `zoneId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostZonesSegmentsCounts(requester: HttpRequester, customerId: string, zoneId: string, payload: SegmentCountsRequest, gamertag?: string): Promise<HttpResponse<SegmentCountsResponse>> {
  let endpoint = "/api/customers/{customerId}/zones/{zoneId}/segments/counts".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
  // Make the API request
  return makeApiRequest<SegmentCountsResponse, SegmentCountsRequest>({
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
 * @param segmentId - The `segmentId` parameter to include in the API request.
 * @param zoneId - The `zoneId` parameter to include in the API request.
 * @param cursor - The `cursor` parameter to include in the API request.
 * @param limit - The `limit` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetZonesSegmentsMembers(requester: HttpRequester, customerId: string, segmentId: string, zoneId: string, cursor?: string, limit?: number, realmId?: string, gamertag?: string): Promise<HttpResponse<SegmentMemberCursorPagedResult>> {
  let endpoint = "/api/customers/{customerId}/zones/{zoneId}/segments/{segmentId}/members".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(segmentIdPlaceholder, endpointEncoder(segmentId)).replace(zoneIdPlaceholder, endpointEncoder(zoneId));
  
  // Make the API request
  return makeApiRequest<SegmentMemberCursorPagedResult>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      cursor,
      limit,
      realmId
    },
    g: gamertag,
    w: true
  });
}
