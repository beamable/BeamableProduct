import { GET } from '@/constants';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { OnlineStatusResponses } from '@/__generated__/schemas/OnlineStatusResponses';
import { POST } from '@/constants';
import { SessionClientHistoryResponse } from '@/__generated__/schemas/SessionClientHistoryResponse';
import { SessionHeartbeat } from '@/__generated__/schemas/SessionHeartbeat';
import { SessionHistoryResponse } from '@/__generated__/schemas/SessionHistoryResponse';
import { StartSessionRequest } from '@/__generated__/schemas/StartSessionRequest';
import { StartSessionResponse } from '@/__generated__/schemas/StartSessionResponse';

export class SessionApi {
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
   * @returns {Promise<HttpResponse<SessionHeartbeat>>} A promise containing the HttpResponse of SessionHeartbeat
   */
  async postSessionHeartbeat(gamertag?: string): Promise<HttpResponse<SessionHeartbeat>> {
    let e = "/basic/session/heartbeat";
    
    // Make the API request
    return makeApiRequest<SessionHeartbeat>({
      r: this.r,
      e,
      m: POST,
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
   * @param {number} month - The `month` parameter to include in the API request.
   * @param {number} year - The `year` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<SessionHistoryResponse>>} A promise containing the HttpResponse of SessionHistoryResponse
   */
  async getSessionHistory(dbid: bigint | string, month?: number, year?: number, gamertag?: string): Promise<HttpResponse<SessionHistoryResponse>> {
    let e = "/basic/session/history";
    
    // Make the API request
    return makeApiRequest<SessionHistoryResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        dbid,
        month,
        year
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
   * @param {bigint | string} intervalSecs - The `intervalSecs` parameter to include in the API request.
   * @param {string} playerIds - The `playerIds` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<OnlineStatusResponses>>} A promise containing the HttpResponse of OnlineStatusResponses
   */
  async getSessionStatus(intervalSecs: bigint | string, playerIds: string, gamertag?: string): Promise<HttpResponse<OnlineStatusResponses>> {
    let e = "/basic/session/status";
    
    // Make the API request
    return makeApiRequest<OnlineStatusResponses>({
      r: this.r,
      e,
      m: GET,
      q: {
        intervalSecs,
        playerIds
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
   * @param {number} month - The `month` parameter to include in the API request.
   * @param {number} year - The `year` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<SessionClientHistoryResponse>>} A promise containing the HttpResponse of SessionClientHistoryResponse
   */
  async getSessionClientHistory(month?: number, year?: number, gamertag?: string): Promise<HttpResponse<SessionClientHistoryResponse>> {
    let e = "/basic/session/client/history";
    
    // Make the API request
    return makeApiRequest<SessionClientHistoryResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        month,
        year
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
   * @param {StartSessionRequest} payload - The `StartSessionRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<StartSessionResponse>>} A promise containing the HttpResponse of StartSessionResponse
   */
  async postSession(payload: StartSessionRequest, gamertag?: string): Promise<HttpResponse<StartSessionResponse>> {
    let e = "/basic/session/";
    
    // Make the API request
    return makeApiRequest<StartSessionResponse, StartSessionRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
    });
  }
}
