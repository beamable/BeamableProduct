import { ApiPartiesInviteDeletePartyResponse } from '@/__generated__/schemas/ApiPartiesInviteDeletePartyResponse';
import { ApiPartiesInvitePostPartyResponse } from '@/__generated__/schemas/ApiPartiesInvitePostPartyResponse';
import { ApiPartiesMembersDeletePartyResponse } from '@/__generated__/schemas/ApiPartiesMembersDeletePartyResponse';
import { CancelInviteToParty } from '@/__generated__/schemas/CancelInviteToParty';
import { CreateParty } from '@/__generated__/schemas/CreateParty';
import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpResponse } from '@/network/http/types/HttpResponse';
import { InviteToParty } from '@/__generated__/schemas/InviteToParty';
import { LeaveParty } from '@/__generated__/schemas/LeaveParty';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/constants';
import { Party } from '@/__generated__/schemas/Party';
import { POST } from '@/constants';
import { PromoteNewLeader } from '@/__generated__/schemas/PromoteNewLeader';
import { PUT } from '@/constants';
import { UpdateParty } from '@/__generated__/schemas/UpdateParty';

export class PartyApi {
  constructor(
    private readonly r: HttpRequester
  ) {
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {CreateParty} payload - The `CreateParty` instance to use for the API request
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Party>>} A promise containing the HttpResponse of Party
   */
  async postParty(payload: CreateParty, gamertag?: string): Promise<HttpResponse<Party>> {
    let e = "/api/parties";
    
    // Make the API request
    return makeApiRequest<Party, CreateParty>({
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
   * @param {UpdateParty} payload - The `UpdateParty` instance to use for the API request
   * @param {string} id - Id of the party
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Party>>} A promise containing the HttpResponse of Party
   */
  async putPartyMetadataById(id: string, payload: UpdateParty, gamertag?: string): Promise<HttpResponse<Party>> {
    let e = "/api/parties/{id}/metadata".replace(objectIdPlaceholder, endpointEncoder(id));
    
    // Make the API request
    return makeApiRequest<Party, UpdateParty>({
      r: this.r,
      e,
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
   * @param {string} id - Id of the party
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Party>>} A promise containing the HttpResponse of Party
   */
  async getPartyById(id: string, gamertag?: string): Promise<HttpResponse<Party>> {
    let e = "/api/parties/{id}".replace(objectIdPlaceholder, endpointEncoder(id));
    
    // Make the API request
    return makeApiRequest<Party>({
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
   * @param {string} id - Id of the party
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Party>>} A promise containing the HttpResponse of Party
   */
  async putPartyById(id: string, gamertag?: string): Promise<HttpResponse<Party>> {
    let e = "/api/parties/{id}".replace(objectIdPlaceholder, endpointEncoder(id));
    
    // Make the API request
    return makeApiRequest<Party>({
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
   * @param {PromoteNewLeader} payload - The `PromoteNewLeader` instance to use for the API request
   * @param {string} id - Id of the party
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Party>>} A promise containing the HttpResponse of Party
   */
  async putPartyPromoteById(id: string, payload: PromoteNewLeader, gamertag?: string): Promise<HttpResponse<Party>> {
    let e = "/api/parties/{id}/promote".replace(objectIdPlaceholder, endpointEncoder(id));
    
    // Make the API request
    return makeApiRequest<Party, PromoteNewLeader>({
      r: this.r,
      e,
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
   * @param {InviteToParty} payload - The `InviteToParty` instance to use for the API request
   * @param {string} id - Id of the party
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<ApiPartiesInvitePostPartyResponse>>} A promise containing the HttpResponse of ApiPartiesInvitePostPartyResponse
   */
  async postPartyInviteById(id: string, payload: InviteToParty, gamertag?: string): Promise<HttpResponse<ApiPartiesInvitePostPartyResponse>> {
    let e = "/api/parties/{id}/invite".replace(objectIdPlaceholder, endpointEncoder(id));
    
    // Make the API request
    return makeApiRequest<ApiPartiesInvitePostPartyResponse, InviteToParty>({
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
   * @param {CancelInviteToParty} payload - The `CancelInviteToParty` instance to use for the API request
   * @param {string} id - Id of the party
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<ApiPartiesInviteDeletePartyResponse>>} A promise containing the HttpResponse of ApiPartiesInviteDeletePartyResponse
   */
  async deletePartyInviteById(id: string, payload: CancelInviteToParty, gamertag?: string): Promise<HttpResponse<ApiPartiesInviteDeletePartyResponse>> {
    let e = "/api/parties/{id}/invite".replace(objectIdPlaceholder, endpointEncoder(id));
    
    // Make the API request
    return makeApiRequest<ApiPartiesInviteDeletePartyResponse, CancelInviteToParty>({
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
   * @param {LeaveParty} payload - The `LeaveParty` instance to use for the API request
   * @param {string} id - Id of the party
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<ApiPartiesMembersDeletePartyResponse>>} A promise containing the HttpResponse of ApiPartiesMembersDeletePartyResponse
   */
  async deletePartyMembersById(id: string, payload: LeaveParty, gamertag?: string): Promise<HttpResponse<ApiPartiesMembersDeletePartyResponse>> {
    let e = "/api/parties/{id}/members".replace(objectIdPlaceholder, endpointEncoder(id));
    
    // Make the API request
    return makeApiRequest<ApiPartiesMembersDeletePartyResponse, LeaveParty>({
      r: this.r,
      e,
      m: DELETE,
      p: payload,
      g: gamertag,
      w: true
    });
  }
}
