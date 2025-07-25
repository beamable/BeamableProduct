/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/__generated__/apis/constants';
import { POST } from '@/constants';
import type { AvailabilityResponse } from '@/__generated__/schemas/AvailabilityResponse';
import type { GroupCreate } from '@/__generated__/schemas/GroupCreate';
import type { GroupCreateResponse } from '@/__generated__/schemas/GroupCreateResponse';
import type { GroupMembershipRequest } from '@/__generated__/schemas/GroupMembershipRequest';
import type { GroupMembershipResponse } from '@/__generated__/schemas/GroupMembershipResponse';
import type { GroupSearchResponse } from '@/__generated__/schemas/GroupSearchResponse';
import type { GroupType } from '@/__generated__/schemas/enums/GroupType';
import type { GroupUser } from '@/__generated__/schemas/GroupUser';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param type - The `type` parameter to include in the API request.
 * @param name - The `name` parameter to include in the API request.
 * @param subGroup - The `subGroup` parameter to include in the API request.
 * @param tag - The `tag` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function groupUsersGetAvailabilityByObjectId(requester: HttpRequester, objectId: bigint | string, type: GroupType, name?: string, subGroup?: boolean, tag?: string, gamertag?: string): Promise<HttpResponse<AvailabilityResponse>> {
  let endpoint = "/object/group-users/{objectId}/availability".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<AvailabilityResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      type,
      name,
      subGroup,
      tag
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
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function groupUsersGetRecommendedByObjectId(requester: HttpRequester, objectId: bigint | string, gamertag?: string): Promise<HttpResponse<GroupSearchResponse>> {
  let endpoint = "/object/group-users/{objectId}/recommended".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<GroupSearchResponse>({
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
 * @param payload - The `GroupMembershipRequest` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function groupUsersPostJoinByObjectId(requester: HttpRequester, objectId: bigint | string, payload: GroupMembershipRequest, gamertag?: string): Promise<HttpResponse<GroupMembershipResponse>> {
  let endpoint = "/object/group-users/{objectId}/join".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<GroupMembershipResponse, GroupMembershipRequest>({
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
 * @param payload - The `GroupMembershipRequest` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function groupUsersDeleteJoinByObjectId(requester: HttpRequester, objectId: bigint | string, payload: GroupMembershipRequest, gamertag?: string): Promise<HttpResponse<GroupMembershipResponse>> {
  let endpoint = "/object/group-users/{objectId}/join".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<GroupMembershipResponse, GroupMembershipRequest>({
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
 * @param payload - The `GroupCreate` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function groupUsersPostGroupByObjectId(requester: HttpRequester, objectId: bigint | string, payload: GroupCreate, gamertag?: string): Promise<HttpResponse<GroupCreateResponse>> {
  let endpoint = "/object/group-users/{objectId}/group".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<GroupCreateResponse, GroupCreate>({
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
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param type - The `type` parameter to include in the API request.
 * @param enrollmentTypes - The `enrollmentTypes` parameter to include in the API request.
 * @param hasSlots - The `hasSlots` parameter to include in the API request.
 * @param limit - The `limit` parameter to include in the API request.
 * @param name - The `name` parameter to include in the API request.
 * @param offset - The `offset` parameter to include in the API request.
 * @param scoreMax - The `scoreMax` parameter to include in the API request.
 * @param scoreMin - The `scoreMin` parameter to include in the API request.
 * @param sortField - The `sortField` parameter to include in the API request.
 * @param sortValue - The `sortValue` parameter to include in the API request.
 * @param subGroup - The `subGroup` parameter to include in the API request.
 * @param userScore - The `userScore` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function groupUsersGetSearchByObjectId(requester: HttpRequester, objectId: bigint | string, type: GroupType, enrollmentTypes?: string, hasSlots?: boolean, limit?: number, name?: string, offset?: number, scoreMax?: bigint | string, scoreMin?: bigint | string, sortField?: string, sortValue?: number, subGroup?: boolean, userScore?: bigint | string, gamertag?: string): Promise<HttpResponse<GroupSearchResponse>> {
  let endpoint = "/object/group-users/{objectId}/search".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<GroupSearchResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      type,
      enrollmentTypes,
      hasSlots,
      limit,
      name,
      offset,
      scoreMax,
      scoreMin,
      sortField,
      sortValue,
      subGroup,
      userScore
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
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function groupUsersGetByObjectId(requester: HttpRequester, objectId: bigint | string, gamertag?: string): Promise<HttpResponse<GroupUser>> {
  let endpoint = "/object/group-users/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<GroupUser>({
    r: requester,
    e: endpoint,
    m: GET,
    g: gamertag,
    w: true
  });
}
