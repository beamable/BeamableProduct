import { Acknowledge } from '@/__generated__/schemas/Acknowledge';
import { AddTags } from '@/__generated__/schemas/AddTags';
import { ApiLobbiesServerPostLobbyResponse } from '@/__generated__/schemas/ApiLobbiesServerPostLobbyResponse';
import { CreateFederatedGameServer } from '@/__generated__/schemas/CreateFederatedGameServer';
import { CreateLobby } from '@/__generated__/schemas/CreateLobby';
import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { JoinLobby } from '@/__generated__/schemas/JoinLobby';
import { Lobby } from '@/__generated__/schemas/Lobby';
import { LobbyQueryResponse } from '@/__generated__/schemas/LobbyQueryResponse';
import { makeQueryString } from '@/utils/makeQueryString';
import { RemoveFromLobby } from '@/__generated__/schemas/RemoveFromLobby';
import { RemoveTags } from '@/__generated__/schemas/RemoveTags';
import { UpdateLobby } from '@/__generated__/schemas/UpdateLobby';

export class LobbyApi {
  constructor(
    private readonly requester: HttpRequester
  ) {
  }
  
  /**
   * @param {number} Limit - The `Limit` parameter to include in the API request.
   * @param {string} MatchType - The `MatchType` parameter to include in the API request.
   * @param {number} Skip - The `Skip` parameter to include in the API request.
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<LobbyQueryResponse>>} A promise containing the HttpResponse of LobbyQueryResponse
   */
  async getLobbies(Limit?: number, MatchType?: string, Skip?: number, gamertag?: string): Promise<HttpResponse<LobbyQueryResponse>> {
    let endpoint = "/api/lobbies";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      Limit,
      MatchType,
      Skip
    });
    
    // Make the API request
    return this.requester.request<LobbyQueryResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {CreateLobby} payload - The `CreateLobby` instance to use for the API request
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Lobby>>} A promise containing the HttpResponse of Lobby
   */
  async postLobby(payload: CreateLobby, gamertag?: string): Promise<HttpResponse<Lobby>> {
    let endpoint = "/api/lobbies";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<Lobby, CreateLobby>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {string} id - The lobby id.
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Lobby>>} A promise containing the HttpResponse of Lobby
   */
  async getLobbyById(id: string, gamertag?: string): Promise<HttpResponse<Lobby>> {
    let endpoint = "/api/lobbies/{id}".replace("{id}", encodeURIComponent(id.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<Lobby>({
      url: endpoint,
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {JoinLobby} payload - The `JoinLobby` instance to use for the API request
   * @param {string} id - Id of the lobby
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Lobby>>} A promise containing the HttpResponse of Lobby
   */
  async putLobbyById(id: string, payload: JoinLobby, gamertag?: string): Promise<HttpResponse<Lobby>> {
    let endpoint = "/api/lobbies/{id}".replace("{id}", encodeURIComponent(id.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<Lobby, JoinLobby>({
      url: endpoint,
      method: HttpMethod.PUT,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {RemoveFromLobby} payload - The `RemoveFromLobby` instance to use for the API request
   * @param {string} id - Id of the lobby
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Acknowledge>>} A promise containing the HttpResponse of Acknowledge
   */
  async deleteLobbyById(id: string, payload: RemoveFromLobby, gamertag?: string): Promise<HttpResponse<Acknowledge>> {
    let endpoint = "/api/lobbies/{id}".replace("{id}", encodeURIComponent(id.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<Acknowledge, RemoveFromLobby>({
      url: endpoint,
      method: HttpMethod.DELETE,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {JoinLobby} payload - The `JoinLobby` instance to use for the API request
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Lobby>>} A promise containing the HttpResponse of Lobby
   */
  async putLobbyPasscode(payload: JoinLobby, gamertag?: string): Promise<HttpResponse<Lobby>> {
    let endpoint = "/api/lobbies/passcode";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<Lobby, JoinLobby>({
      url: endpoint,
      method: HttpMethod.PUT,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {UpdateLobby} payload - The `UpdateLobby` instance to use for the API request
   * @param {string} id - Id of the lobby
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Lobby>>} A promise containing the HttpResponse of Lobby
   */
  async putLobbyMetadataById(id: string, payload: UpdateLobby, gamertag?: string): Promise<HttpResponse<Lobby>> {
    let endpoint = "/api/lobbies/{id}/metadata".replace("{id}", encodeURIComponent(id.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<Lobby, UpdateLobby>({
      url: endpoint,
      method: HttpMethod.PUT,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {AddTags} payload - The `AddTags` instance to use for the API request
   * @param {string} id - Id of the lobby
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Lobby>>} A promise containing the HttpResponse of Lobby
   */
  async putLobbyTagsById(id: string, payload: AddTags, gamertag?: string): Promise<HttpResponse<Lobby>> {
    let endpoint = "/api/lobbies/{id}/tags".replace("{id}", encodeURIComponent(id.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<Lobby, AddTags>({
      url: endpoint,
      method: HttpMethod.PUT,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {RemoveTags} payload - The `RemoveTags` instance to use for the API request
   * @param {string} id - Id of the lobby
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Lobby>>} A promise containing the HttpResponse of Lobby
   */
  async deleteLobbyTagsById(id: string, payload: RemoveTags, gamertag?: string): Promise<HttpResponse<Lobby>> {
    let endpoint = "/api/lobbies/{id}/tags".replace("{id}", encodeURIComponent(id.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<Lobby, RemoveTags>({
      url: endpoint,
      method: HttpMethod.DELETE,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {CreateFederatedGameServer} payload - The `CreateFederatedGameServer` instance to use for the API request
   * @param {string} id - Id of the lobby
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<ApiLobbiesServerPostLobbyResponse>>} A promise containing the HttpResponse of ApiLobbiesServerPostLobbyResponse
   */
  async postLobbyServerById(id: string, payload: CreateFederatedGameServer, gamertag?: string): Promise<HttpResponse<ApiLobbiesServerPostLobbyResponse>> {
    let endpoint = "/api/lobbies/{id}/server".replace("{id}", encodeURIComponent(id.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<ApiLobbiesServerPostLobbyResponse, CreateFederatedGameServer>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
}
