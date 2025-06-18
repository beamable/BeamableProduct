import { CreateTrialRestRequest } from '@/__generated__/schemas/CreateTrialRestRequest';
import { DELETE } from '@/constants';
import { DeleteTrialDataRequest } from '@/__generated__/schemas/DeleteTrialDataRequest';
import { DeleteTrialRequest } from '@/__generated__/schemas/DeleteTrialRequest';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { GetPlayerTrialsResponse } from '@/__generated__/schemas/GetPlayerTrialsResponse';
import { GetS3DataResponse } from '@/__generated__/schemas/GetS3DataResponse';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { ListTrialsResponse } from '@/__generated__/schemas/ListTrialsResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/constants';
import { PauseTrialRequest } from '@/__generated__/schemas/PauseTrialRequest';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import { SaveGameDataResponse } from '@/__generated__/schemas/SaveGameDataResponse';
import { ScheduleTrialRequest } from '@/__generated__/schemas/ScheduleTrialRequest';
import { StartTrialRequest } from '@/__generated__/schemas/StartTrialRequest';
import { TrialSuccessResponse } from '@/__generated__/schemas/TrialSuccessResponse';
import { UploadTrialDataRequest } from '@/__generated__/schemas/UploadTrialDataRequest';

export class TrialsApi {
  constructor(
    private readonly r: HttpRequester
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
    let e = "/basic/trials/admin/data";
    
    // Make the API request
    return makeApiRequest<GetS3DataResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        id
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
   * @param {UploadTrialDataRequest} payload - The `UploadTrialDataRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<SaveGameDataResponse>>} A promise containing the HttpResponse of SaveGameDataResponse
   */
  async postTrialData(payload: UploadTrialDataRequest, gamertag?: string): Promise<HttpResponse<SaveGameDataResponse>> {
    let e = "/basic/trials/data";
    
    // Make the API request
    return makeApiRequest<SaveGameDataResponse, UploadTrialDataRequest>({
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
   * @param {DeleteTrialDataRequest} payload - The `DeleteTrialDataRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<TrialSuccessResponse>>} A promise containing the HttpResponse of TrialSuccessResponse
   */
  async deleteTrialData(payload: DeleteTrialDataRequest, gamertag?: string): Promise<HttpResponse<TrialSuccessResponse>> {
    let e = "/basic/trials/data";
    
    // Make the API request
    return makeApiRequest<TrialSuccessResponse, DeleteTrialDataRequest>({
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
   * @param {PauseTrialRequest} payload - The `PauseTrialRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<TrialSuccessResponse>>} A promise containing the HttpResponse of TrialSuccessResponse
   */
  async putTrialPause(payload: PauseTrialRequest, gamertag?: string): Promise<HttpResponse<TrialSuccessResponse>> {
    let e = "/basic/trials/pause";
    
    // Make the API request
    return makeApiRequest<TrialSuccessResponse, PauseTrialRequest>({
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
   * @param {ScheduleTrialRequest} payload - The `ScheduleTrialRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<TrialSuccessResponse>>} A promise containing the HttpResponse of TrialSuccessResponse
   */
  async putTrialSchedule(payload: ScheduleTrialRequest, gamertag?: string): Promise<HttpResponse<TrialSuccessResponse>> {
    let e = "/basic/trials/schedule";
    
    // Make the API request
    return makeApiRequest<TrialSuccessResponse, ScheduleTrialRequest>({
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
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ListTrialsResponse>>} A promise containing the HttpResponse of ListTrialsResponse
   */
  async getTrials(gamertag?: string): Promise<HttpResponse<ListTrialsResponse>> {
    let e = "/basic/trials/";
    
    // Make the API request
    return makeApiRequest<ListTrialsResponse>({
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
   * @param {CreateTrialRestRequest} payload - The `CreateTrialRestRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<TrialSuccessResponse>>} A promise containing the HttpResponse of TrialSuccessResponse
   */
  async postTrial(payload: CreateTrialRestRequest, gamertag?: string): Promise<HttpResponse<TrialSuccessResponse>> {
    let e = "/basic/trials/";
    
    // Make the API request
    return makeApiRequest<TrialSuccessResponse, CreateTrialRestRequest>({
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
   * @param {DeleteTrialRequest} payload - The `DeleteTrialRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<TrialSuccessResponse>>} A promise containing the HttpResponse of TrialSuccessResponse
   */
  async deleteTrial(payload: DeleteTrialRequest, gamertag?: string): Promise<HttpResponse<TrialSuccessResponse>> {
    let e = "/basic/trials/";
    
    // Make the API request
    return makeApiRequest<TrialSuccessResponse, DeleteTrialRequest>({
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
   * @param {bigint | string} dbid - The `dbid` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetPlayerTrialsResponse>>} A promise containing the HttpResponse of GetPlayerTrialsResponse
   */
  async getTrialsAdmin(dbid: bigint | string, gamertag?: string): Promise<HttpResponse<GetPlayerTrialsResponse>> {
    let e = "/basic/trials/admin";
    
    // Make the API request
    return makeApiRequest<GetPlayerTrialsResponse>({
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
   * @param {StartTrialRequest} payload - The `StartTrialRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<TrialSuccessResponse>>} A promise containing the HttpResponse of TrialSuccessResponse
   */
  async putTrialStart(payload: StartTrialRequest, gamertag?: string): Promise<HttpResponse<TrialSuccessResponse>> {
    let e = "/basic/trials/start";
    
    // Make the API request
    return makeApiRequest<TrialSuccessResponse, StartTrialRequest>({
      r: this.r,
      e,
      m: PUT,
      p: payload,
      g: gamertag,
      w: true
    });
  }
}
