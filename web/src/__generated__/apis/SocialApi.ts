import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { DELETE } from '@/constants';
import { EmptyResponse } from '@/__generated__/schemas/EmptyResponse';
import { FriendshipStatus } from '@/__generated__/schemas/FriendshipStatus';
import { GET } from '@/constants';
import { GetSocialStatusesResponse } from '@/__generated__/schemas/GetSocialStatusesResponse';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpResponse } from '@/network/http/types/HttpResponse';
import { ImportFriendsRequest } from '@/__generated__/schemas/ImportFriendsRequest';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { MakeFriendshipRequest } from '@/__generated__/schemas/MakeFriendshipRequest';
import { PlayerIdRequest } from '@/__generated__/schemas/PlayerIdRequest';
import { POST } from '@/constants';
import { SendFriendRequest } from '@/__generated__/schemas/SendFriendRequest';
import { Social } from '@/__generated__/schemas/Social';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getSocialMy(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<Social>> {
  let endpoint = "/basic/social/my";
  
  // Make the API request
  return makeApiRequest<Social>({
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
 * @param payload - The `SendFriendRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postSocialFriendsInvite(requester: HttpRequester, payload: SendFriendRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/basic/social/friends/invite";
  
  // Make the API request
  return makeApiRequest<EmptyResponse, SendFriendRequest>({
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
 * @param payload - The `SendFriendRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function deleteSocialFriendsInvite(requester: HttpRequester, payload: SendFriendRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/basic/social/friends/invite";
  
  // Make the API request
  return makeApiRequest<EmptyResponse, SendFriendRequest>({
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
 * @param payload - The `PlayerIdRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function deleteSocialFriends(requester: HttpRequester, payload: PlayerIdRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/basic/social/friends";
  
  // Make the API request
  return makeApiRequest<EmptyResponse, PlayerIdRequest>({
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
 * @param payload - The `ImportFriendsRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postSocialFriendsImport(requester: HttpRequester, payload: ImportFriendsRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/basic/social/friends/import";
  
  // Make the API request
  return makeApiRequest<EmptyResponse, ImportFriendsRequest>({
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
 * @param payload - The `MakeFriendshipRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postSocialFriendsMake(requester: HttpRequester, payload: MakeFriendshipRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/basic/social/friends/make";
  
  // Make the API request
  return makeApiRequest<CommonResponse, MakeFriendshipRequest>({
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
 * @param playerIds - The `playerIds` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getSocial(requester: HttpRequester, playerIds: string[], gamertag?: string): Promise<HttpResponse<GetSocialStatusesResponse>> {
  let endpoint = "/basic/social/";
  
  // Make the API request
  return makeApiRequest<GetSocialStatusesResponse>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `PlayerIdRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postSocialBlocked(requester: HttpRequester, payload: PlayerIdRequest, gamertag?: string): Promise<HttpResponse<FriendshipStatus>> {
  let endpoint = "/basic/social/blocked";
  
  // Make the API request
  return makeApiRequest<FriendshipStatus, PlayerIdRequest>({
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
 * @param payload - The `PlayerIdRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function deleteSocialBlocked(requester: HttpRequester, payload: PlayerIdRequest, gamertag?: string): Promise<HttpResponse<FriendshipStatus>> {
  let endpoint = "/basic/social/blocked";
  
  // Make the API request
  return makeApiRequest<FriendshipStatus, PlayerIdRequest>({
    r: requester,
    e: endpoint,
    m: DELETE,
    p: payload,
    g: gamertag,
    w: true
  });
}
