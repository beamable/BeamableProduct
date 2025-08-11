/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import type { AuthResponse } from '@/__generated__/schemas/AuthResponse';
import type { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import type { GuestAuthRequest } from '@/__generated__/schemas/GuestAuthRequest';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { ListTokenResponse } from '@/__generated__/schemas/ListTokenResponse';
import type { PasswordAuthRequest } from '@/__generated__/schemas/PasswordAuthRequest';
import type { RefreshTokenAuthRequest } from '@/__generated__/schemas/RefreshTokenAuthRequest';
import type { RevokeTokenRequest } from '@/__generated__/schemas/RevokeTokenRequest';
import type { ServerTokenAuthRequest } from '@/__generated__/schemas/ServerTokenAuthRequest';
import type { ServerTokenResponse } from '@/__generated__/schemas/ServerTokenResponse';
import type { Token } from '@/__generated__/schemas/Token';
import type { TokenRequestWrapper } from '@/__generated__/schemas/TokenRequestWrapper';
import type { TokenResponse } from '@/__generated__/schemas/TokenResponse';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @deprecated
 * This API method is deprecated and may be removed in future versions.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `RefreshTokenAuthRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function authPostRefreshToken(requester: HttpRequester, payload: RefreshTokenAuthRequest, gamertag?: string): Promise<HttpResponse<AuthResponse>> {
  let endpoint = "/api/auth/refresh-token";
  
  // Make the API request
  return makeApiRequest<AuthResponse, RefreshTokenAuthRequest>({
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
 * @param payload - The `RefreshTokenAuthRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function authPostTokensRefreshToken(requester: HttpRequester, payload: RefreshTokenAuthRequest, gamertag?: string): Promise<HttpResponse<AuthResponse>> {
  let endpoint = "/api/auth/tokens/refresh-token";
  
  // Make the API request
  return makeApiRequest<AuthResponse, RefreshTokenAuthRequest>({
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
 * @param payload - The `GuestAuthRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function authPostTokensGuest(requester: HttpRequester, payload: GuestAuthRequest, gamertag?: string): Promise<HttpResponse<AuthResponse>> {
  let endpoint = "/api/auth/tokens/guest";
  
  // Make the API request
  return makeApiRequest<AuthResponse, GuestAuthRequest>({
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
 * @param payload - The `PasswordAuthRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function authPostTokensPassword(requester: HttpRequester, payload: PasswordAuthRequest, gamertag?: string): Promise<HttpResponse<AuthResponse>> {
  let endpoint = "/api/auth/tokens/password";
  
  // Make the API request
  return makeApiRequest<AuthResponse, PasswordAuthRequest>({
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
 * @param payload - The `ServerTokenAuthRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function authPostServer(requester: HttpRequester, payload: ServerTokenAuthRequest, gamertag?: string): Promise<HttpResponse<ServerTokenResponse>> {
  let endpoint = "/api/auth/server";
  
  // Make the API request
  return makeApiRequest<ServerTokenResponse, ServerTokenAuthRequest>({
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
 * @param gamerTagOrAccountId - The `gamerTagOrAccountId` parameter to include in the API request.
 * @param page - The `page` parameter to include in the API request.
 * @param pageSize - The `pageSize` parameter to include in the API request.
 * @param cid - The `cid` parameter to include in the API request.
 * @param pid - The `pid` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function authGetTokenListBasic(requester: HttpRequester, gamerTagOrAccountId: bigint | string, page: number, pageSize: number, cid?: bigint | string, pid?: string, gamertag?: string): Promise<HttpResponse<ListTokenResponse>> {
  let endpoint = "/basic/auth/token/list";
  
  // Make the API request
  return makeApiRequest<ListTokenResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      gamerTagOrAccountId,
      page,
      pageSize,
      cid,
      pid
    },
    g: gamertag,
    w: true
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param token - The `token` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function authGetTokenBasic(requester: HttpRequester, token: string, gamertag?: string): Promise<HttpResponse<Token>> {
  let endpoint = "/basic/auth/token";
  
  // Make the API request
  return makeApiRequest<Token>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      token
    },
    g: gamertag
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `TokenRequestWrapper` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function authPostTokenBasic(requester: HttpRequester, payload: TokenRequestWrapper, gamertag?: string): Promise<HttpResponse<TokenResponse>> {
  let endpoint = "/basic/auth/token";
  
  // Make the API request
  return makeApiRequest<TokenResponse, TokenRequestWrapper>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag
  });
}

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `RevokeTokenRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function authPutTokenRevokeBasic(requester: HttpRequester, payload: RevokeTokenRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/basic/auth/token/revoke";
  
  // Make the API request
  return makeApiRequest<CommonResponse, RevokeTokenRequest>({
    r: requester,
    e: endpoint,
    m: PUT,
    p: payload,
    g: gamertag,
    w: true
  });
}
