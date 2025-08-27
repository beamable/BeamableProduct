/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import { viewIdPlaceholder } from '@/__generated__/apis/constants';
import type { ApiBeamoOtelViewsDeleteBeamoOtelResponse } from '@/__generated__/schemas/ApiBeamoOtelViewsDeleteBeamoOtelResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { OtelAuthConfig } from '@/__generated__/schemas/OtelAuthConfig';
import type { OtelView } from '@/__generated__/schemas/OtelView';
import type { OtelViewsResponse } from '@/__generated__/schemas/OtelViewsResponse';
import type { UpdateOtelViewRequest } from '@/__generated__/schemas/UpdateOtelViewRequest';

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
export async function beamoGetOtelViews(requester: HttpRequester, playerId?: string, gamertag?: string): Promise<HttpResponse<OtelViewsResponse>> {
  let endpoint = "/api/beamo/otel/views";
  
  // Make the API request
  return makeApiRequest<OtelViewsResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
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
 * @param payload - The `OtelView` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoPostOtelViews(requester: HttpRequester, payload: OtelView, gamertag?: string): Promise<HttpResponse<OtelView>> {
  let endpoint = "/api/beamo/otel/views";
  
  // Make the API request
  return makeApiRequest<OtelView, OtelView>({
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
 * @param viewId - The `viewId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoDeleteOtelViewsByViewId(requester: HttpRequester, viewId: string, gamertag?: string): Promise<HttpResponse<ApiBeamoOtelViewsDeleteBeamoOtelResponse>> {
  let endpoint = "/api/beamo/otel/views/{viewId}".replace(viewIdPlaceholder, endpointEncoder(viewId));
  
  // Make the API request
  return makeApiRequest<ApiBeamoOtelViewsDeleteBeamoOtelResponse>({
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
 * @param payload - The `UpdateOtelViewRequest` instance to use for the API request
 * @param viewId - The `viewId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoPutOtelViewsByViewId(requester: HttpRequester, viewId: string, payload: UpdateOtelViewRequest, gamertag?: string): Promise<HttpResponse<OtelView>> {
  let endpoint = "/api/beamo/otel/views/{viewId}".replace(viewIdPlaceholder, endpointEncoder(viewId));
  
  // Make the API request
  return makeApiRequest<OtelView, UpdateOtelViewRequest>({
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
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoGetOtelAuthReaderConfig(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<OtelAuthConfig>> {
  let endpoint = "/api/beamo/otel/auth/reader/config";
  
  // Make the API request
  return makeApiRequest<OtelAuthConfig>({
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
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoGetOtelAuthWriterConfig(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<OtelAuthConfig>> {
  let endpoint = "/api/beamo/otel/auth/writer/config";
  
  // Make the API request
  return makeApiRequest<OtelAuthConfig>({
    r: requester,
    e: endpoint,
    m: GET,
    g: gamertag,
    w: true
  });
}
