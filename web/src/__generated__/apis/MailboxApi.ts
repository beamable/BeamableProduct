import { ApiMailboxPublishPostMailboxResponse } from '@/__generated__/schemas/ApiMailboxPublishPostMailboxResponse';
import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { MessageRequest } from '@/__generated__/schemas/MessageRequest';

export class MailboxApi {
  constructor(
    private readonly requester: HttpRequester
  ) {
  }
  
  /**
   * @param {MessageRequest} payload - The `MessageRequest` instance to use for the API request
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<ApiMailboxPublishPostMailboxResponse>>} A promise containing the HttpResponse of ApiMailboxPublishPostMailboxResponse
   */
  async postMailboxPublish(payload: MessageRequest, gamertag?: string): Promise<HttpResponse<ApiMailboxPublishPostMailboxResponse>> {
    let endpoint = "/api/mailbox/publish";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<ApiMailboxPublishPostMailboxResponse, MessageRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
}
