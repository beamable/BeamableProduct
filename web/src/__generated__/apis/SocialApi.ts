import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { EmptyResponse } from '@/__generated__/schemas/EmptyResponse';
import { FriendshipStatus } from '@/__generated__/schemas/FriendshipStatus';
import { GetSocialStatusesResponse } from '@/__generated__/schemas/GetSocialStatusesResponse';
import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { ImportFriendsRequest } from '@/__generated__/schemas/ImportFriendsRequest';
import { MakeFriendshipRequest } from '@/__generated__/schemas/MakeFriendshipRequest';
import { makeQueryString } from '@/utils/makeQueryString';
import { PlayerIdRequest } from '@/__generated__/schemas/PlayerIdRequest';
import { SendFriendRequest } from '@/__generated__/schemas/SendFriendRequest';
import { Social } from '@/__generated__/schemas/Social';

export class SocialApi {
  constructor(
    private readonly requester: HttpRequester
  ) {
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<Social>>} A promise containing the HttpResponse of Social
   */
  async getSocialMy(gamertag?: string): Promise<HttpResponse<Social>> {
    let endpoint = "/basic/social/my";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<Social>({
      url: endpoint,
      method: HttpMethod.GET,
      headers,
      withAuth: true
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {SendFriendRequest} payload - The `SendFriendRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postSocialFriendsInvite(payload: SendFriendRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let endpoint = "/basic/social/friends/invite";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyResponse, SendFriendRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {SendFriendRequest} payload - The `SendFriendRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async deleteSocialFriendsInvite(payload: SendFriendRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let endpoint = "/basic/social/friends/invite";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyResponse, SendFriendRequest>({
      url: endpoint,
      method: HttpMethod.DELETE,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {PlayerIdRequest} payload - The `PlayerIdRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async deleteSocialFriends(payload: PlayerIdRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let endpoint = "/basic/social/friends";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyResponse, PlayerIdRequest>({
      url: endpoint,
      method: HttpMethod.DELETE,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {ImportFriendsRequest} payload - The `ImportFriendsRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postSocialFriendsImport(payload: ImportFriendsRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let endpoint = "/basic/social/friends/import";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyResponse, ImportFriendsRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {MakeFriendshipRequest} payload - The `MakeFriendshipRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postSocialFriendsMake(payload: MakeFriendshipRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/basic/social/friends/make";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, MakeFriendshipRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {string[]} playerIds - The `playerIds` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetSocialStatusesResponse>>} A promise containing the HttpResponse of GetSocialStatusesResponse
   */
  async getSocial(playerIds: string[], gamertag?: string): Promise<HttpResponse<GetSocialStatusesResponse>> {
    let endpoint = "/basic/social/";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      playerIds
    });
    
    // Make the API request
    return this.requester.request<GetSocialStatusesResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers,
      withAuth: true
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {PlayerIdRequest} payload - The `PlayerIdRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<FriendshipStatus>>} A promise containing the HttpResponse of FriendshipStatus
   */
  async postSocialBlocked(payload: PlayerIdRequest, gamertag?: string): Promise<HttpResponse<FriendshipStatus>> {
    let endpoint = "/basic/social/blocked";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<FriendshipStatus, PlayerIdRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {PlayerIdRequest} payload - The `PlayerIdRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<FriendshipStatus>>} A promise containing the HttpResponse of FriendshipStatus
   */
  async deleteSocialBlocked(payload: PlayerIdRequest, gamertag?: string): Promise<HttpResponse<FriendshipStatus>> {
    let endpoint = "/basic/social/blocked";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<FriendshipStatus, PlayerIdRequest>({
      url: endpoint,
      method: HttpMethod.DELETE,
      headers,
      body: payload,
      withAuth: true
    });
  }
}
