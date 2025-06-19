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
import { DELETE } from '@/constants';
import { EmptyResponse } from '@/__generated__/schemas/EmptyResponse';
import { GET } from '@/constants';
import { GetGameResponse } from '@/__generated__/schemas/GetGameResponse';
import { HtmlResponse } from '@/__generated__/schemas/HtmlResponse';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { InFlightFailureResponse } from '@/__generated__/schemas/InFlightFailureResponse';
import { LaunchMessageListResponse } from '@/__generated__/schemas/LaunchMessageListResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { NewCustomerRequest } from '@/__generated__/schemas/NewCustomerRequest';
import { NewCustomerResponse } from '@/__generated__/schemas/NewCustomerResponse';
import { NewGameRequest } from '@/__generated__/schemas/NewGameRequest';
import { POST } from '@/constants';
import { ProjectView } from '@/__generated__/schemas/ProjectView';
import { PromoteRealmRequest } from '@/__generated__/schemas/PromoteRealmRequest';
import { PromoteRealmResponse } from '@/__generated__/schemas/PromoteRealmResponse';
import { PromoteRealmResponseOld } from '@/__generated__/schemas/PromoteRealmResponseOld';
import { PUT } from '@/constants';
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
    private readonly r: HttpRequester
  ) {
  }
  
  /**
   * @param {bigint | string} cid - The `cid` parameter to include in the API request.
   * @param {string} token - The `token` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<HtmlResponse>>} A promise containing the HttpResponse of HtmlResponse
   */
  async getRealmsCustomerActivate(cid: bigint | string, token: string, gamertag?: string): Promise<HttpResponse<HtmlResponse>> {
    let e = "/basic/realms/customer/activate";
    
    // Make the API request
    return makeApiRequest<HtmlResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        cid,
        token
      },
      g: gamertag
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
    let e = "/basic/realms/project/beamable";
    
    // Make the API request
    return makeApiRequest<CommonResponse, CreateProjectRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {string} alias - The `alias` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AliasAvailableResponse>>} A promise containing the HttpResponse of AliasAvailableResponse
   */
  async getRealmsCustomerAliasAvailable(alias: string, gamertag?: string): Promise<HttpResponse<AliasAvailableResponse>> {
    let e = "/basic/realms/customer/alias/available";
    
    // Make the API request
    return makeApiRequest<AliasAvailableResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        alias
      },
      g: gamertag
    });
  }
  
  /**
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ProjectView>>} A promise containing the HttpResponse of ProjectView
   */
  async getRealmsProject(gamertag?: string): Promise<HttpResponse<ProjectView>> {
    let e = "/basic/realms/project";
    
    // Make the API request
    return makeApiRequest<ProjectView>({
      r: this.r,
      e,
      m: GET,
      g: gamertag
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
    let e = "/basic/realms/project";
    
    // Make the API request
    return makeApiRequest<CommonResponse, CreateProjectRequest>({
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
   * @param {UnarchiveProjectRequest} payload - The `UnarchiveProjectRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putRealmProject(payload: UnarchiveProjectRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/basic/realms/project";
    
    // Make the API request
    return makeApiRequest<CommonResponse, UnarchiveProjectRequest>({
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
   * @param {ArchiveProjectRequest} payload - The `ArchiveProjectRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async deleteRealmProject(payload: ArchiveProjectRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/basic/realms/project";
    
    // Make the API request
    return makeApiRequest<CommonResponse, ArchiveProjectRequest>({
      r: this.r,
      e,
      m: DELETE,
      p: payload,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {NewCustomerRequest} payload - The `NewCustomerRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<NewCustomerResponse>>} A promise containing the HttpResponse of NewCustomerResponse
   */
  async postRealmCustomerVerify(payload: NewCustomerRequest, gamertag?: string): Promise<HttpResponse<NewCustomerResponse>> {
    let e = "/basic/realms/customer/verify";
    
    // Make the API request
    return makeApiRequest<NewCustomerResponse, NewCustomerRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
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
    let e = "/basic/realms/games";
    
    // Make the API request
    return makeApiRequest<GetGameResponse>({
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
   * @returns {Promise<HttpResponse<RealmConfigResponse>>} A promise containing the HttpResponse of RealmConfigResponse
   */
  async getRealmsConfig(gamertag?: string): Promise<HttpResponse<RealmConfigResponse>> {
    let e = "/basic/realms/config";
    
    // Make the API request
    return makeApiRequest<RealmConfigResponse>({
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
   * @param {RealmConfigChangeRequest} payload - The `RealmConfigChangeRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postRealmConfig(payload: RealmConfigChangeRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/basic/realms/config";
    
    // Make the API request
    return makeApiRequest<CommonResponse, RealmConfigChangeRequest>({
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
   * @param {RealmConfigSaveRequest} payload - The `RealmConfigSaveRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putRealmConfig(payload: RealmConfigSaveRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/basic/realms/config";
    
    // Make the API request
    return makeApiRequest<CommonResponse, RealmConfigSaveRequest>({
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
   * @param {RenameProjectRequest} payload - The `RenameProjectRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putRealmProjectRename(payload: RenameProjectRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/basic/realms/project/rename";
    
    // Make the API request
    return makeApiRequest<CommonResponse, RenameProjectRequest>({
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
   * @returns {Promise<HttpResponse<ServicePlansResponse>>} A promise containing the HttpResponse of ServicePlansResponse
   */
  async getRealmsPlans(gamertag?: string): Promise<HttpResponse<ServicePlansResponse>> {
    let e = "/basic/realms/plans";
    
    // Make the API request
    return makeApiRequest<ServicePlansResponse>({
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
   * @param {CreatePlanRequest} payload - The `CreatePlanRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postRealmPlans(payload: CreatePlanRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/basic/realms/plans";
    
    // Make the API request
    return makeApiRequest<CommonResponse, CreatePlanRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<RealmConfiguration>>} A promise containing the HttpResponse of RealmConfiguration
   */
  async getRealmsClientDefaults(gamertag?: string): Promise<HttpResponse<RealmConfiguration>> {
    let e = "/basic/realms/client/defaults";
    
    // Make the API request
    return makeApiRequest<RealmConfiguration>({
      r: this.r,
      e,
      m: GET,
      g: gamertag
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
    let e = "/basic/realms/customer";
    
    // Make the API request
    return makeApiRequest<CustomerViewResponse>({
      r: this.r,
      e,
      m: GET,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {NewCustomerRequest} payload - The `NewCustomerRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<NewCustomerResponse>>} A promise containing the HttpResponse of NewCustomerResponse
   */
  async postRealmCustomer(payload: NewCustomerRequest, gamertag?: string): Promise<HttpResponse<NewCustomerResponse>> {
    let e = "/basic/realms/customer";
    
    // Make the API request
    return makeApiRequest<NewCustomerResponse, NewCustomerRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag
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
    let e = "/basic/realms/admin/inflight/failures";
    
    // Make the API request
    return makeApiRequest<InFlightFailureResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        serviceName,
        serviceObjectId
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
   * @param {BatchDeleteInFlightRequest} payload - The `BatchDeleteInFlightRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async deleteRealmAdminInflightFailures(payload: BatchDeleteInFlightRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/basic/realms/admin/inflight/failures";
    
    // Make the API request
    return makeApiRequest<CommonResponse, BatchDeleteInFlightRequest>({
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
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<LaunchMessageListResponse>>} A promise containing the HttpResponse of LaunchMessageListResponse
   */
  async getRealmsLaunchMessage(gamertag?: string): Promise<HttpResponse<LaunchMessageListResponse>> {
    let e = "/basic/realms/launch-message";
    
    // Make the API request
    return makeApiRequest<LaunchMessageListResponse>({
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
   * @param {CreateLaunchMessageRequest} payload - The `CreateLaunchMessageRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postRealmLaunchMessage(payload: CreateLaunchMessageRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/basic/realms/launch-message";
    
    // Make the API request
    return makeApiRequest<CommonResponse, CreateLaunchMessageRequest>({
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
   * @param {RemoveLaunchMessageRequest} payload - The `RemoveLaunchMessageRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async deleteRealmLaunchMessage(payload: RemoveLaunchMessageRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/basic/realms/launch-message";
    
    // Make the API request
    return makeApiRequest<CommonResponse, RemoveLaunchMessageRequest>({
      r: this.r,
      e,
      m: DELETE,
      p: payload,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async getRealmsIsCustomer(gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let e = "/basic/realms/is-customer";
    
    // Make the API request
    return makeApiRequest<EmptyResponse>({
      r: this.r,
      e,
      m: GET,
      g: gamertag
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
    let e = "/basic/realms/admin/customer";
    
    // Make the API request
    return makeApiRequest<CustomerResponse>({
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
   * @param {string} rootPID - The `rootPID` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetGameResponse>>} A promise containing the HttpResponse of GetGameResponse
   */
  async getRealmsGame(rootPID: string, gamertag?: string): Promise<HttpResponse<GetGameResponse>> {
    let e = "/basic/realms/game";
    
    // Make the API request
    return makeApiRequest<GetGameResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        rootPID
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
   * @param {NewGameRequest} payload - The `NewGameRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async postRealmGame(payload: NewGameRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/basic/realms/game";
    
    // Make the API request
    return makeApiRequest<CommonResponse, NewGameRequest>({
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
   * @param {UpdateGameHierarchyRequest} payload - The `UpdateGameHierarchyRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putRealmGame(payload: UpdateGameHierarchyRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/basic/realms/game";
    
    // Make the API request
    return makeApiRequest<CommonResponse, UpdateGameHierarchyRequest>({
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
   * @param {string} sourcePid - The `sourcePid` parameter to include in the API request.
   * @param {string[]} contentManifestIds - The `contentManifestIds` parameter to include in the API request.
   * @param {string[]} promotions - The `promotions` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PromoteRealmResponseOld>>} A promise containing the HttpResponse of PromoteRealmResponseOld
   */
  async getRealmsProjectPromote(sourcePid: string, contentManifestIds?: string[], promotions?: string[], gamertag?: string): Promise<HttpResponse<PromoteRealmResponseOld>> {
    let e = "/basic/realms/project/promote";
    
    // Make the API request
    return makeApiRequest<PromoteRealmResponseOld>({
      r: this.r,
      e,
      m: GET,
      q: {
        sourcePid,
        contentManifestIds,
        promotions
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
   * @param {PromoteRealmRequest} payload - The `PromoteRealmRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PromoteRealmResponseOld>>} A promise containing the HttpResponse of PromoteRealmResponseOld
   */
  async postRealmProjectPromote(payload: PromoteRealmRequest, gamertag?: string): Promise<HttpResponse<PromoteRealmResponseOld>> {
    let e = "/basic/realms/project/promote";
    
    // Make the API request
    return makeApiRequest<PromoteRealmResponseOld, PromoteRealmRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CustomersResponse>>} A promise containing the HttpResponse of CustomersResponse
   */
  async getRealmsCustomers(gamertag?: string): Promise<HttpResponse<CustomersResponse>> {
    let e = "/basic/realms/customers";
    
    // Make the API request
    return makeApiRequest<CustomersResponse>({
      r: this.r,
      e,
      m: GET,
      g: gamertag
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
    let e = "/basic/realms/promotion";
    
    // Make the API request
    return makeApiRequest<PromoteRealmResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        sourcePid,
        contentManifestIds,
        promotions
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
   * @param {PromoteRealmRequest} payload - The `PromoteRealmRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<PromoteRealmResponse>>} A promise containing the HttpResponse of PromoteRealmResponse
   */
  async postRealmPromotion(payload: PromoteRealmRequest, gamertag?: string): Promise<HttpResponse<PromoteRealmResponse>> {
    let e = "/basic/realms/promotion";
    
    // Make the API request
    return makeApiRequest<PromoteRealmResponse, PromoteRealmRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
    });
  }
}
