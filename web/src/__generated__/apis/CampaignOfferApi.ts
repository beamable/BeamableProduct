/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { CampaignOfferEntitlementsResponse } from '@/__generated__/schemas/CampaignOfferEntitlementsResponse';
import type { CampaignOfferRedeemResponse } from '@/__generated__/schemas/CampaignOfferRedeemResponse';
import type { RedeemCampaignOfferRequest } from '@/__generated__/schemas/RedeemCampaignOfferRequest';

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
export async function campaignOfferGetEntitlements(requester: HttpRequester, federationId: string, playerId: string, gamertag?: string): Promise<HttpResponse<CampaignOfferEntitlementsResponse>> {
  let endpoint = "/api/campaign-offer/entitlements";
  
  // Make the API request
  return makeApiRequest<CampaignOfferEntitlementsResponse>({
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
 * @param payload - The `RedeemCampaignOfferRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function campaignOfferPostRedeem(requester: HttpRequester, payload: RedeemCampaignOfferRequest, gamertag?: string): Promise<HttpResponse<CampaignOfferRedeemResponse>> {
  let endpoint = "/api/campaign-offer/redeem";
  
  // Make the API request
  return makeApiRequest<CampaignOfferRedeemResponse, RedeemCampaignOfferRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}
