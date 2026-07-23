/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import type { GetStagingStateResponse } from '@/__generated__/schemas/GetStagingStateResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { MessageRailRegistrationResponse } from '@/__generated__/schemas/MessageRailRegistrationResponse';
import type { RegisterUserWithMessageRailRequest } from '@/__generated__/schemas/RegisterUserWithMessageRailRequest';
import type { SendMessagesRequest } from '@/__generated__/schemas/SendMessagesRequest';
import type { SendMessagesResponse } from '@/__generated__/schemas/SendMessagesResponse';
import type { UnregisterUserWithMessageRailRequest } from '@/__generated__/schemas/UnregisterUserWithMessageRailRequest';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `SendMessagesRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function messageRailPostMessages(requester: HttpRequester, payload: SendMessagesRequest, gamertag?: string): Promise<HttpResponse<SendMessagesResponse>> {
  let endpoint = "/api/message-rail/messages";
  
  // Make the API request
  return makeApiRequest<SendMessagesResponse, SendMessagesRequest>({
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
 * @param externalSystemTrackId - Optional originating-system id to filter.
 * @param federationId - Optional federation id to filter.
 * @param trackId - Optional track id to filter staged items.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function messageRailGetStaging(requester: HttpRequester, externalSystemTrackId?: string, federationId?: string, trackId?: string, gamertag?: string): Promise<HttpResponse<GetStagingStateResponse>> {
  let endpoint = "/api/message-rail/staging";
  
  // Make the API request
  return makeApiRequest<GetStagingStateResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      externalSystemTrackId,
      federationId,
      trackId
    },
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
 * @param payload - The `RegisterUserWithMessageRailRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function messageRailPostRegister(requester: HttpRequester, payload: RegisterUserWithMessageRailRequest, gamertag?: string): Promise<HttpResponse<MessageRailRegistrationResponse>> {
  let endpoint = "/api/message-rail/register";
  
  // Make the API request
  return makeApiRequest<MessageRailRegistrationResponse, RegisterUserWithMessageRailRequest>({
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
 * @param payload - The `UnregisterUserWithMessageRailRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function messageRailPostUnregister(requester: HttpRequester, payload: UnregisterUserWithMessageRailRequest, gamertag?: string): Promise<HttpResponse<MessageRailRegistrationResponse>> {
  let endpoint = "/api/message-rail/unregister";
  
  // Make the API request
  return makeApiRequest<MessageRailRegistrationResponse, UnregisterUserWithMessageRailRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}
