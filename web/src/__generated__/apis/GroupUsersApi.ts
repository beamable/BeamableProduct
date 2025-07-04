import { AvailabilityResponse } from '@/__generated__/schemas/AvailabilityResponse';
import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { GroupCreate } from '@/__generated__/schemas/GroupCreate';
import { GroupCreateResponse } from '@/__generated__/schemas/GroupCreateResponse';
import { GroupMembershipRequest } from '@/__generated__/schemas/GroupMembershipRequest';
import { GroupMembershipResponse } from '@/__generated__/schemas/GroupMembershipResponse';
import { GroupSearchResponse } from '@/__generated__/schemas/GroupSearchResponse';
import { GroupType } from '@/__generated__/schemas/enums/GroupType';
import { GroupUser } from '@/__generated__/schemas/GroupUser';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpResponse } from '@/network/http/types/HttpResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/constants';
import { POST } from '@/constants';

export class GroupUsersApi {
  constructor(
    private readonly r: HttpRequester
  ) {
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {GroupType} type - The `type` parameter to include in the API request.
   * @param {string} name - The `name` parameter to include in the API request.
   * @param {boolean} subGroup - The `subGroup` parameter to include in the API request.
   * @param {string} tag - The `tag` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AvailabilityResponse>>} A promise containing the HttpResponse of AvailabilityResponse
   */
  async getGroupUsersAvailabilityByObjectId(objectId: bigint | string, type: GroupType, name?: string, subGroup?: boolean, tag?: string, gamertag?: string): Promise<HttpResponse<AvailabilityResponse>> {
    let e = "/object/group-users/{objectId}/availability".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<AvailabilityResponse>({
      r: this.r,
      e,
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
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GroupSearchResponse>>} A promise containing the HttpResponse of GroupSearchResponse
   */
  async getGroupUsersRecommendedByObjectId(objectId: bigint | string, gamertag?: string): Promise<HttpResponse<GroupSearchResponse>> {
    let e = "/object/group-users/{objectId}/recommended".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<GroupSearchResponse>({
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
   * @param {GroupMembershipRequest} payload - The `GroupMembershipRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GroupMembershipResponse>>} A promise containing the HttpResponse of GroupMembershipResponse
   */
  async postGroupUsersJoinByObjectId(objectId: bigint | string, payload: GroupMembershipRequest, gamertag?: string): Promise<HttpResponse<GroupMembershipResponse>> {
    let e = "/object/group-users/{objectId}/join".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<GroupMembershipResponse, GroupMembershipRequest>({
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
   * @param {GroupMembershipRequest} payload - The `GroupMembershipRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GroupMembershipResponse>>} A promise containing the HttpResponse of GroupMembershipResponse
   */
  async deleteGroupUsersJoinByObjectId(objectId: bigint | string, payload: GroupMembershipRequest, gamertag?: string): Promise<HttpResponse<GroupMembershipResponse>> {
    let e = "/object/group-users/{objectId}/join".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<GroupMembershipResponse, GroupMembershipRequest>({
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
   * @param {GroupCreate} payload - The `GroupCreate` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GroupCreateResponse>>} A promise containing the HttpResponse of GroupCreateResponse
   */
  async postGroupUsersByObjectId(objectId: bigint | string, payload: GroupCreate, gamertag?: string): Promise<HttpResponse<GroupCreateResponse>> {
    let e = "/object/group-users/{objectId}/group".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<GroupCreateResponse, GroupCreate>({
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
    let e = "/object/group-users/{objectId}/search".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<GroupSearchResponse>({
      r: this.r,
      e,
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
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GroupUser>>} A promise containing the HttpResponse of GroupUser
   */
  async getGroupUsersByObjectId(objectId: bigint | string, gamertag?: string): Promise<HttpResponse<GroupUser>> {
    let e = "/object/group-users/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<GroupUser>({
      r: this.r,
      e,
      m: GET,
      g: gamertag,
      w: true
    });
  }
}
