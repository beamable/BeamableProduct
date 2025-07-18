import { ApiMailboxPublishPostMailboxResponse } from '@/__generated__/schemas/ApiMailboxPublishPostMailboxResponse';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpResponse } from '@/network/http/types/HttpResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { MessageRequest } from '@/__generated__/schemas/MessageRequest';
import { POST } from '@/constants';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `MessageRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function mailboxPostPublish(requester: HttpRequester, payload: MessageRequest, gamertag?: string): Promise<HttpResponse<ApiMailboxPublishPostMailboxResponse>> {
  let endpoint = "/api/mailbox/publish";
  
  // Make the API request
  return makeApiRequest<ApiMailboxPublishPostMailboxResponse, MessageRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}
