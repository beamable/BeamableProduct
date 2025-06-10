import { ApiSchedulerJobActivityGetSchedulerResponse } from '@/__generated__/schemas/ApiSchedulerJobActivityGetSchedulerResponse';
import { ApiSchedulerJobCancelPutSchedulerResponse } from '@/__generated__/schemas/ApiSchedulerJobCancelPutSchedulerResponse';
import { ApiSchedulerJobDeleteSchedulerResponse } from '@/__generated__/schemas/ApiSchedulerJobDeleteSchedulerResponse';
import { ApiSchedulerJobNextExecutionsGetSchedulerResponse } from '@/__generated__/schemas/ApiSchedulerJobNextExecutionsGetSchedulerResponse';
import { ApiSchedulerJobsGetSchedulerResponse } from '@/__generated__/schemas/ApiSchedulerJobsGetSchedulerResponse';
import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { JobActivityViewCursorPagedResult } from '@/__generated__/schemas/JobActivityViewCursorPagedResult';
import { JobDefinitionSaveRequest } from '@/__generated__/schemas/JobDefinitionSaveRequest';
import { JobDefinitionView } from '@/__generated__/schemas/JobDefinitionView';
import { JobDefinitionViewCursorPagedResult } from '@/__generated__/schemas/JobDefinitionViewCursorPagedResult';
import { JobExecutionEvent } from '@/__generated__/schemas/JobExecutionEvent';
import { JobExecutionResult } from '@/__generated__/schemas/JobExecutionResult';
import { makeQueryString } from '@/utils/makeQueryString';

export class SchedulerApi {
  constructor(
    private readonly requester: HttpRequester
  ) {
  }
  
  /**
   * @param {JobExecutionEvent} payload - The `JobExecutionEvent` instance to use for the API request
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<JobExecutionResult>>} A promise containing the HttpResponse of JobExecutionResult
   */
  async postSchedulerJobExecuteInternal(payload: JobExecutionEvent, gamertag?: string): Promise<HttpResponse<JobExecutionResult>> {
    let endpoint = "/api/internal/scheduler/job/execute";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<JobExecutionResult, JobExecutionEvent>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {JobDefinitionSaveRequest} payload - The `JobDefinitionSaveRequest` instance to use for the API request
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<JobDefinitionView>>} A promise containing the HttpResponse of JobDefinitionView
   */
  async postSchedulerJob(payload: JobDefinitionSaveRequest, gamertag?: string): Promise<HttpResponse<JobDefinitionView>> {
    let endpoint = "/api/scheduler/job";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<JobDefinitionView, JobDefinitionSaveRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
  
  /**
   * @param {JobDefinitionSaveRequest} payload - The `JobDefinitionSaveRequest` instance to use for the API request
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<JobDefinitionView>>} A promise containing the HttpResponse of JobDefinitionView
   */
  async postSchedulerJobInternal(payload: JobDefinitionSaveRequest, gamertag?: string): Promise<HttpResponse<JobDefinitionView>> {
    let endpoint = "/api/internal/scheduler/job";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<JobDefinitionView, JobDefinitionSaveRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
  
  /**
   * @deprecated
   * This API method is deprecated and may be removed in future versions.
   * 
   * @param {number} limit - The `limit` parameter to include in the API request.
   * @param {string} name - The `name` parameter to include in the API request.
   * @param {string} source - The `source` parameter to include in the API request.
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<ApiSchedulerJobsGetSchedulerResponse>>} A promise containing the HttpResponse of ApiSchedulerJobsGetSchedulerResponse
   */
  async getSchedulerJobs(limit?: number, name?: string, source?: string, gamertag?: string): Promise<HttpResponse<ApiSchedulerJobsGetSchedulerResponse>> {
    let endpoint = "/api/scheduler/jobs";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      limit,
      name,
      source
    });
    
    // Make the API request
    return this.requester.request<ApiSchedulerJobsGetSchedulerResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {string} cursor - The `cursor` parameter to include in the API request.
   * @param {string} name - The `name` parameter to include in the API request.
   * @param {boolean} onlyUnique - The `onlyUnique` parameter to include in the API request.
   * @param {string} source - The `source` parameter to include in the API request.
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<JobDefinitionViewCursorPagedResult>>} A promise containing the HttpResponse of JobDefinitionViewCursorPagedResult
   */
  async getSchedulerJobsPaged(cursor?: string, name?: string, onlyUnique?: boolean, source?: string, gamertag?: string): Promise<HttpResponse<JobDefinitionViewCursorPagedResult>> {
    let endpoint = "/api/scheduler/jobs-paged";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      cursor,
      name,
      onlyUnique,
      source
    });
    
    // Make the API request
    return this.requester.request<JobDefinitionViewCursorPagedResult>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {string} cursor - The `cursor` parameter to include in the API request.
   * @param {Date} from - The `from` parameter to include in the API request.
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<JobDefinitionViewCursorPagedResult>>} A promise containing the HttpResponse of JobDefinitionViewCursorPagedResult
   */
  async getSchedulerJobsSuspended(cursor?: string, from?: Date, gamertag?: string): Promise<HttpResponse<JobDefinitionViewCursorPagedResult>> {
    let endpoint = "/api/scheduler/jobs/suspended";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      cursor,
      from
    });
    
    // Make the API request
    return this.requester.request<JobDefinitionViewCursorPagedResult>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {string} jobId - The `jobId` parameter to include in the API request.
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<JobDefinitionView>>} A promise containing the HttpResponse of JobDefinitionView
   */
  async getSchedulerJobByJobId(jobId: string, gamertag?: string): Promise<HttpResponse<JobDefinitionView>> {
    let endpoint = "/api/scheduler/job/{jobId}";
    endpoint = endpoint.replace("{jobId}", encodeURIComponent(jobId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<JobDefinitionView>({
      url: endpoint,
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {string} jobId - The `jobId` parameter to include in the API request.
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<ApiSchedulerJobDeleteSchedulerResponse>>} A promise containing the HttpResponse of ApiSchedulerJobDeleteSchedulerResponse
   */
  async deleteSchedulerJobByJobId(jobId: string, gamertag?: string): Promise<HttpResponse<ApiSchedulerJobDeleteSchedulerResponse>> {
    let endpoint = "/api/scheduler/job/{jobId}";
    endpoint = endpoint.replace("{jobId}", encodeURIComponent(jobId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<ApiSchedulerJobDeleteSchedulerResponse>({
      url: endpoint,
      method: HttpMethod.DELETE,
      headers
    });
  }
  
  /**
   * @deprecated
   * This API method is deprecated and may be removed in future versions.
   * 
   * @param {string} jobId - The `jobId` parameter to include in the API request.
   * @param {number} limit - The `limit` parameter to include in the API request.
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<ApiSchedulerJobActivityGetSchedulerResponse>>} A promise containing the HttpResponse of ApiSchedulerJobActivityGetSchedulerResponse
   */
  async getSchedulerJobActivityByJobId(jobId: string, limit?: number, gamertag?: string): Promise<HttpResponse<ApiSchedulerJobActivityGetSchedulerResponse>> {
    let endpoint = "/api/scheduler/job/{jobId}/activity";
    endpoint = endpoint.replace("{jobId}", encodeURIComponent(jobId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      limit
    });
    
    // Make the API request
    return this.requester.request<ApiSchedulerJobActivityGetSchedulerResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {string} jobId - The `jobId` parameter to include in the API request.
   * @param {string} cursor - The `cursor` parameter to include in the API request.
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<JobActivityViewCursorPagedResult>>} A promise containing the HttpResponse of JobActivityViewCursorPagedResult
   */
  async getSchedulerJobActivityPagedByJobId(jobId: string, cursor?: string, gamertag?: string): Promise<HttpResponse<JobActivityViewCursorPagedResult>> {
    let endpoint = "/api/scheduler/job/{jobId}/activity-paged";
    endpoint = endpoint.replace("{jobId}", encodeURIComponent(jobId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      cursor
    });
    
    // Make the API request
    return this.requester.request<JobActivityViewCursorPagedResult>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {string} cursor - The `cursor` parameter to include in the API request.
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<JobActivityViewCursorPagedResult>>} A promise containing the HttpResponse of JobActivityViewCursorPagedResult
   */
  async getSchedulerJobsActivityPaged(cursor?: string, gamertag?: string): Promise<HttpResponse<JobActivityViewCursorPagedResult>> {
    let endpoint = "/api/scheduler/jobs/activity-paged";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      cursor
    });
    
    // Make the API request
    return this.requester.request<JobActivityViewCursorPagedResult>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {string} jobId - The `jobId` parameter to include in the API request.
   * @param {Date} from - The `from` parameter to include in the API request.
   * @param {number} limit - The `limit` parameter to include in the API request.
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<ApiSchedulerJobNextExecutionsGetSchedulerResponse>>} A promise containing the HttpResponse of ApiSchedulerJobNextExecutionsGetSchedulerResponse
   */
  async getSchedulerJobNextExecutionsByJobId(jobId: string, from?: Date, limit?: number, gamertag?: string): Promise<HttpResponse<ApiSchedulerJobNextExecutionsGetSchedulerResponse>> {
    let endpoint = "/api/scheduler/job/{jobId}/next-executions";
    endpoint = endpoint.replace("{jobId}", encodeURIComponent(jobId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      from,
      limit
    });
    
    // Make the API request
    return this.requester.request<ApiSchedulerJobNextExecutionsGetSchedulerResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {string} jobId - The `jobId` parameter to include in the API request.
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<ApiSchedulerJobCancelPutSchedulerResponse>>} A promise containing the HttpResponse of ApiSchedulerJobCancelPutSchedulerResponse
   */
  async putSchedulerJobCancelByJobId(jobId: string, gamertag?: string): Promise<HttpResponse<ApiSchedulerJobCancelPutSchedulerResponse>> {
    let endpoint = "/api/scheduler/job/{jobId}/cancel";
    endpoint = endpoint.replace("{jobId}", encodeURIComponent(jobId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<ApiSchedulerJobCancelPutSchedulerResponse>({
      url: endpoint,
      method: HttpMethod.PUT,
      headers
    });
  }
}
