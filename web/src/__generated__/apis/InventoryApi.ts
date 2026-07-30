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
import type { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import type { CurrencyContentResponse } from '@/__generated__/schemas/CurrencyContentResponse';
import type { EndTransactionRequest } from '@/__generated__/schemas/EndTransactionRequest';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { InventoryQueryRequest } from '@/__generated__/schemas/InventoryQueryRequest';
import type { InventoryUpdateRequest } from '@/__generated__/schemas/InventoryUpdateRequest';
import type { InventoryView } from '@/__generated__/schemas/InventoryView';
import type { ItemContentResponse } from '@/__generated__/schemas/ItemContentResponse';
import type { MultipliersGetResponse } from '@/__generated__/schemas/MultipliersGetResponse';
import type { PreviewVipBonusResponse } from '@/__generated__/schemas/PreviewVipBonusResponse';
import type { TransferRequest } from '@/__generated__/schemas/TransferRequest';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function inventoryGetItemsBasic(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<ItemContentResponse>> {
  let endpoint = "/basic/inventory/items";
  
  // Make the API request
  return makeApiRequest<ItemContentResponse>({
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
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function inventoryGetCurrencyBasic(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<CurrencyContentResponse>> {
  let endpoint = "/basic/inventory/currency";
  
  // Make the API request
  return makeApiRequest<CurrencyContentResponse>({
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
 * @param payload - The `InventoryUpdateRequest` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function inventoryPutPreviewByObjectId(requester: HttpRequester, objectId: bigint | string, payload: InventoryUpdateRequest, gamertag?: string): Promise<HttpResponse<PreviewVipBonusResponse>> {
  let endpoint = "/object/inventory/{objectId}/preview".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<PreviewVipBonusResponse, InventoryUpdateRequest>({
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
export async function inventoryGetMultipliersByObjectId(requester: HttpRequester, objectId: bigint | string, gamertag?: string): Promise<HttpResponse<MultipliersGetResponse>> {
  let endpoint = "/object/inventory/{objectId}/multipliers".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<MultipliersGetResponse>({
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
 * @param payload - The `EndTransactionRequest` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function inventoryDeleteTransactionByObjectId(requester: HttpRequester, objectId: bigint | string, payload: EndTransactionRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/object/inventory/{objectId}/transaction".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<CommonResponse, EndTransactionRequest>({
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
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param scope - The `scope` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function inventoryGetByObjectId(requester: HttpRequester, objectId: bigint | string, scope?: string, gamertag?: string): Promise<HttpResponse<InventoryView>> {
  let endpoint = "/object/inventory/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<InventoryView>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      scope
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
 * @param payload - The `InventoryQueryRequest` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function inventoryPostByObjectId(requester: HttpRequester, objectId: bigint | string, payload: InventoryQueryRequest, gamertag?: string): Promise<HttpResponse<InventoryView>> {
  let endpoint = "/object/inventory/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<InventoryView, InventoryQueryRequest>({
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
 * @param payload - The `InventoryUpdateRequest` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function inventoryPutByObjectId(requester: HttpRequester, objectId: bigint | string, payload: InventoryUpdateRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/object/inventory/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<CommonResponse, InventoryUpdateRequest>({
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
export async function inventoryPutProxyReloadByObjectId(requester: HttpRequester, objectId: bigint | string, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/object/inventory/{objectId}/proxy/reload".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<CommonResponse>({
    r: requester,
    e: endpoint,
    m: PUT,
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
 * @param payload - The `TransferRequest` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function inventoryPutTransferByObjectId(requester: HttpRequester, objectId: bigint | string, payload: TransferRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/object/inventory/{objectId}/transfer".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<CommonResponse, TransferRequest>({
    r: requester,
    e: endpoint,
    m: PUT,
    p: payload,
    g: gamertag,
    w: true
  });
}
