/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import type { CampaignOfferGrantResponse } from '@/__generated__/schemas/CampaignOfferGrantResponse';
import type { CampaignOfferRedeemResponse } from '@/__generated__/schemas/CampaignOfferRedeemResponse';
import type { CampaignOffersResponse } from '@/__generated__/schemas/CampaignOffersResponse';
import type { CampaignOfferState } from '@/__generated__/schemas/enums/CampaignOfferState';
import type { GrantCampaignOfferRequest } from '@/__generated__/schemas/GrantCampaignOfferRequest';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { RedeemCampaignOfferRequest } from '@/__generated__/schemas/RedeemCampaignOfferRequest';
import type { RevokeCampaignOfferRequest } from '@/__generated__/schemas/RevokeCampaignOfferRequest';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `GrantCampaignOfferRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function campaignOfferPostGrant(requester: HttpRequester, payload: GrantCampaignOfferRequest, gamertag?: string): Promise<HttpResponse<CampaignOfferGrantResponse>> {
  let endpoint = "/api/campaign-offer/grant";
  
  // Make the API request
  return makeApiRequest<CampaignOfferGrantResponse, GrantCampaignOfferRequest>({
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
 * @param payload - The `RevokeCampaignOfferRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function campaignOfferPostRevoke(requester: HttpRequester, payload: RevokeCampaignOfferRequest, gamertag?: string): Promise<HttpResponse<CampaignOfferGrantResponse>> {
  let endpoint = "/api/campaign-offer/revoke";
  
  // Make the API request
  return makeApiRequest<CampaignOfferGrantResponse, RevokeCampaignOfferRequest>({
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

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param federationId - Which store to ask.
 * @param playerId - The player. Must be the caller unless privileged.
 * @param states - Which states to return, repeated (`states=Granted&states=Redeemed`). Omitted means all.
 * `states=Granted` is what a store screen wants — "unredeemed" would also include revoked and
 * expired. A comma-separated value (`states=Granted,Redeemed`) is accepted too; see
 * M:BeamableGateway.Controllers.CampaignOfferController.ParseStates(BeamableShared.Federation.CampaignOffer.CampaignOfferState[]).
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function campaignOfferGetCampaignOffers(requester: HttpRequester, federationId?: string, playerId?: string, states?: CampaignOfferState[], gamertag?: string): Promise<HttpResponse<CampaignOffersResponse>> {
  let endpoint = "/api/campaign-offer/campaign-offers";
  
  // Make the API request
  return makeApiRequest<CampaignOffersResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      federationId,
      playerId,
      states
    },
    g: gamertag,
    w: true
  });
}
