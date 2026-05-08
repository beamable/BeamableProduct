/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { endpointEncoder } from '@/utils/endpointEncoder';
import { executionIdPlaceholder } from '@/__generated__/apis/constants';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import type { Acknowledge } from '@/__generated__/schemas/Acknowledge';
import type { CancelWorkflowRequest } from '@/__generated__/schemas/CancelWorkflowRequest';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { WorkflowStatusResponse } from '@/__generated__/schemas/WorkflowStatusResponse';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param executionId - The `executionId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function workflowGetStatusByExecutionId(requester: HttpRequester, executionId: string, gamertag?: string): Promise<HttpResponse<WorkflowStatusResponse>> {
  let endpoint = "/api/workflow/{executionId}/status".replace(executionIdPlaceholder, endpointEncoder(executionId));
  
  // Make the API request
  return makeApiRequest<WorkflowStatusResponse>({
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
 * @param payload - The `CancelWorkflowRequest` instance to use for the API request
 * @param executionId - The `executionId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function workflowPostCancelByExecutionId(requester: HttpRequester, executionId: string, payload: CancelWorkflowRequest, gamertag?: string): Promise<HttpResponse<Acknowledge>> {
  let endpoint = "/api/workflow/{executionId}/cancel".replace(executionIdPlaceholder, endpointEncoder(executionId));
  
  // Make the API request
  return makeApiRequest<Acknowledge, CancelWorkflowRequest>({
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
 * @param executionId - The `executionId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function workflowPostRestartByExecutionId(requester: HttpRequester, executionId: string, gamertag?: string): Promise<HttpResponse<Acknowledge>> {
  let endpoint = "/api/workflow/{executionId}/restart".replace(executionIdPlaceholder, endpointEncoder(executionId));
  
  // Make the API request
  return makeApiRequest<Acknowledge>({
    r: requester,
    e: endpoint,
    m: POST,
    g: gamertag,
    w: true
  });
}
