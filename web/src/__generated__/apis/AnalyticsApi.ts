/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import type { AnalyticsQueryRequest } from '@/__generated__/schemas/AnalyticsQueryRequest';
import type { ApiAnalyticsQueryPostAnalyticsResponse } from '@/__generated__/schemas/ApiAnalyticsQueryPostAnalyticsResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `AnalyticsQueryRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function analyticsPostQuery(requester: HttpRequester, payload: AnalyticsQueryRequest, gamertag?: string): Promise<HttpResponse<ApiAnalyticsQueryPostAnalyticsResponse>> {
  let endpoint = "/api/analytics/query";
  
  // Make the API request
  return makeApiRequest<ApiAnalyticsQueryPostAnalyticsResponse, AnalyticsQueryRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}
