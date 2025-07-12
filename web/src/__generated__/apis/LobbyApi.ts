import { Acknowledge } from '@/__generated__/schemas/Acknowledge';
import { AddTags } from '@/__generated__/schemas/AddTags';
import { ApiLobbiesServerPostLobbyResponse } from '@/__generated__/schemas/ApiLobbiesServerPostLobbyResponse';
import { CreateFederatedGameServer } from '@/__generated__/schemas/CreateFederatedGameServer';
import { CreateLobby } from '@/__generated__/schemas/CreateLobby';
import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpResponse } from '@/network/http/types/HttpResponse';
import { idPlaceholder } from '@/__generated__/apis/constants';
import { JoinLobby } from '@/__generated__/schemas/JoinLobby';
import { Lobby } from '@/__generated__/schemas/Lobby';
import { LobbyQueryResponse } from '@/__generated__/schemas/LobbyQueryResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import { RemoveFromLobby } from '@/__generated__/schemas/RemoveFromLobby';
import { RemoveTags } from '@/__generated__/schemas/RemoveTags';
import { UpdateLobby } from '@/__generated__/schemas/UpdateLobby';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param Limit - The `Limit` parameter to include in the API request.
 * @param MatchType - The `MatchType` parameter to include in the API request.
 * @param Skip - The `Skip` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function getLobbies(requester: HttpRequester, Limit?: number, MatchType?: string, Skip?: number, gamertag?: string): Promise<HttpResponse<LobbyQueryResponse>> {
  let endpoint = "/api/lobbies";
  
  // Make the API request
  return makeApiRequest<LobbyQueryResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      Limit,
      MatchType,
      Skip
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
 * @param payload - The `CreateLobby` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function postLobby(requester: HttpRequester, payload: CreateLobby, gamertag?: string): Promise<HttpResponse<Lobby>> {
  let endpoint = "/api/lobbies";
  
  // Make the API request
  return makeApiRequest<Lobby, CreateLobby>({
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
 * @param id - The lobby id.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function getLobbyById(requester: HttpRequester, id: string, gamertag?: string): Promise<HttpResponse<Lobby>> {
  let endpoint = "/api/lobbies/{id}".replace(idPlaceholder, endpointEncoder(id));
  
  // Make the API request
  return makeApiRequest<Lobby>({
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
 * @param payload - The `JoinLobby` instance to use for the API request
 * @param id - Id of the lobby
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function putLobbyById(requester: HttpRequester, id: string, payload: JoinLobby, gamertag?: string): Promise<HttpResponse<Lobby>> {
  let endpoint = "/api/lobbies/{id}".replace(idPlaceholder, endpointEncoder(id));
  
  // Make the API request
  return makeApiRequest<Lobby, JoinLobby>({
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
 * @param payload - The `RemoveFromLobby` instance to use for the API request
 * @param id - Id of the lobby
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function deleteLobbyById(requester: HttpRequester, id: string, payload: RemoveFromLobby, gamertag?: string): Promise<HttpResponse<Acknowledge>> {
  let endpoint = "/api/lobbies/{id}".replace(idPlaceholder, endpointEncoder(id));
  
  // Make the API request
  return makeApiRequest<Acknowledge, RemoveFromLobby>({
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
 * @param payload - The `JoinLobby` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function putLobbyPasscode(requester: HttpRequester, payload: JoinLobby, gamertag?: string): Promise<HttpResponse<Lobby>> {
  let endpoint = "/api/lobbies/passcode";
  
  // Make the API request
  return makeApiRequest<Lobby, JoinLobby>({
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
 * @param payload - The `UpdateLobby` instance to use for the API request
 * @param id - Id of the lobby
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function putLobbyMetadataById(requester: HttpRequester, id: string, payload: UpdateLobby, gamertag?: string): Promise<HttpResponse<Lobby>> {
  let endpoint = "/api/lobbies/{id}/metadata".replace(idPlaceholder, endpointEncoder(id));
  
  // Make the API request
  return makeApiRequest<Lobby, UpdateLobby>({
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
 * @param payload - The `AddTags` instance to use for the API request
 * @param id - Id of the lobby
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function putLobbyTagsById(requester: HttpRequester, id: string, payload: AddTags, gamertag?: string): Promise<HttpResponse<Lobby>> {
  let endpoint = "/api/lobbies/{id}/tags".replace(idPlaceholder, endpointEncoder(id));
  
  // Make the API request
  return makeApiRequest<Lobby, AddTags>({
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
 * @param payload - The `RemoveTags` instance to use for the API request
 * @param id - Id of the lobby
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function deleteLobbyTagsById(requester: HttpRequester, id: string, payload: RemoveTags, gamertag?: string): Promise<HttpResponse<Lobby>> {
  let endpoint = "/api/lobbies/{id}/tags".replace(idPlaceholder, endpointEncoder(id));
  
  // Make the API request
  return makeApiRequest<Lobby, RemoveTags>({
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
 * @param payload - The `CreateFederatedGameServer` instance to use for the API request
 * @param id - Id of the lobby
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function postLobbyServerById(requester: HttpRequester, id: string, payload: CreateFederatedGameServer, gamertag?: string): Promise<HttpResponse<ApiLobbiesServerPostLobbyResponse>> {
  let endpoint = "/api/lobbies/{id}/server".replace(idPlaceholder, endpointEncoder(id));
  
  // Make the API request
  return makeApiRequest<ApiLobbiesServerPostLobbyResponse, CreateFederatedGameServer>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}
