/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { DELETE } from '@/constants';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import type { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import type { EmptyResponse } from '@/__generated__/schemas/EmptyResponse';
import type { FriendshipStatus } from '@/__generated__/schemas/FriendshipStatus';
import type { GetSocialStatusesResponse } from '@/__generated__/schemas/GetSocialStatusesResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { ImportFriendsRequest } from '@/__generated__/schemas/ImportFriendsRequest';
import type { MakeFriendshipRequest } from '@/__generated__/schemas/MakeFriendshipRequest';
import type { PlayerIdRequest } from '@/__generated__/schemas/PlayerIdRequest';
import type { SendFriendRequest } from '@/__generated__/schemas/SendFriendRequest';
import type { Social } from '@/__generated__/schemas/Social';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function socialGetMyBasic(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<Social>> {
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
export async function socialPostFriendsInviteBasic(requester: HttpRequester, payload: SendFriendRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
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
export async function socialDeleteFriendsInviteBasic(requester: HttpRequester, payload: SendFriendRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
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
export async function socialDeleteFriendsBasic(requester: HttpRequester, payload: PlayerIdRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
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
export async function socialPostFriendsImportBasic(requester: HttpRequester, payload: ImportFriendsRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
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
export async function socialPostFriendsMakeBasic(requester: HttpRequester, payload: MakeFriendshipRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
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
export async function socialGetBasic(requester: HttpRequester, playerIds: string[], gamertag?: string): Promise<HttpResponse<GetSocialStatusesResponse>> {
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
export async function socialPostBlockedBasic(requester: HttpRequester, payload: PlayerIdRequest, gamertag?: string): Promise<HttpResponse<FriendshipStatus>> {
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
export async function socialDeleteBlockedBasic(requester: HttpRequester, payload: PlayerIdRequest, gamertag?: string): Promise<HttpResponse<FriendshipStatus>> {
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
