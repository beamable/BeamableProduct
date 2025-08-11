/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { DELETE } from '@/constants';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import type { CreateTrialRestRequest } from '@/__generated__/schemas/CreateTrialRestRequest';
import type { DeleteTrialDataRequest } from '@/__generated__/schemas/DeleteTrialDataRequest';
import type { DeleteTrialRequest } from '@/__generated__/schemas/DeleteTrialRequest';
import type { GetPlayerTrialsResponse } from '@/__generated__/schemas/GetPlayerTrialsResponse';
import type { GetS3DataResponse } from '@/__generated__/schemas/GetS3DataResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { ListTrialsResponse } from '@/__generated__/schemas/ListTrialsResponse';
import type { PauseTrialRequest } from '@/__generated__/schemas/PauseTrialRequest';
import type { SaveGameDataResponse } from '@/__generated__/schemas/SaveGameDataResponse';
import type { ScheduleTrialRequest } from '@/__generated__/schemas/ScheduleTrialRequest';
import type { StartTrialRequest } from '@/__generated__/schemas/StartTrialRequest';
import type { TrialSuccessResponse } from '@/__generated__/schemas/TrialSuccessResponse';
import type { UploadTrialDataRequest } from '@/__generated__/schemas/UploadTrialDataRequest';

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
export async function trialsGetAdminDataBasic(requester: HttpRequester, id: bigint | string, gamertag?: string): Promise<HttpResponse<GetS3DataResponse>> {
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
export async function trialsPostDataBasic(requester: HttpRequester, payload: UploadTrialDataRequest, gamertag?: string): Promise<HttpResponse<SaveGameDataResponse>> {
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
export async function trialsDeleteDataBasic(requester: HttpRequester, payload: DeleteTrialDataRequest, gamertag?: string): Promise<HttpResponse<TrialSuccessResponse>> {
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
export async function trialsPutPauseBasic(requester: HttpRequester, payload: PauseTrialRequest, gamertag?: string): Promise<HttpResponse<TrialSuccessResponse>> {
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
export async function trialsPutScheduleBasic(requester: HttpRequester, payload: ScheduleTrialRequest, gamertag?: string): Promise<HttpResponse<TrialSuccessResponse>> {
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
export async function trialsGetBasic(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<ListTrialsResponse>> {
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
export async function trialsPostBasic(requester: HttpRequester, payload: CreateTrialRestRequest, gamertag?: string): Promise<HttpResponse<TrialSuccessResponse>> {
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
export async function trialsDeleteBasic(requester: HttpRequester, payload: DeleteTrialRequest, gamertag?: string): Promise<HttpResponse<TrialSuccessResponse>> {
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
export async function trialsGetAdminBasic(requester: HttpRequester, dbid: bigint | string, gamertag?: string): Promise<HttpResponse<GetPlayerTrialsResponse>> {
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
export async function trialsPutStartBasic(requester: HttpRequester, payload: StartTrialRequest, gamertag?: string): Promise<HttpResponse<TrialSuccessResponse>> {
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
