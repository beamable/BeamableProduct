import { ApiSchedulerJobActivityGetSchedulerResponse } from '@/__generated__/schemas/ApiSchedulerJobActivityGetSchedulerResponse';
import { ApiSchedulerJobCancelPutSchedulerResponse } from '@/__generated__/schemas/ApiSchedulerJobCancelPutSchedulerResponse';
import { ApiSchedulerJobDeleteSchedulerResponse } from '@/__generated__/schemas/ApiSchedulerJobDeleteSchedulerResponse';
import { ApiSchedulerJobNextExecutionsGetSchedulerResponse } from '@/__generated__/schemas/ApiSchedulerJobNextExecutionsGetSchedulerResponse';
import { ApiSchedulerJobsGetSchedulerResponse } from '@/__generated__/schemas/ApiSchedulerJobsGetSchedulerResponse';
import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpResponse } from '@/network/http/types/HttpResponse';
import { JobActivityViewCursorPagedResult } from '@/__generated__/schemas/JobActivityViewCursorPagedResult';
import { JobDefinitionSaveRequest } from '@/__generated__/schemas/JobDefinitionSaveRequest';
import { JobDefinitionView } from '@/__generated__/schemas/JobDefinitionView';
import { JobDefinitionViewCursorPagedResult } from '@/__generated__/schemas/JobDefinitionViewCursorPagedResult';
import { JobExecutionEvent } from '@/__generated__/schemas/JobExecutionEvent';
import { JobExecutionResult } from '@/__generated__/schemas/JobExecutionResult';
import { jobIdPlaceholder } from '@/__generated__/apis/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import { PUT } from '@/constants';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `JobExecutionEvent` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function postSchedulerJobExecuteInternal(requester: HttpRequester, payload: JobExecutionEvent, gamertag?: string): Promise<HttpResponse<JobExecutionResult>> {
  let endpoint = "/api/internal/scheduler/job/execute";
  
  // Make the API request
  return makeApiRequest<JobExecutionResult, JobExecutionEvent>({
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
 * @param payload - The `JobDefinitionSaveRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function postSchedulerJob(requester: HttpRequester, payload: JobDefinitionSaveRequest, gamertag?: string): Promise<HttpResponse<JobDefinitionView>> {
  let endpoint = "/api/scheduler/job";
  
  // Make the API request
  return makeApiRequest<JobDefinitionView, JobDefinitionSaveRequest>({
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
 * @param payload - The `JobDefinitionSaveRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function postSchedulerJobInternal(requester: HttpRequester, payload: JobDefinitionSaveRequest, gamertag?: string): Promise<HttpResponse<JobDefinitionView>> {
  let endpoint = "/api/internal/scheduler/job";
  
  // Make the API request
  return makeApiRequest<JobDefinitionView, JobDefinitionSaveRequest>({
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
 * @deprecated
 * This API method is deprecated and may be removed in future versions.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param limit - The `limit` parameter to include in the API request.
 * @param name - The `name` parameter to include in the API request.
 * @param source - The `source` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function getSchedulerJobs(requester: HttpRequester, limit?: number, name?: string, source?: string, gamertag?: string): Promise<HttpResponse<ApiSchedulerJobsGetSchedulerResponse>> {
  let endpoint = "/api/scheduler/jobs";
  
  // Make the API request
  return makeApiRequest<ApiSchedulerJobsGetSchedulerResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      limit,
      name,
      source
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
 * @param cursor - The `cursor` parameter to include in the API request.
 * @param name - The `name` parameter to include in the API request.
 * @param onlyUnique - The `onlyUnique` parameter to include in the API request.
 * @param source - The `source` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function getSchedulerJobsPaged(requester: HttpRequester, cursor?: string, name?: string, onlyUnique?: boolean, source?: string, gamertag?: string): Promise<HttpResponse<JobDefinitionViewCursorPagedResult>> {
  let endpoint = "/api/scheduler/jobs-paged";
  
  // Make the API request
  return makeApiRequest<JobDefinitionViewCursorPagedResult>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      cursor,
      name,
      onlyUnique,
      source
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
 * @param cursor - The `cursor` parameter to include in the API request.
 * @param from - The `from` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function getSchedulerJobsSuspended(requester: HttpRequester, cursor?: string, from?: Date, gamertag?: string): Promise<HttpResponse<JobDefinitionViewCursorPagedResult>> {
  let endpoint = "/api/scheduler/jobs/suspended";
  
  // Make the API request
  return makeApiRequest<JobDefinitionViewCursorPagedResult>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      cursor,
      from
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
 * @param jobId - The `jobId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function getSchedulerJobByJobId(requester: HttpRequester, jobId: string, gamertag?: string): Promise<HttpResponse<JobDefinitionView>> {
  let endpoint = "/api/scheduler/job/{jobId}".replace(jobIdPlaceholder, endpointEncoder(jobId));
  
  // Make the API request
  return makeApiRequest<JobDefinitionView>({
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
 * @param jobId - The `jobId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function deleteSchedulerJobByJobId(requester: HttpRequester, jobId: string, gamertag?: string): Promise<HttpResponse<ApiSchedulerJobDeleteSchedulerResponse>> {
  let endpoint = "/api/scheduler/job/{jobId}".replace(jobIdPlaceholder, endpointEncoder(jobId));
  
  // Make the API request
  return makeApiRequest<ApiSchedulerJobDeleteSchedulerResponse>({
    r: requester,
    e: endpoint,
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
 * @deprecated
 * This API method is deprecated and may be removed in future versions.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param jobId - The `jobId` parameter to include in the API request.
 * @param limit - The `limit` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function getSchedulerJobActivityByJobId(requester: HttpRequester, jobId: string, limit?: number, gamertag?: string): Promise<HttpResponse<ApiSchedulerJobActivityGetSchedulerResponse>> {
  let endpoint = "/api/scheduler/job/{jobId}/activity".replace(jobIdPlaceholder, endpointEncoder(jobId));
  
  // Make the API request
  return makeApiRequest<ApiSchedulerJobActivityGetSchedulerResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      limit
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
 * @param jobId - The `jobId` parameter to include in the API request.
 * @param cursor - The `cursor` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function getSchedulerJobActivityPagedByJobId(requester: HttpRequester, jobId: string, cursor?: string, gamertag?: string): Promise<HttpResponse<JobActivityViewCursorPagedResult>> {
  let endpoint = "/api/scheduler/job/{jobId}/activity-paged".replace(jobIdPlaceholder, endpointEncoder(jobId));
  
  // Make the API request
  return makeApiRequest<JobActivityViewCursorPagedResult>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      cursor
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
 * @param cursor - The `cursor` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function getSchedulerJobsActivityPaged(requester: HttpRequester, cursor?: string, gamertag?: string): Promise<HttpResponse<JobActivityViewCursorPagedResult>> {
  let endpoint = "/api/scheduler/jobs/activity-paged";
  
  // Make the API request
  return makeApiRequest<JobActivityViewCursorPagedResult>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      cursor
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
 * @param jobId - The `jobId` parameter to include in the API request.
 * @param from - The `from` parameter to include in the API request.
 * @param limit - The `limit` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function getSchedulerJobNextExecutionsByJobId(requester: HttpRequester, jobId: string, from?: Date, limit?: number, gamertag?: string): Promise<HttpResponse<ApiSchedulerJobNextExecutionsGetSchedulerResponse>> {
  let endpoint = "/api/scheduler/job/{jobId}/next-executions".replace(jobIdPlaceholder, endpointEncoder(jobId));
  
  // Make the API request
  return makeApiRequest<ApiSchedulerJobNextExecutionsGetSchedulerResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      from,
      limit
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
 * @param jobId - The `jobId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function putSchedulerJobCancelByJobId(requester: HttpRequester, jobId: string, gamertag?: string): Promise<HttpResponse<ApiSchedulerJobCancelPutSchedulerResponse>> {
  let endpoint = "/api/scheduler/job/{jobId}/cancel".replace(jobIdPlaceholder, endpointEncoder(jobId));
  
  // Make the API request
  return makeApiRequest<ApiSchedulerJobCancelPutSchedulerResponse>({
    r: requester,
    e: endpoint,
    m: PUT,
    g: gamertag,
    w: true
  });
}
