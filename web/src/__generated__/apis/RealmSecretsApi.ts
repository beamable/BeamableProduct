/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { customerIdPlaceholder } from '@/__generated__/apis/constants';
import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { keyPlaceholder } from '@/__generated__/apis/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import { realmIdPlaceholder } from '@/__generated__/apis/constants';
import type { Acknowledge } from '@/__generated__/schemas/Acknowledge';
import type { GetRealmSecretResponse } from '@/__generated__/schemas/GetRealmSecretResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { PutRealmSecretRequest } from '@/__generated__/schemas/PutRealmSecretRequest';
import type { RealmDataKeyInfoCursorPagedResult } from '@/__generated__/schemas/RealmDataKeyInfoCursorPagedResult';
import type { RealmSecretInfoCursorPagedResult } from '@/__generated__/schemas/RealmSecretInfoCursorPagedResult';
import type { RealmSecretsAuditInfoCursorPagedResult } from '@/__generated__/schemas/RealmSecretsAuditInfoCursorPagedResult';
import type { RotateRealmDataKeyResponse } from '@/__generated__/schemas/RotateRealmDataKeyResponse';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @deprecated
 * This API method is deprecated and may be removed in future versions.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param key - The `key` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetRealmsSecretsValuesByCustomerIdAndRealmIdAndKey(requester: HttpRequester, customerId: string, key: string, realmId: string, gamertag?: string): Promise<HttpResponse<GetRealmSecretResponse>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/secrets/values/{key}".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(keyPlaceholder, endpointEncoder(key)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
  // Make the API request
  return makeApiRequest<GetRealmSecretResponse>({
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
 * @deprecated
 * This API method is deprecated and may be removed in future versions.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `PutRealmSecretRequest` instance to use for the API request
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param key - The `key` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPutRealmsSecretsValues(requester: HttpRequester, customerId: string, key: string, realmId: string, payload: PutRealmSecretRequest, gamertag?: string): Promise<HttpResponse<Acknowledge>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/secrets/values/{key}".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(keyPlaceholder, endpointEncoder(key)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
  // Make the API request
  return makeApiRequest<Acknowledge, PutRealmSecretRequest>({
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
 * @deprecated
 * This API method is deprecated and may be removed in future versions.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param key - The `key` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersDeleteRealmsSecretsValues(requester: HttpRequester, customerId: string, key: string, realmId: string, gamertag?: string): Promise<HttpResponse<Acknowledge>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/secrets/values/{key}".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(keyPlaceholder, endpointEncoder(key)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
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
 * @deprecated
 * This API method is deprecated and may be removed in future versions.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param cursor - The `cursor` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetRealmsSecretsValues(requester: HttpRequester, customerId: string, realmId: string, cursor?: string, gamertag?: string): Promise<HttpResponse<RealmSecretInfoCursorPagedResult>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/secrets/values".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
  // Make the API request
  return makeApiRequest<RealmSecretInfoCursorPagedResult>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      cursor
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
 * @deprecated
 * This API method is deprecated and may be removed in future versions.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param cursor - The `cursor` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetRealmsSecretsDataKeys(requester: HttpRequester, customerId: string, realmId: string, cursor?: string, gamertag?: string): Promise<HttpResponse<RealmDataKeyInfoCursorPagedResult>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/secrets/data-keys".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
  // Make the API request
  return makeApiRequest<RealmDataKeyInfoCursorPagedResult>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      cursor
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
 * @deprecated
 * This API method is deprecated and may be removed in future versions.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostRealmsSecretsDataKeys(requester: HttpRequester, customerId: string, realmId: string, gamertag?: string): Promise<HttpResponse<RotateRealmDataKeyResponse>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/secrets/data-keys".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
  // Make the API request
  return makeApiRequest<RotateRealmDataKeyResponse>({
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
 * @deprecated
 * This API method is deprecated and may be removed in future versions.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param AccountId - The `AccountId` parameter to include in the API request.
 * @param PlayerId - The `PlayerId` parameter to include in the API request.
 * @param SecretKey - The `SecretKey` parameter to include in the API request.
 * @param cursor - The `cursor` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetRealmsSecretsAuditLogs(requester: HttpRequester, customerId: string, realmId: string, AccountId?: string, PlayerId?: string, SecretKey?: string, cursor?: string, gamertag?: string): Promise<HttpResponse<RealmSecretsAuditInfoCursorPagedResult>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/secrets/audit-logs".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
  // Make the API request
  return makeApiRequest<RealmSecretsAuditInfoCursorPagedResult>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      AccountId,
      PlayerId,
      SecretKey,
      cursor
    },
    g: gamertag,
    w: true
  });
}
