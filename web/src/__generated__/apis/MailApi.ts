/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/__generated__/apis/constants';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import type { AcceptMultipleAttachments } from '@/__generated__/schemas/AcceptMultipleAttachments';
import type { BulkSendMailRequest } from '@/__generated__/schemas/BulkSendMailRequest';
import type { BulkUpdateMailObjectRequest } from '@/__generated__/schemas/BulkUpdateMailObjectRequest';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { ListMailCategoriesResponse } from '@/__generated__/schemas/ListMailCategoriesResponse';
import type { MailQueryResponse } from '@/__generated__/schemas/MailQueryResponse';
import type { MailResponse } from '@/__generated__/schemas/MailResponse';
import type { MailSearchRequest } from '@/__generated__/schemas/MailSearchRequest';
import type { MailSearchResponse } from '@/__generated__/schemas/MailSearchResponse';
import type { MailSuccessResponse } from '@/__generated__/schemas/MailSuccessResponse';
import type { MailTemplate } from '@/__generated__/schemas/MailTemplate';
import type { SendMailObjectRequest } from '@/__generated__/schemas/SendMailObjectRequest';
import type { SendMailResponse } from '@/__generated__/schemas/SendMailResponse';
import type { UpdateMailRequest } from '@/__generated__/schemas/UpdateMailRequest';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `AcceptMultipleAttachments` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function mailPutAttachmentsBasic(requester: HttpRequester, payload: AcceptMultipleAttachments, gamertag?: string): Promise<HttpResponse<MailSuccessResponse>> {
  let endpoint = "/basic/mail/attachments";
  
  // Make the API request
  return makeApiRequest<MailSuccessResponse, AcceptMultipleAttachments>({
    r: requester,
    e: endpoint,
    m: PUT,
    p: payload,
    g: gamertag,
    w: true
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param gamerTag - The `gamerTag` parameter to include in the API request.
 * @param templateName - The `templateName` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function mailGetTemplateBasic(requester: HttpRequester, gamerTag: bigint | string, templateName: string, gamertag?: string): Promise<HttpResponse<MailTemplate>> {
  let endpoint = "/basic/mail/template";
  
  // Make the API request
  return makeApiRequest<MailTemplate>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      gamerTag,
      templateName
    },
    g: gamertag
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param mid - The `mid` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function mailGetBasic(requester: HttpRequester, mid: bigint | string, gamertag?: string): Promise<HttpResponse<MailResponse>> {
  let endpoint = "/basic/mail/";
  
  // Make the API request
  return makeApiRequest<MailResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      mid
    },
    g: gamertag
  });
}

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `UpdateMailRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function mailPutBasic(requester: HttpRequester, payload: UpdateMailRequest, gamertag?: string): Promise<HttpResponse<MailSuccessResponse>> {
  let endpoint = "/basic/mail/";
  
  // Make the API request
  return makeApiRequest<MailSuccessResponse, UpdateMailRequest>({
    r: requester,
    e: endpoint,
    m: PUT,
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
 * @param payload - The `BulkSendMailRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function mailPostBulkBasic(requester: HttpRequester, payload: BulkSendMailRequest, gamertag?: string): Promise<HttpResponse<MailSuccessResponse>> {
  let endpoint = "/basic/mail/bulk";
  
  // Make the API request
  return makeApiRequest<MailSuccessResponse, BulkSendMailRequest>({
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
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param mid - The `mid` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function mailGetDetailByObjectId(requester: HttpRequester, objectId: bigint | string, mid: bigint | string, gamertag?: string): Promise<HttpResponse<MailResponse>> {
  let endpoint = "/object/mail/{objectId}/detail".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<MailResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      mid
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
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function mailGetCategoriesByObjectId(requester: HttpRequester, objectId: bigint | string, gamertag?: string): Promise<HttpResponse<ListMailCategoriesResponse>> {
  let endpoint = "/object/mail/{objectId}/categories".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<ListMailCategoriesResponse>({
    r: requester,
    e: endpoint,
    m: GET,
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
 * @param payload - The `MailSearchRequest` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function mailPostSearchByObjectId(requester: HttpRequester, objectId: bigint | string, payload: MailSearchRequest, gamertag?: string): Promise<HttpResponse<MailSearchResponse>> {
  let endpoint = "/object/mail/{objectId}/search".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<MailSearchResponse, MailSearchRequest>({
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
 * @param payload - The `BulkSendMailRequest` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function mailPostBulkByObjectId(requester: HttpRequester, objectId: bigint | string, payload: BulkSendMailRequest, gamertag?: string): Promise<HttpResponse<MailSuccessResponse>> {
  let endpoint = "/object/mail/{objectId}/bulk".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<MailSuccessResponse, BulkSendMailRequest>({
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
 * @param payload - The `BulkUpdateMailObjectRequest` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function mailPutBulkByObjectId(requester: HttpRequester, objectId: bigint | string, payload: BulkUpdateMailObjectRequest, gamertag?: string): Promise<HttpResponse<MailSuccessResponse>> {
  let endpoint = "/object/mail/{objectId}/bulk".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<MailSuccessResponse, BulkUpdateMailObjectRequest>({
    r: requester,
    e: endpoint,
    m: PUT,
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
 * @param payload - The `AcceptMultipleAttachments` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function mailPutAcceptManyByObjectId(requester: HttpRequester, objectId: bigint | string, payload: AcceptMultipleAttachments, gamertag?: string): Promise<HttpResponse<MailSuccessResponse>> {
  let endpoint = "/object/mail/{objectId}/accept/many".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<MailSuccessResponse, AcceptMultipleAttachments>({
    r: requester,
    e: endpoint,
    m: PUT,
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
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function mailGetByObjectId(requester: HttpRequester, objectId: bigint | string, gamertag?: string): Promise<HttpResponse<MailQueryResponse>> {
  let endpoint = "/object/mail/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<MailQueryResponse>({
    r: requester,
    e: endpoint,
    m: GET,
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
 * @param payload - The `SendMailObjectRequest` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function mailPostByObjectId(requester: HttpRequester, objectId: bigint | string, payload: SendMailObjectRequest, gamertag?: string): Promise<HttpResponse<SendMailResponse>> {
  let endpoint = "/object/mail/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<SendMailResponse, SendMailObjectRequest>({
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
 * @param payload - The `UpdateMailRequest` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function mailPutByObjectId(requester: HttpRequester, objectId: bigint | string, payload: UpdateMailRequest, gamertag?: string): Promise<HttpResponse<MailSuccessResponse>> {
  let endpoint = "/object/mail/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<MailSuccessResponse, UpdateMailRequest>({
    r: requester,
    e: endpoint,
    m: PUT,
    p: payload,
    g: gamertag,
    w: true
  });
}
