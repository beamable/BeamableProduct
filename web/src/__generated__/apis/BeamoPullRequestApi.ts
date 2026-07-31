/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import { pullRequestIdPlaceholder } from '@/__generated__/apis/constants';
import type { BeamoPullRequestActorManifestChecksum } from '@/__generated__/schemas/BeamoPullRequestActorManifestChecksum';
import type { CommentPullRequestRequest } from '@/__generated__/schemas/CommentPullRequestRequest';
import type { EmptyMessage } from '@/__generated__/schemas/EmptyMessage';
import type { GetPullRequestResponse } from '@/__generated__/schemas/GetPullRequestResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { ListPullRequestsResponse } from '@/__generated__/schemas/ListPullRequestsResponse';
import type { PullRequestStatus } from '@/__generated__/schemas/enums/PullRequestStatus';
import type { SubmitPullRequestRequest } from '@/__generated__/schemas/SubmitPullRequestRequest';
import type { SubmitPullRequestResponse } from '@/__generated__/schemas/SubmitPullRequestResponse';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `SubmitPullRequestRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoPostPrs(requester: HttpRequester, payload: SubmitPullRequestRequest, gamertag?: string): Promise<HttpResponse<SubmitPullRequestResponse>> {
  let endpoint = "/api/beamo/prs";
  
  // Make the API request
  return makeApiRequest<SubmitPullRequestResponse, SubmitPullRequestRequest>({
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
 * @param limit - The `limit` parameter to include in the API request.
 * @param offset - The `offset` parameter to include in the API request.
 * @param status - The `status` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoGetPrs(requester: HttpRequester, limit?: number, offset?: number, status?: PullRequestStatus, gamertag?: string): Promise<HttpResponse<ListPullRequestsResponse>> {
  let endpoint = "/api/beamo/prs";
  
  // Make the API request
  return makeApiRequest<ListPullRequestsResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      limit,
      offset,
      status
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
 * @param pullRequestId - The `pullRequestId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoGetPrsByPullRequestId(requester: HttpRequester, pullRequestId: string, gamertag?: string): Promise<HttpResponse<GetPullRequestResponse>> {
  let endpoint = "/api/beamo/prs/{pullRequestId}".replace(pullRequestIdPlaceholder, endpointEncoder(pullRequestId));
  
  // Make the API request
  return makeApiRequest<GetPullRequestResponse>({
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
 * @param pullRequestId - The `pullRequestId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoPostPrsApproveByPullRequestId(requester: HttpRequester, pullRequestId: string, gamertag?: string): Promise<HttpResponse<BeamoPullRequestActorManifestChecksum>> {
  let endpoint = "/api/beamo/prs/{pullRequestId}/approve".replace(pullRequestIdPlaceholder, endpointEncoder(pullRequestId));
  
  // Make the API request
  return makeApiRequest<BeamoPullRequestActorManifestChecksum>({
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
 * @param pullRequestId - The `pullRequestId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoPostPrsRejectByPullRequestId(requester: HttpRequester, pullRequestId: string, gamertag?: string): Promise<HttpResponse<EmptyMessage>> {
  let endpoint = "/api/beamo/prs/{pullRequestId}/reject".replace(pullRequestIdPlaceholder, endpointEncoder(pullRequestId));
  
  // Make the API request
  return makeApiRequest<EmptyMessage>({
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
 * @param payload - The `CommentPullRequestRequest` instance to use for the API request
 * @param pullRequestId - The `pullRequestId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoPostPrsCommentsByPullRequestId(requester: HttpRequester, pullRequestId: string, payload: CommentPullRequestRequest, gamertag?: string): Promise<HttpResponse<EmptyMessage>> {
  let endpoint = "/api/beamo/prs/{pullRequestId}/comments".replace(pullRequestIdPlaceholder, endpointEncoder(pullRequestId));
  
  // Make the API request
  return makeApiRequest<EmptyMessage, CommentPullRequestRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}
