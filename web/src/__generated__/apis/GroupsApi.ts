import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { CreateDonationRequest } from '@/__generated__/schemas/CreateDonationRequest';
import { DisbandRequest } from '@/__generated__/schemas/DisbandRequest';
import { EmptyResponse } from '@/__generated__/schemas/EmptyResponse';
import { Group } from '@/__generated__/schemas/Group';
import { GroupApplication } from '@/__generated__/schemas/GroupApplication';
import { GroupInvite } from '@/__generated__/schemas/GroupInvite';
import { GroupMembershipResponse } from '@/__generated__/schemas/GroupMembershipResponse';
import { GroupUpdate } from '@/__generated__/schemas/GroupUpdate';
import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { KickRequest } from '@/__generated__/schemas/KickRequest';
import { MakeDonationRequest } from '@/__generated__/schemas/MakeDonationRequest';
import { RoleChangeRequest } from '@/__generated__/schemas/RoleChangeRequest';

export class GroupsApi {
  constructor(
    private readonly requester: HttpRequester
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
    let endpoint = "/object/groups/{objectId}/role".replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, RoleChangeRequest>({
      url: endpoint,
      method: HttpMethod.PUT,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {KickRequest} payload - The `KickRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GroupMembershipResponse>>} A promise containing the HttpResponse of GroupMembershipResponse
   */
  async postGroupKickByObjectId(objectId: bigint | string, payload: KickRequest, gamertag?: string): Promise<HttpResponse<GroupMembershipResponse>> {
    let endpoint = "/object/groups/{objectId}/kick".replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<GroupMembershipResponse, KickRequest>({
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
   * @param {GroupApplication} payload - The `GroupApplication` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postGroupApplyByObjectId(objectId: bigint | string, payload: GroupApplication, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/object/groups/{objectId}/apply".replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, GroupApplication>({
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
   * @param {CreateDonationRequest} payload - The `CreateDonationRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postGroupDonationsByObjectId(objectId: bigint | string, payload: CreateDonationRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let endpoint = "/object/groups/{objectId}/donations".replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyResponse, CreateDonationRequest>({
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
   * @param {MakeDonationRequest} payload - The `MakeDonationRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async putGroupDonationsByObjectId(objectId: bigint | string, payload: MakeDonationRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let endpoint = "/object/groups/{objectId}/donations".replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyResponse, MakeDonationRequest>({
      url: endpoint,
      method: HttpMethod.PUT,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {KickRequest} payload - The `KickRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GroupMembershipResponse>>} A promise containing the HttpResponse of GroupMembershipResponse
   */
  async deleteGroupMemberByObjectId(objectId: bigint | string, payload: KickRequest, gamertag?: string): Promise<HttpResponse<GroupMembershipResponse>> {
    let endpoint = "/object/groups/{objectId}/member".replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<GroupMembershipResponse, KickRequest>({
      url: endpoint,
      method: HttpMethod.DELETE,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<Group>>} A promise containing the HttpResponse of Group
   */
  async getGroupByObjectId(objectId: bigint | string, gamertag?: string): Promise<HttpResponse<Group>> {
    let endpoint = "/object/groups/{objectId}/".replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<Group>({
      url: endpoint,
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {GroupUpdate} payload - The `GroupUpdate` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putGroupByObjectId(objectId: bigint | string, payload: GroupUpdate, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/object/groups/{objectId}/".replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, GroupUpdate>({
      url: endpoint,
      method: HttpMethod.PUT,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {DisbandRequest} payload - The `DisbandRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async deleteGroupByObjectId(objectId: bigint | string, payload: DisbandRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/object/groups/{objectId}/".replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, DisbandRequest>({
      url: endpoint,
      method: HttpMethod.DELETE,
      headers,
      body: payload
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
    let endpoint = "/object/groups/{objectId}/donations/claim".replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyResponse>({
      url: endpoint,
      method: HttpMethod.PUT,
      headers,
      withAuth: true
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
    let endpoint = "/object/groups/{objectId}/invite".replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, GroupInvite>({
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
   * @param {GroupApplication} payload - The `GroupApplication` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postGroupPetitionByObjectId(objectId: bigint | string, payload: GroupApplication, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/object/groups/{objectId}/petition".replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, GroupApplication>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
}
