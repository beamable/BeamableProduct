/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { datasetIdPlaceholder } from '@/__generated__/apis/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { importIdPlaceholder } from '@/__generated__/apis/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import type { AnalyticsEventsImportSpec } from '@/__generated__/schemas/AnalyticsEventsImportSpec';
import type { DatasetImport } from '@/__generated__/schemas/DatasetImport';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { ImportPreviewResponse } from '@/__generated__/schemas/ImportPreviewResponse';
import type { ImportResponse } from '@/__generated__/schemas/ImportResponse';
import type { ListImportIdPartsResponse } from '@/__generated__/schemas/ListImportIdPartsResponse';
import type { ListImportsResponse } from '@/__generated__/schemas/ListImportsResponse';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param datasetId - The `datasetId` parameter to include in the API request.
 * @param target - The `target` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function datasetsGetImportsPreviewByDatasetId(requester: HttpRequester, datasetId: string, target?: string, gamertag?: string): Promise<HttpResponse<ImportPreviewResponse>> {
  let endpoint = "/api/datasets/{datasetId}/imports/preview".replace(datasetIdPlaceholder, endpointEncoder(datasetId));
  
  // Make the API request
  return makeApiRequest<ImportPreviewResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      target
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
 * @param datasetId - The `datasetId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function datasetsGetImportsByDatasetId(requester: HttpRequester, datasetId: string, gamertag?: string): Promise<HttpResponse<ListImportsResponse>> {
  let endpoint = "/api/datasets/{datasetId}/imports".replace(datasetIdPlaceholder, endpointEncoder(datasetId));
  
  // Make the API request
  return makeApiRequest<ListImportsResponse>({
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
 * @param datasetId - The `datasetId` parameter to include in the API request.
 * @param importId - The `importId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function datasetsGetImports(requester: HttpRequester, datasetId: string, importId: string, gamertag?: string): Promise<HttpResponse<ImportResponse>> {
  let endpoint = "/api/datasets/{datasetId}/imports/{importId}".replace(datasetIdPlaceholder, endpointEncoder(datasetId)).replace(importIdPlaceholder, endpointEncoder(importId));
  
  // Make the API request
  return makeApiRequest<ImportResponse>({
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
 * @param datasetId - The `datasetId` parameter to include in the API request.
 * @param importId - The `importId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function datasetsGetImportsIdParts(requester: HttpRequester, datasetId: string, importId: string, gamertag?: string): Promise<HttpResponse<ListImportIdPartsResponse>> {
  let endpoint = "/api/datasets/{datasetId}/imports/{importId}/id-parts".replace(datasetIdPlaceholder, endpointEncoder(datasetId)).replace(importIdPlaceholder, endpointEncoder(importId));
  
  // Make the API request
  return makeApiRequest<ListImportIdPartsResponse>({
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
 * @param payload - The `AnalyticsEventsImportSpec` instance to use for the API request
 * @param datasetId - The `datasetId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function datasetsPostImportsAnalyticsEventsByDatasetId(requester: HttpRequester, datasetId: string, payload: AnalyticsEventsImportSpec, gamertag?: string): Promise<HttpResponse<DatasetImport>> {
  let endpoint = "/api/datasets/{datasetId}/imports/analytics-events".replace(datasetIdPlaceholder, endpointEncoder(datasetId));
  
  // Make the API request
  return makeApiRequest<DatasetImport, AnalyticsEventsImportSpec>({
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
 * @param datasetId - The `datasetId` parameter to include in the API request.
 * @param importId - The `importId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function datasetsPostImportsCancel(requester: HttpRequester, datasetId: string, importId: string, gamertag?: string): Promise<HttpResponse<DatasetImport>> {
  let endpoint = "/api/datasets/{datasetId}/imports/{importId}/cancel".replace(datasetIdPlaceholder, endpointEncoder(datasetId)).replace(importIdPlaceholder, endpointEncoder(importId));
  
  // Make the API request
  return makeApiRequest<DatasetImport>({
    r: requester,
    e: endpoint,
    m: POST,
    g: gamertag,
    w: true
  });
}
