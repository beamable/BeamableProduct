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
import type { Acknowledge } from '@/__generated__/schemas/Acknowledge';
import type { CreateTriggerRequest } from '@/__generated__/schemas/CreateTriggerRequest';
import type { FireResponse } from '@/__generated__/schemas/FireResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { ListTriggersResponse } from '@/__generated__/schemas/ListTriggersResponse';
import type { TriggerView } from '@/__generated__/schemas/TriggerView';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `CreateTriggerRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function workflowPostTriggers(requester: HttpRequester, payload: CreateTriggerRequest, gamertag?: string): Promise<HttpResponse<TriggerView>> {
  let endpoint = "/api/workflow/triggers";
  
  // Make the API request
  return makeApiRequest<TriggerView, CreateTriggerRequest>({
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
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function workflowGetTriggers(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<ListTriggersResponse>> {
  let endpoint = "/api/workflow/triggers";
  
  // Make the API request
  return makeApiRequest<ListTriggersResponse>({
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
 * @param id - The `id` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function workflowDeleteTriggersById(requester: HttpRequester, id: string, gamertag?: string): Promise<HttpResponse<Acknowledge>> {
  let endpoint = "/api/workflow/triggers/{id}".replace(idPlaceholder, endpointEncoder(id));
  
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
 * @param payload - The `any` instance to use for the API request
 * @param id - The `id` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function workflowPostTriggerById(requester: HttpRequester, id: string, payload: any, gamertag?: string): Promise<HttpResponse<FireResponse>> {
  let endpoint = "/api/workflow/trigger/{id}".replace(idPlaceholder, endpointEncoder(id));
  
  // Make the API request
  return makeApiRequest<FireResponse, any>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}
