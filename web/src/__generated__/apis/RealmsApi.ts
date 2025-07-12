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
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpResponse } from '@/network/http/types/HttpResponse';
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

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param cid - The `cid` parameter to include in the API request.
 * @param token - The `token` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getRealmsCustomerActivate(requester: HttpRequester, cid: bigint | string, token: string, gamertag?: string): Promise<HttpResponse<HtmlResponse>> {
  let endpoint = "/basic/realms/customer/activate";
  
  // Make the API request
  return makeApiRequest<HtmlResponse>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `CreateProjectRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postRealmProjectBeamable(requester: HttpRequester, payload: CreateProjectRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/basic/realms/project/beamable";
  
  // Make the API request
  return makeApiRequest<CommonResponse, CreateProjectRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param alias - The `alias` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getRealmsCustomerAliasAvailable(requester: HttpRequester, alias: string, gamertag?: string): Promise<HttpResponse<AliasAvailableResponse>> {
  let endpoint = "/basic/realms/customer/alias/available";
  
  // Make the API request
  return makeApiRequest<AliasAvailableResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      alias
    },
    g: gamertag
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getRealmsProject(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<ProjectView>> {
  let endpoint = "/basic/realms/project";
  
  // Make the API request
  return makeApiRequest<ProjectView>({
    r: requester,
    e: endpoint,
    m: GET,
    g: gamertag
  });
}

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `CreateProjectRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postRealmProject(requester: HttpRequester, payload: CreateProjectRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/basic/realms/project";
  
  // Make the API request
  return makeApiRequest<CommonResponse, CreateProjectRequest>({
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
 * @param payload - The `UnarchiveProjectRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function putRealmProject(requester: HttpRequester, payload: UnarchiveProjectRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/basic/realms/project";
  
  // Make the API request
  return makeApiRequest<CommonResponse, UnarchiveProjectRequest>({
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
 * @param payload - The `ArchiveProjectRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function deleteRealmProject(requester: HttpRequester, payload: ArchiveProjectRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/basic/realms/project";
  
  // Make the API request
  return makeApiRequest<CommonResponse, ArchiveProjectRequest>({
    r: requester,
    e: endpoint,
    m: DELETE,
    p: payload,
    g: gamertag,
    w: true
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `NewCustomerRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postRealmCustomerVerify(requester: HttpRequester, payload: NewCustomerRequest, gamertag?: string): Promise<HttpResponse<NewCustomerResponse>> {
  let endpoint = "/basic/realms/customer/verify";
  
  // Make the API request
  return makeApiRequest<NewCustomerResponse, NewCustomerRequest>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getRealmsGames(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<GetGameResponse>> {
  let endpoint = "/basic/realms/games";
  
  // Make the API request
  return makeApiRequest<GetGameResponse>({
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
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getRealmsConfig(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<RealmConfigResponse>> {
  let endpoint = "/basic/realms/config";
  
  // Make the API request
  return makeApiRequest<RealmConfigResponse>({
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
 * @param payload - The `RealmConfigChangeRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postRealmConfig(requester: HttpRequester, payload: RealmConfigChangeRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/basic/realms/config";
  
  // Make the API request
  return makeApiRequest<CommonResponse, RealmConfigChangeRequest>({
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
 * @param payload - The `RealmConfigSaveRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function putRealmConfig(requester: HttpRequester, payload: RealmConfigSaveRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/basic/realms/config";
  
  // Make the API request
  return makeApiRequest<CommonResponse, RealmConfigSaveRequest>({
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
 * @param payload - The `RenameProjectRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function putRealmProjectRename(requester: HttpRequester, payload: RenameProjectRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/basic/realms/project/rename";
  
  // Make the API request
  return makeApiRequest<CommonResponse, RenameProjectRequest>({
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
export async function getRealmsPlans(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<ServicePlansResponse>> {
  let endpoint = "/basic/realms/plans";
  
  // Make the API request
  return makeApiRequest<ServicePlansResponse>({
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
 * @param payload - The `CreatePlanRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postRealmPlans(requester: HttpRequester, payload: CreatePlanRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/basic/realms/plans";
  
  // Make the API request
  return makeApiRequest<CommonResponse, CreatePlanRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getRealmsClientDefaults(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<RealmConfiguration>> {
  let endpoint = "/basic/realms/client/defaults";
  
  // Make the API request
  return makeApiRequest<RealmConfiguration>({
    r: requester,
    e: endpoint,
    m: GET,
    g: gamertag
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
export async function getRealmsCustomer(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<CustomerViewResponse>> {
  let endpoint = "/basic/realms/customer";
  
  // Make the API request
  return makeApiRequest<CustomerViewResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    g: gamertag,
    w: true
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `NewCustomerRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postRealmCustomer(requester: HttpRequester, payload: NewCustomerRequest, gamertag?: string): Promise<HttpResponse<NewCustomerResponse>> {
  let endpoint = "/basic/realms/customer";
  
  // Make the API request
  return makeApiRequest<NewCustomerResponse, NewCustomerRequest>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param serviceName - The `serviceName` parameter to include in the API request.
 * @param serviceObjectId - The `serviceObjectId` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getRealmsAdminInflightFailures(requester: HttpRequester, serviceName: string, serviceObjectId?: string, gamertag?: string): Promise<HttpResponse<InFlightFailureResponse>> {
  let endpoint = "/basic/realms/admin/inflight/failures";
  
  // Make the API request
  return makeApiRequest<InFlightFailureResponse>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `BatchDeleteInFlightRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function deleteRealmAdminInflightFailures(requester: HttpRequester, payload: BatchDeleteInFlightRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/basic/realms/admin/inflight/failures";
  
  // Make the API request
  return makeApiRequest<CommonResponse, BatchDeleteInFlightRequest>({
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
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getRealmsLaunchMessage(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<LaunchMessageListResponse>> {
  let endpoint = "/basic/realms/launch-message";
  
  // Make the API request
  return makeApiRequest<LaunchMessageListResponse>({
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
 * @param payload - The `CreateLaunchMessageRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postRealmLaunchMessage(requester: HttpRequester, payload: CreateLaunchMessageRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/basic/realms/launch-message";
  
  // Make the API request
  return makeApiRequest<CommonResponse, CreateLaunchMessageRequest>({
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
 * @param payload - The `RemoveLaunchMessageRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function deleteRealmLaunchMessage(requester: HttpRequester, payload: RemoveLaunchMessageRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/basic/realms/launch-message";
  
  // Make the API request
  return makeApiRequest<CommonResponse, RemoveLaunchMessageRequest>({
    r: requester,
    e: endpoint,
    m: DELETE,
    p: payload,
    g: gamertag,
    w: true
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getRealmsIsCustomer(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/basic/realms/is-customer";
  
  // Make the API request
  return makeApiRequest<EmptyResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    g: gamertag
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
export async function getRealmsAdminCustomer(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<CustomerResponse>> {
  let endpoint = "/basic/realms/admin/customer";
  
  // Make the API request
  return makeApiRequest<CustomerResponse>({
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
 * @param rootPID - The `rootPID` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getRealmsGame(requester: HttpRequester, rootPID: string, gamertag?: string): Promise<HttpResponse<GetGameResponse>> {
  let endpoint = "/basic/realms/game";
  
  // Make the API request
  return makeApiRequest<GetGameResponse>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `NewGameRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postRealmGame(requester: HttpRequester, payload: NewGameRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/basic/realms/game";
  
  // Make the API request
  return makeApiRequest<CommonResponse, NewGameRequest>({
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
 * @param payload - The `UpdateGameHierarchyRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function putRealmGame(requester: HttpRequester, payload: UpdateGameHierarchyRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/basic/realms/game";
  
  // Make the API request
  return makeApiRequest<CommonResponse, UpdateGameHierarchyRequest>({
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
 * @param sourcePid - The `sourcePid` parameter to include in the API request.
 * @param contentManifestIds - The `contentManifestIds` parameter to include in the API request.
 * @param promotions - The `promotions` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getRealmsProjectPromote(requester: HttpRequester, sourcePid: string, contentManifestIds?: string[], promotions?: string[], gamertag?: string): Promise<HttpResponse<PromoteRealmResponseOld>> {
  let endpoint = "/basic/realms/project/promote";
  
  // Make the API request
  return makeApiRequest<PromoteRealmResponseOld>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `PromoteRealmRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postRealmProjectPromote(requester: HttpRequester, payload: PromoteRealmRequest, gamertag?: string): Promise<HttpResponse<PromoteRealmResponseOld>> {
  let endpoint = "/basic/realms/project/promote";
  
  // Make the API request
  return makeApiRequest<PromoteRealmResponseOld, PromoteRealmRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getRealmsCustomers(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<CustomersResponse>> {
  let endpoint = "/basic/realms/customers";
  
  // Make the API request
  return makeApiRequest<CustomersResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    g: gamertag
  });
}

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param sourcePid - The `sourcePid` parameter to include in the API request.
 * @param contentManifestIds - The `contentManifestIds` parameter to include in the API request.
 * @param promotions - The `promotions` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getRealmsPromotion(requester: HttpRequester, sourcePid: string, contentManifestIds?: string[], promotions?: string[], gamertag?: string): Promise<HttpResponse<PromoteRealmResponse>> {
  let endpoint = "/basic/realms/promotion";
  
  // Make the API request
  return makeApiRequest<PromoteRealmResponse>({
    r: requester,
    e: endpoint,
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
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `PromoteRealmRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postRealmPromotion(requester: HttpRequester, payload: PromoteRealmRequest, gamertag?: string): Promise<HttpResponse<PromoteRealmResponse>> {
  let endpoint = "/basic/realms/promotion";
  
  // Make the API request
  return makeApiRequest<PromoteRealmResponse, PromoteRealmRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}
