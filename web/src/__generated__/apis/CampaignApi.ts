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
import type { ApiCampaignsReactivatePostCampaignResponse } from '@/__generated__/schemas/ApiCampaignsReactivatePostCampaignResponse';
import type { CampaignFunnelDto } from '@/__generated__/schemas/CampaignFunnelDto';
import type { CampaignGraphDto } from '@/__generated__/schemas/CampaignGraphDto';
import type { CampaignLifecycle } from '@/__generated__/schemas/enums/CampaignLifecycle';
import type { CampaignStatusDto } from '@/__generated__/schemas/CampaignStatusDto';
import type { CampaignSummaryDtoCampaignPageDto } from '@/__generated__/schemas/CampaignSummaryDtoCampaignPageDto';
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
 * @param campaignId - The `campaignId` parameter to include in the API request.
 * @param createdAfter - The `createdAfter` parameter to include in the API request.
 * @param createdBefore - The `createdBefore` parameter to include in the API request.
 * @param createdBy - The `createdBy` parameter to include in the API request.
 * @param descending - The `descending` parameter to include in the API request.
 * @param limit - The `limit` parameter to include in the API request.
 * @param name - The `name` parameter to include in the API request.
 * @param phase - The `phase` parameter to include in the API request.
 * @param publishedAfter - The `publishedAfter` parameter to include in the API request.
 * @param publishedBefore - The `publishedBefore` parameter to include in the API request.
 * @param publishedBy - The `publishedBy` parameter to include in the API request.
 * @param realm - The `realm` parameter to include in the API request.
 * @param skip - The `skip` parameter to include in the API request.
 * @param sort - The `sort` parameter to include in the API request.
 * @param tag - The `tag` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function campaignsGet(requester: HttpRequester, campaignId?: string, createdAfter?: Date, createdBefore?: Date, createdBy?: string, descending?: boolean, limit?: number, name?: string, phase?: CampaignLifecycle[], publishedAfter?: Date, publishedBefore?: Date, publishedBy?: string, realm?: string, skip?: number, sort?: unknown, tag?: string, gamertag?: string): Promise<HttpResponse<CampaignSummaryDtoCampaignPageDto>> {
  let endpoint = "/api/campaigns";
  
  // Make the API request
  return makeApiRequest<CampaignSummaryDtoCampaignPageDto>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      campaignId,
      createdAfter,
      createdBefore,
      createdBy,
      descending,
      limit,
      name,
      phase,
      publishedAfter,
      publishedBefore,
      publishedBy,
      realm,
      skip,
      sort,
      tag
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
