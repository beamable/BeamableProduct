/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { servicePlaceholder } from '@/__generated__/apis/constants';
import type { AuthoringServicesResponse } from '@/__generated__/schemas/AuthoringServicesResponse';
import type { AuthoringStepsResponse } from '@/__generated__/schemas/AuthoringStepsResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function workflowGetAuthoringServices(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<AuthoringServicesResponse>> {
  let endpoint = "/api/workflow/authoring/services";
  
  // Make the API request
  return makeApiRequest<AuthoringServicesResponse>({
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
 * @param service - The `service` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function workflowGetAuthoringServicesStepsByService(requester: HttpRequester, service: string, gamertag?: string): Promise<HttpResponse<AuthoringStepsResponse>> {
  let endpoint = "/api/workflow/authoring/services/{service}/steps".replace(servicePlaceholder, endpointEncoder(service));
  
  // Make the API request
  return makeApiRequest<AuthoringStepsResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    g: gamertag,
    w: true
  });
}
