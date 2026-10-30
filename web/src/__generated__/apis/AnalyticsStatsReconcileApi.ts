/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { idPlaceholder } from '@/__generated__/apis/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import type { GetStatsReconcileRunsResponse } from '@/__generated__/schemas/GetStatsReconcileRunsResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { StatsReconcileRunView } from '@/__generated__/schemas/StatsReconcileRunView';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function analyticsGetStatsReconcile(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<GetStatsReconcileRunsResponse>> {
  let endpoint = "/api/analytics/stats-reconcile";
  
  // Make the API request
  return makeApiRequest<GetStatsReconcileRunsResponse>({
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
export async function analyticsGetStatsReconcileById(requester: HttpRequester, id: string, gamertag?: string): Promise<HttpResponse<StatsReconcileRunView>> {
  let endpoint = "/api/analytics/stats-reconcile/{id}".replace(idPlaceholder, endpointEncoder(id));
  
  // Make the API request
  return makeApiRequest<StatsReconcileRunView>({
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
export async function analyticsPostStatsReconcileCancelById(requester: HttpRequester, id: string, gamertag?: string): Promise<HttpResponse<StatsReconcileRunView>> {
  let endpoint = "/api/analytics/stats-reconcile/{id}/cancel".replace(idPlaceholder, endpointEncoder(id));
  
  // Make the API request
  return makeApiRequest<StatsReconcileRunView>({
    r: requester,
    e: endpoint,
    m: POST,
    g: gamertag,
    w: true
  });
}
