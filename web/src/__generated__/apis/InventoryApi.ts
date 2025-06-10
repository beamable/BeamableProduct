import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { CurrencyContentResponse } from '@/__generated__/schemas/CurrencyContentResponse';
import { EndTransactionRequest } from '@/__generated__/schemas/EndTransactionRequest';
import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { InventoryQueryRequest } from '@/__generated__/schemas/InventoryQueryRequest';
import { InventoryUpdateRequest } from '@/__generated__/schemas/InventoryUpdateRequest';
import { InventoryView } from '@/__generated__/schemas/InventoryView';
import { ItemContentResponse } from '@/__generated__/schemas/ItemContentResponse';
import { makeQueryString } from '@/utils/makeQueryString';
import { MultipliersGetResponse } from '@/__generated__/schemas/MultipliersGetResponse';
import { PreviewVipBonusResponse } from '@/__generated__/schemas/PreviewVipBonusResponse';
import { TransferRequest } from '@/__generated__/schemas/TransferRequest';

export class InventoryApi {
  constructor(
    private readonly requester: HttpRequester
  ) {
  }
  
  /**
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ItemContentResponse>>} A promise containing the HttpResponse of ItemContentResponse
   */
  async getInventoryItems(gamertag?: string): Promise<HttpResponse<ItemContentResponse>> {
    let endpoint = "/basic/inventory/items";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<ItemContentResponse>({
      url: endpoint,
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CurrencyContentResponse>>} A promise containing the HttpResponse of CurrencyContentResponse
   */
  async getInventoryCurrency(gamertag?: string): Promise<HttpResponse<CurrencyContentResponse>> {
    let endpoint = "/basic/inventory/currency";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CurrencyContentResponse>({
      url: endpoint,
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {InventoryUpdateRequest} payload - The `InventoryUpdateRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PreviewVipBonusResponse>>} A promise containing the HttpResponse of PreviewVipBonusResponse
   */
  async putInventoryPreviewByObjectId(objectId: bigint | string, payload: InventoryUpdateRequest, gamertag?: string): Promise<HttpResponse<PreviewVipBonusResponse>> {
    let endpoint = "/object/inventory/{objectId}/preview";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PreviewVipBonusResponse, InventoryUpdateRequest>({
      url: endpoint,
      method: HttpMethod.PUT,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<MultipliersGetResponse>>} A promise containing the HttpResponse of MultipliersGetResponse
   */
  async getInventoryMultipliersByObjectId(objectId: bigint | string, gamertag?: string): Promise<HttpResponse<MultipliersGetResponse>> {
    let endpoint = "/object/inventory/{objectId}/multipliers";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<MultipliersGetResponse>({
      url: endpoint,
      method: HttpMethod.GET,
      headers
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
    let endpoint = "/object/inventory/{objectId}/transaction";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, EndTransactionRequest>({
      url: endpoint,
      method: HttpMethod.DELETE,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} scope - The `scope` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<InventoryView>>} A promise containing the HttpResponse of InventoryView
   */
  async getInventoryByObjectId(objectId: bigint | string, scope?: string, gamertag?: string): Promise<HttpResponse<InventoryView>> {
    let endpoint = "/object/inventory/{objectId}/";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      scope
    });
    
    // Make the API request
    return this.requester.request<InventoryView>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {InventoryQueryRequest} payload - The `InventoryQueryRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<InventoryView>>} A promise containing the HttpResponse of InventoryView
   */
  async postInventoryByObjectId(objectId: bigint | string, payload: InventoryQueryRequest, gamertag?: string): Promise<HttpResponse<InventoryView>> {
    let endpoint = "/object/inventory/{objectId}/";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<InventoryView, InventoryQueryRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {InventoryUpdateRequest} payload - The `InventoryUpdateRequest` instance to use for the API request
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putInventoryByObjectId(objectId: bigint | string, payload: InventoryUpdateRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/object/inventory/{objectId}/";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, InventoryUpdateRequest>({
      url: endpoint,
      method: HttpMethod.PUT,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putInventoryProxyReloadByObjectId(objectId: bigint | string, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/object/inventory/{objectId}/proxy/reload";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse>({
      url: endpoint,
      method: HttpMethod.PUT,
      headers
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
    let endpoint = "/object/inventory/{objectId}/transfer";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, TransferRequest>({
      url: endpoint,
      method: HttpMethod.PUT,
      headers,
      body: payload,
      withAuth: true
    });
  }
}
