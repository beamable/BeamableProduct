/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { OnlineStatusQuery } from '@/__generated__/schemas/OnlineStatusQuery';
import type { PlayersStatusResponse } from '@/__generated__/schemas/PlayersStatusResponse';

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
