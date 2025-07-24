/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/__generated__/apis/constants';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import type { AnnouncementContentResponse } from '@/__generated__/schemas/AnnouncementContentResponse';
import type { AnnouncementDto } from '@/__generated__/schemas/AnnouncementDto';
import type { AnnouncementQueryResponse } from '@/__generated__/schemas/AnnouncementQueryResponse';
import type { AnnouncementRawResponse } from '@/__generated__/schemas/AnnouncementRawResponse';
import type { AnnouncementRequest } from '@/__generated__/schemas/AnnouncementRequest';
import type { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import type { DeleteAnnouncementRequest } from '@/__generated__/schemas/DeleteAnnouncementRequest';
import type { EmptyResponse } from '@/__generated__/schemas/EmptyResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { ListDefinitionsResponse } from '@/__generated__/schemas/ListDefinitionsResponse';
import type { ListTagsResponse } from '@/__generated__/schemas/ListTagsResponse';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param tagNameFilter - The `tagNameFilter` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function announcementsGetListTagsBasic(requester: HttpRequester, tagNameFilter?: string, gamertag?: string): Promise<HttpResponse<ListTagsResponse>> {
  let endpoint = "/basic/announcements/list/tags";
  
  // Make the API request
  return makeApiRequest<ListTagsResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      tagNameFilter
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
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function announcementsGetListBasic(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<AnnouncementContentResponse>> {
  let endpoint = "/basic/announcements/list";
  
  // Make the API request
  return makeApiRequest<AnnouncementContentResponse>({
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
 * @param date - The `date` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function announcementsGetSearchBasic(requester: HttpRequester, date?: string, gamertag?: string): Promise<HttpResponse<AnnouncementContentResponse>> {
  let endpoint = "/basic/announcements/search";
  
  // Make the API request
  return makeApiRequest<AnnouncementContentResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      date
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
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function announcementsGetListDefinitionsBasic(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<ListDefinitionsResponse>> {
  let endpoint = "/basic/announcements/list/definitions";
  
  // Make the API request
  return makeApiRequest<ListDefinitionsResponse>({
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
 * @param payload - The `AnnouncementDto` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function announcementsPostBasic(requester: HttpRequester, payload: AnnouncementDto, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/basic/announcements/";
  
  // Make the API request
  return makeApiRequest<EmptyResponse, AnnouncementDto>({
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
 * @param payload - The `DeleteAnnouncementRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function announcementsDeleteBasic(requester: HttpRequester, payload: DeleteAnnouncementRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/basic/announcements/";
  
  // Make the API request
  return makeApiRequest<EmptyResponse, DeleteAnnouncementRequest>({
    r: requester,
    e: endpoint,
    m: DELETE,
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
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function announcementsGetContentBasic(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<AnnouncementContentResponse>> {
  let endpoint = "/basic/announcements/content";
  
  // Make the API request
  return makeApiRequest<AnnouncementContentResponse>({
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
 * @param payload - The `AnnouncementRequest` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function announcementsPutReadByObjectId(requester: HttpRequester, objectId: bigint | string, payload: AnnouncementRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/object/announcements/{objectId}/read".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<CommonResponse, AnnouncementRequest>({
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
 * @param payload - The `AnnouncementRequest` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function announcementsPostClaimByObjectId(requester: HttpRequester, objectId: bigint | string, payload: AnnouncementRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/object/announcements/{objectId}/claim".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<CommonResponse, AnnouncementRequest>({
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
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function announcementsGetRawByObjectId(requester: HttpRequester, objectId: bigint | string, gamertag?: string): Promise<HttpResponse<AnnouncementRawResponse>> {
  let endpoint = "/object/announcements/{objectId}/raw".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<AnnouncementRawResponse>({
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
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param include_deleted - The `include_deleted` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function announcementsGetByObjectId(requester: HttpRequester, objectId: bigint | string, include_deleted?: boolean, gamertag?: string): Promise<HttpResponse<AnnouncementQueryResponse>> {
  let endpoint = "/object/announcements/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<AnnouncementQueryResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      include_deleted
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
 * @param payload - The `AnnouncementRequest` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function announcementsDeleteByObjectId(requester: HttpRequester, objectId: bigint | string, payload: AnnouncementRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/object/announcements/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<CommonResponse, AnnouncementRequest>({
    r: requester,
    e: endpoint,
    m: DELETE,
    p: payload,
    g: gamertag,
    w: true
  });
}
