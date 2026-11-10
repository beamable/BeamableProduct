/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { grantIdPlaceholder } from '@/__generated__/apis/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { originIdPlaceholder } from '@/__generated__/apis/constants';
import { POST } from '@/constants';
import type { GrantOffersRequest } from '@/__generated__/schemas/GrantOffersRequest';
import type { GrantOffersResult } from '@/__generated__/schemas/GrantOffersResult';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { OfferGrantState } from '@/__generated__/schemas/enums/OfferGrantState';
import type { OnHoldGrantsResult } from '@/__generated__/schemas/OnHoldGrantsResult';
import type { PlayerOffersPage } from '@/__generated__/schemas/PlayerOffersPage';
import type { RedeemOfferRequest } from '@/__generated__/schemas/RedeemOfferRequest';
import type { RedeemOfferResult } from '@/__generated__/schemas/RedeemOfferResult';
import type { ReportGrantsRedeemedRequest } from '@/__generated__/schemas/ReportGrantsRedeemedRequest';
import type { ReportGrantsRedeemedResult } from '@/__generated__/schemas/ReportGrantsRedeemedResult';
import type { ResolveGrantsRequest } from '@/__generated__/schemas/ResolveGrantsRequest';
import type { ResolveGrantsResult } from '@/__generated__/schemas/ResolveGrantsResult';
import type { RevokeGrantsRequest } from '@/__generated__/schemas/RevokeGrantsRequest';
import type { RevokeGrantsResult } from '@/__generated__/schemas/RevokeGrantsResult';
import type { RevokeOfferOriginResult } from '@/__generated__/schemas/RevokeOfferOriginResult';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `GrantOffersRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function offersPostGrants(requester: HttpRequester, payload: GrantOffersRequest, gamertag?: string): Promise<HttpResponse<GrantOffersResult>> {
  let endpoint = "/api/offers/grants";
  
  // Make the API request
  return makeApiRequest<GrantOffersResult, GrantOffersRequest>({
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
 * @param payload - The `RevokeGrantsRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function offersPostGrantsRevocations(requester: HttpRequester, payload: RevokeGrantsRequest, gamertag?: string): Promise<HttpResponse<RevokeGrantsResult>> {
  let endpoint = "/api/offers/grants/revocations";
  
  // Make the API request
  return makeApiRequest<RevokeGrantsResult, RevokeGrantsRequest>({
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
 * @param limit - Most grants in one page.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function offersGetGrantsOnHold(requester: HttpRequester, cursor?: string, limit?: number, gamertag?: string): Promise<HttpResponse<OnHoldGrantsResult>> {
  let endpoint = "/api/offers/grants/on-hold";
  
  // Make the API request
  return makeApiRequest<OnHoldGrantsResult>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      cursor,
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
 * @param payload - The `ResolveGrantsRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function offersPostGrantsResolutions(requester: HttpRequester, payload: ResolveGrantsRequest, gamertag?: string): Promise<HttpResponse<ResolveGrantsResult>> {
  let endpoint = "/api/offers/grants/resolutions";
  
  // Make the API request
  return makeApiRequest<ResolveGrantsResult, ResolveGrantsRequest>({
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
 * @param payload - The `ReportGrantsRedeemedRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function offersPostGrantsRedemptions(requester: HttpRequester, payload: ReportGrantsRedeemedRequest, gamertag?: string): Promise<HttpResponse<ReportGrantsRedeemedResult>> {
  let endpoint = "/api/offers/grants/redemptions";
  
  // Make the API request
  return makeApiRequest<ReportGrantsRedeemedResult, ReportGrantsRedeemedRequest>({
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
 * @param originId - The batch, as its grants named it.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function offersDeleteOriginsByOriginId(requester: HttpRequester, originId: string, gamertag?: string): Promise<HttpResponse<RevokeOfferOriginResult>> {
  let endpoint = "/api/offers/origins/{originId}".replace(originIdPlaceholder, endpointEncoder(originId));
  
  // Make the API request
  return makeApiRequest<RevokeOfferOriginResult>({
    r: requester,
    e: endpoint,
    m: DELETE,
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
 * @param grantId - The grant to redeem.
 * @param payload - The `RedeemOfferRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function offersPostGrantsRedeemByGrantId(requester: HttpRequester, grantId: string, payload: RedeemOfferRequest, gamertag?: string): Promise<HttpResponse<RedeemOfferResult>> {
  let endpoint = "/api/offers/grants/{grantId}/redeem".replace(grantIdPlaceholder, endpointEncoder(grantId));
  
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
export async function offersGetGrants(requester: HttpRequester, cursor?: string, federationId?: string, playerId?: bigint | string, states?: OfferGrantState[], gamertag?: string): Promise<HttpResponse<PlayerOffersPage>> {
  let endpoint = "/api/offers/grants";
  
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
