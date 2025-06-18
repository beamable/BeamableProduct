import { BeamoBasicGetManifestsResponse } from '@/__generated__/schemas/BeamoBasicGetManifestsResponse';
import { BeamoBasicManifestChecksums } from '@/__generated__/schemas/BeamoBasicManifestChecksums';
import { CommitImageRequest } from '@/__generated__/schemas/CommitImageRequest';
import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { ConnectionString } from '@/__generated__/schemas/ConnectionString';
import { DELETE } from '@/constants';
import { EmptyResponse } from '@/__generated__/schemas/EmptyResponse';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
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
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { LambdaResponse } from '@/__generated__/schemas/LambdaResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { MicroserviceRegistrationRequest } from '@/__generated__/schemas/MicroserviceRegistrationRequest';
import { MicroserviceRegistrationsQuery } from '@/__generated__/schemas/MicroserviceRegistrationsQuery';
import { MicroserviceRegistrationsResponse } from '@/__generated__/schemas/MicroserviceRegistrationsResponse';
import { MicroserviceSecretResponse } from '@/__generated__/schemas/MicroserviceSecretResponse';
import { objectIdPlaceholder } from '@/constants';
import { PerformanceResponse } from '@/__generated__/schemas/PerformanceResponse';
import { POST } from '@/constants';
import { PostManifestRequest } from '@/__generated__/schemas/PostManifestRequest';
import { PostManifestResponse } from '@/__generated__/schemas/PostManifestResponse';
import { PreSignedUrlsResponse } from '@/__generated__/schemas/PreSignedUrlsResponse';
import { PullBeamoManifestRequest } from '@/__generated__/schemas/PullBeamoManifestRequest';
import { PUT } from '@/constants';
import { Query } from '@/__generated__/schemas/Query';
import { SupportedFederationsResponse } from '@/__generated__/schemas/SupportedFederationsResponse';

export class BeamoApi {
  constructor(
    private readonly r: HttpRequester
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
    let e = "/basic/beamo/microservice/registrations";
    
    // Make the API request
    return makeApiRequest<MicroserviceRegistrationsResponse, MicroserviceRegistrationsQuery>({
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
   * @param {MicroserviceRegistrationRequest} payload - The `MicroserviceRegistrationRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putBeamoMicroserviceFederationTraffic(payload: MicroserviceRegistrationRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/basic/beamo/microservice/federation/traffic";
    
    // Make the API request
    return makeApiRequest<CommonResponse, MicroserviceRegistrationRequest>({
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
   * @param {MicroserviceRegistrationRequest} payload - The `MicroserviceRegistrationRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async deleteBeamoMicroserviceFederationTraffic(payload: MicroserviceRegistrationRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/basic/beamo/microservice/federation/traffic";
    
    // Make the API request
    return makeApiRequest<CommonResponse, MicroserviceRegistrationRequest>({
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
   * @param {GetServiceURLsRequest} payload - The `GetServiceURLsRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PreSignedUrlsResponse>>} A promise containing the HttpResponse of PreSignedUrlsResponse
   */
  async postBeamoImageUrls(payload: GetServiceURLsRequest, gamertag?: string): Promise<HttpResponse<PreSignedUrlsResponse>> {
    let e = "/basic/beamo/image/urls";
    
    // Make the API request
    return makeApiRequest<PreSignedUrlsResponse, GetServiceURLsRequest>({
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
   * @param {GetMetricsUrlRequest} payload - The `GetMetricsUrlRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetSignedUrlResponse>>} A promise containing the HttpResponse of GetSignedUrlResponse
   */
  async postBeamoMetricsUrl(payload: GetMetricsUrlRequest, gamertag?: string): Promise<HttpResponse<GetSignedUrlResponse>> {
    let e = "/basic/beamo/metricsUrl";
    
    // Make the API request
    return makeApiRequest<GetSignedUrlResponse, GetMetricsUrlRequest>({
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
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<MicroserviceSecretResponse>>} A promise containing the HttpResponse of MicroserviceSecretResponse
   */
  async getBeamoMicroserviceSecret(gamertag?: string): Promise<HttpResponse<MicroserviceSecretResponse>> {
    let e = "/basic/beamo/microservice/secret";
    
    // Make the API request
    return makeApiRequest<MicroserviceSecretResponse>({
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
   * @param {Query} payload - The `Query` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetSignedUrlResponse>>} A promise containing the HttpResponse of GetSignedUrlResponse
   */
  async postBeamoQueryLogsResult(payload: Query, gamertag?: string): Promise<HttpResponse<GetSignedUrlResponse>> {
    let e = "/basic/beamo/queryLogs/result";
    
    // Make the API request
    return makeApiRequest<GetSignedUrlResponse, Query>({
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
   * @param {string} granularity - The `granularity` parameter to include in the API request.
   * @param {string} storageObjectName - The `storageObjectName` parameter to include in the API request.
   * @param {string} endDate - The `endDate` parameter to include in the API request.
   * @param {string} period - The `period` parameter to include in the API request.
   * @param {string} startDate - The `startDate` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PerformanceResponse>>} A promise containing the HttpResponse of PerformanceResponse
   */
  async getBeamoStoragePerformance(granularity: string, storageObjectName: string, endDate?: string, period?: string, startDate?: string, gamertag?: string): Promise<HttpResponse<PerformanceResponse>> {
    let e = "/basic/beamo/storage/performance";
    
    // Make the API request
    return makeApiRequest<PerformanceResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        granularity,
        storageObjectName,
        endDate,
        period,
        startDate
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
   * @param {boolean} archived - The `archived` parameter to include in the API request.
   * @param {number} limit - The `limit` parameter to include in the API request.
   * @param {number} offset - The `offset` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<BeamoBasicGetManifestsResponse>>} A promise containing the HttpResponse of BeamoBasicGetManifestsResponse
   */
  async getBeamoManifests(archived?: boolean, limit?: number, offset?: number, gamertag?: string): Promise<HttpResponse<BeamoBasicGetManifestsResponse>> {
    let e = "/basic/beamo/manifests";
    
    // Make the API request
    return makeApiRequest<BeamoBasicGetManifestsResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        archived,
        limit,
        offset
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
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetTemplatesResponse>>} A promise containing the HttpResponse of GetTemplatesResponse
   */
  async getBeamoTemplates(gamertag?: string): Promise<HttpResponse<GetTemplatesResponse>> {
    let e = "/basic/beamo/templates";
    
    // Make the API request
    return makeApiRequest<GetTemplatesResponse>({
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
   * @param {GetLogsInsightUrlRequest} payload - The `GetLogsInsightUrlRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<Query>>} A promise containing the HttpResponse of Query
   */
  async postBeamoQueryLogs(payload: GetLogsInsightUrlRequest, gamertag?: string): Promise<HttpResponse<Query>> {
    let e = "/basic/beamo/queryLogs";
    
    // Make the API request
    return makeApiRequest<Query, GetLogsInsightUrlRequest>({
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
   * @param {Query} payload - The `Query` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async deleteBeamoQueryLogs(payload: Query, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/basic/beamo/queryLogs";
    
    // Make the API request
    return makeApiRequest<CommonResponse, Query>({
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
   * @param {GetLogsUrlRequest} payload - The `GetLogsUrlRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetSignedUrlResponse>>} A promise containing the HttpResponse of GetSignedUrlResponse
   */
  async postBeamoLogsUrl(payload: GetLogsUrlRequest, gamertag?: string): Promise<HttpResponse<GetSignedUrlResponse>> {
    let e = "/basic/beamo/logsUrl";
    
    // Make the API request
    return makeApiRequest<GetSignedUrlResponse, GetLogsUrlRequest>({
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
   * @param {CommitImageRequest} payload - The `CommitImageRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<LambdaResponse>>} A promise containing the HttpResponse of LambdaResponse
   */
  async putBeamoImageCommit(payload: CommitImageRequest, gamertag?: string): Promise<HttpResponse<LambdaResponse>> {
    let e = "/basic/beamo/image/commit";
    
    // Make the API request
    return makeApiRequest<LambdaResponse, CommitImageRequest>({
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
   * @returns {Promise<HttpResponse<GetLambdaURI>>} A promise containing the HttpResponse of GetLambdaURI
   */
  async getBeamoUploadAPI(gamertag?: string): Promise<HttpResponse<GetLambdaURI>> {
    let e = "/basic/beamo/uploadAPI";
    
    // Make the API request
    return makeApiRequest<GetLambdaURI>({
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
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetStatusResponse>>} A promise containing the HttpResponse of GetStatusResponse
   */
  async getBeamoStatus(gamertag?: string): Promise<HttpResponse<GetStatusResponse>> {
    let e = "/basic/beamo/status";
    
    // Make the API request
    return makeApiRequest<GetStatusResponse>({
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
   * @param {boolean} archived - The `archived` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetCurrentManifestResponse>>} A promise containing the HttpResponse of GetCurrentManifestResponse
   */
  async getBeamoManifestCurrent(archived?: boolean, gamertag?: string): Promise<HttpResponse<GetCurrentManifestResponse>> {
    let e = "/basic/beamo/manifest/current";
    
    // Make the API request
    return makeApiRequest<GetCurrentManifestResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        archived
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
   * @param {PullBeamoManifestRequest} payload - The `PullBeamoManifestRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<BeamoBasicManifestChecksums>>} A promise containing the HttpResponse of BeamoBasicManifestChecksums
   */
  async postBeamoManifestPull(payload: PullBeamoManifestRequest, gamertag?: string): Promise<HttpResponse<BeamoBasicManifestChecksums>> {
    let e = "/basic/beamo/manifest/pull";
    
    // Make the API request
    return makeApiRequest<BeamoBasicManifestChecksums, PullBeamoManifestRequest>({
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
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetElasticContainerRegistryURI>>} A promise containing the HttpResponse of GetElasticContainerRegistryURI
   */
  async getBeamoRegistry(gamertag?: string): Promise<HttpResponse<GetElasticContainerRegistryURI>> {
    let e = "/basic/beamo/registry";
    
    // Make the API request
    return makeApiRequest<GetElasticContainerRegistryURI>({
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
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postBeamoManifestDeploy(gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let e = "/basic/beamo/manifest/deploy";
    
    // Make the API request
    return makeApiRequest<EmptyResponse>({
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
   * @param {MicroserviceRegistrationsQuery} payload - The `MicroserviceRegistrationsQuery` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<SupportedFederationsResponse>>} A promise containing the HttpResponse of SupportedFederationsResponse
   */
  async postBeamoMicroserviceFederation(payload: MicroserviceRegistrationsQuery, gamertag?: string): Promise<HttpResponse<SupportedFederationsResponse>> {
    let e = "/basic/beamo/microservice/federation";
    
    // Make the API request
    return makeApiRequest<SupportedFederationsResponse, MicroserviceRegistrationsQuery>({
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
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ConnectionString>>} A promise containing the HttpResponse of ConnectionString
   */
  async getBeamoStorageConnection(gamertag?: string): Promise<HttpResponse<ConnectionString>> {
    let e = "/basic/beamo/storage/connection";
    
    // Make the API request
    return makeApiRequest<ConnectionString>({
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
   * @param {string} id - The `id` parameter to include in the API request.
   * @param {boolean} archived - The `archived` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetManifestResponse>>} A promise containing the HttpResponse of GetManifestResponse
   */
  async getBeamoManifest(id: string, archived?: boolean, gamertag?: string): Promise<HttpResponse<GetManifestResponse>> {
    let e = "/basic/beamo/manifest";
    
    // Make the API request
    return makeApiRequest<GetManifestResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        id,
        archived
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
   * @param {PostManifestRequest} payload - The `PostManifestRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PostManifestResponse>>} A promise containing the HttpResponse of PostManifestResponse
   */
  async postBeamoManifest(payload: PostManifestRequest, gamertag?: string): Promise<HttpResponse<PostManifestResponse>> {
    let e = "/basic/beamo/manifest";
    
    // Make the API request
    return makeApiRequest<PostManifestResponse, PostManifestRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
    });
  }
}
