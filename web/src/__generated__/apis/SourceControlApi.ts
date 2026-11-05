/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { connectionIdPlaceholder } from '@/__generated__/apis/constants';
import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { hostPlaceholder } from '@/__generated__/apis/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import type { Acknowledge } from '@/__generated__/schemas/Acknowledge';
import type { CreateScmConnectionsRequest } from '@/__generated__/schemas/CreateScmConnectionsRequest';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { ListScmConnectionsResponse } from '@/__generated__/schemas/ListScmConnectionsResponse';
import type { ScmConnection } from '@/__generated__/schemas/ScmConnection';
import type { ScmConnectionRequest } from '@/__generated__/schemas/ScmConnectionRequest';
import type { ScmHostTokenResponse } from '@/__generated__/schemas/ScmHostTokenResponse';
import type { SetScmTokenRequest } from '@/__generated__/schemas/SetScmTokenRequest';
import type { UpdateScmConnectionsRequest } from '@/__generated__/schemas/UpdateScmConnectionsRequest';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param ids - The `ids` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function scmGetConnections(requester: HttpRequester, ids?: string[], gamertag?: string): Promise<HttpResponse<ListScmConnectionsResponse>> {
  let endpoint = "/api/scm/connections";
  
  // Make the API request
  return makeApiRequest<ListScmConnectionsResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      ids
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
 * @param payload - The `ScmConnectionRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function scmPostConnections(requester: HttpRequester, payload: ScmConnectionRequest, gamertag?: string): Promise<HttpResponse<ScmConnection>> {
  let endpoint = "/api/scm/connections";
  
  // Make the API request
  return makeApiRequest<ScmConnection, ScmConnectionRequest>({
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
 * @param ids - The `ids` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function scmDeleteConnections(requester: HttpRequester, ids?: string[], gamertag?: string): Promise<HttpResponse<Acknowledge>> {
  let endpoint = "/api/scm/connections";
  
  // Make the API request
  return makeApiRequest<Acknowledge>({
    r: requester,
    e: endpoint,
    m: DELETE,
    q: {
      ids
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
 * @param connectionId - The `connectionId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function scmGetConnectionsByConnectionId(requester: HttpRequester, connectionId: string, gamertag?: string): Promise<HttpResponse<ScmConnection>> {
  let endpoint = "/api/scm/connections/{connectionId}".replace(connectionIdPlaceholder, endpointEncoder(connectionId));
  
  // Make the API request
  return makeApiRequest<ScmConnection>({
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
 * @param payload - The `ScmConnectionRequest` instance to use for the API request
 * @param connectionId - The `connectionId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function scmPutConnectionsByConnectionId(requester: HttpRequester, connectionId: string, payload: ScmConnectionRequest, gamertag?: string): Promise<HttpResponse<ScmConnection>> {
  let endpoint = "/api/scm/connections/{connectionId}".replace(connectionIdPlaceholder, endpointEncoder(connectionId));
  
  // Make the API request
  return makeApiRequest<ScmConnection, ScmConnectionRequest>({
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
 * @param connectionId - The `connectionId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function scmDeleteConnectionsByConnectionId(requester: HttpRequester, connectionId: string, gamertag?: string): Promise<HttpResponse<Acknowledge>> {
  let endpoint = "/api/scm/connections/{connectionId}".replace(connectionIdPlaceholder, endpointEncoder(connectionId));
  
  // Make the API request
  return makeApiRequest<Acknowledge>({
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
 * @param payload - The `CreateScmConnectionsRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function scmPostConnectionsBatch(requester: HttpRequester, payload: CreateScmConnectionsRequest, gamertag?: string): Promise<HttpResponse<ListScmConnectionsResponse>> {
  let endpoint = "/api/scm/connections/batch";
  
  // Make the API request
  return makeApiRequest<ListScmConnectionsResponse, CreateScmConnectionsRequest>({
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
 * @param payload - The `UpdateScmConnectionsRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function scmPutConnectionsBatch(requester: HttpRequester, payload: UpdateScmConnectionsRequest, gamertag?: string): Promise<HttpResponse<ListScmConnectionsResponse>> {
  let endpoint = "/api/scm/connections/batch";
  
  // Make the API request
  return makeApiRequest<ListScmConnectionsResponse, UpdateScmConnectionsRequest>({
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
 * @param payload - The `SetScmTokenRequest` instance to use for the API request
 * @param connectionId - The `connectionId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function scmPutConnectionsTokenByConnectionId(requester: HttpRequester, connectionId: string, payload: SetScmTokenRequest, gamertag?: string): Promise<HttpResponse<ScmConnection>> {
  let endpoint = "/api/scm/connections/{connectionId}/token".replace(connectionIdPlaceholder, endpointEncoder(connectionId));
  
  // Make the API request
  return makeApiRequest<ScmConnection, SetScmTokenRequest>({
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
 * @param connectionId - The `connectionId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function scmDeleteConnectionsTokenByConnectionId(requester: HttpRequester, connectionId: string, gamertag?: string): Promise<HttpResponse<ScmConnection>> {
  let endpoint = "/api/scm/connections/{connectionId}/token".replace(connectionIdPlaceholder, endpointEncoder(connectionId));
  
  // Make the API request
  return makeApiRequest<ScmConnection>({
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
 * @param host - The `host` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function scmGetHostsTokenByHost(requester: HttpRequester, host: string, gamertag?: string): Promise<HttpResponse<ScmHostTokenResponse>> {
  let endpoint = "/api/scm/hosts/{host}/token".replace(hostPlaceholder, endpointEncoder(host));
  
  // Make the API request
  return makeApiRequest<ScmHostTokenResponse>({
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
 * @param payload - The `SetScmTokenRequest` instance to use for the API request
 * @param host - The `host` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function scmPutHostsTokenByHost(requester: HttpRequester, host: string, payload: SetScmTokenRequest, gamertag?: string): Promise<HttpResponse<ScmHostTokenResponse>> {
  let endpoint = "/api/scm/hosts/{host}/token".replace(hostPlaceholder, endpointEncoder(host));
  
  // Make the API request
  return makeApiRequest<ScmHostTokenResponse, SetScmTokenRequest>({
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
 * @param host - The `host` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function scmDeleteHostsTokenByHost(requester: HttpRequester, host: string, gamertag?: string): Promise<HttpResponse<ScmHostTokenResponse>> {
  let endpoint = "/api/scm/hosts/{host}/token".replace(hostPlaceholder, endpointEncoder(host));
  
  // Make the API request
  return makeApiRequest<ScmHostTokenResponse>({
    r: requester,
    e: endpoint,
    m: DELETE,
    g: gamertag,
    w: true
  });
}
