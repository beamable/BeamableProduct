import { ArchiveOrUnarchiveManifestsRequest } from '@/__generated__/schemas/ArchiveOrUnarchiveManifestsRequest';
import { ClientManifestJsonResponse } from '@/__generated__/schemas/ClientManifestJsonResponse';
import { ClientManifestResponse } from '@/__generated__/schemas/ClientManifestResponse';
import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { ContentBasicGetManifestsResponse } from '@/__generated__/schemas/ContentBasicGetManifestsResponse';
import { ContentBasicManifest } from '@/__generated__/schemas/ContentBasicManifest';
import { ContentBasicManifestChecksum } from '@/__generated__/schemas/ContentBasicManifestChecksum';
import { ContentBasicManifestChecksums } from '@/__generated__/schemas/ContentBasicManifestChecksums';
import { ContentOrText } from '@/__generated__/schemas/ContentOrText';
import { DeleteLocalizationRequest } from '@/__generated__/schemas/DeleteLocalizationRequest';
import { EmptyResponse } from '@/__generated__/schemas/EmptyResponse';
import { GetLocalizationsResponse } from '@/__generated__/schemas/GetLocalizationsResponse';
import { GetManifestHistoryResponse } from '@/__generated__/schemas/GetManifestHistoryResponse';
import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { makeQueryString } from '@/utils/makeQueryString';
import { PullAllManifestsRequest } from '@/__generated__/schemas/PullAllManifestsRequest';
import { PullManifestRequest } from '@/__generated__/schemas/PullManifestRequest';
import { PutLocalizationsRequest } from '@/__generated__/schemas/PutLocalizationsRequest';
import { RepeatManifestRequest } from '@/__generated__/schemas/RepeatManifestRequest';
import { SaveBinaryRequest } from '@/__generated__/schemas/SaveBinaryRequest';
import { SaveBinaryResponse } from '@/__generated__/schemas/SaveBinaryResponse';
import { SaveContentRequest } from '@/__generated__/schemas/SaveContentRequest';
import { SaveContentResponse } from '@/__generated__/schemas/SaveContentResponse';
import { SaveManifestRequest } from '@/__generated__/schemas/SaveManifestRequest';
import { SaveTextRequest } from '@/__generated__/schemas/SaveTextRequest';
import { SaveTextResponse } from '@/__generated__/schemas/SaveTextResponse';

export class ContentApi {
  constructor(
    private readonly requester: HttpRequester
  ) {
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {ArchiveOrUnarchiveManifestsRequest} payload - The `ArchiveOrUnarchiveManifestsRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postContentManifestsUnarchive(payload: ArchiveOrUnarchiveManifestsRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let endpoint = "/basic/content/manifests/unarchive";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyResponse, ArchiveOrUnarchiveManifestsRequest>({
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
   * @param {PullManifestRequest} payload - The `PullManifestRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ContentBasicManifest>>} A promise containing the HttpResponse of ContentBasicManifest
   */
  async postContentManifestPull(payload: PullManifestRequest, gamertag?: string): Promise<HttpResponse<ContentBasicManifest>> {
    let endpoint = "/basic/content/manifest/pull";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<ContentBasicManifest, PullManifestRequest>({
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
   * @param {string} id - The `id` parameter to include in the API request.
   * @param {number} limit - The `limit` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetManifestHistoryResponse>>} A promise containing the HttpResponse of GetManifestHistoryResponse
   */
  async getContentManifestHistory(id?: string, limit?: number, gamertag?: string): Promise<HttpResponse<GetManifestHistoryResponse>> {
    let endpoint = "/basic/content/manifest/history";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      id,
      limit
    });
    
    // Make the API request
    return this.requester.request<GetManifestHistoryResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers,
      withAuth: true
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {SaveBinaryRequest} payload - The `SaveBinaryRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<SaveBinaryResponse>>} A promise containing the HttpResponse of SaveBinaryResponse
   */
  async postContentBinary(payload: SaveBinaryRequest, gamertag?: string): Promise<HttpResponse<SaveBinaryResponse>> {
    let endpoint = "/basic/content/binary";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<SaveBinaryResponse, SaveBinaryRequest>({
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
   * @param {PullAllManifestsRequest} payload - The `PullAllManifestsRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ContentBasicManifestChecksums>>} A promise containing the HttpResponse of ContentBasicManifestChecksums
   */
  async postContentManifestsPull(payload: PullAllManifestsRequest, gamertag?: string): Promise<HttpResponse<ContentBasicManifestChecksums>> {
    let endpoint = "/basic/content/manifests/pull";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<ContentBasicManifestChecksums, PullAllManifestsRequest>({
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
   * @param {string} contentId - The `contentId` parameter to include in the API request.
   * @param {string} version - The `version` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ContentOrText>>} A promise containing the HttpResponse of ContentOrText
   */
  async getContent(contentId: string, version: string, gamertag?: string): Promise<HttpResponse<ContentOrText>> {
    let endpoint = "/basic/content/content";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      contentId,
      version
    });
    
    // Make the API request
    return this.requester.request<ContentOrText>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers,
      withAuth: true
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetLocalizationsResponse>>} A promise containing the HttpResponse of GetLocalizationsResponse
   */
  async getContentLocalizations(gamertag?: string): Promise<HttpResponse<GetLocalizationsResponse>> {
    let endpoint = "/basic/content/localizations";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<GetLocalizationsResponse>({
      url: endpoint,
      method: HttpMethod.GET,
      headers,
      withAuth: true
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {PutLocalizationsRequest} payload - The `PutLocalizationsRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putContentLocalizations(payload: PutLocalizationsRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/basic/content/localizations";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, PutLocalizationsRequest>({
      url: endpoint,
      method: HttpMethod.PUT,
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
   * @param {DeleteLocalizationRequest} payload - The `DeleteLocalizationRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async deleteContentLocalizations(payload: DeleteLocalizationRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/basic/content/localizations";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, DeleteLocalizationRequest>({
      url: endpoint,
      method: HttpMethod.DELETE,
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
   * @param {SaveTextRequest} payload - The `SaveTextRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<SaveTextResponse>>} A promise containing the HttpResponse of SaveTextResponse
   */
  async postContentText(payload: SaveTextRequest, gamertag?: string): Promise<HttpResponse<SaveTextResponse>> {
    let endpoint = "/basic/content/text";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<SaveTextResponse, SaveTextRequest>({
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
   * @param {string} uid - The `uid` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ContentBasicManifest>>} A promise containing the HttpResponse of ContentBasicManifest
   */
  async getContentManifestExact(uid: string, gamertag?: string): Promise<HttpResponse<ContentBasicManifest>> {
    let endpoint = "/basic/content/manifest/exact";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      uid
    });
    
    // Make the API request
    return this.requester.request<ContentBasicManifest>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers,
      withAuth: true
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {string} id - ID of the content manifest
   * @param {string} uid - UID of the content manifest
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ContentBasicManifest>>} A promise containing the HttpResponse of ContentBasicManifest
   */
  async getContentManifest(id?: string, uid?: string, gamertag?: string): Promise<HttpResponse<ContentBasicManifest>> {
    let endpoint = "/basic/content/manifest";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      id,
      uid
    });
    
    // Make the API request
    return this.requester.request<ContentBasicManifest>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers,
      withAuth: true
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {SaveManifestRequest} payload - The `SaveManifestRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ContentBasicManifest>>} A promise containing the HttpResponse of ContentBasicManifest
   */
  async postContentManifest(payload: SaveManifestRequest, gamertag?: string): Promise<HttpResponse<ContentBasicManifest>> {
    let endpoint = "/basic/content/manifest";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<ContentBasicManifest, SaveManifestRequest>({
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
   * @param {ArchiveOrUnarchiveManifestsRequest} payload - The `ArchiveOrUnarchiveManifestsRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postContentManifestsArchive(payload: ArchiveOrUnarchiveManifestsRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let endpoint = "/basic/content/manifests/archive";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyResponse, ArchiveOrUnarchiveManifestsRequest>({
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
   * @param {SaveContentRequest} payload - The `SaveContentRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<SaveContentResponse>>} A promise containing the HttpResponse of SaveContentResponse
   */
  async postContent(payload: SaveContentRequest, gamertag?: string): Promise<HttpResponse<SaveContentResponse>> {
    let endpoint = "/basic/content/";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<SaveContentResponse, SaveContentRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {string} id - ID of the content manifest
   * @param {string} uid - UID of the content manifest
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ClientManifestResponse>>} A promise containing the HttpResponse of ClientManifestResponse
   */
  async getContentManifestPublic(id?: string, uid?: string, gamertag?: string): Promise<HttpResponse<ClientManifestResponse>> {
    let endpoint = "/basic/content/manifest/public";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      id,
      uid
    });
    
    // Make the API request
    return this.requester.request<ClientManifestResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {string} id - Content ID of the content manifest
   * @param {string} uid - UID of the content manifest
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ClientManifestJsonResponse>>} A promise containing the HttpResponse of ClientManifestJsonResponse
   */
  async getContentManifestPublicJson(id?: string, uid?: string, gamertag?: string): Promise<HttpResponse<ClientManifestJsonResponse>> {
    let endpoint = "/basic/content/manifest/public/json";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      id,
      uid
    });
    
    // Make the API request
    return this.requester.request<ClientManifestJsonResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {RepeatManifestRequest} payload - The `RepeatManifestRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putContentManifestRepeat(payload: RepeatManifestRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/basic/content/manifest/repeat";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, RepeatManifestRequest>({
      url: endpoint,
      method: HttpMethod.PUT,
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
   * @param {string} id - Content ID of the content manifest
   * @param {string} uid - UID of the content manifest
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ClientManifestJsonResponse>>} A promise containing the HttpResponse of ClientManifestJsonResponse
   */
  async getContentManifestPrivateJson(id?: string, uid?: string, gamertag?: string): Promise<HttpResponse<ClientManifestJsonResponse>> {
    let endpoint = "/basic/content/manifest/private/json";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      id,
      uid
    });
    
    // Make the API request
    return this.requester.request<ClientManifestJsonResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers,
      withAuth: true
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {string} id - ID of the content manifest
   * @param {string} uid - UID of the content manifest
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ClientManifestResponse>>} A promise containing the HttpResponse of ClientManifestResponse
   */
  async getContentManifestPrivate(id?: string, uid?: string, gamertag?: string): Promise<HttpResponse<ClientManifestResponse>> {
    let endpoint = "/basic/content/manifest/private";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      id,
      uid
    });
    
    // Make the API request
    return this.requester.request<ClientManifestResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers,
      withAuth: true
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ContentBasicManifestChecksums>>} A promise containing the HttpResponse of ContentBasicManifestChecksums
   */
  async getContentManifestChecksums(gamertag?: string): Promise<HttpResponse<ContentBasicManifestChecksums>> {
    let endpoint = "/basic/content/manifest/checksums";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<ContentBasicManifestChecksums>({
      url: endpoint,
      method: HttpMethod.GET,
      headers,
      withAuth: true
    });
  }
  
  /**
   * @param {string} id - ID of the content manifest
   * @param {string} uid - UID of the content manifest
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ContentBasicManifestChecksum>>} A promise containing the HttpResponse of ContentBasicManifestChecksum
   */
  async getContentManifestChecksum(id?: string, uid?: string, gamertag?: string): Promise<HttpResponse<ContentBasicManifestChecksum>> {
    let endpoint = "/basic/content/manifest/checksum";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      id,
      uid
    });
    
    // Make the API request
    return this.requester.request<ContentBasicManifestChecksum>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ContentBasicGetManifestsResponse>>} A promise containing the HttpResponse of ContentBasicGetManifestsResponse
   */
  async getContentManifests(gamertag?: string): Promise<HttpResponse<ContentBasicGetManifestsResponse>> {
    let endpoint = "/basic/content/manifests";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<ContentBasicGetManifestsResponse>({
      url: endpoint,
      method: HttpMethod.GET,
      headers,
      withAuth: true
    });
  }
}
