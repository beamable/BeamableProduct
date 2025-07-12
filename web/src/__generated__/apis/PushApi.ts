import { EmptyRsp } from '@/__generated__/schemas/EmptyRsp';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpResponse } from '@/network/http/types/HttpResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import { RegisterReq } from '@/__generated__/schemas/RegisterReq';
import { SendReq } from '@/__generated__/schemas/SendReq';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `RegisterReq` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postPushRegister(requester: HttpRequester, payload: RegisterReq, gamertag?: string): Promise<HttpResponse<EmptyRsp>> {
  let endpoint = "/basic/push/register";
  
  // Make the API request
  return makeApiRequest<EmptyRsp, RegisterReq>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `SendReq` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postPushSend(requester: HttpRequester, payload: SendReq, gamertag?: string): Promise<HttpResponse<EmptyRsp>> {
  let endpoint = "/basic/push/send";
  
  // Make the API request
  return makeApiRequest<EmptyRsp, SendReq>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}
