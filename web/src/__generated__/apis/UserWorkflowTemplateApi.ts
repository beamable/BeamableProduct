/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { idPlaceholder } from '@/__generated__/apis/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import type { Acknowledge } from '@/__generated__/schemas/Acknowledge';
import type { ExecuteRequest } from '@/__generated__/schemas/ExecuteRequest';
import type { ExecuteResponse } from '@/__generated__/schemas/ExecuteResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { ListTemplatesResponse } from '@/__generated__/schemas/ListTemplatesResponse';
import type { SaveTemplateRequest } from '@/__generated__/schemas/SaveTemplateRequest';
import type { TemplateView } from '@/__generated__/schemas/TemplateView';
import type { TestExecuteResponse } from '@/__generated__/schemas/TestExecuteResponse';
import type { ValidateResponse } from '@/__generated__/schemas/ValidateResponse';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `SaveTemplateRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function workflowPostTemplates(requester: HttpRequester, payload: SaveTemplateRequest, gamertag?: string): Promise<HttpResponse<TemplateView>> {
  let endpoint = "/api/workflow/templates";
  
  // Make the API request
  return makeApiRequest<TemplateView, SaveTemplateRequest>({
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
 * @param includeArchived - The `includeArchived` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function workflowGetTemplates(requester: HttpRequester, includeArchived?: boolean, gamertag?: string): Promise<HttpResponse<ListTemplatesResponse>> {
  let endpoint = "/api/workflow/templates";
  
  // Make the API request
  return makeApiRequest<ListTemplatesResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      includeArchived
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
 * @param id - The `id` parameter to include in the API request.
 * @param version - The `version` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function workflowGetTemplatesById(requester: HttpRequester, id: string, version?: number, gamertag?: string): Promise<HttpResponse<TemplateView>> {
  let endpoint = "/api/workflow/templates/{id}".replace(idPlaceholder, endpointEncoder(id));
  
  // Make the API request
  return makeApiRequest<TemplateView>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      version
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
 * @param payload - The `SaveTemplateRequest` instance to use for the API request
 * @param id - The `id` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function workflowPutTemplatesById(requester: HttpRequester, id: string, payload: SaveTemplateRequest, gamertag?: string): Promise<HttpResponse<TemplateView>> {
  let endpoint = "/api/workflow/templates/{id}".replace(idPlaceholder, endpointEncoder(id));
  
  // Make the API request
  return makeApiRequest<TemplateView, SaveTemplateRequest>({
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
 * @param id - The `id` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function workflowDeleteTemplatesById(requester: HttpRequester, id: string, gamertag?: string): Promise<HttpResponse<Acknowledge>> {
  let endpoint = "/api/workflow/templates/{id}".replace(idPlaceholder, endpointEncoder(id));
  
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
 * @param payload - The `SaveTemplateRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function workflowPostTemplatesValidate(requester: HttpRequester, payload: SaveTemplateRequest, gamertag?: string): Promise<HttpResponse<ValidateResponse>> {
  let endpoint = "/api/workflow/templates/validate";
  
  // Make the API request
  return makeApiRequest<ValidateResponse, SaveTemplateRequest>({
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
 * @param payload - The `ExecuteRequest` instance to use for the API request
 * @param id - The `id` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function workflowPostTemplatesExecuteById(requester: HttpRequester, id: string, payload: ExecuteRequest, gamertag?: string): Promise<HttpResponse<ExecuteResponse>> {
  let endpoint = "/api/workflow/templates/{id}/execute".replace(idPlaceholder, endpointEncoder(id));
  
  // Make the API request
  return makeApiRequest<ExecuteResponse, ExecuteRequest>({
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
 * @param payload - The `ExecuteRequest` instance to use for the API request
 * @param id - The `id` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function workflowPostTemplatesTestById(requester: HttpRequester, id: string, payload: ExecuteRequest, gamertag?: string): Promise<HttpResponse<TestExecuteResponse>> {
  let endpoint = "/api/workflow/templates/{id}/test".replace(idPlaceholder, endpointEncoder(id));
  
  // Make the API request
  return makeApiRequest<TestExecuteResponse, ExecuteRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}
