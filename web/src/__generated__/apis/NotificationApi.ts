import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { GET } from '@/constants';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpResponse } from '@/network/http/types/HttpResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { NotificationRequest } from '@/__generated__/schemas/NotificationRequest';
import { POST } from '@/constants';
import { ServerEvent } from '@/__generated__/schemas/ServerEvent';
import { SubscriberDetailsResponse } from '@/__generated__/schemas/SubscriberDetailsResponse';

export class NotificationApi {
  constructor(
    private readonly r: HttpRequester
  ) {
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {NotificationRequest} payload - The `NotificationRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postNotificationChannel(payload: NotificationRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/basic/notification/channel";
    
    // Make the API request
    return makeApiRequest<CommonResponse, NotificationRequest>({
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
   * @param {NotificationRequest} payload - The `NotificationRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postNotificationPlayer(payload: NotificationRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/basic/notification/player";
    
    // Make the API request
    return makeApiRequest<CommonResponse, NotificationRequest>({
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
   * @param {NotificationRequest} payload - The `NotificationRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postNotificationCustom(payload: NotificationRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/basic/notification/custom";
    
    // Make the API request
    return makeApiRequest<CommonResponse, NotificationRequest>({
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
   * @param {ServerEvent} payload - The `ServerEvent` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postNotificationServer(payload: ServerEvent, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/basic/notification/server";
    
    // Make the API request
    return makeApiRequest<CommonResponse, ServerEvent>({
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
   * @param {NotificationRequest} payload - The `NotificationRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postNotificationGeneric(payload: NotificationRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/basic/notification/generic";
    
    // Make the API request
    return makeApiRequest<CommonResponse, NotificationRequest>({
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
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<SubscriberDetailsResponse>>} A promise containing the HttpResponse of SubscriberDetailsResponse
   */
  async getNotification(gamertag?: string): Promise<HttpResponse<SubscriberDetailsResponse>> {
    let e = "/basic/notification/";
    
    // Make the API request
    return makeApiRequest<SubscriberDetailsResponse>({
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
   * @param {NotificationRequest} payload - The `NotificationRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postNotificationGame(payload: NotificationRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/basic/notification/game";
    
    // Make the API request
    return makeApiRequest<CommonResponse, NotificationRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
    });
  }
}
