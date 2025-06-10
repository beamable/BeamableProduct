import { ApiPartiesInviteDeletePartyResponse } from '@/__generated__/schemas/ApiPartiesInviteDeletePartyResponse';
import { ApiPartiesInvitePostPartyResponse } from '@/__generated__/schemas/ApiPartiesInvitePostPartyResponse';
import { ApiPartiesMembersDeletePartyResponse } from '@/__generated__/schemas/ApiPartiesMembersDeletePartyResponse';
import { CancelInviteToParty } from '@/__generated__/schemas/CancelInviteToParty';
import { CreateParty } from '@/__generated__/schemas/CreateParty';
import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { InviteToParty } from '@/__generated__/schemas/InviteToParty';
import { LeaveParty } from '@/__generated__/schemas/LeaveParty';
import { Party } from '@/__generated__/schemas/Party';
import { PromoteNewLeader } from '@/__generated__/schemas/PromoteNewLeader';
import { UpdateParty } from '@/__generated__/schemas/UpdateParty';

export class PartyApi {
  constructor(
    private readonly requester: HttpRequester
  ) {
  }
  
  /**
   * @param {CreateParty} payload - The `CreateParty` instance to use for the API request
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Party>>} A promise containing the HttpResponse of Party
   */
  async postParty(payload: CreateParty, gamertag?: string): Promise<HttpResponse<Party>> {
    let endpoint = "/api/parties";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<Party, CreateParty>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {UpdateParty} payload - The `UpdateParty` instance to use for the API request
   * @param {string} id - Id of the party
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Party>>} A promise containing the HttpResponse of Party
   */
  async putPartyMetadataById(id: string, payload: UpdateParty, gamertag?: string): Promise<HttpResponse<Party>> {
    let endpoint = "/api/parties/{id}/metadata";
    endpoint = endpoint.replace("{id}", encodeURIComponent(id.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<Party, UpdateParty>({
      url: endpoint,
      method: HttpMethod.PUT,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {string} id - Id of the party
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Party>>} A promise containing the HttpResponse of Party
   */
  async getPartyById(id: string, gamertag?: string): Promise<HttpResponse<Party>> {
    let endpoint = "/api/parties/{id}";
    endpoint = endpoint.replace("{id}", encodeURIComponent(id.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<Party>({
      url: endpoint,
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {string} id - Id of the party
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Party>>} A promise containing the HttpResponse of Party
   */
  async putPartyById(id: string, gamertag?: string): Promise<HttpResponse<Party>> {
    let endpoint = "/api/parties/{id}";
    endpoint = endpoint.replace("{id}", encodeURIComponent(id.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<Party>({
      url: endpoint,
      method: HttpMethod.PUT,
      headers
    });
  }
  
  /**
   * @param {PromoteNewLeader} payload - The `PromoteNewLeader` instance to use for the API request
   * @param {string} id - Id of the party
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Party>>} A promise containing the HttpResponse of Party
   */
  async putPartyPromoteById(id: string, payload: PromoteNewLeader, gamertag?: string): Promise<HttpResponse<Party>> {
    let endpoint = "/api/parties/{id}/promote";
    endpoint = endpoint.replace("{id}", encodeURIComponent(id.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<Party, PromoteNewLeader>({
      url: endpoint,
      method: HttpMethod.PUT,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {InviteToParty} payload - The `InviteToParty` instance to use for the API request
   * @param {string} id - Id of the party
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<ApiPartiesInvitePostPartyResponse>>} A promise containing the HttpResponse of ApiPartiesInvitePostPartyResponse
   */
  async postPartyInviteById(id: string, payload: InviteToParty, gamertag?: string): Promise<HttpResponse<ApiPartiesInvitePostPartyResponse>> {
    let endpoint = "/api/parties/{id}/invite";
    endpoint = endpoint.replace("{id}", encodeURIComponent(id.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<ApiPartiesInvitePostPartyResponse, InviteToParty>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {CancelInviteToParty} payload - The `CancelInviteToParty` instance to use for the API request
   * @param {string} id - Id of the party
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<ApiPartiesInviteDeletePartyResponse>>} A promise containing the HttpResponse of ApiPartiesInviteDeletePartyResponse
   */
  async deletePartyInviteById(id: string, payload: CancelInviteToParty, gamertag?: string): Promise<HttpResponse<ApiPartiesInviteDeletePartyResponse>> {
    let endpoint = "/api/parties/{id}/invite";
    endpoint = endpoint.replace("{id}", encodeURIComponent(id.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<ApiPartiesInviteDeletePartyResponse, CancelInviteToParty>({
      url: endpoint,
      method: HttpMethod.DELETE,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {LeaveParty} payload - The `LeaveParty` instance to use for the API request
   * @param {string} id - Id of the party
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<ApiPartiesMembersDeletePartyResponse>>} A promise containing the HttpResponse of ApiPartiesMembersDeletePartyResponse
   */
  async deletePartyMembersById(id: string, payload: LeaveParty, gamertag?: string): Promise<HttpResponse<ApiPartiesMembersDeletePartyResponse>> {
    let endpoint = "/api/parties/{id}/members";
    endpoint = endpoint.replace("{id}", encodeURIComponent(id.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<ApiPartiesMembersDeletePartyResponse, LeaveParty>({
      url: endpoint,
      method: HttpMethod.DELETE,
      headers,
      body: payload
    });
  }
}
