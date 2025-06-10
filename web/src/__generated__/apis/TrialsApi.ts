import { CreateTrialRestRequest } from '@/__generated__/schemas/CreateTrialRestRequest';
import { DeleteTrialDataRequest } from '@/__generated__/schemas/DeleteTrialDataRequest';
import { DeleteTrialRequest } from '@/__generated__/schemas/DeleteTrialRequest';
import { GetPlayerTrialsResponse } from '@/__generated__/schemas/GetPlayerTrialsResponse';
import { GetS3DataResponse } from '@/__generated__/schemas/GetS3DataResponse';
import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { ListTrialsResponse } from '@/__generated__/schemas/ListTrialsResponse';
import { makeQueryString } from '@/utils/makeQueryString';
import { PauseTrialRequest } from '@/__generated__/schemas/PauseTrialRequest';
import { SaveGameDataResponse } from '@/__generated__/schemas/SaveGameDataResponse';
import { ScheduleTrialRequest } from '@/__generated__/schemas/ScheduleTrialRequest';
import { StartTrialRequest } from '@/__generated__/schemas/StartTrialRequest';
import { TrialSuccessResponse } from '@/__generated__/schemas/TrialSuccessResponse';
import { UploadTrialDataRequest } from '@/__generated__/schemas/UploadTrialDataRequest';

export class TrialsApi {
  constructor(
    private readonly requester: HttpRequester
  ) {
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {bigint | string} id - The `id` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetS3DataResponse>>} A promise containing the HttpResponse of GetS3DataResponse
   */
  async getTrialsAdminData(id: bigint | string, gamertag?: string): Promise<HttpResponse<GetS3DataResponse>> {
    let endpoint = "/basic/trials/admin/data";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      id
    });
    
    // Make the API request
    return this.requester.request<GetS3DataResponse>({
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
   * @param {UploadTrialDataRequest} payload - The `UploadTrialDataRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<SaveGameDataResponse>>} A promise containing the HttpResponse of SaveGameDataResponse
   */
  async postTrialData(payload: UploadTrialDataRequest, gamertag?: string): Promise<HttpResponse<SaveGameDataResponse>> {
    let endpoint = "/basic/trials/data";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<SaveGameDataResponse, UploadTrialDataRequest>({
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
   * @param {DeleteTrialDataRequest} payload - The `DeleteTrialDataRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<TrialSuccessResponse>>} A promise containing the HttpResponse of TrialSuccessResponse
   */
  async deleteTrialData(payload: DeleteTrialDataRequest, gamertag?: string): Promise<HttpResponse<TrialSuccessResponse>> {
    let endpoint = "/basic/trials/data";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<TrialSuccessResponse, DeleteTrialDataRequest>({
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
   * @param {PauseTrialRequest} payload - The `PauseTrialRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<TrialSuccessResponse>>} A promise containing the HttpResponse of TrialSuccessResponse
   */
  async putTrialPause(payload: PauseTrialRequest, gamertag?: string): Promise<HttpResponse<TrialSuccessResponse>> {
    let endpoint = "/basic/trials/pause";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<TrialSuccessResponse, PauseTrialRequest>({
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
   * @param {ScheduleTrialRequest} payload - The `ScheduleTrialRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<TrialSuccessResponse>>} A promise containing the HttpResponse of TrialSuccessResponse
   */
  async putTrialSchedule(payload: ScheduleTrialRequest, gamertag?: string): Promise<HttpResponse<TrialSuccessResponse>> {
    let endpoint = "/basic/trials/schedule";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<TrialSuccessResponse, ScheduleTrialRequest>({
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
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ListTrialsResponse>>} A promise containing the HttpResponse of ListTrialsResponse
   */
  async getTrials(gamertag?: string): Promise<HttpResponse<ListTrialsResponse>> {
    let endpoint = "/basic/trials/";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<ListTrialsResponse>({
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
   * @param {CreateTrialRestRequest} payload - The `CreateTrialRestRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<TrialSuccessResponse>>} A promise containing the HttpResponse of TrialSuccessResponse
   */
  async postTrial(payload: CreateTrialRestRequest, gamertag?: string): Promise<HttpResponse<TrialSuccessResponse>> {
    let endpoint = "/basic/trials/";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<TrialSuccessResponse, CreateTrialRestRequest>({
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
   * @param {DeleteTrialRequest} payload - The `DeleteTrialRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<TrialSuccessResponse>>} A promise containing the HttpResponse of TrialSuccessResponse
   */
  async deleteTrial(payload: DeleteTrialRequest, gamertag?: string): Promise<HttpResponse<TrialSuccessResponse>> {
    let endpoint = "/basic/trials/";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<TrialSuccessResponse, DeleteTrialRequest>({
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
   * @param {bigint | string} dbid - The `dbid` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetPlayerTrialsResponse>>} A promise containing the HttpResponse of GetPlayerTrialsResponse
   */
  async getTrialsAdmin(dbid: bigint | string, gamertag?: string): Promise<HttpResponse<GetPlayerTrialsResponse>> {
    let endpoint = "/basic/trials/admin";
    
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
    return this.requester.request<GetPlayerTrialsResponse>({
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
   * @param {StartTrialRequest} payload - The `StartTrialRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<TrialSuccessResponse>>} A promise containing the HttpResponse of TrialSuccessResponse
   */
  async putTrialStart(payload: StartTrialRequest, gamertag?: string): Promise<HttpResponse<TrialSuccessResponse>> {
    let endpoint = "/basic/trials/start";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<TrialSuccessResponse, StartTrialRequest>({
      url: endpoint,
      method: HttpMethod.PUT,
      headers,
      body: payload,
      withAuth: true
    });
  }
}
