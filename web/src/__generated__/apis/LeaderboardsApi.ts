import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
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
import { makeApiRequest } from '@/utils/makeApiRequest';
import { MatchMakingMatchesPvpResponse } from '@/__generated__/schemas/MatchMakingMatchesPvpResponse';
import { objectIdPlaceholder } from '@/constants';
import { POST } from '@/constants';
import { PUT } from '@/constants';

export class LeaderboardsApi {
  constructor(
    private readonly r: HttpRequester
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
    let e = "/basic/leaderboards/list";
    
    // Make the API request
    return makeApiRequest<LeaderboardListResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        includePartitions,
        limit,
        prefix,
        skip
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
   * @param {bigint | string} dbid - The `dbid` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ListLeaderBoardViewResponse>>} A promise containing the HttpResponse of ListLeaderBoardViewResponse
   */
  async getLeaderboardsPlayer(dbid: bigint | string, gamertag?: string): Promise<HttpResponse<ListLeaderBoardViewResponse>> {
    let e = "/basic/leaderboards/player";
    
    // Make the API request
    return makeApiRequest<ListLeaderBoardViewResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        dbid
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
   * @param {string} boardId - The `boardId` parameter to include in the API request.
   * @param {boolean} joinBoard - The `joinBoard` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<LeaderboardAssignmentInfo>>} A promise containing the HttpResponse of LeaderboardAssignmentInfo
   */
  async getLeaderboardsAssignment(boardId: string, joinBoard?: boolean, gamertag?: string): Promise<HttpResponse<LeaderboardAssignmentInfo>> {
    let e = "/basic/leaderboards/assignment";
    
    // Make the API request
    return makeApiRequest<LeaderboardAssignmentInfo>({
      r: this.r,
      e,
      m: GET,
      q: {
        boardId,
        joinBoard
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
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<LeaderboardUidResponse>>} A promise containing the HttpResponse of LeaderboardUidResponse
   */
  async getLeaderboardsUid(gamertag?: string): Promise<HttpResponse<LeaderboardUidResponse>> {
    let e = "/basic/leaderboards/uid";
    
    // Make the API request
    return makeApiRequest<LeaderboardUidResponse>({
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
   * @param {string} objectId - Gamertag of the player.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async deleteLeaderboardEntriesByObjectId(objectId: string, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/object/leaderboards/{objectId}/entries".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CommonResponse>({
      r: this.r,
      e,
      m: DELETE,
      g: gamertag,
      w: true
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
    let e = "/object/leaderboards/{objectId}/membership".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<LeaderboardMembershipResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        playerId
      },
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {string} objectId - Gamertag of the player.
   * @param {string} ids - The `ids` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<LeaderBoardViewResponse>>} A promise containing the HttpResponse of LeaderBoardViewResponse
   */
  async getLeaderboardRanksByObjectId(objectId: string, ids: string, gamertag?: string): Promise<HttpResponse<LeaderBoardViewResponse>> {
    let e = "/object/leaderboards/{objectId}/ranks".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<LeaderBoardViewResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        ids
      },
      g: gamertag
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
    let e = "/object/leaderboards/{objectId}/partition".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<LeaderboardPartitionInfo>({
      r: this.r,
      e,
      m: GET,
      q: {
        playerId
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
   * @param {string} objectId - Gamertag of the player.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<LeaderBoardViewResponse>>} A promise containing the HttpResponse of LeaderBoardViewResponse
   */
  async getLeaderboardFriendsByObjectId(objectId: string, gamertag?: string): Promise<HttpResponse<LeaderBoardViewResponse>> {
    let e = "/object/leaderboards/{objectId}/friends".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<LeaderBoardViewResponse>({
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
   * @param {LeaderboardCreateRequest} payload - The `LeaderboardCreateRequest` instance to use for the API request
   * @param {string} objectId - Gamertag of the player.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postLeaderboardByObjectId(objectId: string, payload: LeaderboardCreateRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/object/leaderboards/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CommonResponse, LeaderboardCreateRequest>({
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
   * @param {string} objectId - Gamertag of the player.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async deleteLeaderboardByObjectId(objectId: string, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/object/leaderboards/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CommonResponse>({
      r: this.r,
      e,
      m: DELETE,
      g: gamertag,
      w: true
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
    let e = "/object/leaderboards/{objectId}/matches".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<MatchMakingMatchesPvpResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        poolSize,
        windowSize,
        windows
      },
      g: gamertag
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
    let e = "/object/leaderboards/{objectId}/assignment".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<LeaderboardAssignmentInfo>({
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
   * @param {LeaderboardRemoveCacheEntryRequest} payload - The `LeaderboardRemoveCacheEntryRequest` instance to use for the API request
   * @param {string} objectId - Gamertag of the player.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async deleteLeaderboardAssignmentByObjectId(objectId: string, payload: LeaderboardRemoveCacheEntryRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/object/leaderboards/{objectId}/assignment".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CommonResponse, LeaderboardRemoveCacheEntryRequest>({
      r: this.r,
      e,
      m: DELETE,
      p: payload,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {LeaderboardAddRequest} payload - The `LeaderboardAddRequest` instance to use for the API request
   * @param {string} objectId - Gamertag of the player.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putLeaderboardEntryByObjectId(objectId: string, payload: LeaderboardAddRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/object/leaderboards/{objectId}/entry".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CommonResponse, LeaderboardAddRequest>({
      r: this.r,
      e,
      m: PUT,
      p: payload,
      g: gamertag
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
    let e = "/object/leaderboards/{objectId}/entry".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CommonResponse, LeaderboardRemoveEntryRequest>({
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
   * @param {string} objectId - Gamertag of the player.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putLeaderboardFreezeByObjectId(objectId: string, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/object/leaderboards/{objectId}/freeze".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
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
   * @param {string} objectId - Gamertag of the player.
   * @param {number} from - The `from` parameter to include in the API request.
   * @param {number} max - The `max` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<LeaderboardDetails>>} A promise containing the HttpResponse of LeaderboardDetails
   */
  async getLeaderboardDetailsByObjectId(objectId: string, from?: number, max?: number, gamertag?: string): Promise<HttpResponse<LeaderboardDetails>> {
    let e = "/object/leaderboards/{objectId}/details".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<LeaderboardDetails>({
      r: this.r,
      e,
      m: GET,
      q: {
        from,
        max
      },
      g: gamertag,
      w: true
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
    let e = "/object/leaderboards/{objectId}/view".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<LeaderBoardViewResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        focus,
        friends,
        from,
        guild,
        max,
        outlier
      },
      g: gamertag
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
    let e = "/object/leaderboards/{objectId}/swap".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<CommonResponse, LeaderboardSwapRequest>({
      r: this.r,
      e,
      m: PUT,
      p: payload,
      g: gamertag,
      w: true
    });
  }
}
