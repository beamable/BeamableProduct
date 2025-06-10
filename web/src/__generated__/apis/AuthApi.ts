import { AuthResponse } from '@/__generated__/schemas/AuthResponse';
import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { GuestAuthRequest } from '@/__generated__/schemas/GuestAuthRequest';
import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { ListTokenResponse } from '@/__generated__/schemas/ListTokenResponse';
import { makeQueryString } from '@/utils/makeQueryString';
import { PasswordAuthRequest } from '@/__generated__/schemas/PasswordAuthRequest';
import { RefreshTokenAuthRequest } from '@/__generated__/schemas/RefreshTokenAuthRequest';
import { RevokeTokenRequest } from '@/__generated__/schemas/RevokeTokenRequest';
import { ServerTokenAuthRequest } from '@/__generated__/schemas/ServerTokenAuthRequest';
import { ServerTokenResponse } from '@/__generated__/schemas/ServerTokenResponse';
import { Token } from '@/__generated__/schemas/Token';
import { TokenRequestWrapper } from '@/__generated__/schemas/TokenRequestWrapper';
import { TokenResponse } from '@/__generated__/schemas/TokenResponse';

export class AuthApi {
  constructor(
    private readonly requester: HttpRequester
  ) {
  }
  
  /**
   * @deprecated
   * This API method is deprecated and may be removed in future versions.
   * 
   * @param {RefreshTokenAuthRequest} payload - The `RefreshTokenAuthRequest` instance to use for the API request
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<AuthResponse>>} A promise containing the HttpResponse of AuthResponse
   */
  async postAuthRefreshToken(payload: RefreshTokenAuthRequest, gamertag?: string): Promise<HttpResponse<AuthResponse>> {
    let endpoint = "/api/auth/refresh-token";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<AuthResponse, RefreshTokenAuthRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {RefreshTokenAuthRequest} payload - The `RefreshTokenAuthRequest` instance to use for the API request
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<AuthResponse>>} A promise containing the HttpResponse of AuthResponse
   */
  async postAuthRefreshTokenV2(payload: RefreshTokenAuthRequest, gamertag?: string): Promise<HttpResponse<AuthResponse>> {
    let endpoint = "/api/auth/tokens/refresh-token";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<AuthResponse, RefreshTokenAuthRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {GuestAuthRequest} payload - The `GuestAuthRequest` instance to use for the API request
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<AuthResponse>>} A promise containing the HttpResponse of AuthResponse
   */
  async postGuestToken(payload: GuestAuthRequest, gamertag?: string): Promise<HttpResponse<AuthResponse>> {
    let endpoint = "/api/auth/tokens/guest";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<AuthResponse, GuestAuthRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {PasswordAuthRequest} payload - The `PasswordAuthRequest` instance to use for the API request
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<AuthResponse>>} A promise containing the HttpResponse of AuthResponse
   */
  async postPasswordToken(payload: PasswordAuthRequest, gamertag?: string): Promise<HttpResponse<AuthResponse>> {
    let endpoint = "/api/auth/tokens/password";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<AuthResponse, PasswordAuthRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {ServerTokenAuthRequest} payload - The `ServerTokenAuthRequest` instance to use for the API request
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<ServerTokenResponse>>} A promise containing the HttpResponse of ServerTokenResponse
   */
  async postAuthServer(payload: ServerTokenAuthRequest, gamertag?: string): Promise<HttpResponse<ServerTokenResponse>> {
    let endpoint = "/api/auth/server";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<ServerTokenResponse, ServerTokenAuthRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {bigint | string} gamerTagOrAccountId - The `gamerTagOrAccountId` parameter to include in the API request.
   * @param {number} page - The `page` parameter to include in the API request.
   * @param {number} pageSize - The `pageSize` parameter to include in the API request.
   * @param {bigint | string} cid - The `cid` parameter to include in the API request.
   * @param {string} pid - The `pid` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ListTokenResponse>>} A promise containing the HttpResponse of ListTokenResponse
   */
  async getAuthTokenList(gamerTagOrAccountId: bigint | string, page: number, pageSize: number, cid?: bigint | string, pid?: string, gamertag?: string): Promise<HttpResponse<ListTokenResponse>> {
    let endpoint = "/basic/auth/token/list";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      gamerTagOrAccountId,
      page,
      pageSize,
      cid,
      pid
    });
    
    // Make the API request
    return this.requester.request<ListTokenResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers,
      withAuth: true
    });
  }
  
  /**
   * @param {string} token - The `token` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<Token>>} A promise containing the HttpResponse of Token
   */
  async getAuthToken(token: string, gamertag?: string): Promise<HttpResponse<Token>> {
    let endpoint = "/basic/auth/token";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      token
    });
    
    // Make the API request
    return this.requester.request<Token>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {TokenRequestWrapper} payload - The `TokenRequestWrapper` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<TokenResponse>>} A promise containing the HttpResponse of TokenResponse
   */
  async postAuthToken(payload: TokenRequestWrapper, gamertag?: string): Promise<HttpResponse<TokenResponse>> {
    let endpoint = "/basic/auth/token";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<TokenResponse, TokenRequestWrapper>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {RevokeTokenRequest} payload - The `RevokeTokenRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putAuthTokenRevoke(payload: RevokeTokenRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/basic/auth/token/revoke";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, RevokeTokenRequest>({
      url: endpoint,
      method: HttpMethod.PUT,
      headers,
      body: payload,
      withAuth: true
    });
  }
}
