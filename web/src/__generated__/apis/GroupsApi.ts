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
import { objectIdPlaceholder } from '@/constants';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import { RoleChangeRequest } from '@/__generated__/schemas/RoleChangeRequest';

export class GroupsApi {
  constructor(
    private readonly r: HttpRequester
  ) {
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {RoleChangeRequest} payload - The `RoleChangeRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putGroupRoleByObjectId(objectId: bigint | string, payload: RoleChangeRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/object/groups/{objectId}/role".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CommonResponse, RoleChangeRequest>({
      r: this.r,
      e,
      m: PUT,
      p: payload,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {KickRequest} payload - The `KickRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GroupMembershipResponse>>} A promise containing the HttpResponse of GroupMembershipResponse
   */
  async postGroupKickByObjectId(objectId: bigint | string, payload: KickRequest, gamertag?: string): Promise<HttpResponse<GroupMembershipResponse>> {
    let e = "/object/groups/{objectId}/kick".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<GroupMembershipResponse, KickRequest>({
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
   * @param {GroupApplication} payload - The `GroupApplication` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postGroupApplyByObjectId(objectId: bigint | string, payload: GroupApplication, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/object/groups/{objectId}/apply".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CommonResponse, GroupApplication>({
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
   * @param {CreateDonationRequest} payload - The `CreateDonationRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postGroupDonationsByObjectId(objectId: bigint | string, payload: CreateDonationRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let e = "/object/groups/{objectId}/donations".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<EmptyResponse, CreateDonationRequest>({
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
   * @param {MakeDonationRequest} payload - The `MakeDonationRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async putGroupDonationsByObjectId(objectId: bigint | string, payload: MakeDonationRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let e = "/object/groups/{objectId}/donations".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<EmptyResponse, MakeDonationRequest>({
      r: this.r,
      e,
      m: PUT,
      p: payload,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {KickRequest} payload - The `KickRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GroupMembershipResponse>>} A promise containing the HttpResponse of GroupMembershipResponse
   */
  async deleteGroupMemberByObjectId(objectId: bigint | string, payload: KickRequest, gamertag?: string): Promise<HttpResponse<GroupMembershipResponse>> {
    let e = "/object/groups/{objectId}/member".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<GroupMembershipResponse, KickRequest>({
      r: this.r,
      e,
      m: DELETE,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<Group>>} A promise containing the HttpResponse of Group
   */
  async getGroupByObjectId(objectId: bigint | string, gamertag?: string): Promise<HttpResponse<Group>> {
    let e = "/object/groups/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<Group>({
      r: this.r,
      e,
      m: GET,
      g: gamertag
    });
  }
  
  /**
   * @param {GroupUpdate} payload - The `GroupUpdate` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putGroupByObjectId(objectId: bigint | string, payload: GroupUpdate, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/object/groups/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CommonResponse, GroupUpdate>({
      r: this.r,
      e,
      m: PUT,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @param {DisbandRequest} payload - The `DisbandRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async deleteGroupByObjectId(objectId: bigint | string, payload: DisbandRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/object/groups/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CommonResponse, DisbandRequest>({
      r: this.r,
      e,
      m: DELETE,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async putGroupDonationsClaimByObjectId(objectId: bigint | string, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let e = "/object/groups/{objectId}/donations/claim".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<EmptyResponse>({
      r: this.r,
      e,
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
   * @param {GroupInvite} payload - The `GroupInvite` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postGroupInviteByObjectId(objectId: bigint | string, payload: GroupInvite, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/object/groups/{objectId}/invite".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CommonResponse, GroupInvite>({
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
   * @param {GroupApplication} payload - The `GroupApplication` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postGroupPetitionByObjectId(objectId: bigint | string, payload: GroupApplication, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/object/groups/{objectId}/petition".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CommonResponse, GroupApplication>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
    });
  }
}
