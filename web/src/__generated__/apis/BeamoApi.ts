import { BeamoBasicGetManifestsResponse } from '@/__generated__/schemas/BeamoBasicGetManifestsResponse';
import { BeamoBasicManifestChecksums } from '@/__generated__/schemas/BeamoBasicManifestChecksums';
import { CommitImageRequest } from '@/__generated__/schemas/CommitImageRequest';
import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { ConnectionString } from '@/__generated__/schemas/ConnectionString';
import { EmptyResponse } from '@/__generated__/schemas/EmptyResponse';
import { GetCurrentManifestResponse } from '@/__generated__/schemas/GetCurrentManifestResponse';
import { GetElasticContainerRegistryURI } from '@/__generated__/schemas/GetElasticContainerRegistryURI';
import { GetLambdaURI } from '@/__generated__/schemas/GetLambdaURI';
import { GetLogsInsightUrlRequest } from '@/__generated__/schemas/GetLogsInsightUrlRequest';
import { GetLogsUrlRequest } from '@/__generated__/schemas/GetLogsUrlRequest';
import { GetManifestResponse } from '@/__generated__/schemas/GetManifestResponse';
import { GetMetricsUrlRequest } from '@/__generated__/schemas/GetMetricsUrlRequest';
import { GetServiceURLsRequest } from '@/__generated__/schemas/GetServiceURLsRequest';
import { GetSignedUrlResponse } from '@/__generated__/schemas/GetSignedUrlResponse';
import { GetStatusResponse } from '@/__generated__/schemas/GetStatusResponse';
import { GetTemplatesResponse } from '@/__generated__/schemas/GetTemplatesResponse';
import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { LambdaResponse } from '@/__generated__/schemas/LambdaResponse';
import { makeQueryString } from '@/utils/makeQueryString';
import { MicroserviceRegistrationRequest } from '@/__generated__/schemas/MicroserviceRegistrationRequest';
import { MicroserviceRegistrationsQuery } from '@/__generated__/schemas/MicroserviceRegistrationsQuery';
import { MicroserviceRegistrationsResponse } from '@/__generated__/schemas/MicroserviceRegistrationsResponse';
import { MicroserviceSecretResponse } from '@/__generated__/schemas/MicroserviceSecretResponse';
import { PerformanceResponse } from '@/__generated__/schemas/PerformanceResponse';
import { PostManifestRequest } from '@/__generated__/schemas/PostManifestRequest';
import { PostManifestResponse } from '@/__generated__/schemas/PostManifestResponse';
import { PreSignedUrlsResponse } from '@/__generated__/schemas/PreSignedUrlsResponse';
import { PullBeamoManifestRequest } from '@/__generated__/schemas/PullBeamoManifestRequest';
import { Query } from '@/__generated__/schemas/Query';
import { SupportedFederationsResponse } from '@/__generated__/schemas/SupportedFederationsResponse';

export class BeamoApi {
  constructor(
    private readonly requester: HttpRequester
  ) {
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {MicroserviceRegistrationsQuery} payload - The `MicroserviceRegistrationsQuery` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<MicroserviceRegistrationsResponse>>} A promise containing the HttpResponse of MicroserviceRegistrationsResponse
   */
  async postBeamoMicroserviceRegistrations(payload: MicroserviceRegistrationsQuery, gamertag?: string): Promise<HttpResponse<MicroserviceRegistrationsResponse>> {
    let endpoint = "/basic/beamo/microservice/registrations";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<MicroserviceRegistrationsResponse, MicroserviceRegistrationsQuery>({
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
   * @param {MicroserviceRegistrationRequest} payload - The `MicroserviceRegistrationRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putBeamoMicroserviceFederationTraffic(payload: MicroserviceRegistrationRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/basic/beamo/microservice/federation/traffic";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, MicroserviceRegistrationRequest>({
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
   * @param {MicroserviceRegistrationRequest} payload - The `MicroserviceRegistrationRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async deleteBeamoMicroserviceFederationTraffic(payload: MicroserviceRegistrationRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/basic/beamo/microservice/federation/traffic";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, MicroserviceRegistrationRequest>({
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
   * @param {GetServiceURLsRequest} payload - The `GetServiceURLsRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PreSignedUrlsResponse>>} A promise containing the HttpResponse of PreSignedUrlsResponse
   */
  async postBeamoImageUrls(payload: GetServiceURLsRequest, gamertag?: string): Promise<HttpResponse<PreSignedUrlsResponse>> {
    let endpoint = "/basic/beamo/image/urls";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PreSignedUrlsResponse, GetServiceURLsRequest>({
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
   * @param {GetMetricsUrlRequest} payload - The `GetMetricsUrlRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetSignedUrlResponse>>} A promise containing the HttpResponse of GetSignedUrlResponse
   */
  async postBeamoMetricsUrl(payload: GetMetricsUrlRequest, gamertag?: string): Promise<HttpResponse<GetSignedUrlResponse>> {
    let endpoint = "/basic/beamo/metricsUrl";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<GetSignedUrlResponse, GetMetricsUrlRequest>({
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
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<MicroserviceSecretResponse>>} A promise containing the HttpResponse of MicroserviceSecretResponse
   */
  async getBeamoMicroserviceSecret(gamertag?: string): Promise<HttpResponse<MicroserviceSecretResponse>> {
    let endpoint = "/basic/beamo/microservice/secret";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<MicroserviceSecretResponse>({
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
   * @param {Query} payload - The `Query` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetSignedUrlResponse>>} A promise containing the HttpResponse of GetSignedUrlResponse
   */
  async postBeamoQueryLogsResult(payload: Query, gamertag?: string): Promise<HttpResponse<GetSignedUrlResponse>> {
    let endpoint = "/basic/beamo/queryLogs/result";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<GetSignedUrlResponse, Query>({
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
   * @param {string} granularity - The `granularity` parameter to include in the API request.
   * @param {string} storageObjectName - The `storageObjectName` parameter to include in the API request.
   * @param {string} endDate - The `endDate` parameter to include in the API request.
   * @param {string} period - The `period` parameter to include in the API request.
   * @param {string} startDate - The `startDate` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PerformanceResponse>>} A promise containing the HttpResponse of PerformanceResponse
   */
  async getBeamoStoragePerformance(granularity: string, storageObjectName: string, endDate?: string, period?: string, startDate?: string, gamertag?: string): Promise<HttpResponse<PerformanceResponse>> {
    let endpoint = "/basic/beamo/storage/performance";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      granularity,
      storageObjectName,
      endDate,
      period,
      startDate
    });
    
    // Make the API request
    return this.requester.request<PerformanceResponse>({
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
   * @param {boolean} archived - The `archived` parameter to include in the API request.
   * @param {number} limit - The `limit` parameter to include in the API request.
   * @param {number} offset - The `offset` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<BeamoBasicGetManifestsResponse>>} A promise containing the HttpResponse of BeamoBasicGetManifestsResponse
   */
  async getBeamoManifests(archived?: boolean, limit?: number, offset?: number, gamertag?: string): Promise<HttpResponse<BeamoBasicGetManifestsResponse>> {
    let endpoint = "/basic/beamo/manifests";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      archived,
      limit,
      offset
    });
    
    // Make the API request
    return this.requester.request<BeamoBasicGetManifestsResponse>({
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
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetTemplatesResponse>>} A promise containing the HttpResponse of GetTemplatesResponse
   */
  async getBeamoTemplates(gamertag?: string): Promise<HttpResponse<GetTemplatesResponse>> {
    let endpoint = "/basic/beamo/templates";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<GetTemplatesResponse>({
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
   * @param {GetLogsInsightUrlRequest} payload - The `GetLogsInsightUrlRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<Query>>} A promise containing the HttpResponse of Query
   */
  async postBeamoQueryLogs(payload: GetLogsInsightUrlRequest, gamertag?: string): Promise<HttpResponse<Query>> {
    let endpoint = "/basic/beamo/queryLogs";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<Query, GetLogsInsightUrlRequest>({
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
   * @param {Query} payload - The `Query` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async deleteBeamoQueryLogs(payload: Query, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/basic/beamo/queryLogs";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, Query>({
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
   * @param {GetLogsUrlRequest} payload - The `GetLogsUrlRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetSignedUrlResponse>>} A promise containing the HttpResponse of GetSignedUrlResponse
   */
  async postBeamoLogsUrl(payload: GetLogsUrlRequest, gamertag?: string): Promise<HttpResponse<GetSignedUrlResponse>> {
    let endpoint = "/basic/beamo/logsUrl";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<GetSignedUrlResponse, GetLogsUrlRequest>({
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
   * @param {CommitImageRequest} payload - The `CommitImageRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<LambdaResponse>>} A promise containing the HttpResponse of LambdaResponse
   */
  async putBeamoImageCommit(payload: CommitImageRequest, gamertag?: string): Promise<HttpResponse<LambdaResponse>> {
    let endpoint = "/basic/beamo/image/commit";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<LambdaResponse, CommitImageRequest>({
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
   * @returns {Promise<HttpResponse<GetLambdaURI>>} A promise containing the HttpResponse of GetLambdaURI
   */
  async getBeamoUploadAPI(gamertag?: string): Promise<HttpResponse<GetLambdaURI>> {
    let endpoint = "/basic/beamo/uploadAPI";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<GetLambdaURI>({
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
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetStatusResponse>>} A promise containing the HttpResponse of GetStatusResponse
   */
  async getBeamoStatus(gamertag?: string): Promise<HttpResponse<GetStatusResponse>> {
    let endpoint = "/basic/beamo/status";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<GetStatusResponse>({
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
   * @param {boolean} archived - The `archived` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetCurrentManifestResponse>>} A promise containing the HttpResponse of GetCurrentManifestResponse
   */
  async getBeamoManifestCurrent(archived?: boolean, gamertag?: string): Promise<HttpResponse<GetCurrentManifestResponse>> {
    let endpoint = "/basic/beamo/manifest/current";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      archived
    });
    
    // Make the API request
    return this.requester.request<GetCurrentManifestResponse>({
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
   * @param {PullBeamoManifestRequest} payload - The `PullBeamoManifestRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<BeamoBasicManifestChecksums>>} A promise containing the HttpResponse of BeamoBasicManifestChecksums
   */
  async postBeamoManifestPull(payload: PullBeamoManifestRequest, gamertag?: string): Promise<HttpResponse<BeamoBasicManifestChecksums>> {
    let endpoint = "/basic/beamo/manifest/pull";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<BeamoBasicManifestChecksums, PullBeamoManifestRequest>({
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
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetElasticContainerRegistryURI>>} A promise containing the HttpResponse of GetElasticContainerRegistryURI
   */
  async getBeamoRegistry(gamertag?: string): Promise<HttpResponse<GetElasticContainerRegistryURI>> {
    let endpoint = "/basic/beamo/registry";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<GetElasticContainerRegistryURI>({
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
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postBeamoManifestDeploy(gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let endpoint = "/basic/beamo/manifest/deploy";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyResponse>({
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
   * @param {MicroserviceRegistrationsQuery} payload - The `MicroserviceRegistrationsQuery` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<SupportedFederationsResponse>>} A promise containing the HttpResponse of SupportedFederationsResponse
   */
  async postBeamoMicroserviceFederation(payload: MicroserviceRegistrationsQuery, gamertag?: string): Promise<HttpResponse<SupportedFederationsResponse>> {
    let endpoint = "/basic/beamo/microservice/federation";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<SupportedFederationsResponse, MicroserviceRegistrationsQuery>({
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
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ConnectionString>>} A promise containing the HttpResponse of ConnectionString
   */
  async getBeamoStorageConnection(gamertag?: string): Promise<HttpResponse<ConnectionString>> {
    let endpoint = "/basic/beamo/storage/connection";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<ConnectionString>({
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
   * @param {string} id - The `id` parameter to include in the API request.
   * @param {boolean} archived - The `archived` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetManifestResponse>>} A promise containing the HttpResponse of GetManifestResponse
   */
  async getBeamoManifest(id: string, archived?: boolean, gamertag?: string): Promise<HttpResponse<GetManifestResponse>> {
    let endpoint = "/basic/beamo/manifest";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      id,
      archived
    });
    
    // Make the API request
    return this.requester.request<GetManifestResponse>({
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
   * @param {PostManifestRequest} payload - The `PostManifestRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PostManifestResponse>>} A promise containing the HttpResponse of PostManifestResponse
   */
  async postBeamoManifest(payload: PostManifestRequest, gamertag?: string): Promise<HttpResponse<PostManifestResponse>> {
    let endpoint = "/basic/beamo/manifest";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PostManifestResponse, PostManifestRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
}
