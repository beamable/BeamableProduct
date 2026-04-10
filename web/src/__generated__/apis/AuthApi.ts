/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import { tokenIdPlaceholder } from '@/__generated__/apis/constants';
import type { AuthV2AuthCode } from '@/__generated__/schemas/AuthV2AuthCode';
import type { AuthV2AuthCodeRequest } from '@/__generated__/schemas/AuthV2AuthCodeRequest';
import type { AuthV2AuthorizationCodeAuthRequest } from '@/__generated__/schemas/AuthV2AuthorizationCodeAuthRequest';
import type { AuthV2AuthResponse } from '@/__generated__/schemas/AuthV2AuthResponse';
import type { AuthV2DeviceIdAuthRequest } from '@/__generated__/schemas/AuthV2DeviceIdAuthRequest';
import type { AuthV2EmptyMessage } from '@/__generated__/schemas/AuthV2EmptyMessage';
import type { AuthV2ExternalAuthRequest } from '@/__generated__/schemas/AuthV2ExternalAuthRequest';
import type { AuthV2ExternalAuthResponse } from '@/__generated__/schemas/AuthV2ExternalAuthResponse';
import type { AuthV2GuestAuthRequest } from '@/__generated__/schemas/AuthV2GuestAuthRequest';
import type { AuthV2JsonWebKeySet } from '@/__generated__/schemas/AuthV2JsonWebKeySet';
import type { AuthV2LegacyAccessToken } from '@/__generated__/schemas/AuthV2LegacyAccessToken';
import type { AuthV2ListTokensResponse } from '@/__generated__/schemas/AuthV2ListTokensResponse';
import type { AuthV2OpenIdConfigResponse } from '@/__generated__/schemas/AuthV2OpenIdConfigResponse';
import type { AuthV2PasswordAuthRequest } from '@/__generated__/schemas/AuthV2PasswordAuthRequest';
import type { AuthV2RefreshToken } from '@/__generated__/schemas/AuthV2RefreshToken';
import type { AuthV2RefreshTokenAuthRequest } from '@/__generated__/schemas/AuthV2RefreshTokenAuthRequest';
import type { AuthV2RevokeRefreshTokensRequest } from '@/__generated__/schemas/AuthV2RevokeRefreshTokensRequest';
import type { AuthV2ServerTokenAuthRequest } from '@/__generated__/schemas/AuthV2ServerTokenAuthRequest';
import type { AuthV2ServerTokenResponse } from '@/__generated__/schemas/AuthV2ServerTokenResponse';
import type { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { ListTokenResponse } from '@/__generated__/schemas/ListTokenResponse';
import type { RevokeTokenRequest } from '@/__generated__/schemas/RevokeTokenRequest';
import type { Token } from '@/__generated__/schemas/Token';
import type { TokenRequestWrapper } from '@/__generated__/schemas/TokenRequestWrapper';
import type { TokenResponse } from '@/__generated__/schemas/TokenResponse';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function wellKnownGetOpenidConfiguration(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<AuthV2OpenIdConfigResponse>> {
  let endpoint = "/api/.well-known/openid-configuration";
  
  // Make the API request
  return makeApiRequest<AuthV2OpenIdConfigResponse>({
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
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function authGetKeys(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<AuthV2JsonWebKeySet>> {
  let endpoint = "/api/auth/keys";
  
  // Make the API request
  return makeApiRequest<AuthV2JsonWebKeySet>({
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
 * @param payload - The `AuthV2RefreshTokenAuthRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function authPostRefreshToken(requester: HttpRequester, payload: AuthV2RefreshTokenAuthRequest, gamertag?: string): Promise<HttpResponse<AuthV2AuthResponse>> {
  let endpoint = "/api/auth/refresh-token";
  
  // Make the API request
  return makeApiRequest<AuthV2AuthResponse, AuthV2RefreshTokenAuthRequest>({
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
 * @param payload - The `AuthV2RefreshTokenAuthRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function authPostTokensRefreshToken(requester: HttpRequester, payload: AuthV2RefreshTokenAuthRequest, gamertag?: string): Promise<HttpResponse<AuthV2AuthResponse>> {
  let endpoint = "/api/auth/tokens/refresh-token";
  
  // Make the API request
  return makeApiRequest<AuthV2AuthResponse, AuthV2RefreshTokenAuthRequest>({
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
 * @param payload - The `AuthV2GuestAuthRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function authPostTokensGuest(requester: HttpRequester, payload: AuthV2GuestAuthRequest, gamertag?: string): Promise<HttpResponse<AuthV2AuthResponse>> {
  let endpoint = "/api/auth/tokens/guest";
  
  // Make the API request
  return makeApiRequest<AuthV2AuthResponse, AuthV2GuestAuthRequest>({
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
 * @param payload - The `AuthV2PasswordAuthRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function authPostTokensPassword(requester: HttpRequester, payload: AuthV2PasswordAuthRequest, gamertag?: string): Promise<HttpResponse<AuthV2AuthResponse>> {
  let endpoint = "/api/auth/tokens/password";
  
  // Make the API request
  return makeApiRequest<AuthV2AuthResponse, AuthV2PasswordAuthRequest>({
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
 * @param payload - The `AuthV2ExternalAuthRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function authPostTokensExternal(requester: HttpRequester, payload: AuthV2ExternalAuthRequest, gamertag?: string): Promise<HttpResponse<AuthV2ExternalAuthResponse>> {
  let endpoint = "/api/auth/tokens/external";
  
  // Make the API request
  return makeApiRequest<AuthV2ExternalAuthResponse, AuthV2ExternalAuthRequest>({
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
 * @param payload - The `AuthV2DeviceIdAuthRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function authPostTokensDeviceId(requester: HttpRequester, payload: AuthV2DeviceIdAuthRequest, gamertag?: string): Promise<HttpResponse<AuthV2AuthResponse>> {
  let endpoint = "/api/auth/tokens/device-id";
  
  // Make the API request
  return makeApiRequest<AuthV2AuthResponse, AuthV2DeviceIdAuthRequest>({
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
 * @param limit - Max number of items to return
 * @param playerIdOrAccountId - The gamer tag or account ID to list tokens for
 * @param skip - Skips N items
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function authGetTokens(requester: HttpRequester, limit?: number, playerIdOrAccountId?: string, skip?: number, gamertag?: string): Promise<HttpResponse<AuthV2ListTokensResponse>> {
  let endpoint = "/api/auth/tokens";
  
  // Make the API request
  return makeApiRequest<AuthV2ListTokensResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      limit,
      playerIdOrAccountId,
      skip
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
 * @param payload - The `AuthV2RevokeRefreshTokensRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function authDeleteTokens(requester: HttpRequester, payload: AuthV2RevokeRefreshTokensRequest, gamertag?: string): Promise<HttpResponse<AuthV2EmptyMessage>> {
  let endpoint = "/api/auth/tokens";
  
  // Make the API request
  return makeApiRequest<AuthV2EmptyMessage, AuthV2RevokeRefreshTokensRequest>({
    r: requester,
    e: endpoint,
    m: DELETE,
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
 * @param tokenId - The refresh token id to look up
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function authGetTokensByTokenId(requester: HttpRequester, tokenId: string, gamertag?: string): Promise<HttpResponse<AuthV2RefreshToken>> {
  let endpoint = "/api/auth/tokens/{tokenId}".replace(tokenIdPlaceholder, endpointEncoder(tokenId));
  
  // Make the API request
  return makeApiRequest<AuthV2RefreshToken>({
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
 * @param tokenId - The access token to validate
 * @param customerId - Customer ID
 * @param realmId - Realm ID
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function authGetTokensAccessByTokenId(requester: HttpRequester, tokenId: string, customerId?: string, realmId?: string, gamertag?: string): Promise<HttpResponse<AuthV2LegacyAccessToken>> {
  let endpoint = "/api/auth/tokens/access/{tokenId}".replace(tokenIdPlaceholder, endpointEncoder(tokenId));
  
  // Make the API request
  return makeApiRequest<AuthV2LegacyAccessToken>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      customerId,
      realmId
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
 * @param payload - The `AuthV2AuthorizationCodeAuthRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function authPostTokensAuthCode(requester: HttpRequester, payload: AuthV2AuthorizationCodeAuthRequest, gamertag?: string): Promise<HttpResponse<AuthV2AuthResponse>> {
  let endpoint = "/api/auth/tokens/auth-code";
  
  // Make the API request
  return makeApiRequest<AuthV2AuthResponse, AuthV2AuthorizationCodeAuthRequest>({
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
 * @param payload - The `AuthV2AuthCodeRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function authPostAuthCodes(requester: HttpRequester, payload: AuthV2AuthCodeRequest, gamertag?: string): Promise<HttpResponse<AuthV2AuthCode>> {
  let endpoint = "/api/auth/auth-codes";
  
  // Make the API request
  return makeApiRequest<AuthV2AuthCode, AuthV2AuthCodeRequest>({
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
 * @param payload - The `AuthV2ServerTokenAuthRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function authPostServer(requester: HttpRequester, payload: AuthV2ServerTokenAuthRequest, gamertag?: string): Promise<HttpResponse<AuthV2ServerTokenResponse>> {
  let endpoint = "/api/auth/server";
  
  // Make the API request
  return makeApiRequest<AuthV2ServerTokenResponse, AuthV2ServerTokenAuthRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}

/**
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
    g: gamertag
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
    g: gamertag
  });
}
