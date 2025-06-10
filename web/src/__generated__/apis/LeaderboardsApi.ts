import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { LeaderboardAddRequest } from '@/__generated__/schemas/LeaderboardAddRequest';
import { LeaderboardAssignmentInfo } from '@/__generated__/schemas/LeaderboardAssignmentInfo';
import { LeaderboardCreateRequest } from '@/__generated__/schemas/LeaderboardCreateRequest';
import { LeaderboardDetails } from '@/__generated__/schemas/LeaderboardDetails';
import { LeaderboardListResponse } from '@/__generated__/schemas/LeaderboardListResponse';
import { LeaderboardMembershipResponse } from '@/__generated__/schemas/LeaderboardMembershipResponse';
import { LeaderboardPartitionInfo } from '@/__generated__/schemas/LeaderboardPartitionInfo';
import { LeaderboardRemoveCacheEntryRequest } from '@/__generated__/schemas/LeaderboardRemoveCacheEntryRequest';
import { LeaderboardRemoveEntryRequest } from '@/__generated__/schemas/LeaderboardRemoveEntryRequest';
import { LeaderboardSwapRequest } from '@/__generated__/schemas/LeaderboardSwapRequest';
import { LeaderboardUidResponse } from '@/__generated__/schemas/LeaderboardUidResponse';
import { LeaderBoardViewResponse } from '@/__generated__/schemas/LeaderBoardViewResponse';
import { ListLeaderBoardViewResponse } from '@/__generated__/schemas/ListLeaderBoardViewResponse';
import { makeQueryString } from '@/utils/makeQueryString';
import { MatchMakingMatchesPvpResponse } from '@/__generated__/schemas/MatchMakingMatchesPvpResponse';

export class LeaderboardsApi {
  constructor(
    private readonly requester: HttpRequester
  ) {
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {boolean} includePartitions - The `includePartitions` parameter to include in the API request.
   * @param {number} limit - The `limit` parameter to include in the API request.
   * @param {string} prefix - The `prefix` parameter to include in the API request.
   * @param {number} skip - The `skip` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<LeaderboardListResponse>>} A promise containing the HttpResponse of LeaderboardListResponse
   */
  async getLeaderboardsList(includePartitions?: boolean, limit?: number, prefix?: string, skip?: number, gamertag?: string): Promise<HttpResponse<LeaderboardListResponse>> {
    let endpoint = "/basic/leaderboards/list";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      includePartitions,
      limit,
      prefix,
      skip
    });
    
    // Make the API request
    return this.requester.request<LeaderboardListResponse>({
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
   * @param {bigint | string} dbid - The `dbid` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ListLeaderBoardViewResponse>>} A promise containing the HttpResponse of ListLeaderBoardViewResponse
   */
  async getLeaderboardsPlayer(dbid: bigint | string, gamertag?: string): Promise<HttpResponse<ListLeaderBoardViewResponse>> {
    let endpoint = "/basic/leaderboards/player";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      dbid
    });
    
    // Make the API request
    return this.requester.request<ListLeaderBoardViewResponse>({
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
   * @param {string} boardId - The `boardId` parameter to include in the API request.
   * @param {boolean} joinBoard - The `joinBoard` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<LeaderboardAssignmentInfo>>} A promise containing the HttpResponse of LeaderboardAssignmentInfo
   */
  async getLeaderboardsAssignment(boardId: string, joinBoard?: boolean, gamertag?: string): Promise<HttpResponse<LeaderboardAssignmentInfo>> {
    let endpoint = "/basic/leaderboards/assignment";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      boardId,
      joinBoard
    });
    
    // Make the API request
    return this.requester.request<LeaderboardAssignmentInfo>({
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
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<LeaderboardUidResponse>>} A promise containing the HttpResponse of LeaderboardUidResponse
   */
  async getLeaderboardsUid(gamertag?: string): Promise<HttpResponse<LeaderboardUidResponse>> {
    let endpoint = "/basic/leaderboards/uid";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<LeaderboardUidResponse>({
      url: endpoint,
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
   * @param {string} objectId - Gamertag of the player.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async deleteLeaderboardEntriesByObjectId(objectId: string, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/object/leaderboards/{objectId}/entries";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse>({
      url: endpoint,
      method: HttpMethod.DELETE,
      headers,
      withAuth: true
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {string} objectId - Gamertag of the player.
   * @param {bigint | string} playerId - The `playerId` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<LeaderboardMembershipResponse>>} A promise containing the HttpResponse of LeaderboardMembershipResponse
   */
  async getLeaderboardMembershipByObjectId(objectId: string, playerId: bigint | string, gamertag?: string): Promise<HttpResponse<LeaderboardMembershipResponse>> {
    let endpoint = "/object/leaderboards/{objectId}/membership";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      playerId
    });
    
    // Make the API request
    return this.requester.request<LeaderboardMembershipResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers,
      withAuth: true
    });
  }
  
  /**
   * @param {string} objectId - Gamertag of the player.
   * @param {string} ids - The `ids` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<LeaderBoardViewResponse>>} A promise containing the HttpResponse of LeaderBoardViewResponse
   */
  async getLeaderboardRanksByObjectId(objectId: string, ids: string, gamertag?: string): Promise<HttpResponse<LeaderBoardViewResponse>> {
    let endpoint = "/object/leaderboards/{objectId}/ranks";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      ids
    });
    
    // Make the API request
    return this.requester.request<LeaderBoardViewResponse>({
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
   * @param {string} objectId - Gamertag of the player.
   * @param {bigint | string} playerId - The `playerId` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<LeaderboardPartitionInfo>>} A promise containing the HttpResponse of LeaderboardPartitionInfo
   */
  async getLeaderboardPartitionByObjectId(objectId: string, playerId: bigint | string, gamertag?: string): Promise<HttpResponse<LeaderboardPartitionInfo>> {
    let endpoint = "/object/leaderboards/{objectId}/partition";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      playerId
    });
    
    // Make the API request
    return this.requester.request<LeaderboardPartitionInfo>({
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
   * @param {string} objectId - Gamertag of the player.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<LeaderBoardViewResponse>>} A promise containing the HttpResponse of LeaderBoardViewResponse
   */
  async getLeaderboardFriendsByObjectId(objectId: string, gamertag?: string): Promise<HttpResponse<LeaderBoardViewResponse>> {
    let endpoint = "/object/leaderboards/{objectId}/friends";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<LeaderBoardViewResponse>({
      url: endpoint,
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
   * @param {LeaderboardCreateRequest} payload - The `LeaderboardCreateRequest` instance to use for the API request
   * @param {string} objectId - Gamertag of the player.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postLeaderboardByObjectId(objectId: string, payload: LeaderboardCreateRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/object/leaderboards/{objectId}/";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, LeaderboardCreateRequest>({
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
   * @param {string} objectId - Gamertag of the player.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async deleteLeaderboardByObjectId(objectId: string, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/object/leaderboards/{objectId}/";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse>({
      url: endpoint,
      method: HttpMethod.DELETE,
      headers,
      withAuth: true
    });
  }
  
  /**
   * @param {string} objectId - Gamertag of the player.
   * @param {number} poolSize - The `poolSize` parameter to include in the API request.
   * @param {number} windowSize - The `windowSize` parameter to include in the API request.
   * @param {number} windows - The `windows` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<MatchMakingMatchesPvpResponse>>} A promise containing the HttpResponse of MatchMakingMatchesPvpResponse
   */
  async getLeaderboardMatchesByObjectId(objectId: string, poolSize: number, windowSize: number, windows: number, gamertag?: string): Promise<HttpResponse<MatchMakingMatchesPvpResponse>> {
    let endpoint = "/object/leaderboards/{objectId}/matches";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      poolSize,
      windowSize,
      windows
    });
    
    // Make the API request
    return this.requester.request<MatchMakingMatchesPvpResponse>({
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
   * @param {string} objectId - Gamertag of the player.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<LeaderboardAssignmentInfo>>} A promise containing the HttpResponse of LeaderboardAssignmentInfo
   */
  async getLeaderboardAssignmentByObjectId(objectId: string, gamertag?: string): Promise<HttpResponse<LeaderboardAssignmentInfo>> {
    let endpoint = "/object/leaderboards/{objectId}/assignment";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<LeaderboardAssignmentInfo>({
      url: endpoint,
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
   * @param {LeaderboardRemoveCacheEntryRequest} payload - The `LeaderboardRemoveCacheEntryRequest` instance to use for the API request
   * @param {string} objectId - Gamertag of the player.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async deleteLeaderboardAssignmentByObjectId(objectId: string, payload: LeaderboardRemoveCacheEntryRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/object/leaderboards/{objectId}/assignment";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, LeaderboardRemoveCacheEntryRequest>({
      url: endpoint,
      method: HttpMethod.DELETE,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {LeaderboardAddRequest} payload - The `LeaderboardAddRequest` instance to use for the API request
   * @param {string} objectId - Gamertag of the player.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putLeaderboardEntryByObjectId(objectId: string, payload: LeaderboardAddRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/object/leaderboards/{objectId}/entry";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, LeaderboardAddRequest>({
      url: endpoint,
      method: HttpMethod.PUT,
      headers,
      body: payload
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {LeaderboardRemoveEntryRequest} payload - The `LeaderboardRemoveEntryRequest` instance to use for the API request
   * @param {string} objectId - Gamertag of the player.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async deleteLeaderboardEntryByObjectId(objectId: string, payload: LeaderboardRemoveEntryRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/object/leaderboards/{objectId}/entry";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, LeaderboardRemoveEntryRequest>({
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
   * @param {string} objectId - Gamertag of the player.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putLeaderboardFreezeByObjectId(objectId: string, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/object/leaderboards/{objectId}/freeze";
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
      headers,
      withAuth: true
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {string} objectId - Gamertag of the player.
   * @param {number} from - The `from` parameter to include in the API request.
   * @param {number} max - The `max` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<LeaderboardDetails>>} A promise containing the HttpResponse of LeaderboardDetails
   */
  async getLeaderboardDetailsByObjectId(objectId: string, from?: number, max?: number, gamertag?: string): Promise<HttpResponse<LeaderboardDetails>> {
    let endpoint = "/object/leaderboards/{objectId}/details";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      from,
      max
    });
    
    // Make the API request
    return this.requester.request<LeaderboardDetails>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers,
      withAuth: true
    });
  }
  
  /**
   * @param {string} objectId - Gamertag of the player.
   * @param {bigint | string} focus - The `focus` parameter to include in the API request.
   * @param {boolean} friends - The `friends` parameter to include in the API request.
   * @param {number} from - The `from` parameter to include in the API request.
   * @param {boolean} guild - The `guild` parameter to include in the API request.
   * @param {number} max - The `max` parameter to include in the API request.
   * @param {bigint | string} outlier - The `outlier` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<LeaderBoardViewResponse>>} A promise containing the HttpResponse of LeaderBoardViewResponse
   */
  async getLeaderboardViewByObjectId(objectId: string, focus?: bigint | string, friends?: boolean, from?: number, guild?: boolean, max?: number, outlier?: bigint | string, gamertag?: string): Promise<HttpResponse<LeaderBoardViewResponse>> {
    let endpoint = "/object/leaderboards/{objectId}/view";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      focus,
      friends,
      from,
      guild,
      max,
      outlier
    });
    
    // Make the API request
    return this.requester.request<LeaderBoardViewResponse>({
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
   * @param {LeaderboardSwapRequest} payload - The `LeaderboardSwapRequest` instance to use for the API request
   * @param {string} objectId - Gamertag of the player.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putLeaderboardSwapByObjectId(objectId: string, payload: LeaderboardSwapRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/object/leaderboards/{objectId}/swap";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, LeaderboardSwapRequest>({
      url: endpoint,
      method: HttpMethod.PUT,
      headers,
      body: payload,
      withAuth: true
    });
  }
}
