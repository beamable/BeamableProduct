import { BeginPurchaseRequest } from '@/__generated__/schemas/BeginPurchaseRequest';
import { BeginPurchaseResponse } from '@/__generated__/schemas/BeginPurchaseResponse';
import { CancelPurchaseRequest } from '@/__generated__/schemas/CancelPurchaseRequest';
import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { CompletePurchaseRequest } from '@/__generated__/schemas/CompletePurchaseRequest';
import { EmptyResponse } from '@/__generated__/schemas/EmptyResponse';
import { FacebookPaymentUpdateRequest } from '@/__generated__/schemas/FacebookPaymentUpdateRequest';
import { FacebookPaymentUpdateResponse } from '@/__generated__/schemas/FacebookPaymentUpdateResponse';
import { FailPurchaseRequest } from '@/__generated__/schemas/FailPurchaseRequest';
import { GetProductResponse } from '@/__generated__/schemas/GetProductResponse';
import { GetProductsResponse } from '@/__generated__/schemas/GetProductsResponse';
import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { ListAuditResponse } from '@/__generated__/schemas/ListAuditResponse';
import { LocalizedPriceMap } from '@/__generated__/schemas/LocalizedPriceMap';
import { makeQueryString } from '@/utils/makeQueryString';
import { PaymentResultResponse } from '@/__generated__/schemas/PaymentResultResponse';
import { SteamAuthRequest } from '@/__generated__/schemas/SteamAuthRequest';
import { SteamOrderInfoResponse } from '@/__generated__/schemas/SteamOrderInfoResponse';
import { SubscriptionVerificationResponse } from '@/__generated__/schemas/SubscriptionVerificationResponse';
import { TrackPurchaseRequest } from '@/__generated__/schemas/TrackPurchaseRequest';
import { VerifyPurchaseRequest } from '@/__generated__/schemas/VerifyPurchaseRequest';

export class PaymentsApi {
  constructor(
    private readonly requester: HttpRequester
  ) {
  }
  
  /**
   * @param {TrackPurchaseRequest} payload - The `TrackPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async trackWindowsPurchase(payload: TrackPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/windows/purchase/track";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, TrackPurchaseRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
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
    let endpoint = "/basic/payments/audits";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      limit,
      player,
      provider,
      providerid,
      start,
      state,
      txid
    });
    
    // Make the API request
    return this.requester.request<ListAuditResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {CompletePurchaseRequest} payload - The `CompletePurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async completeWindowsPurchase(payload: CompletePurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/windows/purchase/complete";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, CompletePurchaseRequest>({
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
   * @param {BeginPurchaseRequest} payload - The `BeginPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<BeginPurchaseResponse>>} A promise containing the HttpResponse of BeginPurchaseResponse
   */
  async beginTestPurchase(payload: BeginPurchaseRequest, gamertag?: string): Promise<HttpResponse<BeginPurchaseResponse>> {
    let endpoint = "/basic/payments/test/purchase/begin";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<BeginPurchaseResponse, BeginPurchaseRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
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
    let endpoint = "/basic/payments/facebook/update";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      hubChallenge,
      hubMode,
      hubVerifyToken
    });
    
    // Make the API request
    return this.requester.request<SubscriptionVerificationResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {FacebookPaymentUpdateRequest} payload - The `FacebookPaymentUpdateRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<FacebookPaymentUpdateResponse>>} A promise containing the HttpResponse of FacebookPaymentUpdateResponse
   */
  async postPaymentFacebookUpdate(payload: FacebookPaymentUpdateRequest, gamertag?: string): Promise<HttpResponse<FacebookPaymentUpdateResponse>> {
    let endpoint = "/basic/payments/facebook/update";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<FacebookPaymentUpdateResponse, FacebookPaymentUpdateRequest>({
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
   * @param {FailPurchaseRequest} payload - The `FailPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async failSteamPurchase(payload: FailPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/steam/purchase/fail";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, FailPurchaseRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {CompletePurchaseRequest} payload - The `CompletePurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async completeFacebookPurchase(payload: CompletePurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/facebook/purchase/complete";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, CompletePurchaseRequest>({
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
   * @param {FailPurchaseRequest} payload - The `FailPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async failFacebookPurchase(payload: FailPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/facebook/purchase/fail";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, FailPurchaseRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {CompletePurchaseRequest} payload - The `CompletePurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async completeTestPurchase(payload: CompletePurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/test/purchase/complete";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, CompletePurchaseRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {string} sku - The `sku` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetProductResponse>>} A promise containing the HttpResponse of GetProductResponse
   */
  async getPaymentItunesProduct(sku: string, gamertag?: string): Promise<HttpResponse<GetProductResponse>> {
    let endpoint = "/basic/payments/itunes/product";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      sku
    });
    
    // Make the API request
    return this.requester.request<GetProductResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {CompletePurchaseRequest} payload - The `CompletePurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async completeGoogleplayPurchase(payload: CompletePurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/googleplay/purchase/complete";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, CompletePurchaseRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {TrackPurchaseRequest} payload - The `TrackPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async trackTestPurchase(payload: TrackPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/test/purchase/track";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, TrackPurchaseRequest>({
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
   * @param {BeginPurchaseRequest} payload - The `BeginPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<BeginPurchaseResponse>>} A promise containing the HttpResponse of BeginPurchaseResponse
   */
  async beginGoogleplayPurchase(payload: BeginPurchaseRequest, gamertag?: string): Promise<HttpResponse<BeginPurchaseResponse>> {
    let endpoint = "/basic/payments/googleplay/purchase/begin";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<BeginPurchaseResponse, BeginPurchaseRequest>({
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
   * @param {BeginPurchaseRequest} payload - The `BeginPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<BeginPurchaseResponse>>} A promise containing the HttpResponse of BeginPurchaseResponse
   */
  async beginItunesPurchase(payload: BeginPurchaseRequest, gamertag?: string): Promise<HttpResponse<BeginPurchaseResponse>> {
    let endpoint = "/basic/payments/itunes/purchase/begin";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<BeginPurchaseResponse, BeginPurchaseRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {VerifyPurchaseRequest} payload - The `VerifyPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async verifyGoogleplayPurchase(payload: VerifyPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/googleplay/purchase/verify";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, VerifyPurchaseRequest>({
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
   * @param {CancelPurchaseRequest} payload - The `CancelPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async cancelFacebookPurchase(payload: CancelPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/facebook/purchase/cancel";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, CancelPurchaseRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {TrackPurchaseRequest} payload - The `TrackPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async trackCouponPurchase(payload: TrackPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/coupon/purchase/track";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, TrackPurchaseRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {CompletePurchaseRequest} payload - The `CompletePurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async completeSteamPurchase(payload: CompletePurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/steam/purchase/complete";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, CompletePurchaseRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {TrackPurchaseRequest} payload - The `TrackPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async trackFacebookPurchase(payload: TrackPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/facebook/purchase/track";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, TrackPurchaseRequest>({
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
   * @param {FailPurchaseRequest} payload - The `FailPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async failItunesPurchase(payload: FailPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/itunes/purchase/fail";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, FailPurchaseRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {VerifyPurchaseRequest} payload - The `VerifyPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async verifyTestPurchase(payload: VerifyPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/test/purchase/verify";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, VerifyPurchaseRequest>({
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
   * @param {CancelPurchaseRequest} payload - The `CancelPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async cancelTestPurchase(payload: CancelPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/test/purchase/cancel";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, CancelPurchaseRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {TrackPurchaseRequest} payload - The `TrackPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async trackGoogleplayPurchase(payload: TrackPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/googleplay/purchase/track";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, TrackPurchaseRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {bigint | string} steamId - The `steamId` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<LocalizedPriceMap>>} A promise containing the HttpResponse of LocalizedPriceMap
   */
  async getPaymentSteamPrices(steamId: bigint | string, gamertag?: string): Promise<HttpResponse<LocalizedPriceMap>> {
    let endpoint = "/basic/payments/steam/prices";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      steamId
    });
    
    // Make the API request
    return this.requester.request<LocalizedPriceMap>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {VerifyPurchaseRequest} payload - The `VerifyPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async verifyWindowsPurchase(payload: VerifyPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/windows/purchase/verify";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, VerifyPurchaseRequest>({
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
   * @param {FailPurchaseRequest} payload - The `FailPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async failTestPurchase(payload: FailPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/test/purchase/fail";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, FailPurchaseRequest>({
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
   * @param {CancelPurchaseRequest} payload - The `CancelPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async cancelCouponPurchase(payload: CancelPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/coupon/purchase/cancel";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, CancelPurchaseRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {VerifyPurchaseRequest} payload - The `VerifyPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async verifyItunesPurchase(payload: VerifyPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/itunes/purchase/verify";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, VerifyPurchaseRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {CompletePurchaseRequest} payload - The `CompletePurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async completeItunesPurchase(payload: CompletePurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/itunes/purchase/complete";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, CompletePurchaseRequest>({
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
   * @param {BeginPurchaseRequest} payload - The `BeginPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<BeginPurchaseResponse>>} A promise containing the HttpResponse of BeginPurchaseResponse
   */
  async beginCouponPurchase(payload: BeginPurchaseRequest, gamertag?: string): Promise<HttpResponse<BeginPurchaseResponse>> {
    let endpoint = "/basic/payments/coupon/purchase/begin";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<BeginPurchaseResponse, BeginPurchaseRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {VerifyPurchaseRequest} payload - The `VerifyPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async verifyFacebookPurchase(payload: VerifyPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/facebook/purchase/verify";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, VerifyPurchaseRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {TrackPurchaseRequest} payload - The `TrackPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async trackSteamPurchase(payload: TrackPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/steam/purchase/track";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, TrackPurchaseRequest>({
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
   * @param {BeginPurchaseRequest} payload - The `BeginPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<BeginPurchaseResponse>>} A promise containing the HttpResponse of BeginPurchaseResponse
   */
  async beginFacebookPurchase(payload: BeginPurchaseRequest, gamertag?: string): Promise<HttpResponse<BeginPurchaseResponse>> {
    let endpoint = "/basic/payments/facebook/purchase/begin";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<BeginPurchaseResponse, BeginPurchaseRequest>({
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
   * @param {string} orderId - The `orderId` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<SteamOrderInfoResponse>>} A promise containing the HttpResponse of SteamOrderInfoResponse
   */
  async getPaymentSteamOrder(orderId: string, gamertag?: string): Promise<HttpResponse<SteamOrderInfoResponse>> {
    let endpoint = "/basic/payments/steam/order";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      orderId
    });
    
    // Make the API request
    return this.requester.request<SteamOrderInfoResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers,
      withAuth: true
    });
  }
  
  /**
   * @param {VerifyPurchaseRequest} payload - The `VerifyPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async verifyCouponPurchase(payload: VerifyPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/coupon/purchase/verify";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, VerifyPurchaseRequest>({
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
   * @param {BeginPurchaseRequest} payload - The `BeginPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<BeginPurchaseResponse>>} A promise containing the HttpResponse of BeginPurchaseResponse
   */
  async beginWindowsPurchase(payload: BeginPurchaseRequest, gamertag?: string): Promise<HttpResponse<BeginPurchaseResponse>> {
    let endpoint = "/basic/payments/windows/purchase/begin";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<BeginPurchaseResponse, BeginPurchaseRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {string} sku - The `sku` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetProductResponse>>} A promise containing the HttpResponse of GetProductResponse
   */
  async getPaymentWindowsProduct(sku: string, gamertag?: string): Promise<HttpResponse<GetProductResponse>> {
    let endpoint = "/basic/payments/windows/product";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      sku
    });
    
    // Make the API request
    return this.requester.request<GetProductResponse>({
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
   * @param {FailPurchaseRequest} payload - The `FailPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async failGoogleplayPurchase(payload: FailPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/googleplay/purchase/fail";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, FailPurchaseRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {string} sku - The `sku` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetProductResponse>>} A promise containing the HttpResponse of GetProductResponse
   */
  async getPaymentFacebookProduct(sku: string, gamertag?: string): Promise<HttpResponse<GetProductResponse>> {
    let endpoint = "/basic/payments/facebook/product";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      sku
    });
    
    // Make the API request
    return this.requester.request<GetProductResponse>({
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
   * @param {CancelPurchaseRequest} payload - The `CancelPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async cancelGoogleplayPurchase(payload: CancelPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/googleplay/purchase/cancel";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, CancelPurchaseRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {string} sku - The `sku` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetProductResponse>>} A promise containing the HttpResponse of GetProductResponse
   */
  async getPaymentCouponProduct(sku: string, gamertag?: string): Promise<HttpResponse<GetProductResponse>> {
    let endpoint = "/basic/payments/coupon/product";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      sku
    });
    
    // Make the API request
    return this.requester.request<GetProductResponse>({
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
   * @param {FailPurchaseRequest} payload - The `FailPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async failCouponPurchase(payload: FailPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/coupon/purchase/fail";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, FailPurchaseRequest>({
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
   * @param {BeginPurchaseRequest} payload - The `BeginPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<BeginPurchaseResponse>>} A promise containing the HttpResponse of BeginPurchaseResponse
   */
  async beginSteamPurchase(payload: BeginPurchaseRequest, gamertag?: string): Promise<HttpResponse<BeginPurchaseResponse>> {
    let endpoint = "/basic/payments/steam/purchase/begin";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<BeginPurchaseResponse, BeginPurchaseRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {bigint | string} steamId - The `steamId` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetProductsResponse>>} A promise containing the HttpResponse of GetProductsResponse
   */
  async getPaymentSteamProducts(steamId: bigint | string, gamertag?: string): Promise<HttpResponse<GetProductsResponse>> {
    let endpoint = "/basic/payments/steam/products";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      steamId
    });
    
    // Make the API request
    return this.requester.request<GetProductsResponse>({
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
   * @param {CancelPurchaseRequest} payload - The `CancelPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async cancelSteamPurchase(payload: CancelPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/steam/purchase/cancel";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, CancelPurchaseRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {SteamAuthRequest} payload - The `SteamAuthRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postPaymentSteamAuth(payload: SteamAuthRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let endpoint = "/basic/payments/steam/auth";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyResponse, SteamAuthRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {string} sku - The `sku` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetProductResponse>>} A promise containing the HttpResponse of GetProductResponse
   */
  async getPaymentSteamProduct(sku: string, gamertag?: string): Promise<HttpResponse<GetProductResponse>> {
    let endpoint = "/basic/payments/steam/product";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      sku
    });
    
    // Make the API request
    return this.requester.request<GetProductResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {CompletePurchaseRequest} payload - The `CompletePurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async completeCouponPurchase(payload: CompletePurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/coupon/purchase/complete";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, CompletePurchaseRequest>({
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
   * @param {CancelPurchaseRequest} payload - The `CancelPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async cancelWindowsPurchase(payload: CancelPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/windows/purchase/cancel";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, CancelPurchaseRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {string} sku - The `sku` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetProductResponse>>} A promise containing the HttpResponse of GetProductResponse
   */
  async getPaymentGoogleplayProduct(sku: string, gamertag?: string): Promise<HttpResponse<GetProductResponse>> {
    let endpoint = "/basic/payments/googleplay/product";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      sku
    });
    
    // Make the API request
    return this.requester.request<GetProductResponse>({
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
   * @param {FailPurchaseRequest} payload - The `FailPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async failWindowsPurchase(payload: FailPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/windows/purchase/fail";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, FailPurchaseRequest>({
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
   * @param {CancelPurchaseRequest} payload - The `CancelPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async cancelItunesPurchase(payload: CancelPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/itunes/purchase/cancel";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, CancelPurchaseRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {string} sku - The `sku` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetProductResponse>>} A promise containing the HttpResponse of GetProductResponse
   */
  async getPaymentTestProduct(sku: string, gamertag?: string): Promise<HttpResponse<GetProductResponse>> {
    let endpoint = "/basic/payments/test/product";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      sku
    });
    
    // Make the API request
    return this.requester.request<GetProductResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {VerifyPurchaseRequest} payload - The `VerifyPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async verifySteamPurchase(payload: VerifyPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/steam/purchase/verify";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, VerifyPurchaseRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {TrackPurchaseRequest} payload - The `TrackPurchaseRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PaymentResultResponse>>} A promise containing the HttpResponse of PaymentResultResponse
   */
  async trackItunesPurchase(payload: TrackPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
    let endpoint = "/basic/payments/itunes/purchase/track";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PaymentResultResponse, TrackPurchaseRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async getPayment(objectId: bigint | string, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/object/payments/{objectId}/".replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse>({
      url: endpoint,
      method: HttpMethod.GET,
      headers
    });
  }
}
