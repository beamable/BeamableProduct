/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { datasetIdPlaceholder } from '@/__generated__/apis/constants';
import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { PATCH } from '@/constants';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import type { ApiDatasetsDeleteDatasetResponse } from '@/__generated__/schemas/ApiDatasetsDeleteDatasetResponse';
import type { CreateDatasetRequest } from '@/__generated__/schemas/CreateDatasetRequest';
import type { Dataset } from '@/__generated__/schemas/Dataset';
import type { DatasetTransferTargetResponse } from '@/__generated__/schemas/DatasetTransferTargetResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { ListDatasetsResponse } from '@/__generated__/schemas/ListDatasetsResponse';
import type { SetDatasetIdentityRequest } from '@/__generated__/schemas/SetDatasetIdentityRequest';
import type { UpdateDatasetRequest } from '@/__generated__/schemas/UpdateDatasetRequest';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function datasetsGet(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<ListDatasetsResponse>> {
  let endpoint = "/api/datasets";
  
  // Make the API request
  return makeApiRequest<ListDatasetsResponse>({
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
 * @param payload - The `CreateDatasetRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function datasetsPost(requester: HttpRequester, payload: CreateDatasetRequest, gamertag?: string): Promise<HttpResponse<Dataset>> {
  let endpoint = "/api/datasets";
  
  // Make the API request
  return makeApiRequest<Dataset, CreateDatasetRequest>({
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
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function datasetsGetByDatasetId(requester: HttpRequester, datasetId: string, gamertag?: string): Promise<HttpResponse<Dataset>> {
  let endpoint = "/api/datasets/{datasetId}".replace(datasetIdPlaceholder, endpointEncoder(datasetId));
  
  // Make the API request
  return makeApiRequest<Dataset>({
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
 * @param payload - The `UpdateDatasetRequest` instance to use for the API request
 * @param datasetId - The `datasetId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function datasetsPatchByDatasetId(requester: HttpRequester, datasetId: string, payload: UpdateDatasetRequest, gamertag?: string): Promise<HttpResponse<Dataset>> {
  let endpoint = "/api/datasets/{datasetId}".replace(datasetIdPlaceholder, endpointEncoder(datasetId));
  
  // Make the API request
  return makeApiRequest<Dataset, UpdateDatasetRequest>({
    r: requester,
    e: endpoint,
    m: PATCH,
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
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function datasetsDeleteByDatasetId(requester: HttpRequester, datasetId: string, gamertag?: string): Promise<HttpResponse<ApiDatasetsDeleteDatasetResponse>> {
  let endpoint = "/api/datasets/{datasetId}".replace(datasetIdPlaceholder, endpointEncoder(datasetId));
  
  // Make the API request
  return makeApiRequest<ApiDatasetsDeleteDatasetResponse>({
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
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function datasetsGetTransferTargetByDatasetId(requester: HttpRequester, datasetId: string, gamertag?: string): Promise<HttpResponse<DatasetTransferTargetResponse>> {
  let endpoint = "/api/datasets/{datasetId}/transfer-target".replace(datasetIdPlaceholder, endpointEncoder(datasetId));
  
  // Make the API request
  return makeApiRequest<DatasetTransferTargetResponse>({
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
 * @param payload - The `SetDatasetIdentityRequest` instance to use for the API request
 * @param datasetId - The `datasetId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function datasetsPutIdentityByDatasetId(requester: HttpRequester, datasetId: string, payload: SetDatasetIdentityRequest, gamertag?: string): Promise<HttpResponse<Dataset>> {
  let endpoint = "/api/datasets/{datasetId}/identity".replace(datasetIdPlaceholder, endpointEncoder(datasetId));
  
  // Make the API request
  return makeApiRequest<Dataset, SetDatasetIdentityRequest>({
    r: requester,
    e: endpoint,
    m: PUT,
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
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function datasetsDeleteIdentityByDatasetId(requester: HttpRequester, datasetId: string, gamertag?: string): Promise<HttpResponse<Dataset>> {
  let endpoint = "/api/datasets/{datasetId}/identity".replace(datasetIdPlaceholder, endpointEncoder(datasetId));
  
  // Make the API request
  return makeApiRequest<Dataset>({
    r: requester,
    e: endpoint,
    m: DELETE,
    g: gamertag,
    w: true
  });
}
