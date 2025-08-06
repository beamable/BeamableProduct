/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { OtelAuthConfig } from '@/__generated__/schemas/OtelAuthConfig';

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
