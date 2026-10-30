/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import type { GrantOfferRequest } from '@/__generated__/schemas/GrantOfferRequest';
import type { GrantOfferResult } from '@/__generated__/schemas/GrantOfferResult';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { AwaitingOffersResult } from '@/__generated__/schemas/AwaitingOffersResult';
import type { OfferGrantState } from '@/__generated__/schemas/enums/OfferGrantState';
import type { PlayerOffersPage } from '@/__generated__/schemas/PlayerOffersPage';
import type { RedeemOfferRequest } from '@/__generated__/schemas/RedeemOfferRequest';
import type { RedeemOfferResult } from '@/__generated__/schemas/RedeemOfferResult';
import type { ResolveOfferRequest } from '@/__generated__/schemas/ResolveOfferRequest';
import type { ResolveOfferResult } from '@/__generated__/schemas/ResolveOfferResult';
import type { ReportOfferRedeemedRequest } from '@/__generated__/schemas/ReportOfferRedeemedRequest';
import type { ReportOfferRedeemedResult } from '@/__generated__/schemas/ReportOfferRedeemedResult';
import type { RevokeOfferOriginRequest } from '@/__generated__/schemas/RevokeOfferOriginRequest';
import type { RevokeOfferOriginResult } from '@/__generated__/schemas/RevokeOfferOriginResult';
import type { RevokeOfferRequest } from '@/__generated__/schemas/RevokeOfferRequest';
import type { RevokeOfferResult } from '@/__generated__/schemas/RevokeOfferResult';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `GrantOfferRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function offersPostGrant(requester: HttpRequester, payload: GrantOfferRequest, gamertag?: string): Promise<HttpResponse<GrantOfferResult>> {
  let endpoint = "/api/offers/grant";
  
  // Make the API request
  return makeApiRequest<GrantOfferResult, GrantOfferRequest>({
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
 * @param payload - The `RevokeOfferRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function offersPostRevoke(requester: HttpRequester, payload: RevokeOfferRequest, gamertag?: string): Promise<HttpResponse<RevokeOfferResult>> {
  let endpoint = "/api/offers/revoke";
  
  // Make the API request
  return makeApiRequest<RevokeOfferResult, RevokeOfferRequest>({
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
 * @param limit - The `limit` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function offersGetAwaitingOperator(requester: HttpRequester, limit?: number, gamertag?: string): Promise<HttpResponse<AwaitingOffersResult>> {
  let endpoint = "/api/offers/awaiting-operator";
  
  // Make the API request
  return makeApiRequest<AwaitingOffersResult>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      limit
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
 * @param payload - The `ResolveOfferRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function offersPostResolve(requester: HttpRequester, payload: ResolveOfferRequest, gamertag?: string): Promise<HttpResponse<ResolveOfferResult>> {
  let endpoint = "/api/offers/resolve";
  
  // Make the API request
  return makeApiRequest<ResolveOfferResult, ResolveOfferRequest>({
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
 * @param payload - The `ReportOfferRedeemedRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function offersPostReportRedeemed(requester: HttpRequester, payload: ReportOfferRedeemedRequest, gamertag?: string): Promise<HttpResponse<ReportOfferRedeemedResult>> {
  let endpoint = "/api/offers/report-redeemed";
  
  // Make the API request
  return makeApiRequest<ReportOfferRedeemedResult, ReportOfferRedeemedRequest>({
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
 * @param payload - The `RevokeOfferOriginRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function offersPostRevokeOrigin(requester: HttpRequester, payload: RevokeOfferOriginRequest, gamertag?: string): Promise<HttpResponse<RevokeOfferOriginResult>> {
  let endpoint = "/api/offers/revoke-origin";
  
  // Make the API request
  return makeApiRequest<RevokeOfferOriginResult, RevokeOfferOriginRequest>({
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
 * @param payload - The `RedeemOfferRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function offersPostRedeem(requester: HttpRequester, payload: RedeemOfferRequest, gamertag?: string): Promise<HttpResponse<RedeemOfferResult>> {
  let endpoint = "/api/offers/redeem";
  
  // Make the API request
  return makeApiRequest<RedeemOfferResult, RedeemOfferRequest>({
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
 * @param cursor - The `next` of the previous page.
 * @param federationId - Only this provider's offers.
 * @param playerId - The player. Must be the caller unless privileged.
 * @param states - Which states to return, repeated (`states=Granted&states=Redeemed`) or comma-separated.
 * Omitted means what the player can still use: Granted and Claiming.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function offersGet(requester: HttpRequester, cursor?: string, federationId?: string, playerId?: bigint | string, states?: OfferGrantState[], gamertag?: string): Promise<HttpResponse<PlayerOffersPage>> {
  let endpoint = "/api/offers";
  
  // Make the API request
  return makeApiRequest<PlayerOffersPage>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      cursor,
      federationId,
      playerId,
      states
    },
    g: gamertag,
    w: true
  });
}
