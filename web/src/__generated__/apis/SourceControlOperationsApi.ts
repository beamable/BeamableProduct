/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { connectionIdPlaceholder } from '@/__generated__/apis/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { hostPlaceholder } from '@/__generated__/apis/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import type { ApiScmConnectionsArchivePostSourceControlOperationsResponse } from '@/__generated__/schemas/ApiScmConnectionsArchivePostSourceControlOperationsResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { InspectScmRepositoryRequest } from '@/__generated__/schemas/InspectScmRepositoryRequest';
import type { ListScmRepositoriesResponse } from '@/__generated__/schemas/ListScmRepositoriesResponse';
import type { ScmArchiveRequest } from '@/__generated__/schemas/ScmArchiveRequest';
import type { ScmPublishRequest } from '@/__generated__/schemas/ScmPublishRequest';
import type { ScmPublishResponse } from '@/__generated__/schemas/ScmPublishResponse';
import type { ScmRepositoryInfo } from '@/__generated__/schemas/ScmRepositoryInfo';
import type { ScmSnapshotRequest } from '@/__generated__/schemas/ScmSnapshotRequest';
import type { ScmSnapshotResponse } from '@/__generated__/schemas/ScmSnapshotResponse';
import type { TestScmConnectionResponse } from '@/__generated__/schemas/TestScmConnectionResponse';

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
export async function scmGetHostsRepositoriesByHost(requester: HttpRequester, host: string, gamertag?: string): Promise<HttpResponse<ListScmRepositoriesResponse>> {
  let endpoint = "/api/scm/hosts/{host}/repositories".replace(hostPlaceholder, endpointEncoder(host));
  
  // Make the API request
  return makeApiRequest<ListScmRepositoriesResponse>({
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
 * @param payload - The `InspectScmRepositoryRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function scmPostRepositoriesInspect(requester: HttpRequester, payload: InspectScmRepositoryRequest, gamertag?: string): Promise<HttpResponse<ScmRepositoryInfo>> {
  let endpoint = "/api/scm/repositories/inspect";
  
  // Make the API request
  return makeApiRequest<ScmRepositoryInfo, InspectScmRepositoryRequest>({
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
 * @param connectionId - The `connectionId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function scmPostConnectionsTestByConnectionId(requester: HttpRequester, connectionId: string, gamertag?: string): Promise<HttpResponse<TestScmConnectionResponse>> {
  let endpoint = "/api/scm/connections/{connectionId}/test".replace(connectionIdPlaceholder, endpointEncoder(connectionId));
  
  // Make the API request
  return makeApiRequest<TestScmConnectionResponse>({
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
 * @param payload - The `ScmSnapshotRequest` instance to use for the API request
 * @param connectionId - The `connectionId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function scmPostConnectionsSnapshotByConnectionId(requester: HttpRequester, connectionId: string, payload: ScmSnapshotRequest, gamertag?: string): Promise<HttpResponse<ScmSnapshotResponse>> {
  let endpoint = "/api/scm/connections/{connectionId}/snapshot".replace(connectionIdPlaceholder, endpointEncoder(connectionId));
  
  // Make the API request
  return makeApiRequest<ScmSnapshotResponse, ScmSnapshotRequest>({
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
 * @param payload - The `ScmArchiveRequest` instance to use for the API request
 * @param connectionId - The `connectionId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function scmPostConnectionsArchiveByConnectionId(requester: HttpRequester, connectionId: string, payload: ScmArchiveRequest, gamertag?: string): Promise<HttpResponse<ApiScmConnectionsArchivePostSourceControlOperationsResponse>> {
  let endpoint = "/api/scm/connections/{connectionId}/archive".replace(connectionIdPlaceholder, endpointEncoder(connectionId));
  
  // Make the API request
  return makeApiRequest<ApiScmConnectionsArchivePostSourceControlOperationsResponse, ScmArchiveRequest>({
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
 * @param payload - The `ScmPublishRequest` instance to use for the API request
 * @param connectionId - The `connectionId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function scmPostConnectionsPublishByConnectionId(requester: HttpRequester, connectionId: string, payload: ScmPublishRequest, gamertag?: string): Promise<HttpResponse<ScmPublishResponse>> {
  let endpoint = "/api/scm/connections/{connectionId}/publish".replace(connectionIdPlaceholder, endpointEncoder(connectionId));
  
  // Make the API request
  return makeApiRequest<ScmPublishResponse, ScmPublishRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}
