/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/__generated__/apis/constants';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import type { ActiveListingResponse } from '@/__generated__/schemas/ActiveListingResponse';
import type { ClearStatusRequest } from '@/__generated__/schemas/ClearStatusRequest';
import type { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import type { CooldownModifierRequest } from '@/__generated__/schemas/CooldownModifierRequest';
import type { GetActiveOffersResponse } from '@/__generated__/schemas/GetActiveOffersResponse';
import type { GetCatalogResponse } from '@/__generated__/schemas/GetCatalogResponse';
import type { GetSKUsResponse } from '@/__generated__/schemas/GetSKUsResponse';
import type { GetTotalCouponResponse } from '@/__generated__/schemas/GetTotalCouponResponse';
import type { GiveCouponReq } from '@/__generated__/schemas/GiveCouponReq';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { PurchaseRequest } from '@/__generated__/schemas/PurchaseRequest';
import type { ReportPurchaseRequest } from '@/__generated__/schemas/ReportPurchaseRequest';
import type { ResultResponse } from '@/__generated__/schemas/ResultResponse';
import type { SaveCatalogRequest } from '@/__generated__/schemas/SaveCatalogRequest';
import type { SaveSKUsRequest } from '@/__generated__/schemas/SaveSKUsRequest';
import type { StatSubscriptionNotification } from '@/__generated__/schemas/StatSubscriptionNotification';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `SaveCatalogRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function commercePostCatalogLegacyBasic(requester: HttpRequester, payload: SaveCatalogRequest, gamertag?: string): Promise<HttpResponse<ResultResponse>> {
  let endpoint = "/basic/commerce/catalog/legacy";
  
  // Make the API request
  return makeApiRequest<ResultResponse, SaveCatalogRequest>({
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
 * @param version - The `version` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function commerceGetCatalogBasic(requester: HttpRequester, version?: bigint | string, gamertag?: string): Promise<HttpResponse<GetCatalogResponse>> {
  let endpoint = "/basic/commerce/catalog";
  
  // Make the API request
  return makeApiRequest<GetCatalogResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      version
    },
    g: gamertag,
    w: true
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param version - The `version` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function commerceGetSkusBasic(requester: HttpRequester, version?: bigint | string, gamertag?: string): Promise<HttpResponse<GetSKUsResponse>> {
  let endpoint = "/basic/commerce/skus";
  
  // Make the API request
  return makeApiRequest<GetSKUsResponse>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `SaveSKUsRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function commercePostSkusBasic(requester: HttpRequester, payload: SaveSKUsRequest, gamertag?: string): Promise<HttpResponse<ResultResponse>> {
  let endpoint = "/basic/commerce/skus";
  
  // Make the API request
  return makeApiRequest<ResultResponse, SaveSKUsRequest>({
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
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param scope - The `scope` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function commerceGetByObjectId(requester: HttpRequester, objectId: bigint | string, scope?: string, gamertag?: string): Promise<HttpResponse<GetActiveOffersResponse>> {
  let endpoint = "/object/commerce/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<GetActiveOffersResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      scope
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function commerceGetCouponsCountByObjectId(requester: HttpRequester, objectId: bigint | string, gamertag?: string): Promise<HttpResponse<GetTotalCouponResponse>> {
  let endpoint = "/object/commerce/{objectId}/coupons/count".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<GetTotalCouponResponse>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `CooldownModifierRequest` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function commercePutListingsCooldownByObjectId(requester: HttpRequester, objectId: bigint | string, payload: CooldownModifierRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/object/commerce/{objectId}/listings/cooldown".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<CommonResponse, CooldownModifierRequest>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param language - The `language` parameter to include in the API request.
 * @param stores - The `stores` parameter to include in the API request.
 * @param time - The `time` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function commerceGetOffersAdminByObjectId(requester: HttpRequester, objectId: bigint | string, language?: string, stores?: string, time?: string, gamertag?: string): Promise<HttpResponse<GetActiveOffersResponse>> {
  let endpoint = "/object/commerce/{objectId}/offersAdmin".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<GetActiveOffersResponse>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `PurchaseRequest` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function commercePostPurchaseByObjectId(requester: HttpRequester, objectId: bigint | string, payload: PurchaseRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/object/commerce/{objectId}/purchase".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<CommonResponse, PurchaseRequest>({
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
 * @param payload - The `ReportPurchaseRequest` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function commercePutPurchaseByObjectId(requester: HttpRequester, objectId: bigint | string, payload: ReportPurchaseRequest, gamertag?: string): Promise<HttpResponse<ResultResponse>> {
  let endpoint = "/object/commerce/{objectId}/purchase".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<ResultResponse, ReportPurchaseRequest>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param listing - The `listing` parameter to include in the API request.
 * @param store - The `store` parameter to include in the API request.
 * @param time - The `time` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function commerceGetListingsByObjectId(requester: HttpRequester, objectId: bigint | string, listing: string, store?: string, time?: string, gamertag?: string): Promise<HttpResponse<ActiveListingResponse>> {
  let endpoint = "/object/commerce/{objectId}/listings".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<ActiveListingResponse>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `ClearStatusRequest` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function commerceDeleteStatusByObjectId(requester: HttpRequester, objectId: bigint | string, payload: ClearStatusRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/object/commerce/{objectId}/status".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<CommonResponse, ClearStatusRequest>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `GiveCouponReq` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function commercePostCouponsByObjectId(requester: HttpRequester, objectId: bigint | string, payload: GiveCouponReq, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/object/commerce/{objectId}/coupons".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<CommonResponse, GiveCouponReq>({
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
 * @param payload - The `StatSubscriptionNotification` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function commercePostStatsUpdateByObjectId(requester: HttpRequester, objectId: bigint | string, payload: StatSubscriptionNotification, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/object/commerce/{objectId}/stats/update".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<CommonResponse, StatSubscriptionNotification>({
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
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param language - The `language` parameter to include in the API request.
 * @param stores - The `stores` parameter to include in the API request.
 * @param time - The `time` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function commerceGetOffersByObjectId(requester: HttpRequester, objectId: bigint | string, language?: string, stores?: string, time?: string, gamertag?: string): Promise<HttpResponse<GetActiveOffersResponse>> {
  let endpoint = "/object/commerce/{objectId}/offers".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<GetActiveOffersResponse>({
    r: requester,
    e: endpoint,
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
