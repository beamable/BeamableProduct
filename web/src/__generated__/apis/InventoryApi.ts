import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { CurrencyContentResponse } from '@/__generated__/schemas/CurrencyContentResponse';
import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { EndTransactionRequest } from '@/__generated__/schemas/EndTransactionRequest';
import { GET } from '@/constants';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpResponse } from '@/network/http/types/HttpResponse';
import { InventoryQueryRequest } from '@/__generated__/schemas/InventoryQueryRequest';
import { InventoryUpdateRequest } from '@/__generated__/schemas/InventoryUpdateRequest';
import { InventoryView } from '@/__generated__/schemas/InventoryView';
import { ItemContentResponse } from '@/__generated__/schemas/ItemContentResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { MultipliersGetResponse } from '@/__generated__/schemas/MultipliersGetResponse';
import { objectIdPlaceholder } from '@/constants';
import { POST } from '@/constants';
import { PreviewVipBonusResponse } from '@/__generated__/schemas/PreviewVipBonusResponse';
import { PUT } from '@/constants';
import { TransferRequest } from '@/__generated__/schemas/TransferRequest';

export class InventoryApi {
  constructor(
    private readonly r: HttpRequester
  ) {
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ItemContentResponse>>} A promise containing the HttpResponse of ItemContentResponse
   */
  async getInventoryItems(gamertag?: string): Promise<HttpResponse<ItemContentResponse>> {
    let e = "/basic/inventory/items";
    
    // Make the API request
    return makeApiRequest<ItemContentResponse>({
      r: this.r,
      e,
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
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CurrencyContentResponse>>} A promise containing the HttpResponse of CurrencyContentResponse
   */
  async getInventoryCurrency(gamertag?: string): Promise<HttpResponse<CurrencyContentResponse>> {
    let e = "/basic/inventory/currency";
    
    // Make the API request
    return makeApiRequest<CurrencyContentResponse>({
      r: this.r,
      e,
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
   * @param {InventoryUpdateRequest} payload - The `InventoryUpdateRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PreviewVipBonusResponse>>} A promise containing the HttpResponse of PreviewVipBonusResponse
   */
  async putInventoryPreviewByObjectId(objectId: bigint | string, payload: InventoryUpdateRequest, gamertag?: string): Promise<HttpResponse<PreviewVipBonusResponse>> {
    let e = "/object/inventory/{objectId}/preview".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<PreviewVipBonusResponse, InventoryUpdateRequest>({
      r: this.r,
      e,
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
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<MultipliersGetResponse>>} A promise containing the HttpResponse of MultipliersGetResponse
   */
  async getInventoryMultipliersByObjectId(objectId: bigint | string, gamertag?: string): Promise<HttpResponse<MultipliersGetResponse>> {
    let e = "/object/inventory/{objectId}/multipliers".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<MultipliersGetResponse>({
      r: this.r,
      e,
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
   * @param {EndTransactionRequest} payload - The `EndTransactionRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async deleteInventoryTransactionByObjectId(objectId: bigint | string, payload: EndTransactionRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/object/inventory/{objectId}/transaction".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CommonResponse, EndTransactionRequest>({
      r: this.r,
      e,
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
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} scope - The `scope` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<InventoryView>>} A promise containing the HttpResponse of InventoryView
   */
  async getInventoryByObjectId(objectId: bigint | string, scope?: string, gamertag?: string): Promise<HttpResponse<InventoryView>> {
    let e = "/object/inventory/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<InventoryView>({
      r: this.r,
      e,
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
   * @param {InventoryQueryRequest} payload - The `InventoryQueryRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<InventoryView>>} A promise containing the HttpResponse of InventoryView
   */
  async postInventoryByObjectId(objectId: bigint | string, payload: InventoryQueryRequest, gamertag?: string): Promise<HttpResponse<InventoryView>> {
    let e = "/object/inventory/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<InventoryView, InventoryQueryRequest>({
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
   * @param {InventoryUpdateRequest} payload - The `InventoryUpdateRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putInventoryByObjectId(objectId: bigint | string, payload: InventoryUpdateRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/object/inventory/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CommonResponse, InventoryUpdateRequest>({
      r: this.r,
      e,
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
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putInventoryProxyReloadByObjectId(objectId: bigint | string, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/object/inventory/{objectId}/proxy/reload".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CommonResponse>({
      r: this.r,
      e,
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
   * @param {TransferRequest} payload - The `TransferRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putInventoryTransferByObjectId(objectId: bigint | string, payload: TransferRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/object/inventory/{objectId}/transfer".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CommonResponse, TransferRequest>({
      r: this.r,
      e,
      m: PUT,
      p: payload,
      g: gamertag,
      w: true
    });
  }
}
