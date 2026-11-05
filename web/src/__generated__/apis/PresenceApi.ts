/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { OnlineStatusQuery } from '@/__generated__/schemas/OnlineStatusQuery';
import type { PlayersStatusResponse } from '@/__generated__/schemas/PlayersStatusResponse';
import type { RecentlyOnlineResponse } from '@/__generated__/schemas/RecentlyOnlineResponse';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param limit - How many players to return; capped at 100.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function presenceGetRecent(requester: HttpRequester, limit?: number, gamertag?: string): Promise<HttpResponse<RecentlyOnlineResponse>> {
  let endpoint = "/api/presence/recent";
  
  // Make the API request
  return makeApiRequest<RecentlyOnlineResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      limit
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
 * @param payload - The `OnlineStatusQuery` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function presencePostQuery(requester: HttpRequester, payload: OnlineStatusQuery, gamertag?: string): Promise<HttpResponse<PlayersStatusResponse>> {
  let endpoint = "/api/presence/query";
  
  // Make the API request
  return makeApiRequest<PlayersStatusResponse, OnlineStatusQuery>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}
