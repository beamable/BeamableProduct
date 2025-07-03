import { EmptyRsp } from '@/__generated__/schemas/EmptyRsp';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpResponse } from '@/network/http/types/HttpResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import { RegisterReq } from '@/__generated__/schemas/RegisterReq';
import { SendReq } from '@/__generated__/schemas/SendReq';

export class PushApi {
  constructor(
    private readonly r: HttpRequester
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
    let e = "/basic/push/register";
    
    // Make the API request
    return makeApiRequest<EmptyRsp, RegisterReq>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
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
    let e = "/basic/push/send";
    
    // Make the API request
    return makeApiRequest<EmptyRsp, SendReq>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
    });
  }
}
