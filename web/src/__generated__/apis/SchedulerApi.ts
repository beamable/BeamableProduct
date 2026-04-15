/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { jobIdPlaceholder } from '@/__generated__/apis/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import type { ApiSchedulerJobActivityGetSchedulerResponse } from '@/__generated__/schemas/ApiSchedulerJobActivityGetSchedulerResponse';
import type { ApiSchedulerJobCancelPutSchedulerResponse } from '@/__generated__/schemas/ApiSchedulerJobCancelPutSchedulerResponse';
import type { ApiSchedulerJobDeleteSchedulerResponse } from '@/__generated__/schemas/ApiSchedulerJobDeleteSchedulerResponse';
import type { ApiSchedulerJobNextExecutionsGetSchedulerResponse } from '@/__generated__/schemas/ApiSchedulerJobNextExecutionsGetSchedulerResponse';
import type { ApiSchedulerJobsGetSchedulerResponse } from '@/__generated__/schemas/ApiSchedulerJobsGetSchedulerResponse';
import type { ExecuteJobRequest } from '@/__generated__/schemas/ExecuteJobRequest';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { JobActivityViewCursorPagedResult } from '@/__generated__/schemas/JobActivityViewCursorPagedResult';
import type { JobDefinitionSaveRequest } from '@/__generated__/schemas/JobDefinitionSaveRequest';
import type { JobDefinitionView } from '@/__generated__/schemas/JobDefinitionView';
import type { JobDefinitionViewCursorPagedResult } from '@/__generated__/schemas/JobDefinitionViewCursorPagedResult';
import type { JobExecutionResult } from '@/__generated__/schemas/JobExecutionResult';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `ExecuteJobRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function schedulerPostJobExecuteInternal(requester: HttpRequester, payload: ExecuteJobRequest, gamertag?: string): Promise<HttpResponse<JobExecutionResult>> {
  let endpoint = "/api/internal/scheduler/job/execute";
  
  // Make the API request
  return makeApiRequest<JobExecutionResult, ExecuteJobRequest>({
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
export async function schedulerPostJob(requester: HttpRequester, payload: JobDefinitionSaveRequest, gamertag?: string): Promise<HttpResponse<JobDefinitionView>> {
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
export async function schedulerPostJobInternal(requester: HttpRequester, payload: JobDefinitionSaveRequest, gamertag?: string): Promise<HttpResponse<JobDefinitionView>> {
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
 * @param limit - Maximum number of results. Cannot exceed 10000.
 * @param name - Optional name filter.
 * @param source - Optional source filter.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function schedulerGetJobs(requester: HttpRequester, limit?: number, name?: string, source?: string, gamertag?: string): Promise<HttpResponse<ApiSchedulerJobsGetSchedulerResponse>> {
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
 * @param cursor - Pagination cursor from a previous response.
 * @param name - Optional name filter.
 * @param onlyUnique - When true, only returns unique jobs.
 * @param source - Optional source filter.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function schedulerGetJobsPaged(requester: HttpRequester, cursor?: string, name?: string, onlyUnique?: boolean, source?: string, gamertag?: string): Promise<HttpResponse<JobDefinitionViewCursorPagedResult>> {
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
 * @param cursor - Pagination cursor from a previous response. Mutually exclusive with from.
 * @param from - Return jobs suspended from this datetime. Mutually exclusive with cursor.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function schedulerGetJobsSuspended(requester: HttpRequester, cursor?: string, from?: Date, gamertag?: string): Promise<HttpResponse<JobDefinitionViewCursorPagedResult>> {
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
 * @param jobId - ID of the job to retrieve.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function schedulerGetJobByJobId(requester: HttpRequester, jobId: string, gamertag?: string): Promise<HttpResponse<JobDefinitionView>> {
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
 * @param jobId - ID of the job to delete.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function schedulerDeleteJobByJobId(requester: HttpRequester, jobId: string, gamertag?: string): Promise<HttpResponse<ApiSchedulerJobDeleteSchedulerResponse>> {
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
 * @param jobId - ID of the job to retrieve activity for.
 * @param limit - Maximum number of results. Cannot exceed 10000.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function schedulerGetJobActivityByJobId(requester: HttpRequester, jobId: string, limit?: number, gamertag?: string): Promise<HttpResponse<ApiSchedulerJobActivityGetSchedulerResponse>> {
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
 * @param jobId - ID of the job to retrieve activity for.
 * @param cursor - Pagination cursor from a previous response.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function schedulerGetJobActivityPagedByJobId(requester: HttpRequester, jobId: string, cursor?: string, gamertag?: string): Promise<HttpResponse<JobActivityViewCursorPagedResult>> {
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
 * @param cursor - Pagination cursor from a previous response.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function schedulerGetJobsActivityPaged(requester: HttpRequester, cursor?: string, gamertag?: string): Promise<HttpResponse<JobActivityViewCursorPagedResult>> {
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
 * @param jobId - ID of the job to compute schedules for.
 * @param from - Start time for the schedule preview. Defaults to now.
 * @param limit - Maximum number of executions to return. Cannot exceed 1000.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function schedulerGetJobNextExecutionsByJobId(requester: HttpRequester, jobId: string, from?: Date, limit?: number, gamertag?: string): Promise<HttpResponse<ApiSchedulerJobNextExecutionsGetSchedulerResponse>> {
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
 * @param jobId - ID of the job to cancel.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function schedulerPutJobCancelByJobId(requester: HttpRequester, jobId: string, gamertag?: string): Promise<HttpResponse<ApiSchedulerJobCancelPutSchedulerResponse>> {
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
