import { Account } from '@/__generated__/schemas/Account';
import { AccountAvailableResponse } from '@/__generated__/schemas/AccountAvailableResponse';
import { AccountPersonallyIdentifiableInformationResponse } from '@/__generated__/schemas/AccountPersonallyIdentifiableInformationResponse';
import { AccountPlayerView } from '@/__generated__/schemas/AccountPlayerView';
import { AccountPortalView } from '@/__generated__/schemas/AccountPortalView';
import { AccountRegistration } from '@/__generated__/schemas/AccountRegistration';
import { AccountRolesReport } from '@/__generated__/schemas/AccountRolesReport';
import { AccountSearchResponse } from '@/__generated__/schemas/AccountSearchResponse';
import { AccountUpdate } from '@/__generated__/schemas/AccountUpdate';
import { AttachExternalIdentityApiRequest } from '@/__generated__/schemas/AttachExternalIdentityApiRequest';
import { AttachExternalIdentityApiResponse } from '@/__generated__/schemas/AttachExternalIdentityApiResponse';
import { AvailableRolesResponse } from '@/__generated__/schemas/AvailableRolesResponse';
import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { CreateElevatedAccountRequest } from '@/__generated__/schemas/CreateElevatedAccountRequest';
import { DELETE } from '@/constants';
import { DeleteDevicesRequest } from '@/__generated__/schemas/DeleteDevicesRequest';
import { DeleteExternalIdentityApiRequest } from '@/__generated__/schemas/DeleteExternalIdentityApiRequest';
import { DeleteRole } from '@/__generated__/schemas/DeleteRole';
import { DeleteThirdPartyAssociation } from '@/__generated__/schemas/DeleteThirdPartyAssociation';
import { EmailUpdateConfirmation } from '@/__generated__/schemas/EmailUpdateConfirmation';
import { EmailUpdateRequest } from '@/__generated__/schemas/EmailUpdateRequest';
import { EmptyResponse } from '@/__generated__/schemas/EmptyResponse';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { GetAdminsResponse } from '@/__generated__/schemas/GetAdminsResponse';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpResponse } from '@/network/http/types/HttpResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/__generated__/apis/constants';
import { PasswordUpdateConfirmation } from '@/__generated__/schemas/PasswordUpdateConfirmation';
import { PasswordUpdateRequest } from '@/__generated__/schemas/PasswordUpdateRequest';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import { ThirdPartyAvailableRequest } from '@/__generated__/schemas/ThirdPartyAvailableRequest';
import { TransferThirdPartyAssociation } from '@/__generated__/schemas/TransferThirdPartyAssociation';
import { UpdateRole } from '@/__generated__/schemas/UpdateRole';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `DeleteDevicesRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function deleteAccountMeDevice(requester: HttpRequester, payload: DeleteDevicesRequest, gamertag?: string): Promise<HttpResponse<AccountPlayerView>> {
  let endpoint = "/basic/accounts/me/device";
  
  // Make the API request
  return makeApiRequest<AccountPlayerView, DeleteDevicesRequest>({
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
export async function getAccountsMe(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<AccountPlayerView>> {
  let endpoint = "/basic/accounts/me";
  
  // Make the API request
  return makeApiRequest<AccountPlayerView>({
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
 * @param payload - The `AccountUpdate` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function putAccountMe(requester: HttpRequester, payload: AccountUpdate, gamertag?: string): Promise<HttpResponse<AccountPlayerView>> {
  let endpoint = "/basic/accounts/me";
  
  // Make the API request
  return makeApiRequest<AccountPlayerView, AccountUpdate>({
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
 * @param payload - The `ThirdPartyAvailableRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function deleteAccountMeThirdParty(requester: HttpRequester, payload: ThirdPartyAvailableRequest, gamertag?: string): Promise<HttpResponse<AccountPlayerView>> {
  let endpoint = "/basic/accounts/me/third-party";
  
  // Make the API request
  return makeApiRequest<AccountPlayerView, ThirdPartyAvailableRequest>({
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
 * @param query - The `query` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getAccountsPersonallyIdentifiableInformation(requester: HttpRequester, query: string, gamertag?: string): Promise<HttpResponse<AccountPersonallyIdentifiableInformationResponse>> {
  let endpoint = "/basic/accounts/get-personally-identifiable-information";
  
  // Make the API request
  return makeApiRequest<AccountPersonallyIdentifiableInformationResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      query
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
 * @param page - The `page` parameter to include in the API request.
 * @param pagesize - The `pagesize` parameter to include in the API request.
 * @param query - The `query` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getAccountsSearch(requester: HttpRequester, page: number, pagesize: number, query: string, gamertag?: string): Promise<HttpResponse<AccountSearchResponse>> {
  let endpoint = "/basic/accounts/search";
  
  // Make the API request
  return makeApiRequest<AccountSearchResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      page,
      pagesize,
      query
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
 * @param payload - The `EmailUpdateRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postAccountEmailUpdateInit(requester: HttpRequester, payload: EmailUpdateRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/basic/accounts/email-update/init";
  
  // Make the API request
  return makeApiRequest<EmptyResponse, EmailUpdateRequest>({
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
 * @param payload - The `EmailUpdateConfirmation` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postAccountEmailUpdateConfirm(requester: HttpRequester, payload: EmailUpdateConfirmation, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/basic/accounts/email-update/confirm";
  
  // Make the API request
  return makeApiRequest<EmptyResponse, EmailUpdateConfirmation>({
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
 * @param provider_service - The `provider_service` parameter to include in the API request.
 * @param user_id - The `user_id` parameter to include in the API request.
 * @param provider_namespace - The `provider_namespace` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getAccountsAvailableExternalIdentity(requester: HttpRequester, provider_service: string, user_id: string, provider_namespace?: string, gamertag?: string): Promise<HttpResponse<AccountAvailableResponse>> {
  let endpoint = "/basic/accounts/available/external_identity";
  
  // Make the API request
  return makeApiRequest<AccountAvailableResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      provider_service,
      user_id,
      provider_namespace
    },
    g: gamertag
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param thirdParty - The `thirdParty` parameter to include in the API request.
 * @param token - The `token` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getAccountsAvailableThirdParty(requester: HttpRequester, thirdParty: string, token: string, gamertag?: string): Promise<HttpResponse<AccountAvailableResponse>> {
  let endpoint = "/basic/accounts/available/third-party";
  
  // Make the API request
  return makeApiRequest<AccountAvailableResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      thirdParty,
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
 * @param payload - The `CreateElevatedAccountRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postAccountAdminUser(requester: HttpRequester, payload: CreateElevatedAccountRequest, gamertag?: string): Promise<HttpResponse<AccountPortalView>> {
  let endpoint = "/basic/accounts/admin/admin-user";
  
  // Make the API request
  return makeApiRequest<AccountPortalView, CreateElevatedAccountRequest>({
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
 * @param payload - The `AccountRegistration` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postAccountRegister(requester: HttpRequester, payload: AccountRegistration, gamertag?: string): Promise<HttpResponse<AccountPlayerView>> {
  let endpoint = "/basic/accounts/register";
  
  // Make the API request
  return makeApiRequest<AccountPlayerView, AccountRegistration>({
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
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getAccountsAdminMe(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<AccountPortalView>> {
  let endpoint = "/basic/accounts/admin/me";
  
  // Make the API request
  return makeApiRequest<AccountPortalView>({
    r: requester,
    e: endpoint,
    m: GET,
    g: gamertag,
    w: true
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `PasswordUpdateRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postAccountPasswordUpdateInit(requester: HttpRequester, payload: PasswordUpdateRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/basic/accounts/password-update/init";
  
  // Make the API request
  return makeApiRequest<EmptyResponse, PasswordUpdateRequest>({
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
export async function getAccountsAdminUsers(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<GetAdminsResponse>> {
  let endpoint = "/basic/accounts/admin/admin-users";
  
  // Make the API request
  return makeApiRequest<GetAdminsResponse>({
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
 * @param query - The `query` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getAccountsFind(requester: HttpRequester, query: string, gamertag?: string): Promise<HttpResponse<Account>> {
  let endpoint = "/basic/accounts/find";
  
  // Make the API request
  return makeApiRequest<Account>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      query
    },
    g: gamertag,
    w: true
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param deviceId - The `deviceId` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getAccountsAvailableDeviceId(requester: HttpRequester, deviceId: string, gamertag?: string): Promise<HttpResponse<AccountAvailableResponse>> {
  let endpoint = "/basic/accounts/available/device-id";
  
  // Make the API request
  return makeApiRequest<AccountAvailableResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      deviceId
    },
    g: gamertag
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param email - The `email` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getAccountsAvailable(requester: HttpRequester, email: string, gamertag?: string): Promise<HttpResponse<AccountAvailableResponse>> {
  let endpoint = "/basic/accounts/available";
  
  // Make the API request
  return makeApiRequest<AccountAvailableResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      email
    },
    g: gamertag
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `PasswordUpdateConfirmation` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postAccountPasswordUpdateConfirm(requester: HttpRequester, payload: PasswordUpdateConfirmation, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/basic/accounts/password-update/confirm";
  
  // Make the API request
  return makeApiRequest<EmptyResponse, PasswordUpdateConfirmation>({
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
 * @param payload - The `AttachExternalIdentityApiRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postAccountExternalIdentity(requester: HttpRequester, payload: AttachExternalIdentityApiRequest, gamertag?: string): Promise<HttpResponse<AttachExternalIdentityApiResponse>> {
  let endpoint = "/basic/accounts/external_identity";
  
  // Make the API request
  return makeApiRequest<AttachExternalIdentityApiResponse, AttachExternalIdentityApiRequest>({
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
 * @param payload - The `DeleteExternalIdentityApiRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function deleteAccountExternalIdentity(requester: HttpRequester, payload: DeleteExternalIdentityApiRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/basic/accounts/external_identity";
  
  // Make the API request
  return makeApiRequest<CommonResponse, DeleteExternalIdentityApiRequest>({
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
 * @param payload - The `EmailUpdateRequest` instance to use for the API request
 * @param objectId - AccountId of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function putAccountAdminEmailByObjectId(requester: HttpRequester, objectId: bigint | string, payload: EmailUpdateRequest, gamertag?: string): Promise<HttpResponse<Account>> {
  let endpoint = "/object/accounts/{objectId}/admin/email".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<Account, EmailUpdateRequest>({
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
 * @param objectId - AccountId of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getAccountAvailableRolesByObjectId(requester: HttpRequester, objectId: bigint | string, gamertag?: string): Promise<HttpResponse<AvailableRolesResponse>> {
  let endpoint = "/object/accounts/{objectId}/available-roles".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<AvailableRolesResponse>({
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
 * @param objectId - AccountId of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getAccountRoleReportByObjectId(requester: HttpRequester, objectId: bigint | string, gamertag?: string): Promise<HttpResponse<AccountRolesReport>> {
  let endpoint = "/object/accounts/{objectId}/role/report".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<AccountRolesReport>({
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
 * @param payload - The `UpdateRole` instance to use for the API request
 * @param objectId - AccountId of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function putAccountRoleByObjectId(requester: HttpRequester, objectId: bigint | string, payload: UpdateRole, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/object/accounts/{objectId}/role".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<EmptyResponse, UpdateRole>({
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
 * @param payload - The `DeleteRole` instance to use for the API request
 * @param objectId - AccountId of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function deleteAccountRoleByObjectId(requester: HttpRequester, objectId: bigint | string, payload: DeleteRole, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/object/accounts/{objectId}/role".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<EmptyResponse, DeleteRole>({
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
 * @param payload - The `UpdateRole` instance to use for the API request
 * @param objectId - AccountId of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function putAccountAdminScopeByObjectId(requester: HttpRequester, objectId: bigint | string, payload: UpdateRole, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/object/accounts/{objectId}/admin/scope".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<EmptyResponse, UpdateRole>({
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
 * @param payload - The `DeleteRole` instance to use for the API request
 * @param objectId - AccountId of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function deleteAccountAdminScopeByObjectId(requester: HttpRequester, objectId: bigint | string, payload: DeleteRole, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/object/accounts/{objectId}/admin/scope".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<EmptyResponse, DeleteRole>({
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
 * @param payload - The `TransferThirdPartyAssociation` instance to use for the API request
 * @param objectId - AccountId of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function putAccountAdminThirdPartyByObjectId(requester: HttpRequester, objectId: bigint | string, payload: TransferThirdPartyAssociation, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/object/accounts/{objectId}/admin/third-party".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<EmptyResponse, TransferThirdPartyAssociation>({
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
 * @param payload - The `DeleteThirdPartyAssociation` instance to use for the API request
 * @param objectId - AccountId of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function deleteAccountAdminThirdPartyByObjectId(requester: HttpRequester, objectId: bigint | string, payload: DeleteThirdPartyAssociation, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/object/accounts/{objectId}/admin/third-party".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<EmptyResponse, DeleteThirdPartyAssociation>({
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
 * @param payload - The `AccountUpdate` instance to use for the API request
 * @param objectId - AccountId of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function putAccountByObjectId(requester: HttpRequester, objectId: bigint | string, payload: AccountUpdate, gamertag?: string): Promise<HttpResponse<Account>> {
  let endpoint = "/object/accounts/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<Account, AccountUpdate>({
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
 * @param objectId - AccountId of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function deleteAccountAdminForgetByObjectId(requester: HttpRequester, objectId: bigint | string, gamertag?: string): Promise<HttpResponse<Account>> {
  let endpoint = "/object/accounts/{objectId}/admin/forget".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<Account>({
    r: requester,
    e: endpoint,
    m: DELETE,
    g: gamertag,
    w: true
  });
}
