import { AdminGetPlayerStatusResponse } from '@/__generated__/schemas/AdminGetPlayerStatusResponse';
import { AdminPlayerStatus } from '@/__generated__/schemas/AdminPlayerStatus';
import { EmptyResponse } from '@/__generated__/schemas/EmptyResponse';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { GetChampionsResponse } from '@/__generated__/schemas/GetChampionsResponse';
import { GetGroupsResponse } from '@/__generated__/schemas/GetGroupsResponse';
import { GetGroupStatusResponse } from '@/__generated__/schemas/GetGroupStatusResponse';
import { GetPlayerStatusResponse } from '@/__generated__/schemas/GetPlayerStatusResponse';
import { GetStandingsResponse } from '@/__generated__/schemas/GetStandingsResponse';
import { GetStatusForGroupsRequest } from '@/__generated__/schemas/GetStatusForGroupsRequest';
import { GetStatusForGroupsResponse } from '@/__generated__/schemas/GetStatusForGroupsResponse';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpResponse } from '@/network/http/types/HttpResponse';
import { JoinRequest } from '@/__generated__/schemas/JoinRequest';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/constants';
import { PlayerStatus } from '@/__generated__/schemas/PlayerStatus';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import { RewardsRequest } from '@/__generated__/schemas/RewardsRequest';
import { RewardsResponse } from '@/__generated__/schemas/RewardsResponse';
import { ScoreRequest } from '@/__generated__/schemas/ScoreRequest';
import { TournamentClientView } from '@/__generated__/schemas/TournamentClientView';
import { TournamentQueryResponse } from '@/__generated__/schemas/TournamentQueryResponse';
import { UpdatePlayerStatusRequest } from '@/__generated__/schemas/UpdatePlayerStatusRequest';

export class TournamentsApi {
  constructor(
    private readonly r: HttpRequester
  ) {
  }
  
  /**
   * @param {GetStatusForGroupsRequest} payload - The `GetStatusForGroupsRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetStatusForGroupsResponse>>} A promise containing the HttpResponse of GetStatusForGroupsResponse
   */
  async postTournamentSearchGroups(payload: GetStatusForGroupsRequest, gamertag?: string): Promise<HttpResponse<GetStatusForGroupsResponse>> {
    let e = "/basic/tournaments/search/groups";
    
    // Make the API request
    return makeApiRequest<GetStatusForGroupsResponse, GetStatusForGroupsRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @param {string} contentId - The `contentId` parameter to include in the API request.
   * @param {number} cycle - The `cycle` parameter to include in the API request.
   * @param {boolean} isRunning - The `isRunning` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<TournamentQueryResponse>>} A promise containing the HttpResponse of TournamentQueryResponse
   */
  async getTournaments(contentId?: string, cycle?: number, isRunning?: boolean, gamertag?: string): Promise<HttpResponse<TournamentQueryResponse>> {
    let e = "/basic/tournaments/";
    
    // Make the API request
    return makeApiRequest<TournamentQueryResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        contentId,
        cycle,
        isRunning
      },
      g: gamertag
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {JoinRequest} payload - The `JoinRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PlayerStatus>>} A promise containing the HttpResponse of PlayerStatus
   */
  async postTournament(payload: JoinRequest, gamertag?: string): Promise<HttpResponse<PlayerStatus>> {
    let e = "/basic/tournaments/";
    
    // Make the API request
    return makeApiRequest<PlayerStatus, JoinRequest>({
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
   * @param {string} contentId - The `contentId` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetGroupStatusResponse>>} A promise containing the HttpResponse of GetGroupStatusResponse
   */
  async getTournamentsMeGroup(contentId?: string, gamertag?: string): Promise<HttpResponse<GetGroupStatusResponse>> {
    let e = "/basic/tournaments/me/group";
    
    // Make the API request
    return makeApiRequest<GetGroupStatusResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        contentId
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
   * @param {string} contentId - The `contentId` parameter to include in the API request.
   * @param {string} tournamentId - The `tournamentId` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<RewardsResponse>>} A promise containing the HttpResponse of RewardsResponse
   */
  async getTournamentsRewards(contentId?: string, tournamentId?: string, gamertag?: string): Promise<HttpResponse<RewardsResponse>> {
    let e = "/basic/tournaments/rewards";
    
    // Make the API request
    return makeApiRequest<RewardsResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        contentId,
        tournamentId
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
   * @param {RewardsRequest} payload - The `RewardsRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<RewardsResponse>>} A promise containing the HttpResponse of RewardsResponse
   */
  async postTournamentRewards(payload: RewardsRequest, gamertag?: string): Promise<HttpResponse<RewardsResponse>> {
    let e = "/basic/tournaments/rewards";
    
    // Make the API request
    return makeApiRequest<RewardsResponse, RewardsRequest>({
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
   * @param {string} tournamentId - The `tournamentId` parameter to include in the API request.
   * @param {string} contentId - The `contentId` parameter to include in the API request.
   * @param {number} cycle - The `cycle` parameter to include in the API request.
   * @param {bigint | string} focus - The `focus` parameter to include in the API request.
   * @param {number} from - The `from` parameter to include in the API request.
   * @param {number} max - The `max` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetStandingsResponse>>} A promise containing the HttpResponse of GetStandingsResponse
   */
  async getTournamentsGlobal(tournamentId: string, contentId?: string, cycle?: number, focus?: bigint | string, from?: number, max?: number, gamertag?: string): Promise<HttpResponse<GetStandingsResponse>> {
    let e = "/basic/tournaments/global";
    
    // Make the API request
    return makeApiRequest<GetStandingsResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        tournamentId,
        contentId,
        cycle,
        focus,
        from,
        max
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
   * @param {string} tournamentId - The `tournamentId` parameter to include in the API request.
   * @param {string} contentId - The `contentId` parameter to include in the API request.
   * @param {number} cycle - The `cycle` parameter to include in the API request.
   * @param {bigint | string} focus - The `focus` parameter to include in the API request.
   * @param {number} from - The `from` parameter to include in the API request.
   * @param {number} max - The `max` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetStandingsResponse>>} A promise containing the HttpResponse of GetStandingsResponse
   */
  async getTournamentsStandingsGroup(tournamentId: string, contentId?: string, cycle?: number, focus?: bigint | string, from?: number, max?: number, gamertag?: string): Promise<HttpResponse<GetStandingsResponse>> {
    let e = "/basic/tournaments/standings/group";
    
    // Make the API request
    return makeApiRequest<GetStandingsResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        tournamentId,
        contentId,
        cycle,
        focus,
        from,
        max
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
   * @param {string} tournamentId - The `tournamentId` parameter to include in the API request.
   * @param {string} contentId - The `contentId` parameter to include in the API request.
   * @param {number} cycle - The `cycle` parameter to include in the API request.
   * @param {bigint | string} focus - The `focus` parameter to include in the API request.
   * @param {number} from - The `from` parameter to include in the API request.
   * @param {number} max - The `max` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetStandingsResponse>>} A promise containing the HttpResponse of GetStandingsResponse
   */
  async getTournamentsStandings(tournamentId: string, contentId?: string, cycle?: number, focus?: bigint | string, from?: number, max?: number, gamertag?: string): Promise<HttpResponse<GetStandingsResponse>> {
    let e = "/basic/tournaments/standings";
    
    // Make the API request
    return makeApiRequest<GetStandingsResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        tournamentId,
        contentId,
        cycle,
        focus,
        from,
        max
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
   * @param {bigint | string} playerId - The `playerId` parameter to include in the API request.
   * @param {string} contentId - The `contentId` parameter to include in the API request.
   * @param {boolean} hasUnclaimedRewards - The `hasUnclaimedRewards` parameter to include in the API request.
   * @param {string} tournamentId - The `tournamentId` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AdminGetPlayerStatusResponse>>} A promise containing the HttpResponse of AdminGetPlayerStatusResponse
   */
  async getTournamentsAdminPlayer(playerId: bigint | string, contentId?: string, hasUnclaimedRewards?: boolean, tournamentId?: string, gamertag?: string): Promise<HttpResponse<AdminGetPlayerStatusResponse>> {
    let e = "/basic/tournaments/admin/player";
    
    // Make the API request
    return makeApiRequest<AdminGetPlayerStatusResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        playerId,
        contentId,
        hasUnclaimedRewards,
        tournamentId
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
   * @param {UpdatePlayerStatusRequest} payload - The `UpdatePlayerStatusRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AdminPlayerStatus>>} A promise containing the HttpResponse of AdminPlayerStatus
   */
  async putTournamentAdminPlayer(payload: UpdatePlayerStatusRequest, gamertag?: string): Promise<HttpResponse<AdminPlayerStatus>> {
    let e = "/basic/tournaments/admin/player";
    
    // Make the API request
    return makeApiRequest<AdminPlayerStatus, UpdatePlayerStatusRequest>({
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
   * @param {string} contentId - The `contentId` parameter to include in the API request.
   * @param {boolean} hasUnclaimedRewards - The `hasUnclaimedRewards` parameter to include in the API request.
   * @param {string} tournamentId - The `tournamentId` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetPlayerStatusResponse>>} A promise containing the HttpResponse of GetPlayerStatusResponse
   */
  async getTournamentsMe(contentId?: string, hasUnclaimedRewards?: boolean, tournamentId?: string, gamertag?: string): Promise<HttpResponse<GetPlayerStatusResponse>> {
    let e = "/basic/tournaments/me";
    
    // Make the API request
    return makeApiRequest<GetPlayerStatusResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        contentId,
        hasUnclaimedRewards,
        tournamentId
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
   * @param {number} cycles - The `cycles` parameter to include in the API request.
   * @param {string} tournamentId - The `tournamentId` parameter to include in the API request.
   * @param {string} contentId - The `contentId` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetChampionsResponse>>} A promise containing the HttpResponse of GetChampionsResponse
   */
  async getTournamentsChampions(cycles: number, tournamentId: string, contentId?: string, gamertag?: string): Promise<HttpResponse<GetChampionsResponse>> {
    let e = "/basic/tournaments/champions";
    
    // Make the API request
    return makeApiRequest<GetChampionsResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        cycles,
        tournamentId,
        contentId
      },
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {ScoreRequest} payload - The `ScoreRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postTournamentScore(payload: ScoreRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let e = "/basic/tournaments/score";
    
    // Make the API request
    return makeApiRequest<EmptyResponse, ScoreRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {string} tournamentId - The `tournamentId` parameter to include in the API request.
   * @param {string} contentId - The `contentId` parameter to include in the API request.
   * @param {number} cycle - The `cycle` parameter to include in the API request.
   * @param {bigint | string} focus - The `focus` parameter to include in the API request.
   * @param {number} from - The `from` parameter to include in the API request.
   * @param {number} max - The `max` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetGroupsResponse>>} A promise containing the HttpResponse of GetGroupsResponse
   */
  async getTournamentsGroups(tournamentId: string, contentId?: string, cycle?: number, focus?: bigint | string, from?: number, max?: number, gamertag?: string): Promise<HttpResponse<GetGroupsResponse>> {
    let e = "/basic/tournaments/groups";
    
    // Make the API request
    return makeApiRequest<GetGroupsResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        tournamentId,
        contentId,
        cycle,
        focus,
        from,
        max
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
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<TournamentClientView>>} A promise containing the HttpResponse of TournamentClientView
   */
  async getTournamentByObjectId(objectId: bigint | string, gamertag?: string): Promise<HttpResponse<TournamentClientView>> {
    let e = "/object/tournaments/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<TournamentClientView>({
      r: this.r,
      e,
      m: GET,
      g: gamertag,
      w: true
    });
  }
}
