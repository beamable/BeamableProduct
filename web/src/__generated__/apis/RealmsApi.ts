import { AliasAvailableResponse } from '@/__generated__/schemas/AliasAvailableResponse';
import { ArchiveProjectRequest } from '@/__generated__/schemas/ArchiveProjectRequest';
import { BatchDeleteInFlightRequest } from '@/__generated__/schemas/BatchDeleteInFlightRequest';
import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { CreateLaunchMessageRequest } from '@/__generated__/schemas/CreateLaunchMessageRequest';
import { CreatePlanRequest } from '@/__generated__/schemas/CreatePlanRequest';
import { CreateProjectRequest } from '@/__generated__/schemas/CreateProjectRequest';
import { CustomerResponse } from '@/__generated__/schemas/CustomerResponse';
import { CustomersResponse } from '@/__generated__/schemas/CustomersResponse';
import { CustomerViewResponse } from '@/__generated__/schemas/CustomerViewResponse';
import { EmptyResponse } from '@/__generated__/schemas/EmptyResponse';
import { GetGameResponse } from '@/__generated__/schemas/GetGameResponse';
import { HtmlResponse } from '@/__generated__/schemas/HtmlResponse';
import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { InFlightFailureResponse } from '@/__generated__/schemas/InFlightFailureResponse';
import { LaunchMessageListResponse } from '@/__generated__/schemas/LaunchMessageListResponse';
import { makeQueryString } from '@/utils/makeQueryString';
import { NewCustomerRequest } from '@/__generated__/schemas/NewCustomerRequest';
import { NewCustomerResponse } from '@/__generated__/schemas/NewCustomerResponse';
import { NewGameRequest } from '@/__generated__/schemas/NewGameRequest';
import { ProjectView } from '@/__generated__/schemas/ProjectView';
import { PromoteRealmRequest } from '@/__generated__/schemas/PromoteRealmRequest';
import { PromoteRealmResponse } from '@/__generated__/schemas/PromoteRealmResponse';
import { PromoteRealmResponseOld } from '@/__generated__/schemas/PromoteRealmResponseOld';
import { RealmConfigChangeRequest } from '@/__generated__/schemas/RealmConfigChangeRequest';
import { RealmConfigResponse } from '@/__generated__/schemas/RealmConfigResponse';
import { RealmConfigSaveRequest } from '@/__generated__/schemas/RealmConfigSaveRequest';
import { RealmConfiguration } from '@/__generated__/schemas/RealmConfiguration';
import { RemoveLaunchMessageRequest } from '@/__generated__/schemas/RemoveLaunchMessageRequest';
import { RenameProjectRequest } from '@/__generated__/schemas/RenameProjectRequest';
import { ServicePlansResponse } from '@/__generated__/schemas/ServicePlansResponse';
import { UnarchiveProjectRequest } from '@/__generated__/schemas/UnarchiveProjectRequest';
import { UpdateGameHierarchyRequest } from '@/__generated__/schemas/UpdateGameHierarchyRequest';

export class RealmsApi {
  constructor(
    private readonly requester: HttpRequester
  ) {
  }
  
  /**
   * @param {bigint | string} cid - The `cid` parameter to include in the API request.
   * @param {string} token - The `token` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<HtmlResponse>>} A promise containing the HttpResponse of HtmlResponse
   */
  async getRealmsCustomerActivate(cid: bigint | string, token: string, gamertag?: string): Promise<HttpResponse<HtmlResponse>> {
    let endpoint = "/basic/realms/customer/activate";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      cid,
      token
    });
    
    // Make the API request
    return this.requester.request<HtmlResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {CreateProjectRequest} payload - The `CreateProjectRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postRealmProjectBeamable(payload: CreateProjectRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/basic/realms/project/beamable";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, CreateProjectRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {string} alias - The `alias` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AliasAvailableResponse>>} A promise containing the HttpResponse of AliasAvailableResponse
   */
  async getRealmsCustomerAliasAvailable(alias: string, gamertag?: string): Promise<HttpResponse<AliasAvailableResponse>> {
    let endpoint = "/basic/realms/customer/alias/available";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      alias
    });
    
    // Make the API request
    return this.requester.request<AliasAvailableResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ProjectView>>} A promise containing the HttpResponse of ProjectView
   */
  async getRealmsProject(gamertag?: string): Promise<HttpResponse<ProjectView>> {
    let endpoint = "/basic/realms/project";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<ProjectView>({
      url: endpoint,
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {CreateProjectRequest} payload - The `CreateProjectRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postRealmProject(payload: CreateProjectRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/basic/realms/project";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, CreateProjectRequest>({
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
   * @param {UnarchiveProjectRequest} payload - The `UnarchiveProjectRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putRealmProject(payload: UnarchiveProjectRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/basic/realms/project";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, UnarchiveProjectRequest>({
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
   * @param {ArchiveProjectRequest} payload - The `ArchiveProjectRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async deleteRealmProject(payload: ArchiveProjectRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/basic/realms/project";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, ArchiveProjectRequest>({
      url: endpoint,
      method: HttpMethod.DELETE,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {NewCustomerRequest} payload - The `NewCustomerRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<NewCustomerResponse>>} A promise containing the HttpResponse of NewCustomerResponse
   */
  async postRealmCustomerVerify(payload: NewCustomerRequest, gamertag?: string): Promise<HttpResponse<NewCustomerResponse>> {
    let endpoint = "/basic/realms/customer/verify";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<NewCustomerResponse, NewCustomerRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetGameResponse>>} A promise containing the HttpResponse of GetGameResponse
   */
  async getRealmsGames(gamertag?: string): Promise<HttpResponse<GetGameResponse>> {
    let endpoint = "/basic/realms/games";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<GetGameResponse>({
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
   * @returns {Promise<HttpResponse<RealmConfigResponse>>} A promise containing the HttpResponse of RealmConfigResponse
   */
  async getRealmsConfig(gamertag?: string): Promise<HttpResponse<RealmConfigResponse>> {
    let endpoint = "/basic/realms/config";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<RealmConfigResponse>({
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
   * @param {RealmConfigChangeRequest} payload - The `RealmConfigChangeRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postRealmConfig(payload: RealmConfigChangeRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/basic/realms/config";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, RealmConfigChangeRequest>({
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
   * @param {RealmConfigSaveRequest} payload - The `RealmConfigSaveRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putRealmConfig(payload: RealmConfigSaveRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/basic/realms/config";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, RealmConfigSaveRequest>({
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
   * @param {RenameProjectRequest} payload - The `RenameProjectRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putRealmProjectRename(payload: RenameProjectRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/basic/realms/project/rename";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, RenameProjectRequest>({
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
   * @returns {Promise<HttpResponse<ServicePlansResponse>>} A promise containing the HttpResponse of ServicePlansResponse
   */
  async getRealmsPlans(gamertag?: string): Promise<HttpResponse<ServicePlansResponse>> {
    let endpoint = "/basic/realms/plans";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<ServicePlansResponse>({
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
   * @param {CreatePlanRequest} payload - The `CreatePlanRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postRealmPlans(payload: CreatePlanRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/basic/realms/plans";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, CreatePlanRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<RealmConfiguration>>} A promise containing the HttpResponse of RealmConfiguration
   */
  async getRealmsClientDefaults(gamertag?: string): Promise<HttpResponse<RealmConfiguration>> {
    let endpoint = "/basic/realms/client/defaults";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<RealmConfiguration>({
      url: endpoint,
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CustomerViewResponse>>} A promise containing the HttpResponse of CustomerViewResponse
   */
  async getRealmsCustomer(gamertag?: string): Promise<HttpResponse<CustomerViewResponse>> {
    let endpoint = "/basic/realms/customer";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CustomerViewResponse>({
      url: endpoint,
      method: HttpMethod.GET,
      headers,
      withAuth: true
    });
  }
  
  /**
   * @param {NewCustomerRequest} payload - The `NewCustomerRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<NewCustomerResponse>>} A promise containing the HttpResponse of NewCustomerResponse
   */
  async postRealmCustomer(payload: NewCustomerRequest, gamertag?: string): Promise<HttpResponse<NewCustomerResponse>> {
    let endpoint = "/basic/realms/customer";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<NewCustomerResponse, NewCustomerRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {string} serviceName - The `serviceName` parameter to include in the API request.
   * @param {string} serviceObjectId - The `serviceObjectId` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<InFlightFailureResponse>>} A promise containing the HttpResponse of InFlightFailureResponse
   */
  async getRealmsAdminInflightFailures(serviceName: string, serviceObjectId?: string, gamertag?: string): Promise<HttpResponse<InFlightFailureResponse>> {
    let endpoint = "/basic/realms/admin/inflight/failures";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      serviceName,
      serviceObjectId
    });
    
    // Make the API request
    return this.requester.request<InFlightFailureResponse>({
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
   * @param {BatchDeleteInFlightRequest} payload - The `BatchDeleteInFlightRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async deleteRealmAdminInflightFailures(payload: BatchDeleteInFlightRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/basic/realms/admin/inflight/failures";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, BatchDeleteInFlightRequest>({
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
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<LaunchMessageListResponse>>} A promise containing the HttpResponse of LaunchMessageListResponse
   */
  async getRealmsLaunchMessage(gamertag?: string): Promise<HttpResponse<LaunchMessageListResponse>> {
    let endpoint = "/basic/realms/launch-message";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<LaunchMessageListResponse>({
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
   * @param {CreateLaunchMessageRequest} payload - The `CreateLaunchMessageRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postRealmLaunchMessage(payload: CreateLaunchMessageRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/basic/realms/launch-message";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, CreateLaunchMessageRequest>({
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
   * @param {RemoveLaunchMessageRequest} payload - The `RemoveLaunchMessageRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async deleteRealmLaunchMessage(payload: RemoveLaunchMessageRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/basic/realms/launch-message";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, RemoveLaunchMessageRequest>({
      url: endpoint,
      method: HttpMethod.DELETE,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async getRealmsIsCustomer(gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let endpoint = "/basic/realms/is-customer";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyResponse>({
      url: endpoint,
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CustomerResponse>>} A promise containing the HttpResponse of CustomerResponse
   */
  async getRealmsAdminCustomer(gamertag?: string): Promise<HttpResponse<CustomerResponse>> {
    let endpoint = "/basic/realms/admin/customer";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CustomerResponse>({
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
   * @param {string} rootPID - The `rootPID` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetGameResponse>>} A promise containing the HttpResponse of GetGameResponse
   */
  async getRealmsGame(rootPID: string, gamertag?: string): Promise<HttpResponse<GetGameResponse>> {
    let endpoint = "/basic/realms/game";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      rootPID
    });
    
    // Make the API request
    return this.requester.request<GetGameResponse>({
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
   * @param {NewGameRequest} payload - The `NewGameRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postRealmGame(payload: NewGameRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/basic/realms/game";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, NewGameRequest>({
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
   * @param {UpdateGameHierarchyRequest} payload - The `UpdateGameHierarchyRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putRealmGame(payload: UpdateGameHierarchyRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/basic/realms/game";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, UpdateGameHierarchyRequest>({
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
   * @param {string} sourcePid - The `sourcePid` parameter to include in the API request.
   * @param {string[]} contentManifestIds - The `contentManifestIds` parameter to include in the API request.
   * @param {string[]} promotions - The `promotions` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PromoteRealmResponseOld>>} A promise containing the HttpResponse of PromoteRealmResponseOld
   */
  async getRealmsProjectPromote(sourcePid: string, contentManifestIds?: string[], promotions?: string[], gamertag?: string): Promise<HttpResponse<PromoteRealmResponseOld>> {
    let endpoint = "/basic/realms/project/promote";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      sourcePid,
      contentManifestIds,
      promotions
    });
    
    // Make the API request
    return this.requester.request<PromoteRealmResponseOld>({
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
   * @param {PromoteRealmRequest} payload - The `PromoteRealmRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PromoteRealmResponseOld>>} A promise containing the HttpResponse of PromoteRealmResponseOld
   */
  async postRealmProjectPromote(payload: PromoteRealmRequest, gamertag?: string): Promise<HttpResponse<PromoteRealmResponseOld>> {
    let endpoint = "/basic/realms/project/promote";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PromoteRealmResponseOld, PromoteRealmRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CustomersResponse>>} A promise containing the HttpResponse of CustomersResponse
   */
  async getRealmsCustomers(gamertag?: string): Promise<HttpResponse<CustomersResponse>> {
    let endpoint = "/basic/realms/customers";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CustomersResponse>({
      url: endpoint,
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {string} sourcePid - The `sourcePid` parameter to include in the API request.
   * @param {string[]} contentManifestIds - The `contentManifestIds` parameter to include in the API request.
   * @param {string[]} promotions - The `promotions` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PromoteRealmResponse>>} A promise containing the HttpResponse of PromoteRealmResponse
   */
  async getRealmsPromotion(sourcePid: string, contentManifestIds?: string[], promotions?: string[], gamertag?: string): Promise<HttpResponse<PromoteRealmResponse>> {
    let endpoint = "/basic/realms/promotion";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      sourcePid,
      contentManifestIds,
      promotions
    });
    
    // Make the API request
    return this.requester.request<PromoteRealmResponse>({
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
   * @param {PromoteRealmRequest} payload - The `PromoteRealmRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PromoteRealmResponse>>} A promise containing the HttpResponse of PromoteRealmResponse
   */
  async postRealmPromotion(payload: PromoteRealmRequest, gamertag?: string): Promise<HttpResponse<PromoteRealmResponse>> {
    let endpoint = "/basic/realms/promotion";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<PromoteRealmResponse, PromoteRealmRequest>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
}
