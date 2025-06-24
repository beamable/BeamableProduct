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
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/constants';
import { POST } from '@/constants';
import { PUT } from '@/constants';

export class SchedulerApi {
  constructor(
    private readonly r: HttpRequester
  ) {
  }
  
  /**
   * @param {JobExecutionEvent} payload - The `JobExecutionEvent` instance to use for the API request
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<JobExecutionResult>>} A promise containing the HttpResponse of JobExecutionResult
   */
  async postSchedulerJobExecuteInternal(payload: JobExecutionEvent, gamertag?: string): Promise<HttpResponse<JobExecutionResult>> {
    let e = "/api/internal/scheduler/job/execute";
    
    // Make the API request
    return makeApiRequest<JobExecutionResult, JobExecutionEvent>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @param {JobDefinitionSaveRequest} payload - The `JobDefinitionSaveRequest` instance to use for the API request
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<JobDefinitionView>>} A promise containing the HttpResponse of JobDefinitionView
   */
  async postSchedulerJob(payload: JobDefinitionSaveRequest, gamertag?: string): Promise<HttpResponse<JobDefinitionView>> {
    let e = "/api/scheduler/job";
    
    // Make the API request
    return makeApiRequest<JobDefinitionView, JobDefinitionSaveRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
    });
  }
  
  /**
   * @param {JobDefinitionSaveRequest} payload - The `JobDefinitionSaveRequest` instance to use for the API request
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<JobDefinitionView>>} A promise containing the HttpResponse of JobDefinitionView
   */
  async postSchedulerJobInternal(payload: JobDefinitionSaveRequest, gamertag?: string): Promise<HttpResponse<JobDefinitionView>> {
    let e = "/api/internal/scheduler/job";
    
    // Make the API request
    return makeApiRequest<JobDefinitionView, JobDefinitionSaveRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
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
    let e = "/api/scheduler/jobs";
    
    // Make the API request
    return makeApiRequest<ApiSchedulerJobsGetSchedulerResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        limit,
        name,
        source
      },
      g: gamertag
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
    let e = "/api/scheduler/jobs-paged";
    
    // Make the API request
    return makeApiRequest<JobDefinitionViewCursorPagedResult>({
      r: this.r,
      e,
      m: GET,
      q: {
        cursor,
        name,
        onlyUnique,
        source
      },
      g: gamertag
    });
  }
  
  /**
   * @param {string} cursor - The `cursor` parameter to include in the API request.
   * @param {Date} from - The `from` parameter to include in the API request.
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<JobDefinitionViewCursorPagedResult>>} A promise containing the HttpResponse of JobDefinitionViewCursorPagedResult
   */
  async getSchedulerJobsSuspended(cursor?: string, from?: Date, gamertag?: string): Promise<HttpResponse<JobDefinitionViewCursorPagedResult>> {
    let e = "/api/scheduler/jobs/suspended";
    
    // Make the API request
    return makeApiRequest<JobDefinitionViewCursorPagedResult>({
      r: this.r,
      e,
      m: GET,
      q: {
        cursor,
        from
      },
      g: gamertag
    });
  }
  
  /**
   * @param {string} jobId - The `jobId` parameter to include in the API request.
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<JobDefinitionView>>} A promise containing the HttpResponse of JobDefinitionView
   */
  async getSchedulerJobByJobId(jobId: string, gamertag?: string): Promise<HttpResponse<JobDefinitionView>> {
    let e = "/api/scheduler/job/{jobId}".replace(objectIdPlaceholder, endpointEncoder(jobId));
    
    // Make the API request
    return makeApiRequest<JobDefinitionView>({
      r: this.r,
      e,
      m: GET,
      g: gamertag
    });
  }
  
  /**
   * @param {string} jobId - The `jobId` parameter to include in the API request.
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<ApiSchedulerJobDeleteSchedulerResponse>>} A promise containing the HttpResponse of ApiSchedulerJobDeleteSchedulerResponse
   */
  async deleteSchedulerJobByJobId(jobId: string, gamertag?: string): Promise<HttpResponse<ApiSchedulerJobDeleteSchedulerResponse>> {
    let e = "/api/scheduler/job/{jobId}".replace(objectIdPlaceholder, endpointEncoder(jobId));
    
    // Make the API request
    return makeApiRequest<ApiSchedulerJobDeleteSchedulerResponse>({
      r: this.r,
      e,
      m: DELETE,
      g: gamertag
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
    let e = "/api/scheduler/job/{jobId}/activity".replace(objectIdPlaceholder, endpointEncoder(jobId));
    
    // Make the API request
    return makeApiRequest<ApiSchedulerJobActivityGetSchedulerResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        limit
      },
      g: gamertag
    });
  }
  
  /**
   * @param {string} jobId - The `jobId` parameter to include in the API request.
   * @param {string} cursor - The `cursor` parameter to include in the API request.
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<JobActivityViewCursorPagedResult>>} A promise containing the HttpResponse of JobActivityViewCursorPagedResult
   */
  async getSchedulerJobActivityPagedByJobId(jobId: string, cursor?: string, gamertag?: string): Promise<HttpResponse<JobActivityViewCursorPagedResult>> {
    let e = "/api/scheduler/job/{jobId}/activity-paged".replace(objectIdPlaceholder, endpointEncoder(jobId));
    
    // Make the API request
    return makeApiRequest<JobActivityViewCursorPagedResult>({
      r: this.r,
      e,
      m: GET,
      q: {
        cursor
      },
      g: gamertag
    });
  }
  
  /**
   * @param {string} cursor - The `cursor` parameter to include in the API request.
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<JobActivityViewCursorPagedResult>>} A promise containing the HttpResponse of JobActivityViewCursorPagedResult
   */
  async getSchedulerJobsActivityPaged(cursor?: string, gamertag?: string): Promise<HttpResponse<JobActivityViewCursorPagedResult>> {
    let e = "/api/scheduler/jobs/activity-paged";
    
    // Make the API request
    return makeApiRequest<JobActivityViewCursorPagedResult>({
      r: this.r,
      e,
      m: GET,
      q: {
        cursor
      },
      g: gamertag
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
    let e = "/api/scheduler/job/{jobId}/next-executions".replace(objectIdPlaceholder, endpointEncoder(jobId));
    
    // Make the API request
    return makeApiRequest<ApiSchedulerJobNextExecutionsGetSchedulerResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        from,
        limit
      },
      g: gamertag
    });
  }
  
  /**
   * @param {string} jobId - The `jobId` parameter to include in the API request.
   * @param {string} gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
   * @returns {Promise<HttpResponse<ApiSchedulerJobCancelPutSchedulerResponse>>} A promise containing the HttpResponse of ApiSchedulerJobCancelPutSchedulerResponse
   */
  async putSchedulerJobCancelByJobId(jobId: string, gamertag?: string): Promise<HttpResponse<ApiSchedulerJobCancelPutSchedulerResponse>> {
    let e = "/api/scheduler/job/{jobId}/cancel".replace(objectIdPlaceholder, endpointEncoder(jobId));
    
    // Make the API request
    return makeApiRequest<ApiSchedulerJobCancelPutSchedulerResponse>({
      r: this.r,
      e,
      m: PUT,
      g: gamertag
    });
  }
}
