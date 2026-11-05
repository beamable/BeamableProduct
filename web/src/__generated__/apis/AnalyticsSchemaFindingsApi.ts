/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { eventPlaceholder } from '@/__generated__/apis/constants';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import { queryExecutionIdPlaceholder } from '@/__generated__/apis/constants';
import type { AnalyticsDeletionStatus } from '@/__generated__/schemas/AnalyticsDeletionStatus';
import type { AnalyticsFindingKind } from '@/__generated__/schemas/enums/AnalyticsFindingKind';
import type { AnalyticsPreviewResponse } from '@/__generated__/schemas/AnalyticsPreviewResponse';
import type { ApiAnalyticsSchemaFindingsDeleteAnalyticsSchemaFindingsResponse } from '@/__generated__/schemas/ApiAnalyticsSchemaFindingsDeleteAnalyticsSchemaFindingsResponse';
import type { DeleteAnalyticsRowsRequest } from '@/__generated__/schemas/DeleteAnalyticsRowsRequest';
import type { DeleteAnalyticsRowsResponse } from '@/__generated__/schemas/DeleteAnalyticsRowsResponse';
import type { GetAnalyticsSchemaFindingsResponse } from '@/__generated__/schemas/GetAnalyticsSchemaFindingsResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param importId - The `importId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function analyticsGetSchemaFindings(requester: HttpRequester, importId?: string, gamertag?: string): Promise<HttpResponse<GetAnalyticsSchemaFindingsResponse>> {
  let endpoint = "/api/analytics/schema-findings";
  
  // Make the API request
  return makeApiRequest<GetAnalyticsSchemaFindingsResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      importId
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
 * @param event - The `event` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function analyticsDeleteSchemaFindingsByEvent(requester: HttpRequester, event: string, gamertag?: string): Promise<HttpResponse<ApiAnalyticsSchemaFindingsDeleteAnalyticsSchemaFindingsResponse>> {
  let endpoint = "/api/analytics/schema-findings/{event}".replace(eventPlaceholder, endpointEncoder(event));
  
  // Make the API request
  return makeApiRequest<ApiAnalyticsSchemaFindingsDeleteAnalyticsSchemaFindingsResponse>({
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
 * @param payload - The `DeleteAnalyticsRowsRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function analyticsPostSchemaFindingsDeleteRows(requester: HttpRequester, payload: DeleteAnalyticsRowsRequest, gamertag?: string): Promise<HttpResponse<DeleteAnalyticsRowsResponse>> {
  let endpoint = "/api/analytics/schema-findings/delete-rows";
  
  // Make the API request
  return makeApiRequest<DeleteAnalyticsRowsResponse, DeleteAnalyticsRowsRequest>({
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
 * @param event - The `event` parameter to include in the API request.
 * @param from - The `from` parameter to include in the API request.
 * @param key - The `key` parameter to include in the API request.
 * @param kind - The `kind` parameter to include in the API request.
 * @param limit - The `limit` parameter to include in the API request.
 * @param to - The `to` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function analyticsGetSchemaFindingsPreviewByEvent(requester: HttpRequester, event: string, from?: string, key?: string, kind?: AnalyticsFindingKind, limit?: number, to?: string, gamertag?: string): Promise<HttpResponse<AnalyticsPreviewResponse>> {
  let endpoint = "/api/analytics/schema-findings/{event}/preview".replace(eventPlaceholder, endpointEncoder(event));
  
  // Make the API request
  return makeApiRequest<AnalyticsPreviewResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      from,
      key,
      kind,
      limit,
      to
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
 * @param queryExecutionId - The `queryExecutionId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function analyticsGetSchemaFindingsDeletionsByQueryExecutionId(requester: HttpRequester, queryExecutionId: string, gamertag?: string): Promise<HttpResponse<AnalyticsDeletionStatus>> {
  let endpoint = "/api/analytics/schema-findings/deletions/{queryExecutionId}".replace(queryExecutionIdPlaceholder, endpointEncoder(queryExecutionId));
  
  // Make the API request
  return makeApiRequest<AnalyticsDeletionStatus>({
    r: requester,
    e: endpoint,
    m: GET,
    g: gamertag,
    w: true
  });
}
