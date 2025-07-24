/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { DELETE } from '@/constants';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import type { AliasAvailableResponse } from '@/__generated__/schemas/AliasAvailableResponse';
import type { ArchiveProjectRequest } from '@/__generated__/schemas/ArchiveProjectRequest';
import type { BatchDeleteInFlightRequest } from '@/__generated__/schemas/BatchDeleteInFlightRequest';
import type { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import type { CreateLaunchMessageRequest } from '@/__generated__/schemas/CreateLaunchMessageRequest';
import type { CreatePlanRequest } from '@/__generated__/schemas/CreatePlanRequest';
import type { CreateProjectRequest } from '@/__generated__/schemas/CreateProjectRequest';
import type { CustomerResponse } from '@/__generated__/schemas/CustomerResponse';
import type { CustomersResponse } from '@/__generated__/schemas/CustomersResponse';
import type { CustomerViewResponse } from '@/__generated__/schemas/CustomerViewResponse';
import type { EmptyResponse } from '@/__generated__/schemas/EmptyResponse';
import type { GetGameResponse } from '@/__generated__/schemas/GetGameResponse';
import type { HtmlResponse } from '@/__generated__/schemas/HtmlResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { InFlightFailureResponse } from '@/__generated__/schemas/InFlightFailureResponse';
import type { LaunchMessageListResponse } from '@/__generated__/schemas/LaunchMessageListResponse';
import type { NewCustomerRequest } from '@/__generated__/schemas/NewCustomerRequest';
import type { NewCustomerResponse } from '@/__generated__/schemas/NewCustomerResponse';
import type { NewGameRequest } from '@/__generated__/schemas/NewGameRequest';
import type { ProjectView } from '@/__generated__/schemas/ProjectView';
import type { PromoteRealmRequest } from '@/__generated__/schemas/PromoteRealmRequest';
import type { PromoteRealmResponse } from '@/__generated__/schemas/PromoteRealmResponse';
import type { PromoteRealmResponseOld } from '@/__generated__/schemas/PromoteRealmResponseOld';
import type { RealmConfigChangeRequest } from '@/__generated__/schemas/RealmConfigChangeRequest';
import type { RealmConfigResponse } from '@/__generated__/schemas/RealmConfigResponse';
import type { RealmConfigSaveRequest } from '@/__generated__/schemas/RealmConfigSaveRequest';
import type { RealmConfiguration } from '@/__generated__/schemas/RealmConfiguration';
import type { RemoveLaunchMessageRequest } from '@/__generated__/schemas/RemoveLaunchMessageRequest';
import type { RenameProjectRequest } from '@/__generated__/schemas/RenameProjectRequest';
import type { ServicePlansResponse } from '@/__generated__/schemas/ServicePlansResponse';
import type { UnarchiveProjectRequest } from '@/__generated__/schemas/UnarchiveProjectRequest';
import type { UpdateGameHierarchyRequest } from '@/__generated__/schemas/UpdateGameHierarchyRequest';

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param cid - The `cid` parameter to include in the API request.
 * @param token - The `token` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function realmsGetCustomerActivateBasic(requester: HttpRequester, cid: bigint | string, token: string, gamertag?: string): Promise<HttpResponse<HtmlResponse>> {
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
export async function realmsPostProjectBeamableBasic(requester: HttpRequester, payload: CreateProjectRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
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
export async function realmsGetCustomerAliasAvailableBasic(requester: HttpRequester, alias: string, gamertag?: string): Promise<HttpResponse<AliasAvailableResponse>> {
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
export async function realmsGetProjectBasic(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<ProjectView>> {
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
export async function realmsPostProjectBasic(requester: HttpRequester, payload: CreateProjectRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
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
export async function realmsPutProjectBasic(requester: HttpRequester, payload: UnarchiveProjectRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
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
export async function realmsDeleteProjectBasic(requester: HttpRequester, payload: ArchiveProjectRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
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
export async function realmsPostCustomerVerifyBasic(requester: HttpRequester, payload: NewCustomerRequest, gamertag?: string): Promise<HttpResponse<NewCustomerResponse>> {
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
export async function realmsGetGamesBasic(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<GetGameResponse>> {
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
export async function realmsGetConfigBasic(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<RealmConfigResponse>> {
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
export async function realmsPostConfigBasic(requester: HttpRequester, payload: RealmConfigChangeRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
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
export async function realmsPutConfigBasic(requester: HttpRequester, payload: RealmConfigSaveRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
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
export async function realmsPutProjectRenameBasic(requester: HttpRequester, payload: RenameProjectRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
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
export async function realmsGetPlansBasic(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<ServicePlansResponse>> {
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
export async function realmsPostPlansBasic(requester: HttpRequester, payload: CreatePlanRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
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
export async function realmsGetClientDefaultsBasic(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<RealmConfiguration>> {
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
export async function realmsGetCustomerBasic(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<CustomerViewResponse>> {
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
export async function realmsPostCustomerBasic(requester: HttpRequester, payload: NewCustomerRequest, gamertag?: string): Promise<HttpResponse<NewCustomerResponse>> {
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
export async function realmsGetAdminInflightFailuresBasic(requester: HttpRequester, serviceName: string, serviceObjectId?: string, gamertag?: string): Promise<HttpResponse<InFlightFailureResponse>> {
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
export async function realmsDeleteAdminInflightFailuresBasic(requester: HttpRequester, payload: BatchDeleteInFlightRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
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
export async function realmsGetLaunchMessageBasic(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<LaunchMessageListResponse>> {
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
export async function realmsPostLaunchMessageBasic(requester: HttpRequester, payload: CreateLaunchMessageRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
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
export async function realmsDeleteLaunchMessageBasic(requester: HttpRequester, payload: RemoveLaunchMessageRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
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
export async function realmsGetIsCustomerBasic(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
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
export async function realmsGetAdminCustomerBasic(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<CustomerResponse>> {
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
export async function realmsGetGameBasic(requester: HttpRequester, rootPID: string, gamertag?: string): Promise<HttpResponse<GetGameResponse>> {
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
export async function realmsPostGameBasic(requester: HttpRequester, payload: NewGameRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
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
export async function realmsPutGameBasic(requester: HttpRequester, payload: UpdateGameHierarchyRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
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
export async function realmsGetProjectPromoteBasic(requester: HttpRequester, sourcePid: string, contentManifestIds?: string[], promotions?: string[], gamertag?: string): Promise<HttpResponse<PromoteRealmResponseOld>> {
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
export async function realmsPostProjectPromoteBasic(requester: HttpRequester, payload: PromoteRealmRequest, gamertag?: string): Promise<HttpResponse<PromoteRealmResponseOld>> {
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
export async function realmsGetCustomersBasic(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<CustomersResponse>> {
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
export async function realmsGetPromotionBasic(requester: HttpRequester, sourcePid: string, contentManifestIds?: string[], promotions?: string[], gamertag?: string): Promise<HttpResponse<PromoteRealmResponse>> {
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
export async function realmsPostPromotionBasic(requester: HttpRequester, payload: PromoteRealmRequest, gamertag?: string): Promise<HttpResponse<PromoteRealmResponse>> {
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
