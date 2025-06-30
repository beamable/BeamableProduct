import { AnnouncementContentResponse } from '@/__generated__/schemas/AnnouncementContentResponse';
import { AnnouncementDto } from '@/__generated__/schemas/AnnouncementDto';
import { AnnouncementQueryResponse } from '@/__generated__/schemas/AnnouncementQueryResponse';
import { AnnouncementRawResponse } from '@/__generated__/schemas/AnnouncementRawResponse';
import { AnnouncementRequest } from '@/__generated__/schemas/AnnouncementRequest';
import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { DELETE } from '@/constants';
import { DeleteAnnouncementRequest } from '@/__generated__/schemas/DeleteAnnouncementRequest';
import { EmptyResponse } from '@/__generated__/schemas/EmptyResponse';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpResponse } from '@/network/http/types/HttpResponse';
import { ListDefinitionsResponse } from '@/__generated__/schemas/ListDefinitionsResponse';
import { ListTagsResponse } from '@/__generated__/schemas/ListTagsResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/constants';
import { POST } from '@/constants';
import { PUT } from '@/constants';

export class AnnouncementsApi {
  constructor(
    private readonly r: HttpRequester
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
    let e = "/basic/announcements/list/tags";
    
    // Make the API request
    return makeApiRequest<ListTagsResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        tagNameFilter
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
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AnnouncementContentResponse>>} A promise containing the HttpResponse of AnnouncementContentResponse
   */
  async getAnnouncementsList(gamertag?: string): Promise<HttpResponse<AnnouncementContentResponse>> {
    let e = "/basic/announcements/list";
    
    // Make the API request
    return makeApiRequest<AnnouncementContentResponse>({
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
   * @param {string} date - The `date` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AnnouncementContentResponse>>} A promise containing the HttpResponse of AnnouncementContentResponse
   */
  async getAnnouncementsSearch(date?: string, gamertag?: string): Promise<HttpResponse<AnnouncementContentResponse>> {
    let e = "/basic/announcements/search";
    
    // Make the API request
    return makeApiRequest<AnnouncementContentResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        date
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
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ListDefinitionsResponse>>} A promise containing the HttpResponse of ListDefinitionsResponse
   */
  async getAnnouncementsListDefinitions(gamertag?: string): Promise<HttpResponse<ListDefinitionsResponse>> {
    let e = "/basic/announcements/list/definitions";
    
    // Make the API request
    return makeApiRequest<ListDefinitionsResponse>({
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
   * @param {AnnouncementDto} payload - The `AnnouncementDto` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postAnnouncement(payload: AnnouncementDto, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let e = "/basic/announcements/";
    
    // Make the API request
    return makeApiRequest<EmptyResponse, AnnouncementDto>({
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
   * @param {DeleteAnnouncementRequest} payload - The `DeleteAnnouncementRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async deleteAnnouncement(payload: DeleteAnnouncementRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let e = "/basic/announcements/";
    
    // Make the API request
    return makeApiRequest<EmptyResponse, DeleteAnnouncementRequest>({
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
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AnnouncementContentResponse>>} A promise containing the HttpResponse of AnnouncementContentResponse
   */
  async getAnnouncementsContent(gamertag?: string): Promise<HttpResponse<AnnouncementContentResponse>> {
    let e = "/basic/announcements/content";
    
    // Make the API request
    return makeApiRequest<AnnouncementContentResponse>({
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
   * @param {AnnouncementRequest} payload - The `AnnouncementRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putAnnouncementReadByObjectId(objectId: bigint | string, payload: AnnouncementRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/object/announcements/{objectId}/read".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CommonResponse, AnnouncementRequest>({
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
   * @param {AnnouncementRequest} payload - The `AnnouncementRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postAnnouncementClaimByObjectId(objectId: bigint | string, payload: AnnouncementRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/object/announcements/{objectId}/claim".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CommonResponse, AnnouncementRequest>({
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
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AnnouncementRawResponse>>} A promise containing the HttpResponse of AnnouncementRawResponse
   */
  async getAnnouncementRawByObjectId(objectId: bigint | string, gamertag?: string): Promise<HttpResponse<AnnouncementRawResponse>> {
    let e = "/object/announcements/{objectId}/raw".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<AnnouncementRawResponse>({
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
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {boolean} include_deleted - The `include_deleted` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AnnouncementQueryResponse>>} A promise containing the HttpResponse of AnnouncementQueryResponse
   */
  async getAnnouncementByObjectId(objectId: bigint | string, include_deleted?: boolean, gamertag?: string): Promise<HttpResponse<AnnouncementQueryResponse>> {
    let e = "/object/announcements/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<AnnouncementQueryResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        include_deleted
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
   * @param {AnnouncementRequest} payload - The `AnnouncementRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async deleteAnnouncementByObjectId(objectId: bigint | string, payload: AnnouncementRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/object/announcements/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CommonResponse, AnnouncementRequest>({
      r: this.r,
      e,
      m: DELETE,
      p: payload,
      g: gamertag,
      w: true
    });
  }
}
