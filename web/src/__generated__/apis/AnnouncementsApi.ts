import { AnnouncementContentResponse } from '@/__generated__/schemas/AnnouncementContentResponse';
import { AnnouncementDto } from '@/__generated__/schemas/AnnouncementDto';
import { AnnouncementQueryResponse } from '@/__generated__/schemas/AnnouncementQueryResponse';
import { AnnouncementRawResponse } from '@/__generated__/schemas/AnnouncementRawResponse';
import { AnnouncementRequest } from '@/__generated__/schemas/AnnouncementRequest';
import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { DeleteAnnouncementRequest } from '@/__generated__/schemas/DeleteAnnouncementRequest';
import { EmptyResponse } from '@/__generated__/schemas/EmptyResponse';
import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { ListDefinitionsResponse } from '@/__generated__/schemas/ListDefinitionsResponse';
import { ListTagsResponse } from '@/__generated__/schemas/ListTagsResponse';
import { makeQueryString } from '@/utils/makeQueryString';

export class AnnouncementsApi {
  constructor(
    private readonly requester: HttpRequester
  ) {
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {string} tagNameFilter - The `tagNameFilter` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ListTagsResponse>>} A promise containing the HttpResponse of ListTagsResponse
   */
  async getAnnouncementsListTags(tagNameFilter?: string, gamertag?: string): Promise<HttpResponse<ListTagsResponse>> {
    let endpoint = "/basic/announcements/list/tags";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      tagNameFilter
    });
    
    // Make the API request
    return this.requester.request<ListTagsResponse>({
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
   * @returns {Promise<HttpResponse<AnnouncementContentResponse>>} A promise containing the HttpResponse of AnnouncementContentResponse
   */
  async getAnnouncementsList(gamertag?: string): Promise<HttpResponse<AnnouncementContentResponse>> {
    let endpoint = "/basic/announcements/list";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<AnnouncementContentResponse>({
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
   * @param {string} date - The `date` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AnnouncementContentResponse>>} A promise containing the HttpResponse of AnnouncementContentResponse
   */
  async getAnnouncementsSearch(date?: string, gamertag?: string): Promise<HttpResponse<AnnouncementContentResponse>> {
    let endpoint = "/basic/announcements/search";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      date
    });
    
    // Make the API request
    return this.requester.request<AnnouncementContentResponse>({
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
   * @returns {Promise<HttpResponse<ListDefinitionsResponse>>} A promise containing the HttpResponse of ListDefinitionsResponse
   */
  async getAnnouncementsListDefinitions(gamertag?: string): Promise<HttpResponse<ListDefinitionsResponse>> {
    let endpoint = "/basic/announcements/list/definitions";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<ListDefinitionsResponse>({
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
   * @param {AnnouncementDto} payload - The `AnnouncementDto` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postAnnouncement(payload: AnnouncementDto, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let endpoint = "/basic/announcements/";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyResponse, AnnouncementDto>({
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
   * @param {DeleteAnnouncementRequest} payload - The `DeleteAnnouncementRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async deleteAnnouncement(payload: DeleteAnnouncementRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let endpoint = "/basic/announcements/";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyResponse, DeleteAnnouncementRequest>({
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
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AnnouncementContentResponse>>} A promise containing the HttpResponse of AnnouncementContentResponse
   */
  async getAnnouncementsContent(gamertag?: string): Promise<HttpResponse<AnnouncementContentResponse>> {
    let endpoint = "/basic/announcements/content";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<AnnouncementContentResponse>({
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
   * @param {AnnouncementRequest} payload - The `AnnouncementRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putAnnouncementReadByObjectId(objectId: bigint | string, payload: AnnouncementRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/object/announcements/{objectId}/read".replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, AnnouncementRequest>({
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
   * @param {AnnouncementRequest} payload - The `AnnouncementRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postAnnouncementClaimByObjectId(objectId: bigint | string, payload: AnnouncementRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/object/announcements/{objectId}/claim".replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, AnnouncementRequest>({
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
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AnnouncementRawResponse>>} A promise containing the HttpResponse of AnnouncementRawResponse
   */
  async getAnnouncementRawByObjectId(objectId: bigint | string, gamertag?: string): Promise<HttpResponse<AnnouncementRawResponse>> {
    let endpoint = "/object/announcements/{objectId}/raw".replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<AnnouncementRawResponse>({
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
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {boolean} include_deleted - The `include_deleted` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AnnouncementQueryResponse>>} A promise containing the HttpResponse of AnnouncementQueryResponse
   */
  async getAnnouncementByObjectId(objectId: bigint | string, include_deleted?: boolean, gamertag?: string): Promise<HttpResponse<AnnouncementQueryResponse>> {
    let endpoint = "/object/announcements/{objectId}/".replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      include_deleted
    });
    
    // Make the API request
    return this.requester.request<AnnouncementQueryResponse>({
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
   * @param {AnnouncementRequest} payload - The `AnnouncementRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async deleteAnnouncementByObjectId(objectId: bigint | string, payload: AnnouncementRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/object/announcements/{objectId}/".replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, AnnouncementRequest>({
      url: endpoint,
      method: HttpMethod.DELETE,
      headers,
      body: payload,
      withAuth: true
    });
  }
}
