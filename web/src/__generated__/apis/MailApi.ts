import { AcceptMultipleAttachments } from '@/__generated__/schemas/AcceptMultipleAttachments';
import { BulkSendMailRequest } from '@/__generated__/schemas/BulkSendMailRequest';
import { BulkUpdateMailObjectRequest } from '@/__generated__/schemas/BulkUpdateMailObjectRequest';
import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { ListMailCategoriesResponse } from '@/__generated__/schemas/ListMailCategoriesResponse';
import { MailQueryResponse } from '@/__generated__/schemas/MailQueryResponse';
import { MailResponse } from '@/__generated__/schemas/MailResponse';
import { MailSearchRequest } from '@/__generated__/schemas/MailSearchRequest';
import { MailSearchResponse } from '@/__generated__/schemas/MailSearchResponse';
import { MailSuccessResponse } from '@/__generated__/schemas/MailSuccessResponse';
import { MailTemplate } from '@/__generated__/schemas/MailTemplate';
import { makeQueryString } from '@/utils/makeQueryString';
import { SendMailObjectRequest } from '@/__generated__/schemas/SendMailObjectRequest';
import { SendMailResponse } from '@/__generated__/schemas/SendMailResponse';
import { UpdateMailRequest } from '@/__generated__/schemas/UpdateMailRequest';

export class MailApi {
  constructor(
    private readonly requester: HttpRequester
  ) {
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {AcceptMultipleAttachments} payload - The `AcceptMultipleAttachments` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<MailSuccessResponse>>} A promise containing the HttpResponse of MailSuccessResponse
   */
  async putMailAttachments(payload: AcceptMultipleAttachments, gamertag?: string): Promise<HttpResponse<MailSuccessResponse>> {
    let endpoint = "/basic/mail/attachments";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<MailSuccessResponse, AcceptMultipleAttachments>({
      url: endpoint,
      method: HttpMethod.PUT,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {bigint | string} gamerTag - The `gamerTag` parameter to include in the API request.
   * @param {string} templateName - The `templateName` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<MailTemplate>>} A promise containing the HttpResponse of MailTemplate
   */
  async getMailTemplate(gamerTag: bigint | string, templateName: string, gamertag?: string): Promise<HttpResponse<MailTemplate>> {
    let endpoint = "/basic/mail/template";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      gamerTag,
      templateName
    });
    
    // Make the API request
    return this.requester.request<MailTemplate>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {bigint | string} mid - The `mid` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<MailResponse>>} A promise containing the HttpResponse of MailResponse
   */
  async getMail(mid: bigint | string, gamertag?: string): Promise<HttpResponse<MailResponse>> {
    let endpoint = "/basic/mail/";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      mid
    });
    
    // Make the API request
    return this.requester.request<MailResponse>({
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
   * @param {UpdateMailRequest} payload - The `UpdateMailRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<MailSuccessResponse>>} A promise containing the HttpResponse of MailSuccessResponse
   */
  async putMail(payload: UpdateMailRequest, gamertag?: string): Promise<HttpResponse<MailSuccessResponse>> {
    let endpoint = "/basic/mail/";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<MailSuccessResponse, UpdateMailRequest>({
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
   * @param {BulkSendMailRequest} payload - The `BulkSendMailRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<MailSuccessResponse>>} A promise containing the HttpResponse of MailSuccessResponse
   */
  async postMailBulk(payload: BulkSendMailRequest, gamertag?: string): Promise<HttpResponse<MailSuccessResponse>> {
    let endpoint = "/basic/mail/bulk";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<MailSuccessResponse, BulkSendMailRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {bigint | string} mid - The `mid` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<MailResponse>>} A promise containing the HttpResponse of MailResponse
   */
  async getMailDetailByObjectId(objectId: bigint | string, mid: bigint | string, gamertag?: string): Promise<HttpResponse<MailResponse>> {
    let endpoint = "/object/mail/{objectId}/detail".replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      mid
    });
    
    // Make the API request
    return this.requester.request<MailResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ListMailCategoriesResponse>>} A promise containing the HttpResponse of ListMailCategoriesResponse
   */
  async getMailCategoriesByObjectId(objectId: bigint | string, gamertag?: string): Promise<HttpResponse<ListMailCategoriesResponse>> {
    let endpoint = "/object/mail/{objectId}/categories".replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<ListMailCategoriesResponse>({
      url: endpoint,
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {MailSearchRequest} payload - The `MailSearchRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<MailSearchResponse>>} A promise containing the HttpResponse of MailSearchResponse
   */
  async postMailSearchByObjectId(objectId: bigint | string, payload: MailSearchRequest, gamertag?: string): Promise<HttpResponse<MailSearchResponse>> {
    let endpoint = "/object/mail/{objectId}/search".replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<MailSearchResponse, MailSearchRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {BulkSendMailRequest} payload - The `BulkSendMailRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<MailSuccessResponse>>} A promise containing the HttpResponse of MailSuccessResponse
   */
  async postMailBulkByObjectId(objectId: bigint | string, payload: BulkSendMailRequest, gamertag?: string): Promise<HttpResponse<MailSuccessResponse>> {
    let endpoint = "/object/mail/{objectId}/bulk".replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<MailSuccessResponse, BulkSendMailRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {BulkUpdateMailObjectRequest} payload - The `BulkUpdateMailObjectRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<MailSuccessResponse>>} A promise containing the HttpResponse of MailSuccessResponse
   */
  async putMailBulkByObjectId(objectId: bigint | string, payload: BulkUpdateMailObjectRequest, gamertag?: string): Promise<HttpResponse<MailSuccessResponse>> {
    let endpoint = "/object/mail/{objectId}/bulk".replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<MailSuccessResponse, BulkUpdateMailObjectRequest>({
      url: endpoint,
      method: HttpMethod.PUT,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {AcceptMultipleAttachments} payload - The `AcceptMultipleAttachments` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<MailSuccessResponse>>} A promise containing the HttpResponse of MailSuccessResponse
   */
  async putMailAcceptManyByObjectId(objectId: bigint | string, payload: AcceptMultipleAttachments, gamertag?: string): Promise<HttpResponse<MailSuccessResponse>> {
    let endpoint = "/object/mail/{objectId}/accept/many".replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<MailSuccessResponse, AcceptMultipleAttachments>({
      url: endpoint,
      method: HttpMethod.PUT,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<MailQueryResponse>>} A promise containing the HttpResponse of MailQueryResponse
   */
  async getMailByObjectId(objectId: bigint | string, gamertag?: string): Promise<HttpResponse<MailQueryResponse>> {
    let endpoint = "/object/mail/{objectId}/".replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<MailQueryResponse>({
      url: endpoint,
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {SendMailObjectRequest} payload - The `SendMailObjectRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<SendMailResponse>>} A promise containing the HttpResponse of SendMailResponse
   */
  async postMailByObjectId(objectId: bigint | string, payload: SendMailObjectRequest, gamertag?: string): Promise<HttpResponse<SendMailResponse>> {
    let endpoint = "/object/mail/{objectId}/".replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<SendMailResponse, SendMailObjectRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {UpdateMailRequest} payload - The `UpdateMailRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<MailSuccessResponse>>} A promise containing the HttpResponse of MailSuccessResponse
   */
  async putMailByObjectId(objectId: bigint | string, payload: UpdateMailRequest, gamertag?: string): Promise<HttpResponse<MailSuccessResponse>> {
    let endpoint = "/object/mail/{objectId}/".replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<MailSuccessResponse, UpdateMailRequest>({
      url: endpoint,
      method: HttpMethod.PUT,
      headers,
      body: payload
    });
  }
}
