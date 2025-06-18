import { Acknowledge } from '@/__generated__/schemas/Acknowledge';
import { AddTags } from '@/__generated__/schemas/AddTags';
import { ApiLobbiesServerPostLobbyResponse } from '@/__generated__/schemas/ApiLobbiesServerPostLobbyResponse';
import { CreateFederatedGameServer } from '@/__generated__/schemas/CreateFederatedGameServer';
import { CreateLobby } from '@/__generated__/schemas/CreateLobby';
import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { JoinLobby } from '@/__generated__/schemas/JoinLobby';
import { Lobby } from '@/__generated__/schemas/Lobby';
import { LobbyQueryResponse } from '@/__generated__/schemas/LobbyQueryResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/constants';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import { RemoveFromLobby } from '@/__generated__/schemas/RemoveFromLobby';
import { RemoveTags } from '@/__generated__/schemas/RemoveTags';
import { UpdateLobby } from '@/__generated__/schemas/UpdateLobby';

export class LobbyApi {
  constructor(
    private readonly r: HttpRequester
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
    let e = "/api/lobbies";
    
    // Make the API request
    return makeApiRequest<LobbyQueryResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        Limit,
        MatchType,
        Skip
      },
      g: gamertag
    });
  }
  
  /**
   * @param {CreateLobby} payload - The `CreateLobby` instance to use for the API request
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Lobby>>} A promise containing the HttpResponse of Lobby
   */
  async postLobby(payload: CreateLobby, gamertag?: string): Promise<HttpResponse<Lobby>> {
    let e = "/api/lobbies";
    
    // Make the API request
    return makeApiRequest<Lobby, CreateLobby>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @param {string} id - The lobby id.
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Lobby>>} A promise containing the HttpResponse of Lobby
   */
  async getLobbyById(id: string, gamertag?: string): Promise<HttpResponse<Lobby>> {
    let e = "/api/lobbies/{id}".replace(objectIdPlaceholder, endpointEncoder(id));
    
    // Make the API request
    return makeApiRequest<Lobby>({
      r: this.r,
      e,
      m: GET,
      g: gamertag
    });
  }
  
  /**
   * @param {JoinLobby} payload - The `JoinLobby` instance to use for the API request
   * @param {string} id - Id of the lobby
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Lobby>>} A promise containing the HttpResponse of Lobby
   */
  async putLobbyById(id: string, payload: JoinLobby, gamertag?: string): Promise<HttpResponse<Lobby>> {
    let e = "/api/lobbies/{id}".replace(objectIdPlaceholder, endpointEncoder(id));
    
    // Make the API request
    return makeApiRequest<Lobby, JoinLobby>({
      r: this.r,
      e,
      m: PUT,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @param {RemoveFromLobby} payload - The `RemoveFromLobby` instance to use for the API request
   * @param {string} id - Id of the lobby
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Acknowledge>>} A promise containing the HttpResponse of Acknowledge
   */
  async deleteLobbyById(id: string, payload: RemoveFromLobby, gamertag?: string): Promise<HttpResponse<Acknowledge>> {
    let e = "/api/lobbies/{id}".replace(objectIdPlaceholder, endpointEncoder(id));
    
    // Make the API request
    return makeApiRequest<Acknowledge, RemoveFromLobby>({
      r: this.r,
      e,
      m: DELETE,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @param {JoinLobby} payload - The `JoinLobby` instance to use for the API request
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Lobby>>} A promise containing the HttpResponse of Lobby
   */
  async putLobbyPasscode(payload: JoinLobby, gamertag?: string): Promise<HttpResponse<Lobby>> {
    let e = "/api/lobbies/passcode";
    
    // Make the API request
    return makeApiRequest<Lobby, JoinLobby>({
      r: this.r,
      e,
      m: PUT,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @param {UpdateLobby} payload - The `UpdateLobby` instance to use for the API request
   * @param {string} id - Id of the lobby
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Lobby>>} A promise containing the HttpResponse of Lobby
   */
  async putLobbyMetadataById(id: string, payload: UpdateLobby, gamertag?: string): Promise<HttpResponse<Lobby>> {
    let e = "/api/lobbies/{id}/metadata".replace(objectIdPlaceholder, endpointEncoder(id));
    
    // Make the API request
    return makeApiRequest<Lobby, UpdateLobby>({
      r: this.r,
      e,
      m: PUT,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @param {AddTags} payload - The `AddTags` instance to use for the API request
   * @param {string} id - Id of the lobby
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Lobby>>} A promise containing the HttpResponse of Lobby
   */
  async putLobbyTagsById(id: string, payload: AddTags, gamertag?: string): Promise<HttpResponse<Lobby>> {
    let e = "/api/lobbies/{id}/tags".replace(objectIdPlaceholder, endpointEncoder(id));
    
    // Make the API request
    return makeApiRequest<Lobby, AddTags>({
      r: this.r,
      e,
      m: PUT,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @param {RemoveTags} payload - The `RemoveTags` instance to use for the API request
   * @param {string} id - Id of the lobby
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<Lobby>>} A promise containing the HttpResponse of Lobby
   */
  async deleteLobbyTagsById(id: string, payload: RemoveTags, gamertag?: string): Promise<HttpResponse<Lobby>> {
    let e = "/api/lobbies/{id}/tags".replace(objectIdPlaceholder, endpointEncoder(id));
    
    // Make the API request
    return makeApiRequest<Lobby, RemoveTags>({
      r: this.r,
      e,
      m: DELETE,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @param {CreateFederatedGameServer} payload - The `CreateFederatedGameServer` instance to use for the API request
   * @param {string} id - Id of the lobby
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<ApiLobbiesServerPostLobbyResponse>>} A promise containing the HttpResponse of ApiLobbiesServerPostLobbyResponse
   */
  async postLobbyServerById(id: string, payload: CreateFederatedGameServer, gamertag?: string): Promise<HttpResponse<ApiLobbiesServerPostLobbyResponse>> {
    let e = "/api/lobbies/{id}/server".replace(objectIdPlaceholder, endpointEncoder(id));
    
    // Make the API request
    return makeApiRequest<ApiLobbiesServerPostLobbyResponse, CreateFederatedGameServer>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
}
