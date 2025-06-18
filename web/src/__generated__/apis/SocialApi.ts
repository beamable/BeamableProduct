import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { DELETE } from '@/constants';
import { EmptyResponse } from '@/__generated__/schemas/EmptyResponse';
import { FriendshipStatus } from '@/__generated__/schemas/FriendshipStatus';
import { GET } from '@/constants';
import { GetSocialStatusesResponse } from '@/__generated__/schemas/GetSocialStatusesResponse';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { ImportFriendsRequest } from '@/__generated__/schemas/ImportFriendsRequest';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { MakeFriendshipRequest } from '@/__generated__/schemas/MakeFriendshipRequest';
import { PlayerIdRequest } from '@/__generated__/schemas/PlayerIdRequest';
import { POST } from '@/constants';
import { SendFriendRequest } from '@/__generated__/schemas/SendFriendRequest';
import { Social } from '@/__generated__/schemas/Social';

export class SocialApi {
  constructor(
    private readonly r: HttpRequester
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
    let e = "/basic/social/my";
    
    // Make the API request
    return makeApiRequest<Social>({
      r: this.r,
      e,
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
   * @param {SendFriendRequest} payload - The `SendFriendRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postSocialFriendsInvite(payload: SendFriendRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let e = "/basic/social/friends/invite";
    
    // Make the API request
    return makeApiRequest<EmptyResponse, SendFriendRequest>({
      r: this.r,
      e,
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
   * @param {SendFriendRequest} payload - The `SendFriendRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async deleteSocialFriendsInvite(payload: SendFriendRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let e = "/basic/social/friends/invite";
    
    // Make the API request
    return makeApiRequest<EmptyResponse, SendFriendRequest>({
      r: this.r,
      e,
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
   * @param {PlayerIdRequest} payload - The `PlayerIdRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async deleteSocialFriends(payload: PlayerIdRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let e = "/basic/social/friends";
    
    // Make the API request
    return makeApiRequest<EmptyResponse, PlayerIdRequest>({
      r: this.r,
      e,
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
   * @param {ImportFriendsRequest} payload - The `ImportFriendsRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postSocialFriendsImport(payload: ImportFriendsRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let e = "/basic/social/friends/import";
    
    // Make the API request
    return makeApiRequest<EmptyResponse, ImportFriendsRequest>({
      r: this.r,
      e,
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
   * @param {MakeFriendshipRequest} payload - The `MakeFriendshipRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postSocialFriendsMake(payload: MakeFriendshipRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/basic/social/friends/make";
    
    // Make the API request
    return makeApiRequest<CommonResponse, MakeFriendshipRequest>({
      r: this.r,
      e,
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
   * @param {string[]} playerIds - The `playerIds` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetSocialStatusesResponse>>} A promise containing the HttpResponse of GetSocialStatusesResponse
   */
  async getSocial(playerIds: string[], gamertag?: string): Promise<HttpResponse<GetSocialStatusesResponse>> {
    let e = "/basic/social/";
    
    // Make the API request
    return makeApiRequest<GetSocialStatusesResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
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
   * @param {PlayerIdRequest} payload - The `PlayerIdRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<FriendshipStatus>>} A promise containing the HttpResponse of FriendshipStatus
   */
  async postSocialBlocked(payload: PlayerIdRequest, gamertag?: string): Promise<HttpResponse<FriendshipStatus>> {
    let e = "/basic/social/blocked";
    
    // Make the API request
    return makeApiRequest<FriendshipStatus, PlayerIdRequest>({
      r: this.r,
      e,
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
   * @param {PlayerIdRequest} payload - The `PlayerIdRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<FriendshipStatus>>} A promise containing the HttpResponse of FriendshipStatus
   */
  async deleteSocialBlocked(payload: PlayerIdRequest, gamertag?: string): Promise<HttpResponse<FriendshipStatus>> {
    let e = "/basic/social/blocked";
    
    // Make the API request
    return makeApiRequest<FriendshipStatus, PlayerIdRequest>({
      r: this.r,
      e,
      m: DELETE,
      p: payload,
      g: gamertag,
      w: true
    });
  }
}
