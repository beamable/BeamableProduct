import { AvailabilityResponse } from '@/__generated__/schemas/AvailabilityResponse';
import { GroupCreate } from '@/__generated__/schemas/GroupCreate';
import { GroupCreateResponse } from '@/__generated__/schemas/GroupCreateResponse';
import { GroupMembershipRequest } from '@/__generated__/schemas/GroupMembershipRequest';
import { GroupMembershipResponse } from '@/__generated__/schemas/GroupMembershipResponse';
import { GroupSearchResponse } from '@/__generated__/schemas/GroupSearchResponse';
import { GroupType } from '@/__generated__/schemas/enums/GroupType';
import { GroupUser } from '@/__generated__/schemas/GroupUser';
import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { makeQueryString } from '@/utils/makeQueryString';

export class GroupUsersApi {
  constructor(
    private readonly requester: HttpRequester
  ) {
  }
  
  /**
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {GroupType} type - The `type` parameter to include in the API request.
   * @param {string} name - The `name` parameter to include in the API request.
   * @param {boolean} subGroup - The `subGroup` parameter to include in the API request.
   * @param {string} tag - The `tag` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AvailabilityResponse>>} A promise containing the HttpResponse of AvailabilityResponse
   */
  async getGroupUsersAvailabilityByObjectId(objectId: bigint | string, type: GroupType, name?: string, subGroup?: boolean, tag?: string, gamertag?: string): Promise<HttpResponse<AvailabilityResponse>> {
    let endpoint = "/object/group-users/{objectId}/availability";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      type,
      name,
      subGroup,
      tag
    });
    
    // Make the API request
    return this.requester.request<AvailabilityResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GroupSearchResponse>>} A promise containing the HttpResponse of GroupSearchResponse
   */
  async getGroupUsersRecommendedByObjectId(objectId: bigint | string, gamertag?: string): Promise<HttpResponse<GroupSearchResponse>> {
    let endpoint = "/object/group-users/{objectId}/recommended";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<GroupSearchResponse>({
      url: endpoint,
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {GroupMembershipRequest} payload - The `GroupMembershipRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GroupMembershipResponse>>} A promise containing the HttpResponse of GroupMembershipResponse
   */
  async postGroupUsersJoinByObjectId(objectId: bigint | string, payload: GroupMembershipRequest, gamertag?: string): Promise<HttpResponse<GroupMembershipResponse>> {
    let endpoint = "/object/group-users/{objectId}/join";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<GroupMembershipResponse, GroupMembershipRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {GroupMembershipRequest} payload - The `GroupMembershipRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GroupMembershipResponse>>} A promise containing the HttpResponse of GroupMembershipResponse
   */
  async deleteGroupUsersJoinByObjectId(objectId: bigint | string, payload: GroupMembershipRequest, gamertag?: string): Promise<HttpResponse<GroupMembershipResponse>> {
    let endpoint = "/object/group-users/{objectId}/join";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<GroupMembershipResponse, GroupMembershipRequest>({
      url: endpoint,
      method: HttpMethod.DELETE,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {GroupCreate} payload - The `GroupCreate` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GroupCreateResponse>>} A promise containing the HttpResponse of GroupCreateResponse
   */
  async postGroupUsersByObjectId(objectId: bigint | string, payload: GroupCreate, gamertag?: string): Promise<HttpResponse<GroupCreateResponse>> {
    let endpoint = "/object/group-users/{objectId}/group";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<GroupCreateResponse, GroupCreate>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {GroupType} type - The `type` parameter to include in the API request.
   * @param {string} enrollmentTypes - The `enrollmentTypes` parameter to include in the API request.
   * @param {boolean} hasSlots - The `hasSlots` parameter to include in the API request.
   * @param {number} limit - The `limit` parameter to include in the API request.
   * @param {string} name - The `name` parameter to include in the API request.
   * @param {number} offset - The `offset` parameter to include in the API request.
   * @param {bigint | string} scoreMax - The `scoreMax` parameter to include in the API request.
   * @param {bigint | string} scoreMin - The `scoreMin` parameter to include in the API request.
   * @param {string} sortField - The `sortField` parameter to include in the API request.
   * @param {number} sortValue - The `sortValue` parameter to include in the API request.
   * @param {boolean} subGroup - The `subGroup` parameter to include in the API request.
   * @param {bigint | string} userScore - The `userScore` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GroupSearchResponse>>} A promise containing the HttpResponse of GroupSearchResponse
   */
  async getGroupUsersSearchByObjectId(objectId: bigint | string, type: GroupType, enrollmentTypes?: string, hasSlots?: boolean, limit?: number, name?: string, offset?: number, scoreMax?: bigint | string, scoreMin?: bigint | string, sortField?: string, sortValue?: number, subGroup?: boolean, userScore?: bigint | string, gamertag?: string): Promise<HttpResponse<GroupSearchResponse>> {
    let endpoint = "/object/group-users/{objectId}/search";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
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
    });
    
    // Make the API request
    return this.requester.request<GroupSearchResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GroupUser>>} A promise containing the HttpResponse of GroupUser
   */
  async getGroupUsersByObjectId(objectId: bigint | string, gamertag?: string): Promise<HttpResponse<GroupUser>> {
    let endpoint = "/object/group-users/{objectId}/";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<GroupUser>({
      url: endpoint,
      method: HttpMethod.GET,
      headers
    });
  }
}
