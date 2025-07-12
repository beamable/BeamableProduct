import { ApiMailboxPublishPostMailboxResponse } from '@/__generated__/schemas/ApiMailboxPublishPostMailboxResponse';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpResponse } from '@/network/http/types/HttpResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { MessageRequest } from '@/__generated__/schemas/MessageRequest';
import { POST } from '@/constants';

export class MailboxApi {
  constructor(
    private readonly r: HttpRequester
  ) {
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {MessageRequest} payload - The `MessageRequest` instance to use for the API request
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<ApiMailboxPublishPostMailboxResponse>>} A promise containing the HttpResponse of ApiMailboxPublishPostMailboxResponse
   */
  async postMailboxPublish(payload: MessageRequest, gamertag?: string): Promise<HttpResponse<ApiMailboxPublishPostMailboxResponse>> {
    let e = "/api/mailbox/publish";
    
    // Make the API request
    return makeApiRequest<ApiMailboxPublishPostMailboxResponse, MessageRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
    });
  }
}
