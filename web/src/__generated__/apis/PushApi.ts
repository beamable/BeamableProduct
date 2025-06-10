import { EmptyRsp } from '@/__generated__/schemas/EmptyRsp';
import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { RegisterReq } from '@/__generated__/schemas/RegisterReq';
import { SendReq } from '@/__generated__/schemas/SendReq';

export class PushApi {
  constructor(
    private readonly requester: HttpRequester
  ) {
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {RegisterReq} payload - The `RegisterReq` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyRsp>>} A promise containing the HttpResponse of EmptyRsp
   */
  async postPushRegister(payload: RegisterReq, gamertag?: string): Promise<HttpResponse<EmptyRsp>> {
    let endpoint = "/basic/push/register";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyRsp, RegisterReq>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {SendReq} payload - The `SendReq` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyRsp>>} A promise containing the HttpResponse of EmptyRsp
   */
  async postPushSend(payload: SendReq, gamertag?: string): Promise<HttpResponse<EmptyRsp>> {
    let endpoint = "/basic/push/send";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyRsp, SendReq>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
}
