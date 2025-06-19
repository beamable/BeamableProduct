import { BatchReadStatsResponse } from '@/__generated__/schemas/BatchReadStatsResponse';
import { BatchSetStatsRequest } from '@/__generated__/schemas/BatchSetStatsRequest';
import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { DELETE } from '@/constants';
import { EmptyResponse } from '@/__generated__/schemas/EmptyResponse';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/constants';
import { POST } from '@/constants';
import { PUT } from '@/constants';
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
    private readonly r: HttpRequester
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
    let e = "/basic/stats/subscribe";
    
    // Make the API request
    return makeApiRequest<CommonResponse, StatsSubscribeRequest>({
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
   * @param {StatsUnsubscribeRequest} payload - The `StatsUnsubscribeRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async deleteStatSubscribe(payload: StatsUnsubscribeRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/basic/stats/subscribe";
    
    // Make the API request
    return makeApiRequest<CommonResponse, StatsUnsubscribeRequest>({
      r: this.r,
      e,
      m: DELETE,
      p: payload,
      g: gamertag,
      w: true
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
    let e = "/basic/stats/client/batch";
    
    // Make the API request
    return makeApiRequest<BatchReadStatsResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        objectIds,
        format,
        stats
      },
      g: gamertag
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
    let e = "/basic/stats/batch";
    
    // Make the API request
    return makeApiRequest<EmptyResponse, BatchSetStatsRequest>({
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
   * @param {StatsSearchRequest} payload - The `StatsSearchRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<StatsSearchResponse>>} A promise containing the HttpResponse of StatsSearchResponse
   */
  async postStatSearch(payload: StatsSearchRequest, gamertag?: string): Promise<HttpResponse<StatsSearchResponse>> {
    let e = "/basic/stats/search";
    
    // Make the API request
    return makeApiRequest<StatsSearchResponse, StatsSearchRequest>({
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
   * @param {SearchExtendedRequest} payload - The `SearchExtendedRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<SearchExtendedResponse>>} A promise containing the HttpResponse of SearchExtendedResponse
   */
  async postStatSearchExtended(payload: SearchExtendedRequest, gamertag?: string): Promise<HttpResponse<SearchExtendedResponse>> {
    let e = "/basic/stats/search/extended";
    
    // Make the API request
    return makeApiRequest<SearchExtendedResponse, SearchExtendedRequest>({
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
   * @param {StatUpdateRequestStringListFormat} payload - The `StatUpdateRequestStringListFormat` instance to use for the API request
   * @param {string} objectId - Format: domain.visibility.type.gamerTag
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postStatClientStringlistByObjectId(objectId: string, payload: StatUpdateRequestStringListFormat, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let e = "/object/stats/{objectId}/client/stringlist".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<EmptyResponse, StatUpdateRequestStringListFormat>({
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
   * @param {string} objectId - Format: domain.visibility.type.gamerTag
   * @param {string} stats - The `stats` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<StatsResponse>>} A promise containing the HttpResponse of StatsResponse
   */
  async getStatByObjectId(objectId: string, stats?: string, gamertag?: string): Promise<HttpResponse<StatsResponse>> {
    let e = "/object/stats/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<StatsResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        stats
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
   * @param {StatUpdateRequest} payload - The `StatUpdateRequest` instance to use for the API request
   * @param {string} objectId - Format: domain.visibility.type.gamerTag
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postStatByObjectId(objectId: string, payload: StatUpdateRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let e = "/object/stats/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<EmptyResponse, StatUpdateRequest>({
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
   * @param {StatRequest} payload - The `StatRequest` instance to use for the API request
   * @param {string} objectId - Format: domain.visibility.type.gamerTag
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async deleteStatByObjectId(objectId: string, payload: StatRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let e = "/object/stats/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<EmptyResponse, StatRequest>({
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
   * @param {string} objectId - Format: domain.visibility.type.gamerTag
   * @param {string} stats - The `stats` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<StatsResponse>>} A promise containing the HttpResponse of StatsResponse
   */
  async getStatClientByObjectId(objectId: string, stats?: string, gamertag?: string): Promise<HttpResponse<StatsResponse>> {
    let e = "/object/stats/{objectId}/client".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<StatsResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        stats
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
   * @param {StatUpdateRequest} payload - The `StatUpdateRequest` instance to use for the API request
   * @param {string} objectId - Format: domain.visibility.type.gamerTag
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postStatClientByObjectId(objectId: string, payload: StatUpdateRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let e = "/object/stats/{objectId}/client".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<EmptyResponse, StatUpdateRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
    });
  }
}
