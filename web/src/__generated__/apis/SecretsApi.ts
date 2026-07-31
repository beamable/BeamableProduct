/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { keyPlaceholder } from '@/__generated__/apis/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import { PUT } from '@/constants';
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param key - The `key` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function secretsGetValuesByKey(requester: HttpRequester, key: string, gamertag?: string): Promise<HttpResponse<GetRealmSecretResponse>> {
  let endpoint = "/api/secrets/values/{key}".replace(keyPlaceholder, endpointEncoder(key));
  
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `PutRealmSecretRequest` instance to use for the API request
 * @param key - The `key` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function secretsPutValuesByKey(requester: HttpRequester, key: string, payload: PutRealmSecretRequest, gamertag?: string): Promise<HttpResponse<Acknowledge>> {
  let endpoint = "/api/secrets/values/{key}".replace(keyPlaceholder, endpointEncoder(key));
  
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param key - The `key` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function secretsDeleteValuesByKey(requester: HttpRequester, key: string, gamertag?: string): Promise<HttpResponse<Acknowledge>> {
  let endpoint = "/api/secrets/values/{key}".replace(keyPlaceholder, endpointEncoder(key));
  
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
 * @param cursor - The `cursor` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function secretsGetValues(requester: HttpRequester, cursor?: string, gamertag?: string): Promise<HttpResponse<RealmSecretInfoCursorPagedResult>> {
  let endpoint = "/api/secrets/values";
  
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param cursor - The `cursor` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function secretsGetDataKeys(requester: HttpRequester, cursor?: string, gamertag?: string): Promise<HttpResponse<RealmDataKeyInfoCursorPagedResult>> {
  let endpoint = "/api/secrets/data-keys";
  
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function secretsPostDataKeys(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<RotateRealmDataKeyResponse>> {
  let endpoint = "/api/secrets/data-keys";
  
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param AccountId - The `AccountId` parameter to include in the API request.
 * @param PlayerId - The `PlayerId` parameter to include in the API request.
 * @param SecretKey - The `SecretKey` parameter to include in the API request.
 * @param cursor - The `cursor` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function secretsGetAuditLogs(requester: HttpRequester, AccountId?: string, PlayerId?: string, SecretKey?: string, cursor?: string, gamertag?: string): Promise<HttpResponse<RealmSecretsAuditInfoCursorPagedResult>> {
  let endpoint = "/api/secrets/audit-logs";
  
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
