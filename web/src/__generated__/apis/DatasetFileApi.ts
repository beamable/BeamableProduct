/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { datasetIdPlaceholder } from '@/__generated__/apis/constants';
import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { fileIdPlaceholder } from '@/__generated__/apis/constants';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { partNumberPlaceholder } from '@/__generated__/apis/constants';
import { POST } from '@/constants';
import type { ApiDatasetsFilesDeleteDatasetFileResponse } from '@/__generated__/schemas/ApiDatasetsFilesDeleteDatasetFileResponse';
import type { CompleteDatasetFileUploadRequest } from '@/__generated__/schemas/CompleteDatasetFileUploadRequest';
import type { DatasetFile } from '@/__generated__/schemas/DatasetFile';
import type { DatasetFilePartUploadUrlResponse } from '@/__generated__/schemas/DatasetFilePartUploadUrlResponse';
import type { DatasetFileUploadResponse } from '@/__generated__/schemas/DatasetFileUploadResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { ListDatasetFilesResponse } from '@/__generated__/schemas/ListDatasetFilesResponse';
import type { RegisterDatasetFileRequest } from '@/__generated__/schemas/RegisterDatasetFileRequest';

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
export async function datasetsGetFilesByDatasetId(requester: HttpRequester, datasetId: string, gamertag?: string): Promise<HttpResponse<ListDatasetFilesResponse>> {
  let endpoint = "/api/datasets/{datasetId}/files".replace(datasetIdPlaceholder, endpointEncoder(datasetId));
  
  // Make the API request
  return makeApiRequest<ListDatasetFilesResponse>({
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
 * @param payload - The `RegisterDatasetFileRequest` instance to use for the API request
 * @param datasetId - The `datasetId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function datasetsPostFilesByDatasetId(requester: HttpRequester, datasetId: string, payload: RegisterDatasetFileRequest, gamertag?: string): Promise<HttpResponse<DatasetFileUploadResponse>> {
  let endpoint = "/api/datasets/{datasetId}/files".replace(datasetIdPlaceholder, endpointEncoder(datasetId));
  
  // Make the API request
  return makeApiRequest<DatasetFileUploadResponse, RegisterDatasetFileRequest>({
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
 * @param fileId - The `fileId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function datasetsGetFiles(requester: HttpRequester, datasetId: string, fileId: string, gamertag?: string): Promise<HttpResponse<DatasetFile>> {
  let endpoint = "/api/datasets/{datasetId}/files/{fileId}".replace(datasetIdPlaceholder, endpointEncoder(datasetId)).replace(fileIdPlaceholder, endpointEncoder(fileId));
  
  // Make the API request
  return makeApiRequest<DatasetFile>({
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
 * @param fileId - The `fileId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function datasetsDeleteFiles(requester: HttpRequester, datasetId: string, fileId: string, gamertag?: string): Promise<HttpResponse<ApiDatasetsFilesDeleteDatasetFileResponse>> {
  let endpoint = "/api/datasets/{datasetId}/files/{fileId}".replace(datasetIdPlaceholder, endpointEncoder(datasetId)).replace(fileIdPlaceholder, endpointEncoder(fileId));
  
  // Make the API request
  return makeApiRequest<ApiDatasetsFilesDeleteDatasetFileResponse>({
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
 * @param datasetId - The `datasetId` parameter to include in the API request.
 * @param fileId - The `fileId` parameter to include in the API request.
 * @param partNumber - The `partNumber` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function datasetsPostFilesPartsUploadUrl(requester: HttpRequester, datasetId: string, fileId: string, partNumber: number, gamertag?: string): Promise<HttpResponse<DatasetFilePartUploadUrlResponse>> {
  let endpoint = "/api/datasets/{datasetId}/files/{fileId}/parts/{partNumber}/upload-url".replace(datasetIdPlaceholder, endpointEncoder(datasetId)).replace(fileIdPlaceholder, endpointEncoder(fileId)).replace(partNumberPlaceholder, endpointEncoder(partNumber));
  
  // Make the API request
  return makeApiRequest<DatasetFilePartUploadUrlResponse>({
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
 * @param payload - The `CompleteDatasetFileUploadRequest` instance to use for the API request
 * @param datasetId - The `datasetId` parameter to include in the API request.
 * @param fileId - The `fileId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function datasetsPostFilesComplete(requester: HttpRequester, datasetId: string, fileId: string, payload: CompleteDatasetFileUploadRequest, gamertag?: string): Promise<HttpResponse<DatasetFile>> {
  let endpoint = "/api/datasets/{datasetId}/files/{fileId}/complete".replace(datasetIdPlaceholder, endpointEncoder(datasetId)).replace(fileIdPlaceholder, endpointEncoder(fileId));
  
  // Make the API request
  return makeApiRequest<DatasetFile, CompleteDatasetFileUploadRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}
