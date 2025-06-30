import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpResponse } from '@/network/http/types/HttpResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { OnlineStatusQuery } from '@/__generated__/schemas/OnlineStatusQuery';
import { PlayersStatusResponse } from '@/__generated__/schemas/PlayersStatusResponse';
import { POST } from '@/constants';

export class PresenceApi {
  constructor(
    private readonly r: HttpRequester
  ) {
  }
  
  /**
   * @param {OnlineStatusQuery} payload - The `OnlineStatusQuery` instance to use for the API request
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<PlayersStatusResponse>>} A promise containing the HttpResponse of PlayersStatusResponse
   */
  async postPresenceQuery(payload: OnlineStatusQuery, gamertag?: string): Promise<HttpResponse<PlayersStatusResponse>> {
    let e = "/api/presence/query";
    
    // Make the API request
    return makeApiRequest<PlayersStatusResponse, OnlineStatusQuery>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
}
