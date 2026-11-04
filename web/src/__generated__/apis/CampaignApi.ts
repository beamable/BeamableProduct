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
import type { ApiCampaignsCancelPostCampaignResponse } from '@/__generated__/schemas/ApiCampaignsCancelPostCampaignResponse';
import type { ApiCampaignsDeleteCampaignResponse } from '@/__generated__/schemas/ApiCampaignsDeleteCampaignResponse';
import type { ApiCampaignsRejectPostCampaignResponse } from '@/__generated__/schemas/ApiCampaignsRejectPostCampaignResponse';
import type { ApiCampaignsUnarchivePostCampaignResponse } from '@/__generated__/schemas/ApiCampaignsUnarchivePostCampaignResponse';
import type { ApiCampaignsWindDownPostCampaignResponse } from '@/__generated__/schemas/ApiCampaignsWindDownPostCampaignResponse';
import type { CampaignFunnelDto } from '@/__generated__/schemas/CampaignFunnelDto';
import type { CampaignGraphDto } from '@/__generated__/schemas/CampaignGraphDto';
import type { CampaignLifecycle } from '@/__generated__/schemas/enums/CampaignLifecycle';
import type { CampaignStatusDto } from '@/__generated__/schemas/CampaignStatusDto';
import type { CampaignSummaryDtoCampaignPageDto } from '@/__generated__/schemas/CampaignSummaryDtoCampaignPageDto';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { NewVersionResponseDto } from '@/__generated__/schemas/NewVersionResponseDto';
import type { QueueCheckDto } from '@/__generated__/schemas/QueueCheckDto';
import type { RejectRequestDto } from '@/__generated__/schemas/RejectRequestDto';
import type { ReviewResponseDto } from '@/__generated__/schemas/ReviewResponseDto';
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
 * @param payload - The `CampaignGraphDto` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function campaignsPostSubmit(requester: HttpRequester, payload: CampaignGraphDto, gamertag?: string): Promise<HttpResponse<ReviewResponseDto>> {
  let endpoint = "/api/campaigns/submit";
  
  // Make the API request
  return makeApiRequest<ReviewResponseDto, CampaignGraphDto>({
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
 * @param override - The `override` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function campaignsPostSchedule(requester: HttpRequester, campaignId: string, version: number, override?: boolean, gamertag?: string): Promise<HttpResponse<ReviewResponseDto>> {
  let endpoint = "/api/campaigns/{campaignId}/{version}/schedule".replace(campaignIdPlaceholder, endpointEncoder(campaignId)).replace(versionPlaceholder, endpointEncoder(version));
  
  // Make the API request
  return makeApiRequest<ReviewResponseDto>({
    r: requester,
    e: endpoint,
    m: POST,
    q: {
      override
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
 * @param payload - The `CampaignGraphDto` instance to use for the API request
 * @param campaignId - The `campaignId` parameter to include in the API request.
 * @param version - The `version` parameter to include in the API request.
 * @param override - The `override` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function campaignsPostScheduleWithEdits(requester: HttpRequester, campaignId: string, version: number, payload: CampaignGraphDto, override?: boolean, gamertag?: string): Promise<HttpResponse<ReviewResponseDto>> {
  let endpoint = "/api/campaigns/{campaignId}/{version}/schedule-with-edits".replace(campaignIdPlaceholder, endpointEncoder(campaignId)).replace(versionPlaceholder, endpointEncoder(version));
  
  // Make the API request
  return makeApiRequest<ReviewResponseDto, CampaignGraphDto>({
    r: requester,
    e: endpoint,
    m: POST,
    q: {
      override
    },
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
export async function campaignsGetQueueCheck(requester: HttpRequester, campaignId: string, version: number, gamertag?: string): Promise<HttpResponse<QueueCheckDto>> {
  let endpoint = "/api/campaigns/{campaignId}/{version}/queue-check".replace(campaignIdPlaceholder, endpointEncoder(campaignId)).replace(versionPlaceholder, endpointEncoder(version));
  
  // Make the API request
  return makeApiRequest<QueueCheckDto>({
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
 * @param payload - The `RejectRequestDto` instance to use for the API request
 * @param campaignId - The `campaignId` parameter to include in the API request.
 * @param version - The `version` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function campaignsPostReject(requester: HttpRequester, campaignId: string, version: number, payload: RejectRequestDto, gamertag?: string): Promise<HttpResponse<ApiCampaignsRejectPostCampaignResponse>> {
  let endpoint = "/api/campaigns/{campaignId}/{version}/reject".replace(campaignIdPlaceholder, endpointEncoder(campaignId)).replace(versionPlaceholder, endpointEncoder(version));
  
  // Make the API request
  return makeApiRequest<ApiCampaignsRejectPostCampaignResponse, RejectRequestDto>({
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
export async function campaignsPostCancel(requester: HttpRequester, campaignId: string, version: number, gamertag?: string): Promise<HttpResponse<ApiCampaignsCancelPostCampaignResponse>> {
  let endpoint = "/api/campaigns/{campaignId}/{version}/cancel".replace(campaignIdPlaceholder, endpointEncoder(campaignId)).replace(versionPlaceholder, endpointEncoder(version));
  
  // Make the API request
  return makeApiRequest<ApiCampaignsCancelPostCampaignResponse>({
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
export async function campaignsPostRestore(requester: HttpRequester, campaignId: string, version: number, gamertag?: string): Promise<HttpResponse<ReviewResponseDto>> {
  let endpoint = "/api/campaigns/{campaignId}/{version}/restore".replace(campaignIdPlaceholder, endpointEncoder(campaignId)).replace(versionPlaceholder, endpointEncoder(version));
  
  // Make the API request
  return makeApiRequest<ReviewResponseDto>({
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
export async function campaignsPostNewVersion(requester: HttpRequester, campaignId: string, version: number, gamertag?: string): Promise<HttpResponse<NewVersionResponseDto>> {
  let endpoint = "/api/campaigns/{campaignId}/{version}/new-version".replace(campaignIdPlaceholder, endpointEncoder(campaignId)).replace(versionPlaceholder, endpointEncoder(version));
  
  // Make the API request
  return makeApiRequest<NewVersionResponseDto>({
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
 * @param createdAfter - The `createdAfter` parameter to include in the API request.
 * @param createdBefore - The `createdBefore` parameter to include in the API request.
 * @param createdBy - The `createdBy` parameter to include in the API request.
 * @param descending - The `descending` parameter to include in the API request.
 * @param includeArchived - The `includeArchived` parameter to include in the API request.
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
export async function campaignsGet(requester: HttpRequester, campaignId?: string, createdAfter?: Date, createdBefore?: Date, createdBy?: string, descending?: boolean, includeArchived?: boolean, limit?: number, name?: string, phase?: CampaignLifecycle[], publishedAfter?: Date, publishedBefore?: Date, publishedBy?: string, realm?: string, skip?: number, sort?: unknown, tag?: string, gamertag?: string): Promise<HttpResponse<CampaignSummaryDtoCampaignPageDto>> {
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
      includeArchived,
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
 * @param live - The `live` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function campaignsGetFunnel(requester: HttpRequester, campaignId: string, version: number, live?: boolean, gamertag?: string): Promise<HttpResponse<CampaignFunnelDto>> {
  let endpoint = "/api/campaigns/{campaignId}/{version}/funnel".replace(campaignIdPlaceholder, endpointEncoder(campaignId)).replace(versionPlaceholder, endpointEncoder(version));
  
  // Make the API request
  return makeApiRequest<CampaignFunnelDto>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      live
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
export async function campaignsPostWindDown(requester: HttpRequester, campaignId: string, version: number, gamertag?: string): Promise<HttpResponse<ApiCampaignsWindDownPostCampaignResponse>> {
  let endpoint = "/api/campaigns/{campaignId}/{version}/wind-down".replace(campaignIdPlaceholder, endpointEncoder(campaignId)).replace(versionPlaceholder, endpointEncoder(version));
  
  // Make the API request
  return makeApiRequest<ApiCampaignsWindDownPostCampaignResponse>({
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
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function campaignsPostArchiveByCampaignId(requester: HttpRequester, campaignId: string, gamertag?: string): Promise<HttpResponse<ApiCampaignsArchivePostCampaignResponse>> {
  let endpoint = "/api/campaigns/{campaignId}/archive".replace(campaignIdPlaceholder, endpointEncoder(campaignId));
  
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
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function campaignsPostUnarchiveByCampaignId(requester: HttpRequester, campaignId: string, gamertag?: string): Promise<HttpResponse<ApiCampaignsUnarchivePostCampaignResponse>> {
  let endpoint = "/api/campaigns/{campaignId}/unarchive".replace(campaignIdPlaceholder, endpointEncoder(campaignId));
  
  // Make the API request
  return makeApiRequest<ApiCampaignsUnarchivePostCampaignResponse>({
    r: requester,
    e: endpoint,
    m: POST,
    g: gamertag,
    w: true
  });
}
