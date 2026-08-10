/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { keyPlaceholder } from '@/__generated__/apis/constants';
import { kindPlaceholder } from '@/__generated__/apis/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { PUT } from '@/constants';
import type { ApiDataBindingsDeleteDataBindingResponse } from '@/__generated__/schemas/ApiDataBindingsDeleteDataBindingResponse';
import type { ApiDataBindingsPutDataBindingResponse } from '@/__generated__/schemas/ApiDataBindingsPutDataBindingResponse';
import type { DataBinding } from '@/__generated__/schemas/DataBinding';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { ListDataBindingsResponse } from '@/__generated__/schemas/ListDataBindingsResponse';
import type { SetDataBindingRequest } from '@/__generated__/schemas/SetDataBindingRequest';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param kind - The `kind` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function dataBindingsGetByKind(requester: HttpRequester, kind: string, gamertag?: string): Promise<HttpResponse<ListDataBindingsResponse>> {
  let endpoint = "/api/data-bindings/{kind}".replace(kindPlaceholder, endpointEncoder(kind));
  
  // Make the API request
  return makeApiRequest<ListDataBindingsResponse>({
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
 * @param key - The `key` parameter to include in the API request.
 * @param kind - The `kind` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function dataBindingsGet(requester: HttpRequester, key: string, kind: string, gamertag?: string): Promise<HttpResponse<DataBinding>> {
  let endpoint = "/api/data-bindings/{kind}/{key}".replace(keyPlaceholder, endpointEncoder(key)).replace(kindPlaceholder, endpointEncoder(kind));
  
  // Make the API request
  return makeApiRequest<DataBinding>({
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
 * @param payload - The `SetDataBindingRequest` instance to use for the API request
 * @param key - The `key` parameter to include in the API request.
 * @param kind - The `kind` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function dataBindingsPut(requester: HttpRequester, key: string, kind: string, payload: SetDataBindingRequest, gamertag?: string): Promise<HttpResponse<ApiDataBindingsPutDataBindingResponse>> {
  let endpoint = "/api/data-bindings/{kind}/{key}".replace(keyPlaceholder, endpointEncoder(key)).replace(kindPlaceholder, endpointEncoder(kind));
  
  // Make the API request
  return makeApiRequest<ApiDataBindingsPutDataBindingResponse, SetDataBindingRequest>({
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
 * @param key - The `key` parameter to include in the API request.
 * @param kind - The `kind` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function dataBindingsDelete(requester: HttpRequester, key: string, kind: string, gamertag?: string): Promise<HttpResponse<ApiDataBindingsDeleteDataBindingResponse>> {
  let endpoint = "/api/data-bindings/{kind}/{key}".replace(keyPlaceholder, endpointEncoder(key)).replace(kindPlaceholder, endpointEncoder(kind));
  
  // Make the API request
  return makeApiRequest<ApiDataBindingsDeleteDataBindingResponse>({
    r: requester,
    e: endpoint,
    m: DELETE,
    g: gamertag,
    w: true
  });
}
