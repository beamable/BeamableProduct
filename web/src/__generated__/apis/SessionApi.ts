/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { OnlineStatusResponses } from '@/__generated__/schemas/OnlineStatusResponses';
import type { SessionClientHistoryResponse } from '@/__generated__/schemas/SessionClientHistoryResponse';
import type { SessionHeartbeat } from '@/__generated__/schemas/SessionHeartbeat';
import type { SessionHistoryResponse } from '@/__generated__/schemas/SessionHistoryResponse';
import type { StartSessionRequest } from '@/__generated__/schemas/StartSessionRequest';
import type { StartSessionResponse } from '@/__generated__/schemas/StartSessionResponse';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function sessionPostHeartbeatBasic(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<SessionHeartbeat>> {
  let endpoint = "/basic/session/heartbeat";
  
  // Make the API request
  return makeApiRequest<SessionHeartbeat>({
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
 * @param dbid - The `dbid` parameter to include in the API request.
 * @param month - The `month` parameter to include in the API request.
 * @param year - The `year` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function sessionGetHistoryBasic(requester: HttpRequester, dbid: bigint | string, month?: number, year?: number, gamertag?: string): Promise<HttpResponse<SessionHistoryResponse>> {
  let endpoint = "/basic/session/history";
  
  // Make the API request
  return makeApiRequest<SessionHistoryResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      dbid,
      month,
      year
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
 * @param intervalSecs - The `intervalSecs` parameter to include in the API request.
 * @param playerIds - The `playerIds` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function sessionGetStatusBasic(requester: HttpRequester, intervalSecs: bigint | string, playerIds: string, gamertag?: string): Promise<HttpResponse<OnlineStatusResponses>> {
  let endpoint = "/basic/session/status";
  
  // Make the API request
  return makeApiRequest<OnlineStatusResponses>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      intervalSecs,
      playerIds
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
 * @param month - The `month` parameter to include in the API request.
 * @param year - The `year` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function sessionGetClientHistoryBasic(requester: HttpRequester, month?: number, year?: number, gamertag?: string): Promise<HttpResponse<SessionClientHistoryResponse>> {
  let endpoint = "/basic/session/client/history";
  
  // Make the API request
  return makeApiRequest<SessionClientHistoryResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      month,
      year
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
 * @param payload - The `StartSessionRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function sessionPostBasic(requester: HttpRequester, payload: StartSessionRequest, gamertag?: string): Promise<HttpResponse<StartSessionResponse>> {
  let endpoint = "/basic/session/";
  
  // Make the API request
  return makeApiRequest<StartSessionResponse, StartSessionRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}
