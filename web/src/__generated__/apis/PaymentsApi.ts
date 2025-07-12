import { BeginPurchaseRequest } from '@/__generated__/schemas/BeginPurchaseRequest';
import { BeginPurchaseResponse } from '@/__generated__/schemas/BeginPurchaseResponse';
import { CancelPurchaseRequest } from '@/__generated__/schemas/CancelPurchaseRequest';
import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { CompletePurchaseRequest } from '@/__generated__/schemas/CompletePurchaseRequest';
import { EmptyResponse } from '@/__generated__/schemas/EmptyResponse';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { FacebookPaymentUpdateRequest } from '@/__generated__/schemas/FacebookPaymentUpdateRequest';
import { FacebookPaymentUpdateResponse } from '@/__generated__/schemas/FacebookPaymentUpdateResponse';
import { FailPurchaseRequest } from '@/__generated__/schemas/FailPurchaseRequest';
import { GET } from '@/constants';
import { GetProductResponse } from '@/__generated__/schemas/GetProductResponse';
import { GetProductsResponse } from '@/__generated__/schemas/GetProductsResponse';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpResponse } from '@/network/http/types/HttpResponse';
import { ListAuditResponse } from '@/__generated__/schemas/ListAuditResponse';
import { LocalizedPriceMap } from '@/__generated__/schemas/LocalizedPriceMap';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/constants';
import { PaymentResultResponse } from '@/__generated__/schemas/PaymentResultResponse';
import { POST } from '@/constants';
import { SteamAuthRequest } from '@/__generated__/schemas/SteamAuthRequest';
import { SteamOrderInfoResponse } from '@/__generated__/schemas/SteamOrderInfoResponse';
import { SubscriptionVerificationResponse } from '@/__generated__/schemas/SubscriptionVerificationResponse';
import { TrackPurchaseRequest } from '@/__generated__/schemas/TrackPurchaseRequest';
import { VerifyPurchaseRequest } from '@/__generated__/schemas/VerifyPurchaseRequest';

export class PaymentsApi {
  constructor(
    private readonly r: HttpRequester
  ) {
  }
  
  /**
   * @param {TrackPurchaseRequest} payload - The `TrackPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async trackWindowsPurchase(payload: TrackPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/windows/purchase/track";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, TrackPurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @param {number} limit - The `limit` parameter to include in the API request.
   * @param {bigint | string} player - The `player` parameter to include in the API request.
   * @param {string} provider - The `provider` parameter to include in the API request.
   * @param {string} providerid - The `providerid` parameter to include in the API request.
   * @param {number} start - The `start` parameter to include in the API request.
   * @param {string} state - The `state` parameter to include in the API request.
   * @param {bigint | string} txid - The `txid` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ListAuditResponse>>} A promise containing the HttpResponse of ListAuditResponse
   */
  async getPaymentsAudits(limit?: number, player?: bigint | string, provider?: string, providerid?: string, start?: number, state?: string, txid?: bigint | string, gamertag?: string): Promise<HttpResponse<ListAuditResponse>> {
    let e = "/basic/payments/audits";
    
    // Make the API request
    return makeApiRequest<ListAuditResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        limit,
        player,
        provider,
        providerid,
        start,
        state,
        txid
      },
      g: gamertag
    });
  }
  
  /**
   * @param {CompletePurchaseRequest} payload - The `CompletePurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async completeWindowsPurchase(payload: CompletePurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/windows/purchase/complete";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, CompletePurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {BeginPurchaseRequest} payload - The `BeginPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<BeginPurchaseResponse>>} A promise containing the HttpResponse of BeginPurchaseResponse
   */
  async beginTestPurchase(payload: BeginPurchaseRequest, gamertag?: string): Promise<HttpResponse<BeginPurchaseResponse>> {
    let e = "/basic/payments/test/purchase/begin";
    
    // Make the API request
    return makeApiRequest<BeginPurchaseResponse, BeginPurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {string} hubChallenge - The `hubChallenge` parameter to include in the API request.
   * @param {string} hubMode - The `hubMode` parameter to include in the API request.
   * @param {string} hubVerifyToken - The `hubVerifyToken` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<SubscriptionVerificationResponse>>} A promise containing the HttpResponse of SubscriptionVerificationResponse
   */
  async getPaymentFacebookUpdate(hubChallenge: string, hubMode: string, hubVerifyToken: string, gamertag?: string): Promise<HttpResponse<SubscriptionVerificationResponse>> {
    let e = "/basic/payments/facebook/update";
    
    // Make the API request
    return makeApiRequest<SubscriptionVerificationResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        hubChallenge,
        hubMode,
        hubVerifyToken
      },
      g: gamertag
    });
  }
  
  /**
   * @param {FacebookPaymentUpdateRequest} payload - The `FacebookPaymentUpdateRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<FacebookPaymentUpdateResponse>>} A promise containing the HttpResponse of FacebookPaymentUpdateResponse
   */
  async postPaymentFacebookUpdate(payload: FacebookPaymentUpdateRequest, gamertag?: string): Promise<HttpResponse<FacebookPaymentUpdateResponse>> {
    let e = "/basic/payments/facebook/update";
    
    // Make the API request
    return makeApiRequest<FacebookPaymentUpdateResponse, FacebookPaymentUpdateRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {FailPurchaseRequest} payload - The `FailPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async failSteamPurchase(payload: FailPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/steam/purchase/fail";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, FailPurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {CompletePurchaseRequest} payload - The `CompletePurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async completeFacebookPurchase(payload: CompletePurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/facebook/purchase/complete";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, CompletePurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {FailPurchaseRequest} payload - The `FailPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async failFacebookPurchase(payload: FailPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/facebook/purchase/fail";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, FailPurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {CompletePurchaseRequest} payload - The `CompletePurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async completeTestPurchase(payload: CompletePurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/test/purchase/complete";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, CompletePurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @param {string} sku - The `sku` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetProductResponse>>} A promise containing the HttpResponse of GetProductResponse
   */
  async getPaymentItunesProduct(sku: string, gamertag?: string): Promise<HttpResponse<GetProductResponse>> {
    let e = "/basic/payments/itunes/product";
    
    // Make the API request
    return makeApiRequest<GetProductResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        sku
      },
      g: gamertag
    });
  }
  
  /**
   * @param {CompletePurchaseRequest} payload - The `CompletePurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async completeGoogleplayPurchase(payload: CompletePurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/googleplay/purchase/complete";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, CompletePurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @param {TrackPurchaseRequest} payload - The `TrackPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async trackTestPurchase(payload: TrackPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/test/purchase/track";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, TrackPurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {BeginPurchaseRequest} payload - The `BeginPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<BeginPurchaseResponse>>} A promise containing the HttpResponse of BeginPurchaseResponse
   */
  async beginGoogleplayPurchase(payload: BeginPurchaseRequest, gamertag?: string): Promise<HttpResponse<BeginPurchaseResponse>> {
    let e = "/basic/payments/googleplay/purchase/begin";
    
    // Make the API request
    return makeApiRequest<BeginPurchaseResponse, BeginPurchaseRequest>({
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
   * @param {BeginPurchaseRequest} payload - The `BeginPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<BeginPurchaseResponse>>} A promise containing the HttpResponse of BeginPurchaseResponse
   */
  async beginItunesPurchase(payload: BeginPurchaseRequest, gamertag?: string): Promise<HttpResponse<BeginPurchaseResponse>> {
    let e = "/basic/payments/itunes/purchase/begin";
    
    // Make the API request
    return makeApiRequest<BeginPurchaseResponse, BeginPurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {VerifyPurchaseRequest} payload - The `VerifyPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async verifyGoogleplayPurchase(payload: VerifyPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/googleplay/purchase/verify";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, VerifyPurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {CancelPurchaseRequest} payload - The `CancelPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async cancelFacebookPurchase(payload: CancelPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/facebook/purchase/cancel";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, CancelPurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {TrackPurchaseRequest} payload - The `TrackPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async trackCouponPurchase(payload: TrackPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/coupon/purchase/track";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, TrackPurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @param {CompletePurchaseRequest} payload - The `CompletePurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async completeSteamPurchase(payload: CompletePurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/steam/purchase/complete";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, CompletePurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @param {TrackPurchaseRequest} payload - The `TrackPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async trackFacebookPurchase(payload: TrackPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/facebook/purchase/track";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, TrackPurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {FailPurchaseRequest} payload - The `FailPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async failItunesPurchase(payload: FailPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/itunes/purchase/fail";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, FailPurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {VerifyPurchaseRequest} payload - The `VerifyPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async verifyTestPurchase(payload: VerifyPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/test/purchase/verify";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, VerifyPurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {CancelPurchaseRequest} payload - The `CancelPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async cancelTestPurchase(payload: CancelPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/test/purchase/cancel";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, CancelPurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {TrackPurchaseRequest} payload - The `TrackPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async trackGoogleplayPurchase(payload: TrackPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/googleplay/purchase/track";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, TrackPurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @param {bigint | string} steamId - The `steamId` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<LocalizedPriceMap>>} A promise containing the HttpResponse of LocalizedPriceMap
   */
  async getPaymentSteamPrices(steamId: bigint | string, gamertag?: string): Promise<HttpResponse<LocalizedPriceMap>> {
    let e = "/basic/payments/steam/prices";
    
    // Make the API request
    return makeApiRequest<LocalizedPriceMap>({
      r: this.r,
      e,
      m: GET,
      q: {
        steamId
      },
      g: gamertag
    });
  }
  
  /**
   * @param {VerifyPurchaseRequest} payload - The `VerifyPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async verifyWindowsPurchase(payload: VerifyPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/windows/purchase/verify";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, VerifyPurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {FailPurchaseRequest} payload - The `FailPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async failTestPurchase(payload: FailPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/test/purchase/fail";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, FailPurchaseRequest>({
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
   * @param {CancelPurchaseRequest} payload - The `CancelPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async cancelCouponPurchase(payload: CancelPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/coupon/purchase/cancel";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, CancelPurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {VerifyPurchaseRequest} payload - The `VerifyPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async verifyItunesPurchase(payload: VerifyPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/itunes/purchase/verify";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, VerifyPurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @param {CompletePurchaseRequest} payload - The `CompletePurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async completeItunesPurchase(payload: CompletePurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/itunes/purchase/complete";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, CompletePurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {BeginPurchaseRequest} payload - The `BeginPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<BeginPurchaseResponse>>} A promise containing the HttpResponse of BeginPurchaseResponse
   */
  async beginCouponPurchase(payload: BeginPurchaseRequest, gamertag?: string): Promise<HttpResponse<BeginPurchaseResponse>> {
    let e = "/basic/payments/coupon/purchase/begin";
    
    // Make the API request
    return makeApiRequest<BeginPurchaseResponse, BeginPurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {VerifyPurchaseRequest} payload - The `VerifyPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async verifyFacebookPurchase(payload: VerifyPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/facebook/purchase/verify";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, VerifyPurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @param {TrackPurchaseRequest} payload - The `TrackPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async trackSteamPurchase(payload: TrackPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/steam/purchase/track";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, TrackPurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {BeginPurchaseRequest} payload - The `BeginPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<BeginPurchaseResponse>>} A promise containing the HttpResponse of BeginPurchaseResponse
   */
  async beginFacebookPurchase(payload: BeginPurchaseRequest, gamertag?: string): Promise<HttpResponse<BeginPurchaseResponse>> {
    let e = "/basic/payments/facebook/purchase/begin";
    
    // Make the API request
    return makeApiRequest<BeginPurchaseResponse, BeginPurchaseRequest>({
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
   * @param {string} orderId - The `orderId` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<SteamOrderInfoResponse>>} A promise containing the HttpResponse of SteamOrderInfoResponse
   */
  async getPaymentSteamOrder(orderId: string, gamertag?: string): Promise<HttpResponse<SteamOrderInfoResponse>> {
    let e = "/basic/payments/steam/order";
    
    // Make the API request
    return makeApiRequest<SteamOrderInfoResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        orderId
      },
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {VerifyPurchaseRequest} payload - The `VerifyPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async verifyCouponPurchase(payload: VerifyPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/coupon/purchase/verify";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, VerifyPurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {BeginPurchaseRequest} payload - The `BeginPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<BeginPurchaseResponse>>} A promise containing the HttpResponse of BeginPurchaseResponse
   */
  async beginWindowsPurchase(payload: BeginPurchaseRequest, gamertag?: string): Promise<HttpResponse<BeginPurchaseResponse>> {
    let e = "/basic/payments/windows/purchase/begin";
    
    // Make the API request
    return makeApiRequest<BeginPurchaseResponse, BeginPurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {string} sku - The `sku` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetProductResponse>>} A promise containing the HttpResponse of GetProductResponse
   */
  async getPaymentWindowsProduct(sku: string, gamertag?: string): Promise<HttpResponse<GetProductResponse>> {
    let e = "/basic/payments/windows/product";
    
    // Make the API request
    return makeApiRequest<GetProductResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        sku
      },
      g: gamertag
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {FailPurchaseRequest} payload - The `FailPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async failGoogleplayPurchase(payload: FailPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/googleplay/purchase/fail";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, FailPurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {string} sku - The `sku` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetProductResponse>>} A promise containing the HttpResponse of GetProductResponse
   */
  async getPaymentFacebookProduct(sku: string, gamertag?: string): Promise<HttpResponse<GetProductResponse>> {
    let e = "/basic/payments/facebook/product";
    
    // Make the API request
    return makeApiRequest<GetProductResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        sku
      },
      g: gamertag
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {CancelPurchaseRequest} payload - The `CancelPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async cancelGoogleplayPurchase(payload: CancelPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/googleplay/purchase/cancel";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, CancelPurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {string} sku - The `sku` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetProductResponse>>} A promise containing the HttpResponse of GetProductResponse
   */
  async getPaymentCouponProduct(sku: string, gamertag?: string): Promise<HttpResponse<GetProductResponse>> {
    let e = "/basic/payments/coupon/product";
    
    // Make the API request
    return makeApiRequest<GetProductResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        sku
      },
      g: gamertag
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {FailPurchaseRequest} payload - The `FailPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async failCouponPurchase(payload: FailPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/coupon/purchase/fail";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, FailPurchaseRequest>({
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
   * @param {BeginPurchaseRequest} payload - The `BeginPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<BeginPurchaseResponse>>} A promise containing the HttpResponse of BeginPurchaseResponse
   */
  async beginSteamPurchase(payload: BeginPurchaseRequest, gamertag?: string): Promise<HttpResponse<BeginPurchaseResponse>> {
    let e = "/basic/payments/steam/purchase/begin";
    
    // Make the API request
    return makeApiRequest<BeginPurchaseResponse, BeginPurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {bigint | string} steamId - The `steamId` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetProductsResponse>>} A promise containing the HttpResponse of GetProductsResponse
   */
  async getPaymentSteamProducts(steamId: bigint | string, gamertag?: string): Promise<HttpResponse<GetProductsResponse>> {
    let e = "/basic/payments/steam/products";
    
    // Make the API request
    return makeApiRequest<GetProductsResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        steamId
      },
      g: gamertag
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {CancelPurchaseRequest} payload - The `CancelPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async cancelSteamPurchase(payload: CancelPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/steam/purchase/cancel";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, CancelPurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {SteamAuthRequest} payload - The `SteamAuthRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postPaymentSteamAuth(payload: SteamAuthRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let e = "/basic/payments/steam/auth";
    
    // Make the API request
    return makeApiRequest<EmptyResponse, SteamAuthRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @param {string} sku - The `sku` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetProductResponse>>} A promise containing the HttpResponse of GetProductResponse
   */
  async getPaymentSteamProduct(sku: string, gamertag?: string): Promise<HttpResponse<GetProductResponse>> {
    let e = "/basic/payments/steam/product";
    
    // Make the API request
    return makeApiRequest<GetProductResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        sku
      },
      g: gamertag
    });
  }
  
  /**
   * @param {CompletePurchaseRequest} payload - The `CompletePurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async completeCouponPurchase(payload: CompletePurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/coupon/purchase/complete";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, CompletePurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {CancelPurchaseRequest} payload - The `CancelPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async cancelWindowsPurchase(payload: CancelPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/windows/purchase/cancel";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, CancelPurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {string} sku - The `sku` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetProductResponse>>} A promise containing the HttpResponse of GetProductResponse
   */
  async getPaymentGoogleplayProduct(sku: string, gamertag?: string): Promise<HttpResponse<GetProductResponse>> {
    let e = "/basic/payments/googleplay/product";
    
    // Make the API request
    return makeApiRequest<GetProductResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        sku
      },
      g: gamertag
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {FailPurchaseRequest} payload - The `FailPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async failWindowsPurchase(payload: FailPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/windows/purchase/fail";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, FailPurchaseRequest>({
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
   * @param {CancelPurchaseRequest} payload - The `CancelPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async cancelItunesPurchase(payload: CancelPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/itunes/purchase/cancel";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, CancelPurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {string} sku - The `sku` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetProductResponse>>} A promise containing the HttpResponse of GetProductResponse
   */
  async getPaymentTestProduct(sku: string, gamertag?: string): Promise<HttpResponse<GetProductResponse>> {
    let e = "/basic/payments/test/product";
    
    // Make the API request
    return makeApiRequest<GetProductResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        sku
      },
      g: gamertag
    });
  }
  
  /**
   * @param {VerifyPurchaseRequest} payload - The `VerifyPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async verifySteamPurchase(payload: VerifyPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/steam/purchase/verify";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, VerifyPurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @param {TrackPurchaseRequest} payload - The `TrackPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async trackItunesPurchase(payload: TrackPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let e = "/basic/payments/itunes/purchase/track";
    
    // Make the API request
    return makeApiRequest<PaymentResultResponse, TrackPurchaseRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async getPayment(objectId: bigint | string, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/object/payments/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CommonResponse>({
      r: this.r,
      e,
      m: GET,
      g: gamertag,
      w: true
    });
  }
}
