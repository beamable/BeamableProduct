import { BatchReadStatsResponse } from '@/__generated__/schemas/BatchReadStatsResponse';
import { BatchSetStatsRequest } from '@/__generated__/schemas/BatchSetStatsRequest';
import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { EmptyResponse } from '@/__generated__/schemas/EmptyResponse';
import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { makeQueryString } from '@/utils/makeQueryString';
import { SearchExtendedRequest } from '@/__generated__/schemas/SearchExtendedRequest';
import { SearchExtendedResponse } from '@/__generated__/schemas/SearchExtendedResponse';
import { StatRequest } from '@/__generated__/schemas/StatRequest';
import { StatsResponse } from '@/__generated__/schemas/StatsResponse';
import { StatsSearchRequest } from '@/__generated__/schemas/StatsSearchRequest';
import { StatsSearchResponse } from '@/__generated__/schemas/StatsSearchResponse';
import { StatsSubscribeRequest } from '@/__generated__/schemas/StatsSubscribeRequest';
import { StatsUnsubscribeRequest } from '@/__generated__/schemas/StatsUnsubscribeRequest';
import { StatUpdateRequest } from '@/__generated__/schemas/StatUpdateRequest';
import { StatUpdateRequestStringListFormat } from '@/__generated__/schemas/StatUpdateRequestStringListFormat';

export class StatsApi {
  constructor(
    private readonly requester: HttpRequester
  ) {
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {StatsSubscribeRequest} payload - The `StatsSubscribeRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putStatSubscribe(payload: StatsSubscribeRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/basic/stats/subscribe";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, StatsSubscribeRequest>({
      url: endpoint,
      method: HttpMethod.PUT,
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
   * @param {StatsUnsubscribeRequest} payload - The `StatsUnsubscribeRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async deleteStatSubscribe(payload: StatsUnsubscribeRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/basic/stats/subscribe";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, StatsUnsubscribeRequest>({
      url: endpoint,
      method: HttpMethod.DELETE,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {string} objectIds - The `objectIds` parameter to include in the API request.
   * @param {string} format - The `format` parameter to include in the API request.
   * @param {string} stats - The `stats` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<BatchReadStatsResponse>>} A promise containing the HttpResponse of BatchReadStatsResponse
   */
  async getStatsClientBatch(objectIds: string, format?: string, stats?: string, gamertag?: string): Promise<HttpResponse<BatchReadStatsResponse>> {
    let endpoint = "/basic/stats/client/batch";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      objectIds,
      format,
      stats
    });
    
    // Make the API request
    return this.requester.request<BatchReadStatsResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {BatchSetStatsRequest} payload - The `BatchSetStatsRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postStatBatch(payload: BatchSetStatsRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let endpoint = "/basic/stats/batch";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyResponse, BatchSetStatsRequest>({
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
   * @param {StatsSearchRequest} payload - The `StatsSearchRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<StatsSearchResponse>>} A promise containing the HttpResponse of StatsSearchResponse
   */
  async postStatSearch(payload: StatsSearchRequest, gamertag?: string): Promise<HttpResponse<StatsSearchResponse>> {
    let endpoint = "/basic/stats/search";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<StatsSearchResponse, StatsSearchRequest>({
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
   * @param {SearchExtendedRequest} payload - The `SearchExtendedRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<SearchExtendedResponse>>} A promise containing the HttpResponse of SearchExtendedResponse
   */
  async postStatSearchExtended(payload: SearchExtendedRequest, gamertag?: string): Promise<HttpResponse<SearchExtendedResponse>> {
    let endpoint = "/basic/stats/search/extended";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<SearchExtendedResponse, SearchExtendedRequest>({
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
   * @param {StatUpdateRequestStringListFormat} payload - The `StatUpdateRequestStringListFormat` instance to use for the API request
   * @param {string} objectId - Format: domain.visibility.type.gamerTag
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postStatClientStringlistByObjectId(objectId: string, payload: StatUpdateRequestStringListFormat, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let endpoint = "/object/stats/{objectId}/client/stringlist";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyResponse, StatUpdateRequestStringListFormat>({
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
   * @param {string} objectId - Format: domain.visibility.type.gamerTag
   * @param {string} stats - The `stats` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<StatsResponse>>} A promise containing the HttpResponse of StatsResponse
   */
  async getStatByObjectId(objectId: string, stats?: string, gamertag?: string): Promise<HttpResponse<StatsResponse>> {
    let endpoint = "/object/stats/{objectId}/";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      stats
    });
    
    // Make the API request
    return this.requester.request<StatsResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers,
      withAuth: true
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {StatUpdateRequest} payload - The `StatUpdateRequest` instance to use for the API request
   * @param {string} objectId - Format: domain.visibility.type.gamerTag
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postStatByObjectId(objectId: string, payload: StatUpdateRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let endpoint = "/object/stats/{objectId}/";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyResponse, StatUpdateRequest>({
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
   * @param {StatRequest} payload - The `StatRequest` instance to use for the API request
   * @param {string} objectId - Format: domain.visibility.type.gamerTag
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async deleteStatByObjectId(objectId: string, payload: StatRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let endpoint = "/object/stats/{objectId}/";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyResponse, StatRequest>({
      url: endpoint,
      method: HttpMethod.DELETE,
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
   * @param {string} objectId - Format: domain.visibility.type.gamerTag
   * @param {string} stats - The `stats` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<StatsResponse>>} A promise containing the HttpResponse of StatsResponse
   */
  async getStatClientByObjectId(objectId: string, stats?: string, gamertag?: string): Promise<HttpResponse<StatsResponse>> {
    let endpoint = "/object/stats/{objectId}/client";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      stats
    });
    
    // Make the API request
    return this.requester.request<StatsResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers,
      withAuth: true
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {StatUpdateRequest} payload - The `StatUpdateRequest` instance to use for the API request
   * @param {string} objectId - Format: domain.visibility.type.gamerTag
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postStatClientByObjectId(objectId: string, payload: StatUpdateRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let endpoint = "/object/stats/{objectId}/client";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyResponse, StatUpdateRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
}
