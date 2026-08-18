/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { campaignIdPlaceholder } from '@/__generated__/apis/constants';
import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import { versionPlaceholder } from '@/__generated__/apis/constants';
import type { ApiCampaignsArchivePostCampaignResponse } from '@/__generated__/schemas/ApiCampaignsArchivePostCampaignResponse';
import type { ApiCampaignsDeactivatePostCampaignResponse } from '@/__generated__/schemas/ApiCampaignsDeactivatePostCampaignResponse';
import type { ApiCampaignsDeleteCampaignResponse } from '@/__generated__/schemas/ApiCampaignsDeleteCampaignResponse';
import type { ApiCampaignsGetCampaignResponse } from '@/__generated__/schemas/ApiCampaignsGetCampaignResponse';
import type { ApiCampaignsReactivatePostCampaignResponse } from '@/__generated__/schemas/ApiCampaignsReactivatePostCampaignResponse';
import type { CampaignFunnelDto } from '@/__generated__/schemas/CampaignFunnelDto';
import type { CampaignGraphDto } from '@/__generated__/schemas/CampaignGraphDto';
import type { CampaignStatusDto } from '@/__generated__/schemas/CampaignStatusDto';
import type { DeactivateRequestDto } from '@/__generated__/schemas/DeactivateRequestDto';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { PublishRequestDto } from '@/__generated__/schemas/PublishRequestDto';
import type { PublishResponseDto } from '@/__generated__/schemas/PublishResponseDto';
import type { SaveDraftResponseDto } from '@/__generated__/schemas/SaveDraftResponseDto';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `CampaignGraphDto` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function campaignsPostDraft(requester: HttpRequester, payload: CampaignGraphDto, gamertag?: string): Promise<HttpResponse<SaveDraftResponseDto>> {
  let endpoint = "/api/campaigns/draft";
  
  // Make the API request
  return makeApiRequest<SaveDraftResponseDto, CampaignGraphDto>({
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
 * @param payload - The `PublishRequestDto` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function campaignsPostPublish(requester: HttpRequester, payload: PublishRequestDto, gamertag?: string): Promise<HttpResponse<PublishResponseDto>> {
  let endpoint = "/api/campaigns/publish";
  
  // Make the API request
  return makeApiRequest<PublishResponseDto, PublishRequestDto>({
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
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function campaignsGet(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<ApiCampaignsGetCampaignResponse>> {
  let endpoint = "/api/campaigns";
  
  // Make the API request
  return makeApiRequest<ApiCampaignsGetCampaignResponse>({
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
 * @param campaignId - The `campaignId` parameter to include in the API request.
 * @param version - The `version` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function campaignsGetByCampaignIdAndVersion(requester: HttpRequester, campaignId: string, version: number, gamertag?: string): Promise<HttpResponse<CampaignGraphDto>> {
  let endpoint = "/api/campaigns/{campaignId}/{version}".replace(campaignIdPlaceholder, endpointEncoder(campaignId)).replace(versionPlaceholder, endpointEncoder(version));
  
  // Make the API request
  return makeApiRequest<CampaignGraphDto>({
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
 * @param campaignId - The `campaignId` parameter to include in the API request.
 * @param version - The `version` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function campaignsDelete(requester: HttpRequester, campaignId: string, version: number, gamertag?: string): Promise<HttpResponse<ApiCampaignsDeleteCampaignResponse>> {
  let endpoint = "/api/campaigns/{campaignId}/{version}".replace(campaignIdPlaceholder, endpointEncoder(campaignId)).replace(versionPlaceholder, endpointEncoder(version));
  
  // Make the API request
  return makeApiRequest<ApiCampaignsDeleteCampaignResponse>({
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
 * @param campaignId - The `campaignId` parameter to include in the API request.
 * @param version - The `version` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function campaignsGetStatus(requester: HttpRequester, campaignId: string, version: number, gamertag?: string): Promise<HttpResponse<CampaignStatusDto>> {
  let endpoint = "/api/campaigns/{campaignId}/{version}/status".replace(campaignIdPlaceholder, endpointEncoder(campaignId)).replace(versionPlaceholder, endpointEncoder(version));
  
  // Make the API request
  return makeApiRequest<CampaignStatusDto>({
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
 * @param campaignId - The `campaignId` parameter to include in the API request.
 * @param version - The `version` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function campaignsGetFunnel(requester: HttpRequester, campaignId: string, version: number, gamertag?: string): Promise<HttpResponse<CampaignFunnelDto>> {
  let endpoint = "/api/campaigns/{campaignId}/{version}/funnel".replace(campaignIdPlaceholder, endpointEncoder(campaignId)).replace(versionPlaceholder, endpointEncoder(version));
  
  // Make the API request
  return makeApiRequest<CampaignFunnelDto>({
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
 * @param payload - The `DeactivateRequestDto` instance to use for the API request
 * @param campaignId - The `campaignId` parameter to include in the API request.
 * @param version - The `version` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function campaignsPostDeactivate(requester: HttpRequester, campaignId: string, version: number, payload: DeactivateRequestDto, gamertag?: string): Promise<HttpResponse<ApiCampaignsDeactivatePostCampaignResponse>> {
  let endpoint = "/api/campaigns/{campaignId}/{version}/deactivate".replace(campaignIdPlaceholder, endpointEncoder(campaignId)).replace(versionPlaceholder, endpointEncoder(version));
  
  // Make the API request
  return makeApiRequest<ApiCampaignsDeactivatePostCampaignResponse, DeactivateRequestDto>({
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
 * @param campaignId - The `campaignId` parameter to include in the API request.
 * @param version - The `version` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function campaignsPostArchive(requester: HttpRequester, campaignId: string, version: number, gamertag?: string): Promise<HttpResponse<ApiCampaignsArchivePostCampaignResponse>> {
  let endpoint = "/api/campaigns/{campaignId}/{version}/archive".replace(campaignIdPlaceholder, endpointEncoder(campaignId)).replace(versionPlaceholder, endpointEncoder(version));
  
  // Make the API request
  return makeApiRequest<ApiCampaignsArchivePostCampaignResponse>({
    r: requester,
    e: endpoint,
    m: POST,
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
 * @param campaignId - The `campaignId` parameter to include in the API request.
 * @param version - The `version` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function campaignsPostReactivate(requester: HttpRequester, campaignId: string, version: number, gamertag?: string): Promise<HttpResponse<ApiCampaignsReactivatePostCampaignResponse>> {
  let endpoint = "/api/campaigns/{campaignId}/{version}/reactivate".replace(campaignIdPlaceholder, endpointEncoder(campaignId)).replace(versionPlaceholder, endpointEncoder(version));
  
  // Make the API request
  return makeApiRequest<ApiCampaignsReactivatePostCampaignResponse>({
    r: requester,
    e: endpoint,
    m: POST,
    g: gamertag,
    w: true
  });
}
