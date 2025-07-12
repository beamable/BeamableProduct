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
import { objectIdPlaceholder } from '@/__generated__/apis/constants';
import { PaymentResultResponse } from '@/__generated__/schemas/PaymentResultResponse';
import { POST } from '@/constants';
import { SteamAuthRequest } from '@/__generated__/schemas/SteamAuthRequest';
import { SteamOrderInfoResponse } from '@/__generated__/schemas/SteamOrderInfoResponse';
import { SubscriptionVerificationResponse } from '@/__generated__/schemas/SubscriptionVerificationResponse';
import { TrackPurchaseRequest } from '@/__generated__/schemas/TrackPurchaseRequest';
import { VerifyPurchaseRequest } from '@/__generated__/schemas/VerifyPurchaseRequest';

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `TrackPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function trackWindowsPurchase(requester: HttpRequester, payload: TrackPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/windows/purchase/track";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, TrackPurchaseRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param limit - The `limit` parameter to include in the API request.
 * @param player - The `player` parameter to include in the API request.
 * @param provider - The `provider` parameter to include in the API request.
 * @param providerid - The `providerid` parameter to include in the API request.
 * @param start - The `start` parameter to include in the API request.
 * @param state - The `state` parameter to include in the API request.
 * @param txid - The `txid` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getPaymentsAudits(requester: HttpRequester, limit?: number, player?: bigint | string, provider?: string, providerid?: string, start?: number, state?: string, txid?: bigint | string, gamertag?: string): Promise<HttpResponse<ListAuditResponse>> {
  let endpoint = "/basic/payments/audits";
  
  // Make the API request
  return makeApiRequest<ListAuditResponse>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `CompletePurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function completeWindowsPurchase(requester: HttpRequester, payload: CompletePurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/windows/purchase/complete";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, CompletePurchaseRequest>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `BeginPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function beginTestPurchase(requester: HttpRequester, payload: BeginPurchaseRequest, gamertag?: string): Promise<HttpResponse<BeginPurchaseResponse>> {
  let endpoint = "/basic/payments/test/purchase/begin";
  
  // Make the API request
  return makeApiRequest<BeginPurchaseResponse, BeginPurchaseRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param hubChallenge - The `hubChallenge` parameter to include in the API request.
 * @param hubMode - The `hubMode` parameter to include in the API request.
 * @param hubVerifyToken - The `hubVerifyToken` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getPaymentFacebookUpdate(requester: HttpRequester, hubChallenge: string, hubMode: string, hubVerifyToken: string, gamertag?: string): Promise<HttpResponse<SubscriptionVerificationResponse>> {
  let endpoint = "/basic/payments/facebook/update";
  
  // Make the API request
  return makeApiRequest<SubscriptionVerificationResponse>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `FacebookPaymentUpdateRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postPaymentFacebookUpdate(requester: HttpRequester, payload: FacebookPaymentUpdateRequest, gamertag?: string): Promise<HttpResponse<FacebookPaymentUpdateResponse>> {
  let endpoint = "/basic/payments/facebook/update";
  
  // Make the API request
  return makeApiRequest<FacebookPaymentUpdateResponse, FacebookPaymentUpdateRequest>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `FailPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function failSteamPurchase(requester: HttpRequester, payload: FailPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/steam/purchase/fail";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, FailPurchaseRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `CompletePurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function completeFacebookPurchase(requester: HttpRequester, payload: CompletePurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/facebook/purchase/complete";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, CompletePurchaseRequest>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `FailPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function failFacebookPurchase(requester: HttpRequester, payload: FailPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/facebook/purchase/fail";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, FailPurchaseRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `CompletePurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function completeTestPurchase(requester: HttpRequester, payload: CompletePurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/test/purchase/complete";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, CompletePurchaseRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param sku - The `sku` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getPaymentItunesProduct(requester: HttpRequester, sku: string, gamertag?: string): Promise<HttpResponse<GetProductResponse>> {
  let endpoint = "/basic/payments/itunes/product";
  
  // Make the API request
  return makeApiRequest<GetProductResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      sku
    },
    g: gamertag
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `CompletePurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function completeGoogleplayPurchase(requester: HttpRequester, payload: CompletePurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/googleplay/purchase/complete";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, CompletePurchaseRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `TrackPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function trackTestPurchase(requester: HttpRequester, payload: TrackPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/test/purchase/track";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, TrackPurchaseRequest>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `BeginPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function beginGoogleplayPurchase(requester: HttpRequester, payload: BeginPurchaseRequest, gamertag?: string): Promise<HttpResponse<BeginPurchaseResponse>> {
  let endpoint = "/basic/payments/googleplay/purchase/begin";
  
  // Make the API request
  return makeApiRequest<BeginPurchaseResponse, BeginPurchaseRequest>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `BeginPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function beginItunesPurchase(requester: HttpRequester, payload: BeginPurchaseRequest, gamertag?: string): Promise<HttpResponse<BeginPurchaseResponse>> {
  let endpoint = "/basic/payments/itunes/purchase/begin";
  
  // Make the API request
  return makeApiRequest<BeginPurchaseResponse, BeginPurchaseRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `VerifyPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function verifyGoogleplayPurchase(requester: HttpRequester, payload: VerifyPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/googleplay/purchase/verify";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, VerifyPurchaseRequest>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `CancelPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function cancelFacebookPurchase(requester: HttpRequester, payload: CancelPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/facebook/purchase/cancel";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, CancelPurchaseRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `TrackPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function trackCouponPurchase(requester: HttpRequester, payload: TrackPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/coupon/purchase/track";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, TrackPurchaseRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `CompletePurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function completeSteamPurchase(requester: HttpRequester, payload: CompletePurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/steam/purchase/complete";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, CompletePurchaseRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `TrackPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function trackFacebookPurchase(requester: HttpRequester, payload: TrackPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/facebook/purchase/track";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, TrackPurchaseRequest>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `FailPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function failItunesPurchase(requester: HttpRequester, payload: FailPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/itunes/purchase/fail";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, FailPurchaseRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `VerifyPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function verifyTestPurchase(requester: HttpRequester, payload: VerifyPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/test/purchase/verify";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, VerifyPurchaseRequest>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `CancelPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function cancelTestPurchase(requester: HttpRequester, payload: CancelPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/test/purchase/cancel";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, CancelPurchaseRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `TrackPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function trackGoogleplayPurchase(requester: HttpRequester, payload: TrackPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/googleplay/purchase/track";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, TrackPurchaseRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param steamId - The `steamId` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getPaymentSteamPrices(requester: HttpRequester, steamId: bigint | string, gamertag?: string): Promise<HttpResponse<LocalizedPriceMap>> {
  let endpoint = "/basic/payments/steam/prices";
  
  // Make the API request
  return makeApiRequest<LocalizedPriceMap>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      steamId
    },
    g: gamertag
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `VerifyPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function verifyWindowsPurchase(requester: HttpRequester, payload: VerifyPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/windows/purchase/verify";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, VerifyPurchaseRequest>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `FailPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function failTestPurchase(requester: HttpRequester, payload: FailPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/test/purchase/fail";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, FailPurchaseRequest>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `CancelPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function cancelCouponPurchase(requester: HttpRequester, payload: CancelPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/coupon/purchase/cancel";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, CancelPurchaseRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `VerifyPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function verifyItunesPurchase(requester: HttpRequester, payload: VerifyPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/itunes/purchase/verify";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, VerifyPurchaseRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `CompletePurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function completeItunesPurchase(requester: HttpRequester, payload: CompletePurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/itunes/purchase/complete";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, CompletePurchaseRequest>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `BeginPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function beginCouponPurchase(requester: HttpRequester, payload: BeginPurchaseRequest, gamertag?: string): Promise<HttpResponse<BeginPurchaseResponse>> {
  let endpoint = "/basic/payments/coupon/purchase/begin";
  
  // Make the API request
  return makeApiRequest<BeginPurchaseResponse, BeginPurchaseRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `VerifyPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function verifyFacebookPurchase(requester: HttpRequester, payload: VerifyPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/facebook/purchase/verify";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, VerifyPurchaseRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `TrackPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function trackSteamPurchase(requester: HttpRequester, payload: TrackPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/steam/purchase/track";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, TrackPurchaseRequest>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `BeginPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function beginFacebookPurchase(requester: HttpRequester, payload: BeginPurchaseRequest, gamertag?: string): Promise<HttpResponse<BeginPurchaseResponse>> {
  let endpoint = "/basic/payments/facebook/purchase/begin";
  
  // Make the API request
  return makeApiRequest<BeginPurchaseResponse, BeginPurchaseRequest>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param orderId - The `orderId` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getPaymentSteamOrder(requester: HttpRequester, orderId: string, gamertag?: string): Promise<HttpResponse<SteamOrderInfoResponse>> {
  let endpoint = "/basic/payments/steam/order";
  
  // Make the API request
  return makeApiRequest<SteamOrderInfoResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      orderId
    },
    g: gamertag,
    w: true
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `VerifyPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function verifyCouponPurchase(requester: HttpRequester, payload: VerifyPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/coupon/purchase/verify";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, VerifyPurchaseRequest>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `BeginPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function beginWindowsPurchase(requester: HttpRequester, payload: BeginPurchaseRequest, gamertag?: string): Promise<HttpResponse<BeginPurchaseResponse>> {
  let endpoint = "/basic/payments/windows/purchase/begin";
  
  // Make the API request
  return makeApiRequest<BeginPurchaseResponse, BeginPurchaseRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param sku - The `sku` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getPaymentWindowsProduct(requester: HttpRequester, sku: string, gamertag?: string): Promise<HttpResponse<GetProductResponse>> {
  let endpoint = "/basic/payments/windows/product";
  
  // Make the API request
  return makeApiRequest<GetProductResponse>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `FailPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function failGoogleplayPurchase(requester: HttpRequester, payload: FailPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/googleplay/purchase/fail";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, FailPurchaseRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param sku - The `sku` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getPaymentFacebookProduct(requester: HttpRequester, sku: string, gamertag?: string): Promise<HttpResponse<GetProductResponse>> {
  let endpoint = "/basic/payments/facebook/product";
  
  // Make the API request
  return makeApiRequest<GetProductResponse>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `CancelPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function cancelGoogleplayPurchase(requester: HttpRequester, payload: CancelPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/googleplay/purchase/cancel";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, CancelPurchaseRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param sku - The `sku` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getPaymentCouponProduct(requester: HttpRequester, sku: string, gamertag?: string): Promise<HttpResponse<GetProductResponse>> {
  let endpoint = "/basic/payments/coupon/product";
  
  // Make the API request
  return makeApiRequest<GetProductResponse>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `FailPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function failCouponPurchase(requester: HttpRequester, payload: FailPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/coupon/purchase/fail";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, FailPurchaseRequest>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `BeginPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function beginSteamPurchase(requester: HttpRequester, payload: BeginPurchaseRequest, gamertag?: string): Promise<HttpResponse<BeginPurchaseResponse>> {
  let endpoint = "/basic/payments/steam/purchase/begin";
  
  // Make the API request
  return makeApiRequest<BeginPurchaseResponse, BeginPurchaseRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param steamId - The `steamId` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getPaymentSteamProducts(requester: HttpRequester, steamId: bigint | string, gamertag?: string): Promise<HttpResponse<GetProductsResponse>> {
  let endpoint = "/basic/payments/steam/products";
  
  // Make the API request
  return makeApiRequest<GetProductsResponse>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `CancelPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function cancelSteamPurchase(requester: HttpRequester, payload: CancelPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/steam/purchase/cancel";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, CancelPurchaseRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `SteamAuthRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postPaymentSteamAuth(requester: HttpRequester, payload: SteamAuthRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/basic/payments/steam/auth";
  
  // Make the API request
  return makeApiRequest<EmptyResponse, SteamAuthRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param sku - The `sku` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getPaymentSteamProduct(requester: HttpRequester, sku: string, gamertag?: string): Promise<HttpResponse<GetProductResponse>> {
  let endpoint = "/basic/payments/steam/product";
  
  // Make the API request
  return makeApiRequest<GetProductResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      sku
    },
    g: gamertag
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `CompletePurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function completeCouponPurchase(requester: HttpRequester, payload: CompletePurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/coupon/purchase/complete";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, CompletePurchaseRequest>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `CancelPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function cancelWindowsPurchase(requester: HttpRequester, payload: CancelPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/windows/purchase/cancel";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, CancelPurchaseRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param sku - The `sku` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getPaymentGoogleplayProduct(requester: HttpRequester, sku: string, gamertag?: string): Promise<HttpResponse<GetProductResponse>> {
  let endpoint = "/basic/payments/googleplay/product";
  
  // Make the API request
  return makeApiRequest<GetProductResponse>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `FailPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function failWindowsPurchase(requester: HttpRequester, payload: FailPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/windows/purchase/fail";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, FailPurchaseRequest>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `CancelPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function cancelItunesPurchase(requester: HttpRequester, payload: CancelPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/itunes/purchase/cancel";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, CancelPurchaseRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param sku - The `sku` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getPaymentTestProduct(requester: HttpRequester, sku: string, gamertag?: string): Promise<HttpResponse<GetProductResponse>> {
  let endpoint = "/basic/payments/test/product";
  
  // Make the API request
  return makeApiRequest<GetProductResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      sku
    },
    g: gamertag
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `VerifyPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function verifySteamPurchase(requester: HttpRequester, payload: VerifyPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/steam/purchase/verify";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, VerifyPurchaseRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `TrackPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function trackItunesPurchase(requester: HttpRequester, payload: TrackPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
  let endpoint = "/basic/payments/itunes/purchase/track";
  
  // Make the API request
  return makeApiRequest<PaymentResultResponse, TrackPurchaseRequest>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getPayment(requester: HttpRequester, objectId: bigint | string, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/object/payments/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<CommonResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    g: gamertag,
    w: true
  });
}
