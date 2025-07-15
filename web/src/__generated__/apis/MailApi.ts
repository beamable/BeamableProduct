import { AcceptMultipleAttachments } from '@/__generated__/schemas/AcceptMultipleAttachments';
import { BulkSendMailRequest } from '@/__generated__/schemas/BulkSendMailRequest';
import { BulkUpdateMailObjectRequest } from '@/__generated__/schemas/BulkUpdateMailObjectRequest';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpResponse } from '@/network/http/types/HttpResponse';
import { ListMailCategoriesResponse } from '@/__generated__/schemas/ListMailCategoriesResponse';
import { MailQueryResponse } from '@/__generated__/schemas/MailQueryResponse';
import { MailResponse } from '@/__generated__/schemas/MailResponse';
import { MailSearchRequest } from '@/__generated__/schemas/MailSearchRequest';
import { MailSearchResponse } from '@/__generated__/schemas/MailSearchResponse';
import { MailSuccessResponse } from '@/__generated__/schemas/MailSuccessResponse';
import { MailTemplate } from '@/__generated__/schemas/MailTemplate';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/constants';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import { SendMailObjectRequest } from '@/__generated__/schemas/SendMailObjectRequest';
import { SendMailResponse } from '@/__generated__/schemas/SendMailResponse';
import { UpdateMailRequest } from '@/__generated__/schemas/UpdateMailRequest';

export class MailApi {
  constructor(
    private readonly r: HttpRequester
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
    let e = "/basic/mail/attachments";
    
    // Make the API request
    return makeApiRequest<MailSuccessResponse, AcceptMultipleAttachments>({
      r: this.r,
      e,
      m: PUT,
      p: payload,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {bigint | string} gamerTag - The `gamerTag` parameter to include in the API request.
   * @param {string} templateName - The `templateName` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<MailTemplate>>} A promise containing the HttpResponse of MailTemplate
   */
  async getMailTemplate(gamerTag: bigint | string, templateName: string, gamertag?: string): Promise<HttpResponse<MailTemplate>> {
    let e = "/basic/mail/template";
    
    // Make the API request
    return makeApiRequest<MailTemplate>({
      r: this.r,
      e,
      m: GET,
      q: {
        gamerTag,
        templateName
      },
      g: gamertag
    });
  }
  
  /**
   * @param {bigint | string} mid - The `mid` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<MailResponse>>} A promise containing the HttpResponse of MailResponse
   */
  async getMail(mid: bigint | string, gamertag?: string): Promise<HttpResponse<MailResponse>> {
    let e = "/basic/mail/";
    
    // Make the API request
    return makeApiRequest<MailResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        mid
      },
      g: gamertag
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
    let e = "/basic/mail/";
    
    // Make the API request
    return makeApiRequest<MailSuccessResponse, UpdateMailRequest>({
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
   * @param {BulkSendMailRequest} payload - The `BulkSendMailRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<MailSuccessResponse>>} A promise containing the HttpResponse of MailSuccessResponse
   */
  async postMailBulk(payload: BulkSendMailRequest, gamertag?: string): Promise<HttpResponse<MailSuccessResponse>> {
    let e = "/basic/mail/bulk";
    
    // Make the API request
    return makeApiRequest<MailSuccessResponse, BulkSendMailRequest>({
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
   * @param {bigint | string} mid - The `mid` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<MailResponse>>} A promise containing the HttpResponse of MailResponse
   */
  async getMailDetailByObjectId(objectId: bigint | string, mid: bigint | string, gamertag?: string): Promise<HttpResponse<MailResponse>> {
    let e = "/object/mail/{objectId}/detail".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<MailResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        mid
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
   * @returns {Promise<HttpResponse<ListMailCategoriesResponse>>} A promise containing the HttpResponse of ListMailCategoriesResponse
   */
  async getMailCategoriesByObjectId(objectId: bigint | string, gamertag?: string): Promise<HttpResponse<ListMailCategoriesResponse>> {
    let e = "/object/mail/{objectId}/categories".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<ListMailCategoriesResponse>({
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
   * @param {MailSearchRequest} payload - The `MailSearchRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<MailSearchResponse>>} A promise containing the HttpResponse of MailSearchResponse
   */
  async postMailSearchByObjectId(objectId: bigint | string, payload: MailSearchRequest, gamertag?: string): Promise<HttpResponse<MailSearchResponse>> {
    let e = "/object/mail/{objectId}/search".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<MailSearchResponse, MailSearchRequest>({
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
   * @param {BulkSendMailRequest} payload - The `BulkSendMailRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<MailSuccessResponse>>} A promise containing the HttpResponse of MailSuccessResponse
   */
  async postMailBulkByObjectId(objectId: bigint | string, payload: BulkSendMailRequest, gamertag?: string): Promise<HttpResponse<MailSuccessResponse>> {
    let e = "/object/mail/{objectId}/bulk".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<MailSuccessResponse, BulkSendMailRequest>({
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
   * @param {BulkUpdateMailObjectRequest} payload - The `BulkUpdateMailObjectRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<MailSuccessResponse>>} A promise containing the HttpResponse of MailSuccessResponse
   */
  async putMailBulkByObjectId(objectId: bigint | string, payload: BulkUpdateMailObjectRequest, gamertag?: string): Promise<HttpResponse<MailSuccessResponse>> {
    let e = "/object/mail/{objectId}/bulk".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<MailSuccessResponse, BulkUpdateMailObjectRequest>({
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
   * @param {AcceptMultipleAttachments} payload - The `AcceptMultipleAttachments` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<MailSuccessResponse>>} A promise containing the HttpResponse of MailSuccessResponse
   */
  async putMailAcceptManyByObjectId(objectId: bigint | string, payload: AcceptMultipleAttachments, gamertag?: string): Promise<HttpResponse<MailSuccessResponse>> {
    let e = "/object/mail/{objectId}/accept/many".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<MailSuccessResponse, AcceptMultipleAttachments>({
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
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<MailQueryResponse>>} A promise containing the HttpResponse of MailQueryResponse
   */
  async getMailByObjectId(objectId: bigint | string, gamertag?: string): Promise<HttpResponse<MailQueryResponse>> {
    let e = "/object/mail/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<MailQueryResponse>({
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
   * @param {SendMailObjectRequest} payload - The `SendMailObjectRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<SendMailResponse>>} A promise containing the HttpResponse of SendMailResponse
   */
  async postMailByObjectId(objectId: bigint | string, payload: SendMailObjectRequest, gamertag?: string): Promise<HttpResponse<SendMailResponse>> {
    let e = "/object/mail/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<SendMailResponse, SendMailObjectRequest>({
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
   * @param {UpdateMailRequest} payload - The `UpdateMailRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<MailSuccessResponse>>} A promise containing the HttpResponse of MailSuccessResponse
   */
  async putMailByObjectId(objectId: bigint | string, payload: UpdateMailRequest, gamertag?: string): Promise<HttpResponse<MailSuccessResponse>> {
    let e = "/object/mail/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<MailSuccessResponse, UpdateMailRequest>({
      r: this.r,
      e,
      m: PUT,
      p: payload,
      g: gamertag,
      w: true
    });
  }
}
