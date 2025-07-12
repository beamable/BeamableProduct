import { CreateTrialRestRequest } from '@/__generated__/schemas/CreateTrialRestRequest';
import { DELETE } from '@/constants';
import { DeleteTrialDataRequest } from '@/__generated__/schemas/DeleteTrialDataRequest';
import { DeleteTrialRequest } from '@/__generated__/schemas/DeleteTrialRequest';
import { GET } from '@/constants';
import { GetPlayerTrialsResponse } from '@/__generated__/schemas/GetPlayerTrialsResponse';
import { GetS3DataResponse } from '@/__generated__/schemas/GetS3DataResponse';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpResponse } from '@/network/http/types/HttpResponse';
import { ListTrialsResponse } from '@/__generated__/schemas/ListTrialsResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { PauseTrialRequest } from '@/__generated__/schemas/PauseTrialRequest';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import { SaveGameDataResponse } from '@/__generated__/schemas/SaveGameDataResponse';
import { ScheduleTrialRequest } from '@/__generated__/schemas/ScheduleTrialRequest';
import { StartTrialRequest } from '@/__generated__/schemas/StartTrialRequest';
import { TrialSuccessResponse } from '@/__generated__/schemas/TrialSuccessResponse';
import { UploadTrialDataRequest } from '@/__generated__/schemas/UploadTrialDataRequest';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param id - The `id` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getTrialsAdminData(requester: HttpRequester, id: bigint | string, gamertag?: string): Promise<HttpResponse<GetS3DataResponse>> {
  let endpoint = "/basic/trials/admin/data";
  
  // Make the API request
  return makeApiRequest<GetS3DataResponse>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `UploadTrialDataRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postTrialData(requester: HttpRequester, payload: UploadTrialDataRequest, gamertag?: string): Promise<HttpResponse<SaveGameDataResponse>> {
  let endpoint = "/basic/trials/data";
  
  // Make the API request
  return makeApiRequest<SaveGameDataResponse, UploadTrialDataRequest>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `DeleteTrialDataRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function deleteTrialData(requester: HttpRequester, payload: DeleteTrialDataRequest, gamertag?: string): Promise<HttpResponse<TrialSuccessResponse>> {
  let endpoint = "/basic/trials/data";
  
  // Make the API request
  return makeApiRequest<TrialSuccessResponse, DeleteTrialDataRequest>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `PauseTrialRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function putTrialPause(requester: HttpRequester, payload: PauseTrialRequest, gamertag?: string): Promise<HttpResponse<TrialSuccessResponse>> {
  let endpoint = "/basic/trials/pause";
  
  // Make the API request
  return makeApiRequest<TrialSuccessResponse, PauseTrialRequest>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `ScheduleTrialRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function putTrialSchedule(requester: HttpRequester, payload: ScheduleTrialRequest, gamertag?: string): Promise<HttpResponse<TrialSuccessResponse>> {
  let endpoint = "/basic/trials/schedule";
  
  // Make the API request
  return makeApiRequest<TrialSuccessResponse, ScheduleTrialRequest>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getTrials(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<ListTrialsResponse>> {
  let endpoint = "/basic/trials/";
  
  // Make the API request
  return makeApiRequest<ListTrialsResponse>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `CreateTrialRestRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postTrial(requester: HttpRequester, payload: CreateTrialRestRequest, gamertag?: string): Promise<HttpResponse<TrialSuccessResponse>> {
  let endpoint = "/basic/trials/";
  
  // Make the API request
  return makeApiRequest<TrialSuccessResponse, CreateTrialRestRequest>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `DeleteTrialRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function deleteTrial(requester: HttpRequester, payload: DeleteTrialRequest, gamertag?: string): Promise<HttpResponse<TrialSuccessResponse>> {
  let endpoint = "/basic/trials/";
  
  // Make the API request
  return makeApiRequest<TrialSuccessResponse, DeleteTrialRequest>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param dbid - The `dbid` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getTrialsAdmin(requester: HttpRequester, dbid: bigint | string, gamertag?: string): Promise<HttpResponse<GetPlayerTrialsResponse>> {
  let endpoint = "/basic/trials/admin";
  
  // Make the API request
  return makeApiRequest<GetPlayerTrialsResponse>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `StartTrialRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function putTrialStart(requester: HttpRequester, payload: StartTrialRequest, gamertag?: string): Promise<HttpResponse<TrialSuccessResponse>> {
  let endpoint = "/basic/trials/start";
  
  // Make the API request
  return makeApiRequest<TrialSuccessResponse, StartTrialRequest>({
    r: requester,
    e: endpoint,
    m: PUT,
    p: payload,
    g: gamertag,
    w: true
  });
}
