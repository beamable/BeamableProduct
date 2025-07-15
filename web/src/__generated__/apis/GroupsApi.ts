import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { CreateDonationRequest } from '@/__generated__/schemas/CreateDonationRequest';
import { DELETE } from '@/constants';
import { DisbandRequest } from '@/__generated__/schemas/DisbandRequest';
import { EmptyResponse } from '@/__generated__/schemas/EmptyResponse';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { Group } from '@/__generated__/schemas/Group';
import { GroupApplication } from '@/__generated__/schemas/GroupApplication';
import { GroupInvite } from '@/__generated__/schemas/GroupInvite';
import { GroupMembershipResponse } from '@/__generated__/schemas/GroupMembershipResponse';
import { GroupUpdate } from '@/__generated__/schemas/GroupUpdate';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpResponse } from '@/network/http/types/HttpResponse';
import { KickRequest } from '@/__generated__/schemas/KickRequest';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { MakeDonationRequest } from '@/__generated__/schemas/MakeDonationRequest';
import { objectIdPlaceholder } from '@/__generated__/apis/constants';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import { RoleChangeRequest } from '@/__generated__/schemas/RoleChangeRequest';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `RoleChangeRequest` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function groupsPutRoleByObjectId(requester: HttpRequester, objectId: bigint | string, payload: RoleChangeRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/object/groups/{objectId}/role".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<CommonResponse, RoleChangeRequest>({
    r: requester,
    e: endpoint,
    m: PUT,
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
 * @param payload - The `KickRequest` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function groupsPostKickByObjectId(requester: HttpRequester, objectId: bigint | string, payload: KickRequest, gamertag?: string): Promise<HttpResponse<GroupMembershipResponse>> {
  let endpoint = "/object/groups/{objectId}/kick".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<GroupMembershipResponse, KickRequest>({
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
 * @param payload - The `GroupApplication` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function groupsPostApplyByObjectId(requester: HttpRequester, objectId: bigint | string, payload: GroupApplication, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/object/groups/{objectId}/apply".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<CommonResponse, GroupApplication>({
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
 * @param payload - The `CreateDonationRequest` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function groupsPostDonationsByObjectId(requester: HttpRequester, objectId: bigint | string, payload: CreateDonationRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/object/groups/{objectId}/donations".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<EmptyResponse, CreateDonationRequest>({
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
 * @param payload - The `MakeDonationRequest` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function groupsPutDonationsByObjectId(requester: HttpRequester, objectId: bigint | string, payload: MakeDonationRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/object/groups/{objectId}/donations".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<EmptyResponse, MakeDonationRequest>({
    r: requester,
    e: endpoint,
    m: PUT,
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
 * @param payload - The `KickRequest` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function groupsDeleteMemberByObjectId(requester: HttpRequester, objectId: bigint | string, payload: KickRequest, gamertag?: string): Promise<HttpResponse<GroupMembershipResponse>> {
  let endpoint = "/object/groups/{objectId}/member".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<GroupMembershipResponse, KickRequest>({
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
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function groupsGetByObjectId(requester: HttpRequester, objectId: bigint | string, gamertag?: string): Promise<HttpResponse<Group>> {
  let endpoint = "/object/groups/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<Group>({
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
 * @param payload - The `GroupUpdate` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function groupsPutByObjectId(requester: HttpRequester, objectId: bigint | string, payload: GroupUpdate, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/object/groups/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<CommonResponse, GroupUpdate>({
    r: requester,
    e: endpoint,
    m: PUT,
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
 * @param payload - The `DisbandRequest` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function groupsDeleteByObjectId(requester: HttpRequester, objectId: bigint | string, payload: DisbandRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/object/groups/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<CommonResponse, DisbandRequest>({
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
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function groupsPutDonationsClaimByObjectId(requester: HttpRequester, objectId: bigint | string, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/object/groups/{objectId}/donations/claim".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<EmptyResponse>({
    r: requester,
    e: endpoint,
    m: PUT,
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
 * @param payload - The `GroupInvite` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function groupsPostInviteByObjectId(requester: HttpRequester, objectId: bigint | string, payload: GroupInvite, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/object/groups/{objectId}/invite".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<CommonResponse, GroupInvite>({
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
 * @param payload - The `GroupApplication` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function groupsPostPetitionByObjectId(requester: HttpRequester, objectId: bigint | string, payload: GroupApplication, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/object/groups/{objectId}/petition".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<CommonResponse, GroupApplication>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}
