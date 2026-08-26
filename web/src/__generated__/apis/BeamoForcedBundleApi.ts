/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { bundleNamePlaceholder } from '@/__generated__/apis/constants';
import { customerIdPlaceholder } from '@/__generated__/apis/constants';
import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { nsPlaceholder } from '@/__generated__/apis/constants';
import { PUT } from '@/constants';
import { realmIdPlaceholder } from '@/__generated__/apis/constants';
import type { ApiBeamoCidsForcedBundlesDeleteBeamoForcedBundleResponse } from '@/__generated__/schemas/ApiBeamoCidsForcedBundlesDeleteBeamoForcedBundleResponse';
import type { ApiBeamoCidsForcedBundlesPutBeamoForcedBundleResponse } from '@/__generated__/schemas/ApiBeamoCidsForcedBundlesPutBeamoForcedBundleResponse';
import type { ApiBeamoRealmsForcedBundlesDeleteBeamoForcedBundleResponse } from '@/__generated__/schemas/ApiBeamoRealmsForcedBundlesDeleteBeamoForcedBundleResponse';
import type { ApiBeamoRealmsForcedBundlesPutBeamoForcedBundleResponse } from '@/__generated__/schemas/ApiBeamoRealmsForcedBundlesPutBeamoForcedBundleResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { ListForcedBundlesResponse } from '@/__generated__/schemas/ListForcedBundlesResponse';
import type { SetForcedBundleRequest } from '@/__generated__/schemas/SetForcedBundleRequest';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoGetCidsForcedBundlesByCustomerId(requester: HttpRequester, customerId: string, gamertag?: string): Promise<HttpResponse<ListForcedBundlesResponse>> {
  let endpoint = "/api/beamo/cids/{customerId}/forced-bundles".replace(customerIdPlaceholder, endpointEncoder(customerId));
  
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
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoPutCidsForcedBundles(requester: HttpRequester, bundleName: string, customerId: string, ns: string, payload: SetForcedBundleRequest, gamertag?: string): Promise<HttpResponse<ApiBeamoCidsForcedBundlesPutBeamoForcedBundleResponse>> {
  let endpoint = "/api/beamo/cids/{customerId}/forced-bundles/{ns}/{bundleName}".replace(bundleNamePlaceholder, endpointEncoder(bundleName)).replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(nsPlaceholder, endpointEncoder(ns));
  
  // Make the API request
  return makeApiRequest<ApiBeamoCidsForcedBundlesPutBeamoForcedBundleResponse, SetForcedBundleRequest>({
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
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoDeleteCidsForcedBundles(requester: HttpRequester, bundleName: string, customerId: string, ns: string, gamertag?: string): Promise<HttpResponse<ApiBeamoCidsForcedBundlesDeleteBeamoForcedBundleResponse>> {
  let endpoint = "/api/beamo/cids/{customerId}/forced-bundles/{ns}/{bundleName}".replace(bundleNamePlaceholder, endpointEncoder(bundleName)).replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(nsPlaceholder, endpointEncoder(ns));
  
  // Make the API request
  return makeApiRequest<ApiBeamoCidsForcedBundlesDeleteBeamoForcedBundleResponse>({
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
 * @param payload - The `SetForcedBundleRequest` instance to use for the API request
 * @param bundleName - The `bundleName` parameter to include in the API request.
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param ns - The `ns` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoPutRealmsForcedBundles(requester: HttpRequester, bundleName: string, customerId: string, ns: string, realmId: string, payload: SetForcedBundleRequest, gamertag?: string): Promise<HttpResponse<ApiBeamoRealmsForcedBundlesPutBeamoForcedBundleResponse>> {
  let endpoint = "/api/beamo/realms/{customerId}/{realmId}/forced-bundles/{ns}/{bundleName}".replace(bundleNamePlaceholder, endpointEncoder(bundleName)).replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(nsPlaceholder, endpointEncoder(ns)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
  // Make the API request
  return makeApiRequest<ApiBeamoRealmsForcedBundlesPutBeamoForcedBundleResponse, SetForcedBundleRequest>({
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
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoDeleteRealmsForcedBundles(requester: HttpRequester, bundleName: string, customerId: string, ns: string, realmId: string, gamertag?: string): Promise<HttpResponse<ApiBeamoRealmsForcedBundlesDeleteBeamoForcedBundleResponse>> {
  let endpoint = "/api/beamo/realms/{customerId}/{realmId}/forced-bundles/{ns}/{bundleName}".replace(bundleNamePlaceholder, endpointEncoder(bundleName)).replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(nsPlaceholder, endpointEncoder(ns)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
  // Make the API request
  return makeApiRequest<ApiBeamoRealmsForcedBundlesDeleteBeamoForcedBundleResponse>({
    r: requester,
    e: endpoint,
    m: DELETE,
    g: gamertag,
    w: true
  });
}
