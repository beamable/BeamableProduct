/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/__generated__/apis/constants';
import { POST } from '@/constants';
import type { BeginPurchaseRequest } from '@/__generated__/schemas/BeginPurchaseRequest';
import type { BeginPurchaseResponse } from '@/__generated__/schemas/BeginPurchaseResponse';
import type { CancelPurchaseRequest } from '@/__generated__/schemas/CancelPurchaseRequest';
import type { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import type { CompletePurchaseRequest } from '@/__generated__/schemas/CompletePurchaseRequest';
import type { EmptyResponse } from '@/__generated__/schemas/EmptyResponse';
import type { FacebookPaymentUpdateRequest } from '@/__generated__/schemas/FacebookPaymentUpdateRequest';
import type { FacebookPaymentUpdateResponse } from '@/__generated__/schemas/FacebookPaymentUpdateResponse';
import type { FailPurchaseRequest } from '@/__generated__/schemas/FailPurchaseRequest';
import type { GetProductResponse } from '@/__generated__/schemas/GetProductResponse';
import type { GetProductsResponse } from '@/__generated__/schemas/GetProductsResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { ListAuditResponse } from '@/__generated__/schemas/ListAuditResponse';
import type { LocalizedPriceMap } from '@/__generated__/schemas/LocalizedPriceMap';
import type { PaymentResultResponse } from '@/__generated__/schemas/PaymentResultResponse';
import type { SteamAuthRequest } from '@/__generated__/schemas/SteamAuthRequest';
import type { SteamOrderInfoResponse } from '@/__generated__/schemas/SteamOrderInfoResponse';
import type { SubscriptionVerificationResponse } from '@/__generated__/schemas/SubscriptionVerificationResponse';
import type { TrackPurchaseRequest } from '@/__generated__/schemas/TrackPurchaseRequest';
import type { VerifyPurchaseRequest } from '@/__generated__/schemas/VerifyPurchaseRequest';

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `TrackPurchaseRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function paymentsPostWindowsPurchaseTrackBasic(requester: HttpRequester, payload: TrackPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsGetAuditsBasic(requester: HttpRequester, limit?: number, player?: bigint | string, provider?: string, providerid?: string, start?: number, state?: string, txid?: bigint | string, gamertag?: string): Promise<HttpResponse<ListAuditResponse>> {
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
export async function paymentsPostWindowsPurchaseCompleteBasic(requester: HttpRequester, payload: CompletePurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsPostTestPurchaseBeginBasic(requester: HttpRequester, payload: BeginPurchaseRequest, gamertag?: string): Promise<HttpResponse<BeginPurchaseResponse>> {
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
export async function paymentsGetFacebookUpdateBasic(requester: HttpRequester, hubChallenge: string, hubMode: string, hubVerifyToken: string, gamertag?: string): Promise<HttpResponse<SubscriptionVerificationResponse>> {
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
export async function paymentsPostFacebookUpdateBasic(requester: HttpRequester, payload: FacebookPaymentUpdateRequest, gamertag?: string): Promise<HttpResponse<FacebookPaymentUpdateResponse>> {
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
export async function paymentsPostSteamPurchaseFailBasic(requester: HttpRequester, payload: FailPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsPostFacebookPurchaseCompleteBasic(requester: HttpRequester, payload: CompletePurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsPostFacebookPurchaseFailBasic(requester: HttpRequester, payload: FailPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsPostTestPurchaseCompleteBasic(requester: HttpRequester, payload: CompletePurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsGetItunesProductBasic(requester: HttpRequester, sku: string, gamertag?: string): Promise<HttpResponse<GetProductResponse>> {
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
export async function paymentsPostGoogleplayPurchaseCompleteBasic(requester: HttpRequester, payload: CompletePurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsPostTestPurchaseTrackBasic(requester: HttpRequester, payload: TrackPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsPostGoogleplayPurchaseBeginBasic(requester: HttpRequester, payload: BeginPurchaseRequest, gamertag?: string): Promise<HttpResponse<BeginPurchaseResponse>> {
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
export async function paymentsPostItunesPurchaseBeginBasic(requester: HttpRequester, payload: BeginPurchaseRequest, gamertag?: string): Promise<HttpResponse<BeginPurchaseResponse>> {
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
export async function paymentsPostGoogleplayPurchaseVerifyBasic(requester: HttpRequester, payload: VerifyPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsPostFacebookPurchaseCancelBasic(requester: HttpRequester, payload: CancelPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsPostCouponPurchaseTrackBasic(requester: HttpRequester, payload: TrackPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsPostSteamPurchaseCompleteBasic(requester: HttpRequester, payload: CompletePurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsPostFacebookPurchaseTrackBasic(requester: HttpRequester, payload: TrackPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsPostItunesPurchaseFailBasic(requester: HttpRequester, payload: FailPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsPostTestPurchaseVerifyBasic(requester: HttpRequester, payload: VerifyPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsPostTestPurchaseCancelBasic(requester: HttpRequester, payload: CancelPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsPostGoogleplayPurchaseTrackBasic(requester: HttpRequester, payload: TrackPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsGetSteamPricesBasic(requester: HttpRequester, steamId: bigint | string, gamertag?: string): Promise<HttpResponse<LocalizedPriceMap>> {
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
export async function paymentsPostWindowsPurchaseVerifyBasic(requester: HttpRequester, payload: VerifyPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsPostTestPurchaseFailBasic(requester: HttpRequester, payload: FailPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsPostCouponPurchaseCancelBasic(requester: HttpRequester, payload: CancelPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsPostItunesPurchaseVerifyBasic(requester: HttpRequester, payload: VerifyPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsPostItunesPurchaseCompleteBasic(requester: HttpRequester, payload: CompletePurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsPostCouponPurchaseBeginBasic(requester: HttpRequester, payload: BeginPurchaseRequest, gamertag?: string): Promise<HttpResponse<BeginPurchaseResponse>> {
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
export async function paymentsPostFacebookPurchaseVerifyBasic(requester: HttpRequester, payload: VerifyPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsPostSteamPurchaseTrackBasic(requester: HttpRequester, payload: TrackPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsPostFacebookPurchaseBeginBasic(requester: HttpRequester, payload: BeginPurchaseRequest, gamertag?: string): Promise<HttpResponse<BeginPurchaseResponse>> {
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
export async function paymentsGetSteamOrderBasic(requester: HttpRequester, orderId: string, gamertag?: string): Promise<HttpResponse<SteamOrderInfoResponse>> {
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
export async function paymentsPostCouponPurchaseVerifyBasic(requester: HttpRequester, payload: VerifyPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsPostWindowsPurchaseBeginBasic(requester: HttpRequester, payload: BeginPurchaseRequest, gamertag?: string): Promise<HttpResponse<BeginPurchaseResponse>> {
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
export async function paymentsGetWindowsProductBasic(requester: HttpRequester, sku: string, gamertag?: string): Promise<HttpResponse<GetProductResponse>> {
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
export async function paymentsPostGoogleplayPurchaseFailBasic(requester: HttpRequester, payload: FailPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsGetFacebookProductBasic(requester: HttpRequester, sku: string, gamertag?: string): Promise<HttpResponse<GetProductResponse>> {
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
export async function paymentsPostGoogleplayPurchaseCancelBasic(requester: HttpRequester, payload: CancelPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsGetCouponProductBasic(requester: HttpRequester, sku: string, gamertag?: string): Promise<HttpResponse<GetProductResponse>> {
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
export async function paymentsPostCouponPurchaseFailBasic(requester: HttpRequester, payload: FailPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsPostSteamPurchaseBeginBasic(requester: HttpRequester, payload: BeginPurchaseRequest, gamertag?: string): Promise<HttpResponse<BeginPurchaseResponse>> {
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
export async function paymentsGetSteamProductsBasic(requester: HttpRequester, steamId: bigint | string, gamertag?: string): Promise<HttpResponse<GetProductsResponse>> {
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
export async function paymentsPostSteamPurchaseCancelBasic(requester: HttpRequester, payload: CancelPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsPostSteamAuthBasic(requester: HttpRequester, payload: SteamAuthRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
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
export async function paymentsGetSteamProductBasic(requester: HttpRequester, sku: string, gamertag?: string): Promise<HttpResponse<GetProductResponse>> {
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
export async function paymentsPostCouponPurchaseCompleteBasic(requester: HttpRequester, payload: CompletePurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsPostWindowsPurchaseCancelBasic(requester: HttpRequester, payload: CancelPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsGetGoogleplayProductBasic(requester: HttpRequester, sku: string, gamertag?: string): Promise<HttpResponse<GetProductResponse>> {
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
export async function paymentsPostWindowsPurchaseFailBasic(requester: HttpRequester, payload: FailPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsPostItunesPurchaseCancelBasic(requester: HttpRequester, payload: CancelPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsGetTestProductBasic(requester: HttpRequester, sku: string, gamertag?: string): Promise<HttpResponse<GetProductResponse>> {
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
export async function paymentsPostSteamPurchaseVerifyBasic(requester: HttpRequester, payload: VerifyPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsPostItunesPurchaseTrackBasic(requester: HttpRequester, payload: TrackPurchaseRequest, gamertag?: string): Promise<HttpResponse<PaymentResultResponse>> {
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
export async function paymentsGetByObjectId(requester: HttpRequester, objectId: bigint | string, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
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
