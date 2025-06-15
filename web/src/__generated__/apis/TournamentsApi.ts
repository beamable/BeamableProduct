import { AdminGetPlayerStatusResponse } from '@/__generated__/schemas/AdminGetPlayerStatusResponse';
import { AdminPlayerStatus } from '@/__generated__/schemas/AdminPlayerStatus';
import { EmptyResponse } from '@/__generated__/schemas/EmptyResponse';
import { GetChampionsResponse } from '@/__generated__/schemas/GetChampionsResponse';
import { GetGroupsResponse } from '@/__generated__/schemas/GetGroupsResponse';
import { GetGroupStatusResponse } from '@/__generated__/schemas/GetGroupStatusResponse';
import { GetPlayerStatusResponse } from '@/__generated__/schemas/GetPlayerStatusResponse';
import { GetStandingsResponse } from '@/__generated__/schemas/GetStandingsResponse';
import { GetStatusForGroupsRequest } from '@/__generated__/schemas/GetStatusForGroupsRequest';
import { GetStatusForGroupsResponse } from '@/__generated__/schemas/GetStatusForGroupsResponse';
import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { JoinRequest } from '@/__generated__/schemas/JoinRequest';
import { makeQueryString } from '@/utils/makeQueryString';
import { PlayerStatus } from '@/__generated__/schemas/PlayerStatus';
import { RewardsRequest } from '@/__generated__/schemas/RewardsRequest';
import { RewardsResponse } from '@/__generated__/schemas/RewardsResponse';
import { ScoreRequest } from '@/__generated__/schemas/ScoreRequest';
import { TournamentClientView } from '@/__generated__/schemas/TournamentClientView';
import { TournamentQueryResponse } from '@/__generated__/schemas/TournamentQueryResponse';
import { UpdatePlayerStatusRequest } from '@/__generated__/schemas/UpdatePlayerStatusRequest';

export class TournamentsApi {
  constructor(
    private readonly requester: HttpRequester
  ) {
  }
  
  /**
   * @param {GetStatusForGroupsRequest} payload - The `GetStatusForGroupsRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetStatusForGroupsResponse>>} A promise containing the HttpResponse of GetStatusForGroupsResponse
   */
  async postTournamentSearchGroups(payload: GetStatusForGroupsRequest, gamertag?: string): Promise<HttpResponse<GetStatusForGroupsResponse>> {
    let endpoint = "/basic/tournaments/search/groups";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<GetStatusForGroupsResponse, GetStatusForGroupsRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
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
    let endpoint = "/basic/tournaments/";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      contentId,
      cycle,
      isRunning
    });
    
    // Make the API request
    return this.requester.request<TournamentQueryResponse>({
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
   * @param {JoinRequest} payload - The `JoinRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PlayerStatus>>} A promise containing the HttpResponse of PlayerStatus
   */
  async postTournament(payload: JoinRequest, gamertag?: string): Promise<HttpResponse<PlayerStatus>> {
    let endpoint = "/basic/tournaments/";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PlayerStatus, JoinRequest>({
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
   * @param {string} contentId - The `contentId` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetGroupStatusResponse>>} A promise containing the HttpResponse of GetGroupStatusResponse
   */
  async getTournamentsMeGroup(contentId?: string, gamertag?: string): Promise<HttpResponse<GetGroupStatusResponse>> {
    let endpoint = "/basic/tournaments/me/group";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      contentId
    });
    
    // Make the API request
    return this.requester.request<GetGroupStatusResponse>({
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
   * @param {string} contentId - The `contentId` parameter to include in the API request.
   * @param {string} tournamentId - The `tournamentId` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<RewardsResponse>>} A promise containing the HttpResponse of RewardsResponse
   */
  async getTournamentsRewards(contentId?: string, tournamentId?: string, gamertag?: string): Promise<HttpResponse<RewardsResponse>> {
    let endpoint = "/basic/tournaments/rewards";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      contentId,
      tournamentId
    });
    
    // Make the API request
    return this.requester.request<RewardsResponse>({
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
   * @param {RewardsRequest} payload - The `RewardsRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<RewardsResponse>>} A promise containing the HttpResponse of RewardsResponse
   */
  async postTournamentRewards(payload: RewardsRequest, gamertag?: string): Promise<HttpResponse<RewardsResponse>> {
    let endpoint = "/basic/tournaments/rewards";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<RewardsResponse, RewardsRequest>({
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
    let endpoint = "/basic/tournaments/global";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      tournamentId,
      contentId,
      cycle,
      focus,
      from,
      max
    });
    
    // Make the API request
    return this.requester.request<GetStandingsResponse>({
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
    let endpoint = "/basic/tournaments/standings/group";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      tournamentId,
      contentId,
      cycle,
      focus,
      from,
      max
    });
    
    // Make the API request
    return this.requester.request<GetStandingsResponse>({
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
    let endpoint = "/basic/tournaments/standings";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      tournamentId,
      contentId,
      cycle,
      focus,
      from,
      max
    });
    
    // Make the API request
    return this.requester.request<GetStandingsResponse>({
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
   * @param {bigint | string} playerId - The `playerId` parameter to include in the API request.
   * @param {string} contentId - The `contentId` parameter to include in the API request.
   * @param {boolean} hasUnclaimedRewards - The `hasUnclaimedRewards` parameter to include in the API request.
   * @param {string} tournamentId - The `tournamentId` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AdminGetPlayerStatusResponse>>} A promise containing the HttpResponse of AdminGetPlayerStatusResponse
   */
  async getTournamentsAdminPlayer(playerId: bigint | string, contentId?: string, hasUnclaimedRewards?: boolean, tournamentId?: string, gamertag?: string): Promise<HttpResponse<AdminGetPlayerStatusResponse>> {
    let endpoint = "/basic/tournaments/admin/player";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      playerId,
      contentId,
      hasUnclaimedRewards,
      tournamentId
    });
    
    // Make the API request
    return this.requester.request<AdminGetPlayerStatusResponse>({
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
   * @param {UpdatePlayerStatusRequest} payload - The `UpdatePlayerStatusRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AdminPlayerStatus>>} A promise containing the HttpResponse of AdminPlayerStatus
   */
  async putTournamentAdminPlayer(payload: UpdatePlayerStatusRequest, gamertag?: string): Promise<HttpResponse<AdminPlayerStatus>> {
    let endpoint = "/basic/tournaments/admin/player";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<AdminPlayerStatus, UpdatePlayerStatusRequest>({
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
   * @param {string} contentId - The `contentId` parameter to include in the API request.
   * @param {boolean} hasUnclaimedRewards - The `hasUnclaimedRewards` parameter to include in the API request.
   * @param {string} tournamentId - The `tournamentId` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetPlayerStatusResponse>>} A promise containing the HttpResponse of GetPlayerStatusResponse
   */
  async getTournamentsMe(contentId?: string, hasUnclaimedRewards?: boolean, tournamentId?: string, gamertag?: string): Promise<HttpResponse<GetPlayerStatusResponse>> {
    let endpoint = "/basic/tournaments/me";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      contentId,
      hasUnclaimedRewards,
      tournamentId
    });
    
    // Make the API request
    return this.requester.request<GetPlayerStatusResponse>({
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
   * @param {number} cycles - The `cycles` parameter to include in the API request.
   * @param {string} tournamentId - The `tournamentId` parameter to include in the API request.
   * @param {string} contentId - The `contentId` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetChampionsResponse>>} A promise containing the HttpResponse of GetChampionsResponse
   */
  async getTournamentsChampions(cycles: number, tournamentId: string, contentId?: string, gamertag?: string): Promise<HttpResponse<GetChampionsResponse>> {
    let endpoint = "/basic/tournaments/champions";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      cycles,
      tournamentId,
      contentId
    });
    
    // Make the API request
    return this.requester.request<GetChampionsResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers,
      withAuth: true
    });
  }
  
  /**
   * @param {ScoreRequest} payload - The `ScoreRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postTournamentScore(payload: ScoreRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let endpoint = "/basic/tournaments/score";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyResponse, ScoreRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
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
    let endpoint = "/basic/tournaments/groups";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      tournamentId,
      contentId,
      cycle,
      focus,
      from,
      max
    });
    
    // Make the API request
    return this.requester.request<GetGroupsResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers,
      withAuth: true
    });
  }
  
  /**
   * @param {bigint | string} objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<TournamentClientView>>} A promise containing the HttpResponse of TournamentClientView
   */
  async getTournamentByObjectId(objectId: bigint | string, gamertag?: string): Promise<HttpResponse<TournamentClientView>> {
    let endpoint = "/object/tournaments/{objectId}/".replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<TournamentClientView>({
      url: endpoint,
      method: HttpMethod.GET,
      headers
    });
  }
}
