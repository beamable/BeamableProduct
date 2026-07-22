/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { bundleNamePlaceholder } from '@/__generated__/apis/constants';
import { checksumPlaceholder } from '@/__generated__/apis/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { nsPlaceholder } from '@/__generated__/apis/constants';
import { PATCH } from '@/constants';
import { POST } from '@/constants';
import { tagPlaceholder } from '@/__generated__/apis/constants';
import type { ApiBeamoBundlesChecksumsAclPatchBeamoBundleResponse } from '@/__generated__/schemas/ApiBeamoBundlesChecksumsAclPatchBeamoBundleResponse';
import type { ApiBeamoBundlesTagsPostBeamoBundleResponse } from '@/__generated__/schemas/ApiBeamoBundlesTagsPostBeamoBundleResponse';
import type { GetBundleResponse } from '@/__generated__/schemas/GetBundleResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { ListBundlesResponse } from '@/__generated__/schemas/ListBundlesResponse';
import type { PromoteBundleTagRequest } from '@/__generated__/schemas/PromoteBundleTagRequest';
import type { PublishBundleRequest } from '@/__generated__/schemas/PublishBundleRequest';
import type { PublishBundleResponse } from '@/__generated__/schemas/PublishBundleResponse';
import type { UpdateBundleAclRequest } from '@/__generated__/schemas/UpdateBundleAclRequest';
import type { YankBundleResponse } from '@/__generated__/schemas/YankBundleResponse';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoGetBundles(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<ListBundlesResponse>> {
  let endpoint = "/api/beamo/bundles";
  
  // Make the API request
  return makeApiRequest<ListBundlesResponse>({
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
 * @param bundleName - The `bundleName` parameter to include in the API request.
 * @param ns - The `ns` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoGetBundles(requester: HttpRequester, bundleName: string, ns: string, gamertag?: string): Promise<HttpResponse<GetBundleResponse>> {
  let endpoint = "/api/beamo/bundles/{ns}/{bundleName}".replace(bundleNamePlaceholder, endpointEncoder(bundleName)).replace(nsPlaceholder, endpointEncoder(ns));
  
  // Make the API request
  return makeApiRequest<GetBundleResponse>({
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
 * @param bundleName - The `bundleName` parameter to include in the API request.
 * @param ns - The `ns` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoGetBundlesHistory(requester: HttpRequester, bundleName: string, ns: string, gamertag?: string): Promise<HttpResponse<ListBundlesResponse>> {
  let endpoint = "/api/beamo/bundles/{ns}/{bundleName}/history".replace(bundleNamePlaceholder, endpointEncoder(bundleName)).replace(nsPlaceholder, endpointEncoder(ns));
  
  // Make the API request
  return makeApiRequest<ListBundlesResponse>({
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
 * @param bundleName - The `bundleName` parameter to include in the API request.
 * @param checksum - The `checksum` parameter to include in the API request.
 * @param ns - The `ns` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoGetBundlesChecksums(requester: HttpRequester, bundleName: string, checksum: string, ns: string, gamertag?: string): Promise<HttpResponse<GetBundleResponse>> {
  let endpoint = "/api/beamo/bundles/{ns}/{bundleName}/checksums/{checksum}".replace(bundleNamePlaceholder, endpointEncoder(bundleName)).replace(checksumPlaceholder, endpointEncoder(checksum)).replace(nsPlaceholder, endpointEncoder(ns));
  
  // Make the API request
  return makeApiRequest<GetBundleResponse>({
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
 * @param payload - The `PublishBundleRequest` instance to use for the API request
 * @param bundleName - The `bundleName` parameter to include in the API request.
 * @param ns - The `ns` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoPostBundlesPublish(requester: HttpRequester, bundleName: string, ns: string, payload: PublishBundleRequest, gamertag?: string): Promise<HttpResponse<PublishBundleResponse>> {
  let endpoint = "/api/beamo/bundles/{ns}/{bundleName}/publish".replace(bundleNamePlaceholder, endpointEncoder(bundleName)).replace(nsPlaceholder, endpointEncoder(ns));
  
  // Make the API request
  return makeApiRequest<PublishBundleResponse, PublishBundleRequest>({
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
 * @param payload - The `PromoteBundleTagRequest` instance to use for the API request
 * @param bundleName - The `bundleName` parameter to include in the API request.
 * @param ns - The `ns` parameter to include in the API request.
 * @param tag - The `tag` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoPostBundlesTags(requester: HttpRequester, bundleName: string, ns: string, tag: string, payload: PromoteBundleTagRequest, gamertag?: string): Promise<HttpResponse<ApiBeamoBundlesTagsPostBeamoBundleResponse>> {
  let endpoint = "/api/beamo/bundles/{ns}/{bundleName}/tags/{tag}".replace(bundleNamePlaceholder, endpointEncoder(bundleName)).replace(nsPlaceholder, endpointEncoder(ns)).replace(tagPlaceholder, endpointEncoder(tag));
  
  // Make the API request
  return makeApiRequest<ApiBeamoBundlesTagsPostBeamoBundleResponse, PromoteBundleTagRequest>({
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
 * @param bundleName - The `bundleName` parameter to include in the API request.
 * @param checksum - The `checksum` parameter to include in the API request.
 * @param ns - The `ns` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoPostBundlesChecksumsYank(requester: HttpRequester, bundleName: string, checksum: string, ns: string, gamertag?: string): Promise<HttpResponse<YankBundleResponse>> {
  let endpoint = "/api/beamo/bundles/{ns}/{bundleName}/checksums/{checksum}/yank".replace(bundleNamePlaceholder, endpointEncoder(bundleName)).replace(checksumPlaceholder, endpointEncoder(checksum)).replace(nsPlaceholder, endpointEncoder(ns));
  
  // Make the API request
  return makeApiRequest<YankBundleResponse>({
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
 * @param payload - The `UpdateBundleAclRequest` instance to use for the API request
 * @param bundleName - The `bundleName` parameter to include in the API request.
 * @param checksum - The `checksum` parameter to include in the API request.
 * @param ns - The `ns` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoPatchBundlesChecksumsAcl(requester: HttpRequester, bundleName: string, checksum: string, ns: string, payload: UpdateBundleAclRequest, gamertag?: string): Promise<HttpResponse<ApiBeamoBundlesChecksumsAclPatchBeamoBundleResponse>> {
  let endpoint = "/api/beamo/bundles/{ns}/{bundleName}/checksums/{checksum}/acl".replace(bundleNamePlaceholder, endpointEncoder(bundleName)).replace(checksumPlaceholder, endpointEncoder(checksum)).replace(nsPlaceholder, endpointEncoder(ns));
  
  // Make the API request
  return makeApiRequest<ApiBeamoBundlesChecksumsAclPatchBeamoBundleResponse, UpdateBundleAclRequest>({
    r: requester,
    e: endpoint,
    m: PATCH,
    p: payload,
    g: gamertag,
    w: true
  });
}
