import { AuthResponse } from '@/__generated__/schemas/AuthResponse';
import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { GET } from '@/constants';
import { GuestAuthRequest } from '@/__generated__/schemas/GuestAuthRequest';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { ListTokenResponse } from '@/__generated__/schemas/ListTokenResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { PasswordAuthRequest } from '@/__generated__/schemas/PasswordAuthRequest';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import { RefreshTokenAuthRequest } from '@/__generated__/schemas/RefreshTokenAuthRequest';
import { RevokeTokenRequest } from '@/__generated__/schemas/RevokeTokenRequest';
import { ServerTokenAuthRequest } from '@/__generated__/schemas/ServerTokenAuthRequest';
import { ServerTokenResponse } from '@/__generated__/schemas/ServerTokenResponse';
import { Token } from '@/__generated__/schemas/Token';
import { TokenRequestWrapper } from '@/__generated__/schemas/TokenRequestWrapper';
import { TokenResponse } from '@/__generated__/schemas/TokenResponse';

export class AuthApi {
  constructor(
    private readonly r: HttpRequester
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
    let e = "/api/auth/refresh-token";
    
    // Make the API request
    return makeApiRequest<AuthResponse, RefreshTokenAuthRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @param {RefreshTokenAuthRequest} payload - The `RefreshTokenAuthRequest` instance to use for the API request
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<AuthResponse>>} A promise containing the HttpResponse of AuthResponse
   */
  async postAuthRefreshTokenV2(payload: RefreshTokenAuthRequest, gamertag?: string): Promise<HttpResponse<AuthResponse>> {
    let e = "/api/auth/tokens/refresh-token";
    
    // Make the API request
    return makeApiRequest<AuthResponse, RefreshTokenAuthRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @param {GuestAuthRequest} payload - The `GuestAuthRequest` instance to use for the API request
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<AuthResponse>>} A promise containing the HttpResponse of AuthResponse
   */
  async postGuestToken(payload: GuestAuthRequest, gamertag?: string): Promise<HttpResponse<AuthResponse>> {
    let e = "/api/auth/tokens/guest";
    
    // Make the API request
    return makeApiRequest<AuthResponse, GuestAuthRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @param {PasswordAuthRequest} payload - The `PasswordAuthRequest` instance to use for the API request
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<AuthResponse>>} A promise containing the HttpResponse of AuthResponse
   */
  async postPasswordToken(payload: PasswordAuthRequest, gamertag?: string): Promise<HttpResponse<AuthResponse>> {
    let e = "/api/auth/tokens/password";
    
    // Make the API request
    return makeApiRequest<AuthResponse, PasswordAuthRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @param {ServerTokenAuthRequest} payload - The `ServerTokenAuthRequest` instance to use for the API request
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<ServerTokenResponse>>} A promise containing the HttpResponse of ServerTokenResponse
   */
  async postAuthServer(payload: ServerTokenAuthRequest, gamertag?: string): Promise<HttpResponse<ServerTokenResponse>> {
    let e = "/api/auth/server";
    
    // Make the API request
    return makeApiRequest<ServerTokenResponse, ServerTokenAuthRequest>({
      r: this.r,
      e,
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
   * @param {bigint | string} gamerTagOrAccountId - The `gamerTagOrAccountId` parameter to include in the API request.
   * @param {number} page - The `page` parameter to include in the API request.
   * @param {number} pageSize - The `pageSize` parameter to include in the API request.
   * @param {bigint | string} cid - The `cid` parameter to include in the API request.
   * @param {string} pid - The `pid` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ListTokenResponse>>} A promise containing the HttpResponse of ListTokenResponse
   */
  async getAuthTokenList(gamerTagOrAccountId: bigint | string, page: number, pageSize: number, cid?: bigint | string, pid?: string, gamertag?: string): Promise<HttpResponse<ListTokenResponse>> {
    let e = "/basic/auth/token/list";
    
    // Make the API request
    return makeApiRequest<ListTokenResponse>({
      r: this.r,
      e,
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
   * @param {string} token - The `token` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<Token>>} A promise containing the HttpResponse of Token
   */
  async getAuthToken(token: string, gamertag?: string): Promise<HttpResponse<Token>> {
    let e = "/basic/auth/token";
    
    // Make the API request
    return makeApiRequest<Token>({
      r: this.r,
      e,
      m: GET,
      q: {
        token
      },
      g: gamertag
    });
  }
  
  /**
   * @param {TokenRequestWrapper} payload - The `TokenRequestWrapper` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<TokenResponse>>} A promise containing the HttpResponse of TokenResponse
   */
  async postAuthToken(payload: TokenRequestWrapper, gamertag?: string): Promise<HttpResponse<TokenResponse>> {
    let e = "/basic/auth/token";
    
    // Make the API request
    return makeApiRequest<TokenResponse, TokenRequestWrapper>({
      r: this.r,
      e,
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
   * @param {RevokeTokenRequest} payload - The `RevokeTokenRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putAuthTokenRevoke(payload: RevokeTokenRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/basic/auth/token/revoke";
    
    // Make the API request
    return makeApiRequest<CommonResponse, RevokeTokenRequest>({
      r: this.r,
      e,
      m: PUT,
      p: payload,
      g: gamertag,
      w: true
    });
  }
}
