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
import { zoneIdPlaceholder } from '@/__generated__/apis/constants';
import type { ApiBeamoZonesForcedBundlesDeleteZonesResponse } from '@/__generated__/schemas/ApiBeamoZonesForcedBundlesDeleteZonesResponse';
import type { ApiBeamoZonesForcedBundlesPutZonesResponse } from '@/__generated__/schemas/ApiBeamoZonesForcedBundlesPutZonesResponse';
import type { CreateZoneRequest } from '@/__generated__/schemas/CreateZoneRequest';
import type { EmptyMessage } from '@/__generated__/schemas/EmptyMessage';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { ListForcedBundlesResponse } from '@/__generated__/schemas/ListForcedBundlesResponse';
import type { PromoteZoneRequest } from '@/__generated__/schemas/PromoteZoneRequest';
import type { PromoteZoneResponse } from '@/__generated__/schemas/PromoteZoneResponse';
import type { RenameZoneRequest } from '@/__generated__/schemas/RenameZoneRequest';
import type { SetForcedBundleRequest } from '@/__generated__/schemas/SetForcedBundleRequest';
import type { SetRealmZoneRequest } from '@/__generated__/schemas/SetRealmZoneRequest';
import type { SetZoneRoleRequest } from '@/__generated__/schemas/SetZoneRoleRequest';
import type { ZonesResponse } from '@/__generated__/schemas/ZonesResponse';
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
