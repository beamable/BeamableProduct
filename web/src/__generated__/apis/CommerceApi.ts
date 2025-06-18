import { ActiveListingResponse } from '@/__generated__/schemas/ActiveListingResponse';
import { ClearStatusRequest } from '@/__generated__/schemas/ClearStatusRequest';
import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { CooldownModifierRequest } from '@/__generated__/schemas/CooldownModifierRequest';
import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { GetActiveOffersResponse } from '@/__generated__/schemas/GetActiveOffersResponse';
import { GetCatalogResponse } from '@/__generated__/schemas/GetCatalogResponse';
import { GetSKUsResponse } from '@/__generated__/schemas/GetSKUsResponse';
import { GetTotalCouponResponse } from '@/__generated__/schemas/GetTotalCouponResponse';
import { GiveCouponReq } from '@/__generated__/schemas/GiveCouponReq';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/constants';
import { POST } from '@/constants';
import { PurchaseRequest } from '@/__generated__/schemas/PurchaseRequest';
import { PUT } from '@/constants';
import { ReportPurchaseRequest } from '@/__generated__/schemas/ReportPurchaseRequest';
import { ResultResponse } from '@/__generated__/schemas/ResultResponse';
import { SaveCatalogRequest } from '@/__generated__/schemas/SaveCatalogRequest';
import { SaveSKUsRequest } from '@/__generated__/schemas/SaveSKUsRequest';
import { StatSubscriptionNotification } from '@/__generated__/schemas/StatSubscriptionNotification';

export class CommerceApi {
  constructor(
    private readonly r: HttpRequester
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
    let e = "/basic/commerce/catalog/legacy";
    
    // Make the API request
    return makeApiRequest<ResultResponse, SaveCatalogRequest>({
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
   * @param {bigint | string} version - The `version` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetCatalogResponse>>} A promise containing the HttpResponse of GetCatalogResponse
   */
  async getCommerceCatalog(version?: bigint | string, gamertag?: string): Promise<HttpResponse<GetCatalogResponse>> {
    let e = "/basic/commerce/catalog";
    
    // Make the API request
    return makeApiRequest<GetCatalogResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        version
      },
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {bigint | string} version - The `version` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetSKUsResponse>>} A promise containing the HttpResponse of GetSKUsResponse
   */
  async getCommerceSkus(version?: bigint | string, gamertag?: string): Promise<HttpResponse<GetSKUsResponse>> {
    let e = "/basic/commerce/skus";
    
    // Make the API request
    return makeApiRequest<GetSKUsResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        version
      },
      g: gamertag
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
    let e = "/basic/commerce/skus";
    
    // Make the API request
    return makeApiRequest<ResultResponse, SaveSKUsRequest>({
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
   * @param {string} scope - The `scope` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetActiveOffersResponse>>} A promise containing the HttpResponse of GetActiveOffersResponse
   */
  async getCommerceByObjectId(objectId: bigint | string, scope?: string, gamertag?: string): Promise<HttpResponse<GetActiveOffersResponse>> {
    let e = "/object/commerce/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<GetActiveOffersResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        scope
      },
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetTotalCouponResponse>>} A promise containing the HttpResponse of GetTotalCouponResponse
   */
  async getCommerceCouponsCountByObjectId(objectId: bigint | string, gamertag?: string): Promise<HttpResponse<GetTotalCouponResponse>> {
    let e = "/object/commerce/{objectId}/coupons/count".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<GetTotalCouponResponse>({
      r: this.r,
      e,
      m: GET,
      g: gamertag
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
    let e = "/object/commerce/{objectId}/listings/cooldown".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CommonResponse, CooldownModifierRequest>({
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
   * @param {string} language - The `language` parameter to include in the API request.
   * @param {string} stores - The `stores` parameter to include in the API request.
   * @param {string} time - The `time` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetActiveOffersResponse>>} A promise containing the HttpResponse of GetActiveOffersResponse
   */
  async getCommerceOffersAdminByObjectId(objectId: bigint | string, language?: string, stores?: string, time?: string, gamertag?: string): Promise<HttpResponse<GetActiveOffersResponse>> {
    let e = "/object/commerce/{objectId}/offersAdmin".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<GetActiveOffersResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        language,
        stores,
        time
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
   * @param {PurchaseRequest} payload - The `PurchaseRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postCommercePurchaseByObjectId(objectId: bigint | string, payload: PurchaseRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/object/commerce/{objectId}/purchase".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CommonResponse, PurchaseRequest>({
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
   * @param {ReportPurchaseRequest} payload - The `ReportPurchaseRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ResultResponse>>} A promise containing the HttpResponse of ResultResponse
   */
  async putCommercePurchaseByObjectId(objectId: bigint | string, payload: ReportPurchaseRequest, gamertag?: string): Promise<HttpResponse<ResultResponse>> {
    let e = "/object/commerce/{objectId}/purchase".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<ResultResponse, ReportPurchaseRequest>({
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
   * @param {string} listing - The `listing` parameter to include in the API request.
   * @param {string} store - The `store` parameter to include in the API request.
   * @param {string} time - The `time` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ActiveListingResponse>>} A promise containing the HttpResponse of ActiveListingResponse
   */
  async getCommerceListingsByObjectId(objectId: bigint | string, listing: string, store?: string, time?: string, gamertag?: string): Promise<HttpResponse<ActiveListingResponse>> {
    let e = "/object/commerce/{objectId}/listings".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<ActiveListingResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        listing,
        store,
        time
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
   * @param {ClearStatusRequest} payload - The `ClearStatusRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async deleteCommerceStatusByObjectId(objectId: bigint | string, payload: ClearStatusRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/object/commerce/{objectId}/status".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CommonResponse, ClearStatusRequest>({
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
   * @param {GiveCouponReq} payload - The `GiveCouponReq` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postCommerceCouponsByObjectId(objectId: bigint | string, payload: GiveCouponReq, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/object/commerce/{objectId}/coupons".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CommonResponse, GiveCouponReq>({
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
   * @param {StatSubscriptionNotification} payload - The `StatSubscriptionNotification` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postCommerceStatsUpdateByObjectId(objectId: bigint | string, payload: StatSubscriptionNotification, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/object/commerce/{objectId}/stats/update".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CommonResponse, StatSubscriptionNotification>({
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
   * @param {string} language - The `language` parameter to include in the API request.
   * @param {string} stores - The `stores` parameter to include in the API request.
   * @param {string} time - The `time` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetActiveOffersResponse>>} A promise containing the HttpResponse of GetActiveOffersResponse
   */
  async getCommerceOffersByObjectId(objectId: bigint | string, language?: string, stores?: string, time?: string, gamertag?: string): Promise<HttpResponse<GetActiveOffersResponse>> {
    let e = "/object/commerce/{objectId}/offers".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<GetActiveOffersResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        language,
        stores,
        time
      },
      g: gamertag,
      w: true
    });
  }
}
