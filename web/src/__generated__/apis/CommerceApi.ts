import { ActiveListingResponse } from '@/__generated__/schemas/ActiveListingResponse';
import { ClearStatusRequest } from '@/__generated__/schemas/ClearStatusRequest';
import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { CooldownModifierRequest } from '@/__generated__/schemas/CooldownModifierRequest';
import { GetActiveOffersResponse } from '@/__generated__/schemas/GetActiveOffersResponse';
import { GetCatalogResponse } from '@/__generated__/schemas/GetCatalogResponse';
import { GetSKUsResponse } from '@/__generated__/schemas/GetSKUsResponse';
import { GetTotalCouponResponse } from '@/__generated__/schemas/GetTotalCouponResponse';
import { GiveCouponReq } from '@/__generated__/schemas/GiveCouponReq';
import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { makeQueryString } from '@/utils/makeQueryString';
import { PurchaseRequest } from '@/__generated__/schemas/PurchaseRequest';
import { ReportPurchaseRequest } from '@/__generated__/schemas/ReportPurchaseRequest';
import { ResultResponse } from '@/__generated__/schemas/ResultResponse';
import { SaveCatalogRequest } from '@/__generated__/schemas/SaveCatalogRequest';
import { SaveSKUsRequest } from '@/__generated__/schemas/SaveSKUsRequest';
import { StatSubscriptionNotification } from '@/__generated__/schemas/StatSubscriptionNotification';

export class CommerceApi {
  constructor(
    private readonly requester: HttpRequester
  ) {
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {SaveCatalogRequest} payload - The `SaveCatalogRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ResultResponse>>} A promise containing the HttpResponse of ResultResponse
   */
  async postCommerceCatalogLegacy(payload: SaveCatalogRequest, gamertag?: string): Promise<HttpResponse<ResultResponse>> {
    let endpoint = "/basic/commerce/catalog/legacy";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<ResultResponse, SaveCatalogRequest>({
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
   * @param {bigint | string} version - The `version` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetCatalogResponse>>} A promise containing the HttpResponse of GetCatalogResponse
   */
  async getCommerceCatalog(version?: bigint | string, gamertag?: string): Promise<HttpResponse<GetCatalogResponse>> {
    let endpoint = "/basic/commerce/catalog";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      version
    });
    
    // Make the API request
    return this.requester.request<GetCatalogResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers,
      withAuth: true
    });
  }
  
  /**
   * @param {bigint | string} version - The `version` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetSKUsResponse>>} A promise containing the HttpResponse of GetSKUsResponse
   */
  async getCommerceSkus(version?: bigint | string, gamertag?: string): Promise<HttpResponse<GetSKUsResponse>> {
    let endpoint = "/basic/commerce/skus";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      version
    });
    
    // Make the API request
    return this.requester.request<GetSKUsResponse>({
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
   * @param {SaveSKUsRequest} payload - The `SaveSKUsRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ResultResponse>>} A promise containing the HttpResponse of ResultResponse
   */
  async postCommerceSkus(payload: SaveSKUsRequest, gamertag?: string): Promise<HttpResponse<ResultResponse>> {
    let endpoint = "/basic/commerce/skus";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<ResultResponse, SaveSKUsRequest>({
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
   * @param {string} scope - The `scope` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetActiveOffersResponse>>} A promise containing the HttpResponse of GetActiveOffersResponse
   */
  async getCommerceByObjectId(objectId: bigint | string, scope?: string, gamertag?: string): Promise<HttpResponse<GetActiveOffersResponse>> {
    let endpoint = "/object/commerce/{objectId}/";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      scope
    });
    
    // Make the API request
    return this.requester.request<GetActiveOffersResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers,
      withAuth: true
    });
  }
  
  /**
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetTotalCouponResponse>>} A promise containing the HttpResponse of GetTotalCouponResponse
   */
  async getCommerceCouponsCountByObjectId(objectId: bigint | string, gamertag?: string): Promise<HttpResponse<GetTotalCouponResponse>> {
    let endpoint = "/object/commerce/{objectId}/coupons/count";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<GetTotalCouponResponse>({
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
   * @param {CooldownModifierRequest} payload - The `CooldownModifierRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putCommerceListingsCooldownByObjectId(objectId: bigint | string, payload: CooldownModifierRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/object/commerce/{objectId}/listings/cooldown";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, CooldownModifierRequest>({
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
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} language - The `language` parameter to include in the API request.
   * @param {string} stores - The `stores` parameter to include in the API request.
   * @param {string} time - The `time` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetActiveOffersResponse>>} A promise containing the HttpResponse of GetActiveOffersResponse
   */
  async getCommerceOffersAdminByObjectId(objectId: bigint | string, language?: string, stores?: string, time?: string, gamertag?: string): Promise<HttpResponse<GetActiveOffersResponse>> {
    let endpoint = "/object/commerce/{objectId}/offersAdmin";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      language,
      stores,
      time
    });
    
    // Make the API request
    return this.requester.request<GetActiveOffersResponse>({
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
   * @param {PurchaseRequest} payload - The `PurchaseRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postCommercePurchaseByObjectId(objectId: bigint | string, payload: PurchaseRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/object/commerce/{objectId}/purchase";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, PurchaseRequest>({
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
   * @param {ReportPurchaseRequest} payload - The `ReportPurchaseRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ResultResponse>>} A promise containing the HttpResponse of ResultResponse
   */
  async putCommercePurchaseByObjectId(objectId: bigint | string, payload: ReportPurchaseRequest, gamertag?: string): Promise<HttpResponse<ResultResponse>> {
    let endpoint = "/object/commerce/{objectId}/purchase";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<ResultResponse, ReportPurchaseRequest>({
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
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} listing - The `listing` parameter to include in the API request.
   * @param {string} store - The `store` parameter to include in the API request.
   * @param {string} time - The `time` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ActiveListingResponse>>} A promise containing the HttpResponse of ActiveListingResponse
   */
  async getCommerceListingsByObjectId(objectId: bigint | string, listing: string, store?: string, time?: string, gamertag?: string): Promise<HttpResponse<ActiveListingResponse>> {
    let endpoint = "/object/commerce/{objectId}/listings";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      listing,
      store,
      time
    });
    
    // Make the API request
    return this.requester.request<ActiveListingResponse>({
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
   * @param {ClearStatusRequest} payload - The `ClearStatusRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async deleteCommerceStatusByObjectId(objectId: bigint | string, payload: ClearStatusRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/object/commerce/{objectId}/status";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, ClearStatusRequest>({
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
   * @param {GiveCouponReq} payload - The `GiveCouponReq` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postCommerceCouponsByObjectId(objectId: bigint | string, payload: GiveCouponReq, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/object/commerce/{objectId}/coupons";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, GiveCouponReq>({
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
   * @param {StatSubscriptionNotification} payload - The `StatSubscriptionNotification` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postCommerceStatsUpdateByObjectId(objectId: bigint | string, payload: StatSubscriptionNotification, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/object/commerce/{objectId}/stats/update";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, StatSubscriptionNotification>({
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
   * @param {string} language - The `language` parameter to include in the API request.
   * @param {string} stores - The `stores` parameter to include in the API request.
   * @param {string} time - The `time` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetActiveOffersResponse>>} A promise containing the HttpResponse of GetActiveOffersResponse
   */
  async getCommerceOffersByObjectId(objectId: bigint | string, language?: string, stores?: string, time?: string, gamertag?: string): Promise<HttpResponse<GetActiveOffersResponse>> {
    let endpoint = "/object/commerce/{objectId}/offers";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      language,
      stores,
      time
    });
    
    // Make the API request
    return this.requester.request<GetActiveOffersResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers,
      withAuth: true
    });
  }
}
