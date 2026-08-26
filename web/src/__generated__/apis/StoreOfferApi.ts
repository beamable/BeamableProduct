/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { OfferEntitlementsResponse } from '@/__generated__/schemas/OfferEntitlementsResponse';
import type { OfferRedeemResponse } from '@/__generated__/schemas/OfferRedeemResponse';
import type { RedeemOfferRequest } from '@/__generated__/schemas/RedeemOfferRequest';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param federationId - The store federation to read entitlements from.
 * @param playerId - The player whose entitlements to read.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function storeOfferGetEntitlements(requester: HttpRequester, federationId: string, playerId: string, gamertag?: string): Promise<HttpResponse<OfferEntitlementsResponse>> {
  let endpoint = "/api/store-offer/entitlements";
  
  // Make the API request
  return makeApiRequest<OfferEntitlementsResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      federationId,
      playerId
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
 * @param payload - The `RedeemOfferRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function storeOfferPostRedeem(requester: HttpRequester, payload: RedeemOfferRequest, gamertag?: string): Promise<HttpResponse<OfferRedeemResponse>> {
  let endpoint = "/api/store-offer/redeem";
  
  // Make the API request
  return makeApiRequest<OfferRedeemResponse, RedeemOfferRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}
