/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { customerIdPlaceholder } from '@/__generated__/apis/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { realmIdPlaceholder } from '@/__generated__/apis/constants';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { RealmErrorLogCursorPagedResult } from '@/__generated__/schemas/RealmErrorLogCursorPagedResult';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param customerId - The `customerId` parameter to include in the API request.
 * @param realmId - The `realmId` parameter to include in the API request.
 * @param cursor - The `cursor` parameter to include in the API request.
 * @param from - The `from` parameter to include in the API request.
 * @param gamerTag - The `gamerTag` parameter to include in the API request.
 * @param limit - The `limit` parameter to include in the API request.
 * @param service - The `service` parameter to include in the API request.
 * @param status - The `status` parameter to include in the API request.
 * @param to - The `to` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetRealmsAdminErrors(requester: HttpRequester, customerId: string, realmId: string, cursor?: string, from?: Date, gamerTag?: string, limit?: number, service?: string, status?: number, to?: Date, gamertag?: string): Promise<HttpResponse<RealmErrorLogCursorPagedResult>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/admin/errors".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
  // Make the API request
  return makeApiRequest<RealmErrorLogCursorPagedResult>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      cursor,
      from,
      gamerTag,
      limit,
      service,
      status,
      to
    },
    g: gamertag,
    w: true
  });
}
