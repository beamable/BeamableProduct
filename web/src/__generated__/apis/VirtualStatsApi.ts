/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { idPlaceholder } from '@/__generated__/apis/constants';
import { keyPlaceholder } from '@/__generated__/apis/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { playerIdPlaceholder } from '@/__generated__/apis/constants';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import { visibilityPlaceholder } from '@/__generated__/apis/constants';
import type { ApiStatsVirtualDeleteVirtualStatsResponse } from '@/__generated__/schemas/ApiStatsVirtualDeleteVirtualStatsResponse';
import type { GetVirtualStatsResponse } from '@/__generated__/schemas/GetVirtualStatsResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { ListJobsResponse } from '@/__generated__/schemas/ListJobsResponse';
import type { ListVirtualStatsResponse } from '@/__generated__/schemas/ListVirtualStatsResponse';
import type { SaveVirtualStatRequest } from '@/__generated__/schemas/SaveVirtualStatRequest';
import type { StatsVisibility } from '@/__generated__/schemas/enums/StatsVisibility';
import type { UpdateVirtualStatRequest } from '@/__generated__/schemas/UpdateVirtualStatRequest';
import type { ValidateVirtualStatRequest } from '@/__generated__/schemas/ValidateVirtualStatRequest';
import type { ValidateVirtualStatResponse } from '@/__generated__/schemas/ValidateVirtualStatResponse';
import type { VirtualStatDefinition } from '@/__generated__/schemas/VirtualStatDefinition';
import type { VirtualStatJobView } from '@/__generated__/schemas/VirtualStatJobView';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function statsGetVirtual(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<ListVirtualStatsResponse>> {
  let endpoint = "/api/stats/virtual";
  
  // Make the API request
  return makeApiRequest<ListVirtualStatsResponse>({
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
 * @param payload - The `SaveVirtualStatRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function statsPostVirtual(requester: HttpRequester, payload: SaveVirtualStatRequest, gamertag?: string): Promise<HttpResponse<VirtualStatDefinition>> {
  let endpoint = "/api/stats/virtual";
  
  // Make the API request
  return makeApiRequest<VirtualStatDefinition, SaveVirtualStatRequest>({
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
 * @param payload - The `ValidateVirtualStatRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function statsPostVirtualValidate(requester: HttpRequester, payload: ValidateVirtualStatRequest, gamertag?: string): Promise<HttpResponse<ValidateVirtualStatResponse>> {
  let endpoint = "/api/stats/virtual/validate";
  
  // Make the API request
  return makeApiRequest<ValidateVirtualStatResponse, ValidateVirtualStatRequest>({
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
 * @param payload - The `UpdateVirtualStatRequest` instance to use for the API request
 * @param key - The `key` parameter to include in the API request.
 * @param visibility - The `visibility` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function statsPutVirtual(requester: HttpRequester, key: string, visibility: StatsVisibility, payload: UpdateVirtualStatRequest, gamertag?: string): Promise<HttpResponse<VirtualStatDefinition>> {
  let endpoint = "/api/stats/virtual/{visibility}/{key}".replace(keyPlaceholder, endpointEncoder(key)).replace(visibilityPlaceholder, endpointEncoder(visibility));
  
  // Make the API request
  return makeApiRequest<VirtualStatDefinition, UpdateVirtualStatRequest>({
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
 * @param key - The `key` parameter to include in the API request.
 * @param visibility - The `visibility` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function statsDeleteVirtual(requester: HttpRequester, key: string, visibility: StatsVisibility, gamertag?: string): Promise<HttpResponse<ApiStatsVirtualDeleteVirtualStatsResponse>> {
  let endpoint = "/api/stats/virtual/{visibility}/{key}".replace(keyPlaceholder, endpointEncoder(key)).replace(visibilityPlaceholder, endpointEncoder(visibility));
  
  // Make the API request
  return makeApiRequest<ApiStatsVirtualDeleteVirtualStatsResponse>({
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
 * @param key - The `key` parameter to include in the API request.
 * @param playerId - The `playerId` parameter to include in the API request.
 * @param visibility - The `visibility` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function statsGetVirtualByKeyAndPlayerIdAndVisibility(requester: HttpRequester, key: string, playerId: string, visibility: StatsVisibility, gamertag?: string): Promise<HttpResponse<GetVirtualStatsResponse>> {
  let endpoint = "/api/stats/virtual/{visibility}/{key}/{playerId}".replace(keyPlaceholder, endpointEncoder(key)).replace(playerIdPlaceholder, endpointEncoder(playerId)).replace(visibilityPlaceholder, endpointEncoder(visibility));
  
  // Make the API request
  return makeApiRequest<GetVirtualStatsResponse>({
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
 * @param importId - The `importId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function statsGetVirtualJobs(requester: HttpRequester, importId?: string, gamertag?: string): Promise<HttpResponse<ListJobsResponse>> {
  let endpoint = "/api/stats/virtual/jobs";
  
  // Make the API request
  return makeApiRequest<ListJobsResponse>({
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
 * @param id - The `id` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function statsGetVirtualJobsById(requester: HttpRequester, id: string, gamertag?: string): Promise<HttpResponse<VirtualStatJobView>> {
  let endpoint = "/api/stats/virtual/jobs/{id}".replace(idPlaceholder, endpointEncoder(id));
  
  // Make the API request
  return makeApiRequest<VirtualStatJobView>({
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
 * @param id - The `id` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function statsPostVirtualJobsCancelById(requester: HttpRequester, id: string, gamertag?: string): Promise<HttpResponse<VirtualStatJobView>> {
  let endpoint = "/api/stats/virtual/jobs/{id}/cancel".replace(idPlaceholder, endpointEncoder(id));
  
  // Make the API request
  return makeApiRequest<VirtualStatJobView>({
    r: requester,
    e: endpoint,
    m: POST,
    g: gamertag,
    w: true
  });
}
