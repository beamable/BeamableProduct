/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { customerIdPlaceholder } from '@/__generated__/apis/constants';
import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import { realmIdPlaceholder } from '@/__generated__/apis/constants';
import type { Acknowledge } from '@/__generated__/schemas/Acknowledge';
import type { AlertRule } from '@/__generated__/schemas/AlertRule';
import type { AlertRuleSaveRequest } from '@/__generated__/schemas/AlertRuleSaveRequest';
import type { EvaluateResult } from '@/__generated__/schemas/EvaluateResult';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `AlertRuleSaveRequest` instance to use for the API request
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPutRealmsAdminAlertRule(requester: HttpRequester, customerId: string, realmId: string, payload: AlertRuleSaveRequest, gamertag?: string): Promise<HttpResponse<AlertRule>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/admin/alert-rule".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
  // Make the API request
  return makeApiRequest<AlertRule, AlertRuleSaveRequest>({
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
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetRealmsAdminAlertRule(requester: HttpRequester, customerId: string, realmId: string, gamertag?: string): Promise<HttpResponse<AlertRule>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/admin/alert-rule".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
  // Make the API request
  return makeApiRequest<AlertRule>({
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
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersDeleteRealmsAdminAlertRule(requester: HttpRequester, customerId: string, realmId: string, gamertag?: string): Promise<HttpResponse<Acknowledge>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/admin/alert-rule".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
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
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostRealmsAdminAlertRuleEvaluate(requester: HttpRequester, customerId: string, realmId: string, gamertag?: string): Promise<HttpResponse<EvaluateResult>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/admin/alert-rule/evaluate".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
  // Make the API request
  return makeApiRequest<EvaluateResult>({
    r: requester,
    e: endpoint,
    m: POST,
    g: gamertag,
    w: true
  });
}
