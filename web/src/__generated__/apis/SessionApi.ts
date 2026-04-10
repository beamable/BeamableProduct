/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import type { ApiSessionsHeartbeatPostSessionResponse } from '@/__generated__/schemas/ApiSessionsHeartbeatPostSessionResponse';
import type { ApiSessionsPostSessionResponse } from '@/__generated__/schemas/ApiSessionsPostSessionResponse';
import type { ApiSessionsStatusGetSessionResponse } from '@/__generated__/schemas/ApiSessionsStatusGetSessionResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { OnlineStatusResponses } from '@/__generated__/schemas/OnlineStatusResponses';
import type { SessionActorStartSessionRequest } from '@/__generated__/schemas/SessionActorStartSessionRequest';
import type { SessionBasicSessionClientHistoryResponse } from '@/__generated__/schemas/SessionBasicSessionClientHistoryResponse';
import type { SessionBasicSessionHistoryResponse } from '@/__generated__/schemas/SessionBasicSessionHistoryResponse';
import type { SessionBasicStartSessionRequest } from '@/__generated__/schemas/SessionBasicStartSessionRequest';
import type { SessionHeartbeat } from '@/__generated__/schemas/SessionHeartbeat';
import type { StartSessionResponse } from '@/__generated__/schemas/StartSessionResponse';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @deprecated
 * This API method is deprecated and may be removed in future versions.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `SessionActorStartSessionRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * @param gamertag - Set the request timeout in seconds. Defaults to 10 seconds.
 * 
 */
export async function sessionsPost(requester: HttpRequester, payload: SessionActorStartSessionRequest, gamertag?: string, timeout?: string): Promise<HttpResponse<ApiSessionsPostSessionResponse>> {
  let endpoint = "/api/sessions";
  
  // Make the API request
  return makeApiRequest<ApiSessionsPostSessionResponse, SessionActorStartSessionRequest>({
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
 * @deprecated
 * This API method is deprecated and may be removed in future versions.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * @param gamertag - Set the request timeout in seconds. Defaults to 10 seconds.
 * 
 */
export async function sessionsPostHeartbeat(requester: HttpRequester, gamertag?: string, timeout?: string): Promise<HttpResponse<ApiSessionsHeartbeatPostSessionResponse>> {
  let endpoint = "/api/sessions/heartbeat";
  
  // Make the API request
  return makeApiRequest<ApiSessionsHeartbeatPostSessionResponse>({
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
 * @param intervalSecs - The `intervalSecs` parameter to include in the API request.
 * @param playerIds - The `playerIds` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * @param gamertag - Set the request timeout in seconds. Defaults to 10 seconds.
 * 
 */
export async function sessionsGetStatus(requester: HttpRequester, intervalSecs?: bigint | string, playerIds?: string, gamertag?: string, timeout?: string): Promise<HttpResponse<ApiSessionsStatusGetSessionResponse>> {
  let endpoint = "/api/sessions/status";
  
  // Make the API request
  return makeApiRequest<ApiSessionsStatusGetSessionResponse>({
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
    g: gamertag
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param dbid - The `dbid` parameter to include in the API request.
 * @param month - The `month` parameter to include in the API request.
 * @param year - The `year` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function sessionGetHistoryBasic(requester: HttpRequester, dbid: bigint | string, month?: number, year?: number, gamertag?: string): Promise<HttpResponse<SessionBasicSessionHistoryResponse>> {
  let endpoint = "/basic/session/history";
  
  // Make the API request
  return makeApiRequest<SessionBasicSessionHistoryResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      dbid,
      month,
      year
    },
    g: gamertag
  });
}

/**
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
    g: gamertag
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param month - The `month` parameter to include in the API request.
 * @param year - The `year` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function sessionGetClientHistoryBasic(requester: HttpRequester, month?: number, year?: number, gamertag?: string): Promise<HttpResponse<SessionBasicSessionClientHistoryResponse>> {
  let endpoint = "/basic/session/client/history";
  
  // Make the API request
  return makeApiRequest<SessionBasicSessionClientHistoryResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      month,
      year
    },
    g: gamertag
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `SessionBasicStartSessionRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function sessionPostBasic(requester: HttpRequester, payload: SessionBasicStartSessionRequest, gamertag?: string): Promise<HttpResponse<StartSessionResponse>> {
  let endpoint = "/basic/session/";
  
  // Make the API request
  return makeApiRequest<StartSessionResponse, SessionBasicStartSessionRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag
  });
}
