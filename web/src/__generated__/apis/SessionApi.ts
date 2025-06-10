import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { makeQueryString } from '@/utils/makeQueryString';
import { OnlineStatusResponses } from '@/__generated__/schemas/OnlineStatusResponses';
import { SessionClientHistoryResponse } from '@/__generated__/schemas/SessionClientHistoryResponse';
import { SessionHeartbeat } from '@/__generated__/schemas/SessionHeartbeat';
import { SessionHistoryResponse } from '@/__generated__/schemas/SessionHistoryResponse';
import { StartSessionRequest } from '@/__generated__/schemas/StartSessionRequest';
import { StartSessionResponse } from '@/__generated__/schemas/StartSessionResponse';

export class SessionApi {
  constructor(
    private readonly requester: HttpRequester
  ) {
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<SessionHeartbeat>>} A promise containing the HttpResponse of SessionHeartbeat
   */
  async postSessionHeartbeat(gamertag?: string): Promise<HttpResponse<SessionHeartbeat>> {
    let endpoint = "/basic/session/heartbeat";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<SessionHeartbeat>({
      url: endpoint,
      method: HttpMethod.POST,
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
   * @param {number} month - The `month` parameter to include in the API request.
   * @param {number} year - The `year` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<SessionHistoryResponse>>} A promise containing the HttpResponse of SessionHistoryResponse
   */
  async getSessionHistory(dbid: bigint | string, month?: number, year?: number, gamertag?: string): Promise<HttpResponse<SessionHistoryResponse>> {
    let endpoint = "/basic/session/history";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      dbid,
      month,
      year
    });
    
    // Make the API request
    return this.requester.request<SessionHistoryResponse>({
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
   * @param {bigint | string} intervalSecs - The `intervalSecs` parameter to include in the API request.
   * @param {string} playerIds - The `playerIds` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<OnlineStatusResponses>>} A promise containing the HttpResponse of OnlineStatusResponses
   */
  async getSessionStatus(intervalSecs: bigint | string, playerIds: string, gamertag?: string): Promise<HttpResponse<OnlineStatusResponses>> {
    let endpoint = "/basic/session/status";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      intervalSecs,
      playerIds
    });
    
    // Make the API request
    return this.requester.request<OnlineStatusResponses>({
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
   * @param {number} month - The `month` parameter to include in the API request.
   * @param {number} year - The `year` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<SessionClientHistoryResponse>>} A promise containing the HttpResponse of SessionClientHistoryResponse
   */
  async getSessionClientHistory(month?: number, year?: number, gamertag?: string): Promise<HttpResponse<SessionClientHistoryResponse>> {
    let endpoint = "/basic/session/client/history";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      month,
      year
    });
    
    // Make the API request
    return this.requester.request<SessionClientHistoryResponse>({
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
   * @param {StartSessionRequest} payload - The `StartSessionRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<StartSessionResponse>>} A promise containing the HttpResponse of StartSessionResponse
   */
  async postSession(payload: StartSessionRequest, gamertag?: string): Promise<HttpResponse<StartSessionResponse>> {
    let endpoint = "/basic/session/";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<StartSessionResponse, StartSessionRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
}
