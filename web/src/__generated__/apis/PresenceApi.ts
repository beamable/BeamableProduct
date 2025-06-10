import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { OnlineStatusQuery } from '@/__generated__/schemas/OnlineStatusQuery';
import { PlayersStatusResponse } from '@/__generated__/schemas/PlayersStatusResponse';

export class PresenceApi {
  constructor(
    private readonly requester: HttpRequester
  ) {
  }
  
  /**
   * @param {OnlineStatusQuery} payload - The `OnlineStatusQuery` instance to use for the API request
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<PlayersStatusResponse>>} A promise containing the HttpResponse of PlayersStatusResponse
   */
  async postPresenceQuery(payload: OnlineStatusQuery, gamertag?: string): Promise<HttpResponse<PlayersStatusResponse>> {
    let endpoint = "/api/presence/query";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PlayersStatusResponse, OnlineStatusQuery>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
}
