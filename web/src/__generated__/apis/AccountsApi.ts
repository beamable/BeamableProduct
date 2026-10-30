/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/__generated__/apis/constants';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import type { AccountPersonallyIdentifiableInformationResponse } from '@/__generated__/schemas/AccountPersonallyIdentifiableInformationResponse';
import type { AccountRegistration } from '@/__generated__/schemas/AccountRegistration';
import type { AccountsBasicAccount } from '@/__generated__/schemas/AccountsBasicAccount';
import type { AccountsBasicAccountAvailableResponse } from '@/__generated__/schemas/AccountsBasicAccountAvailableResponse';
import type { AccountsBasicAccountPlayerView } from '@/__generated__/schemas/AccountsBasicAccountPlayerView';
import type { AccountsBasicAccountPortalView } from '@/__generated__/schemas/AccountsBasicAccountPortalView';
import type { AccountsBasicAccountSearchResponse } from '@/__generated__/schemas/AccountsBasicAccountSearchResponse';
import type { AccountsBasicCreateElevatedAccountRequest } from '@/__generated__/schemas/AccountsBasicCreateElevatedAccountRequest';
import type { AccountsBasicEmailUpdateConfirmation } from '@/__generated__/schemas/AccountsBasicEmailUpdateConfirmation';
import type { AccountsObjectAccount } from '@/__generated__/schemas/AccountsObjectAccount';
import type { AccountsObjectAccountRolesReport } from '@/__generated__/schemas/AccountsObjectAccountRolesReport';
import type { AccountsObjectAvailableRolesResponse } from '@/__generated__/schemas/AccountsObjectAvailableRolesResponse';
import type { AccountUpdate } from '@/__generated__/schemas/AccountUpdate';
import type { AttachExternalIdentityApiRequest } from '@/__generated__/schemas/AttachExternalIdentityApiRequest';
import type { AttachExternalIdentityApiResponse } from '@/__generated__/schemas/AttachExternalIdentityApiResponse';
import type { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import type { CreateAccountWithCredsApiResponse } from '@/__generated__/schemas/CreateAccountWithCredsApiResponse';
import type { CreateAccountWithCredsRequest } from '@/__generated__/schemas/CreateAccountWithCredsRequest';
import type { DeleteDevicesRequest } from '@/__generated__/schemas/DeleteDevicesRequest';
import type { DeleteExternalIdentityApiRequest } from '@/__generated__/schemas/DeleteExternalIdentityApiRequest';
import type { DeleteRole } from '@/__generated__/schemas/DeleteRole';
import type { DeleteThirdPartyAssociation } from '@/__generated__/schemas/DeleteThirdPartyAssociation';
import type { EmailUpdateRequest } from '@/__generated__/schemas/EmailUpdateRequest';
import type { EmptyResponse } from '@/__generated__/schemas/EmptyResponse';
import type { GetAdminsResponse } from '@/__generated__/schemas/GetAdminsResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { PasswordUpdateConfirmation } from '@/__generated__/schemas/PasswordUpdateConfirmation';
import type { PasswordUpdateRequest } from '@/__generated__/schemas/PasswordUpdateRequest';
import type { ThirdPartyAvailableRequest } from '@/__generated__/schemas/ThirdPartyAvailableRequest';
import type { TransferThirdPartyAssociation } from '@/__generated__/schemas/TransferThirdPartyAssociation';
import type { UpdateRole } from '@/__generated__/schemas/UpdateRole';

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
export async function accountsDeleteMeDeviceBasic(requester: HttpRequester, payload: DeleteDevicesRequest, gamertag?: string): Promise<HttpResponse<AccountsBasicAccountPlayerView>> {
  let endpoint = "/basic/accounts/me/device";
  
  // Make the API request
  return makeApiRequest<AccountsBasicAccountPlayerView, DeleteDevicesRequest>({
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
export async function accountsGetMeBasic(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<AccountsBasicAccountPlayerView>> {
  let endpoint = "/basic/accounts/me";
  
  // Make the API request
  return makeApiRequest<AccountsBasicAccountPlayerView>({
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
export async function accountsPutMeBasic(requester: HttpRequester, payload: AccountUpdate, gamertag?: string): Promise<HttpResponse<AccountsBasicAccountPlayerView>> {
  let endpoint = "/basic/accounts/me";
  
  // Make the API request
  return makeApiRequest<AccountsBasicAccountPlayerView, AccountUpdate>({
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
export async function accountsDeleteMeThirdPartyBasic(requester: HttpRequester, payload: ThirdPartyAvailableRequest, gamertag?: string): Promise<HttpResponse<AccountsBasicAccountPlayerView>> {
  let endpoint = "/basic/accounts/me/third-party";
  
  // Make the API request
  return makeApiRequest<AccountsBasicAccountPlayerView, ThirdPartyAvailableRequest>({
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
export async function accountsGetGetPersonallyIdentifiableInformationBasic(requester: HttpRequester, query: string, gamertag?: string): Promise<HttpResponse<AccountPersonallyIdentifiableInformationResponse>> {
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
export async function accountsGetSearchBasic(requester: HttpRequester, page: number, pagesize: number, query: string, gamertag?: string): Promise<HttpResponse<AccountsBasicAccountSearchResponse>> {
  let endpoint = "/basic/accounts/search";
  
  // Make the API request
  return makeApiRequest<AccountsBasicAccountSearchResponse>({
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
export async function accountsPostEmailUpdateInitBasic(requester: HttpRequester, payload: EmailUpdateRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
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
 * @param payload - The `AccountsBasicEmailUpdateConfirmation` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function accountsPostEmailUpdateConfirmBasic(requester: HttpRequester, payload: AccountsBasicEmailUpdateConfirmation, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/basic/accounts/email-update/confirm";
  
  // Make the API request
  return makeApiRequest<EmptyResponse, AccountsBasicEmailUpdateConfirmation>({
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
export async function accountsGetAvailableExternalIdentityBasic(requester: HttpRequester, provider_service: string, user_id: string, provider_namespace?: string, gamertag?: string): Promise<HttpResponse<AccountsBasicAccountAvailableResponse>> {
  let endpoint = "/basic/accounts/available/external_identity";
  
  // Make the API request
  return makeApiRequest<AccountsBasicAccountAvailableResponse>({
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
export async function accountsGetAvailableThirdPartyBasic(requester: HttpRequester, thirdParty: string, token: string, gamertag?: string): Promise<HttpResponse<AccountsBasicAccountAvailableResponse>> {
  let endpoint = "/basic/accounts/available/third-party";
  
  // Make the API request
  return makeApiRequest<AccountsBasicAccountAvailableResponse>({
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
 * @param payload - The `AccountsBasicCreateElevatedAccountRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function accountsPostAdminAdminUserBasic(requester: HttpRequester, payload: AccountsBasicCreateElevatedAccountRequest, gamertag?: string): Promise<HttpResponse<AccountsBasicAccountPortalView>> {
  let endpoint = "/basic/accounts/admin/admin-user";
  
  // Make the API request
  return makeApiRequest<AccountsBasicAccountPortalView, AccountsBasicCreateElevatedAccountRequest>({
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
export async function accountsPostRegisterBasic(requester: HttpRequester, payload: AccountRegistration, gamertag?: string): Promise<HttpResponse<AccountsBasicAccountPlayerView>> {
  let endpoint = "/basic/accounts/register";
  
  // Make the API request
  return makeApiRequest<AccountsBasicAccountPlayerView, AccountRegistration>({
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
export async function accountsGetAdminMeBasic(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<AccountsBasicAccountPortalView>> {
  let endpoint = "/basic/accounts/admin/me";
  
  // Make the API request
  return makeApiRequest<AccountsBasicAccountPortalView>({
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
export async function accountsPostPasswordUpdateInitBasic(requester: HttpRequester, payload: PasswordUpdateRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
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
export async function accountsGetAdminAdminUsersBasic(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<GetAdminsResponse>> {
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
export async function accountsGetFindBasic(requester: HttpRequester, query: string, gamertag?: string): Promise<HttpResponse<AccountsBasicAccount>> {
  let endpoint = "/basic/accounts/find";
  
  // Make the API request
  return makeApiRequest<AccountsBasicAccount>({
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
 * @param payload - The `CreateAccountWithCredsRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function accountsPostSignupBasic(requester: HttpRequester, payload: CreateAccountWithCredsRequest, gamertag?: string): Promise<HttpResponse<CreateAccountWithCredsApiResponse>> {
  let endpoint = "/basic/accounts/signup";
  
  // Make the API request
  return makeApiRequest<CreateAccountWithCredsApiResponse, CreateAccountWithCredsRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param deviceId - The `deviceId` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function accountsGetAvailableDeviceIdBasic(requester: HttpRequester, deviceId: string, gamertag?: string): Promise<HttpResponse<AccountsBasicAccountAvailableResponse>> {
  let endpoint = "/basic/accounts/available/device-id";
  
  // Make the API request
  return makeApiRequest<AccountsBasicAccountAvailableResponse>({
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
export async function accountsGetAvailableBasic(requester: HttpRequester, email: string, gamertag?: string): Promise<HttpResponse<AccountsBasicAccountAvailableResponse>> {
  let endpoint = "/basic/accounts/available";
  
  // Make the API request
  return makeApiRequest<AccountsBasicAccountAvailableResponse>({
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
export async function accountsPostPasswordUpdateConfirmBasic(requester: HttpRequester, payload: PasswordUpdateConfirmation, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
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
export async function accountsPostExternalIdentityBasic(requester: HttpRequester, payload: AttachExternalIdentityApiRequest, gamertag?: string): Promise<HttpResponse<AttachExternalIdentityApiResponse>> {
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
export async function accountsDeleteExternalIdentityBasic(requester: HttpRequester, payload: DeleteExternalIdentityApiRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
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
export async function accountsPutAdminEmailByObjectId(requester: HttpRequester, objectId: bigint | string, payload: EmailUpdateRequest, gamertag?: string): Promise<HttpResponse<AccountsObjectAccount>> {
  let endpoint = "/object/accounts/{objectId}/admin/email".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<AccountsObjectAccount, EmailUpdateRequest>({
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
export async function accountsGetAvailableRolesByObjectId(requester: HttpRequester, objectId: bigint | string, gamertag?: string): Promise<HttpResponse<AccountsObjectAvailableRolesResponse>> {
  let endpoint = "/object/accounts/{objectId}/available-roles".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<AccountsObjectAvailableRolesResponse>({
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
export async function accountsGetRoleReportByObjectId(requester: HttpRequester, objectId: bigint | string, gamertag?: string): Promise<HttpResponse<AccountsObjectAccountRolesReport>> {
  let endpoint = "/object/accounts/{objectId}/role/report".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<AccountsObjectAccountRolesReport>({
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
export async function accountsPutRoleByObjectId(requester: HttpRequester, objectId: bigint | string, payload: UpdateRole, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
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
export async function accountsDeleteRoleByObjectId(requester: HttpRequester, objectId: bigint | string, payload: DeleteRole, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
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
export async function accountsPutAdminScopeByObjectId(requester: HttpRequester, objectId: bigint | string, payload: UpdateRole, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
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
export async function accountsDeleteAdminScopeByObjectId(requester: HttpRequester, objectId: bigint | string, payload: DeleteRole, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
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
export async function accountsPutAdminThirdPartyByObjectId(requester: HttpRequester, objectId: bigint | string, payload: TransferThirdPartyAssociation, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
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
export async function accountsDeleteAdminThirdPartyByObjectId(requester: HttpRequester, objectId: bigint | string, payload: DeleteThirdPartyAssociation, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
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
export async function accountsPutByObjectId(requester: HttpRequester, objectId: bigint | string, payload: AccountUpdate, gamertag?: string): Promise<HttpResponse<AccountsObjectAccount>> {
  let endpoint = "/object/accounts/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<AccountsObjectAccount, AccountUpdate>({
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
export async function accountsDeleteAdminForgetByObjectId(requester: HttpRequester, objectId: bigint | string, gamertag?: string): Promise<HttpResponse<AccountsObjectAccount>> {
  let endpoint = "/object/accounts/{objectId}/admin/forget".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<AccountsObjectAccount>({
    r: requester,
    e: endpoint,
    m: DELETE,
    g: gamertag,
    w: true
  });
}
