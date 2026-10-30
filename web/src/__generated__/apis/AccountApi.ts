/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { accountIdPlaceholder } from '@/__generated__/apis/constants';
import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import type { AccountActorAccount } from '@/__generated__/schemas/AccountActorAccount';
import type { AccountActorAccountAvailableResponse } from '@/__generated__/schemas/AccountActorAccountAvailableResponse';
import type { AccountActorAccountPlayerView } from '@/__generated__/schemas/AccountActorAccountPlayerView';
import type { AccountActorAccountPortalView } from '@/__generated__/schemas/AccountActorAccountPortalView';
import type { AccountActorAccountRolesReport } from '@/__generated__/schemas/AccountActorAccountRolesReport';
import type { AccountActorAccountSearchResponse } from '@/__generated__/schemas/AccountActorAccountSearchResponse';
import type { AccountActorAccountUpdate } from '@/__generated__/schemas/AccountActorAccountUpdate';
import type { AccountActorAvailableRolesResponse } from '@/__generated__/schemas/AccountActorAvailableRolesResponse';
import type { AccountActorCommonResponse } from '@/__generated__/schemas/AccountActorCommonResponse';
import type { AccountActorCreateElevatedAccountRequest } from '@/__generated__/schemas/AccountActorCreateElevatedAccountRequest';
import type { AccountActorEmailUpdateConfirmation } from '@/__generated__/schemas/AccountActorEmailUpdateConfirmation';
import type { AccountActorEmailUpdateRequest } from '@/__generated__/schemas/AccountActorEmailUpdateRequest';
import type { AccountPiiResponse } from '@/__generated__/schemas/AccountPiiResponse';
import type { AccountPortalViewCursorPagedResult } from '@/__generated__/schemas/AccountPortalViewCursorPagedResult';
import type { AccountRegistration } from '@/__generated__/schemas/AccountRegistration';
import type { AdminAccountUpdate } from '@/__generated__/schemas/AdminAccountUpdate';
import type { AttachExternalIdentityResponse } from '@/__generated__/schemas/AttachExternalIdentityResponse';
import type { CreateGuestRequest } from '@/__generated__/schemas/CreateGuestRequest';
import type { EmailChange } from '@/__generated__/schemas/EmailChange';
import type { EmptyMessage } from '@/__generated__/schemas/EmptyMessage';
import type { ExternalIdentityRequest } from '@/__generated__/schemas/ExternalIdentityRequest';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { MergeRequest } from '@/__generated__/schemas/MergeRequest';
import type { PasswordResetConfirmation } from '@/__generated__/schemas/PasswordResetConfirmation';
import type { PasswordResetRequest } from '@/__generated__/schemas/PasswordResetRequest';
import type { RemoveDevicesRequest } from '@/__generated__/schemas/RemoveDevicesRequest';
import type { RoleUpdate } from '@/__generated__/schemas/RoleUpdate';
import type { SignupRequest } from '@/__generated__/schemas/SignupRequest';
import type { SignupResponse } from '@/__generated__/schemas/SignupResponse';
import type { ThirdPartyTransfer } from '@/__generated__/schemas/ThirdPartyTransfer';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsGetMe(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<AccountActorAccountPlayerView>> {
  let endpoint = "/api/accounts/me";
  
  // Make the API request
  return makeApiRequest<AccountActorAccountPlayerView>({
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
 * @param payload - The `AccountActorAccountUpdate` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsPutMe(requester: HttpRequester, payload: AccountActorAccountUpdate, gamertag?: string): Promise<HttpResponse<AccountActorAccountPlayerView>> {
  let endpoint = "/api/accounts/me";
  
  // Make the API request
  return makeApiRequest<AccountActorAccountPlayerView, AccountActorAccountUpdate>({
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
 * @param payload - The `AccountRegistration` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsPostMeRegister(requester: HttpRequester, payload: AccountRegistration, gamertag?: string): Promise<HttpResponse<AccountActorAccountPlayerView>> {
  let endpoint = "/api/accounts/me/register";
  
  // Make the API request
  return makeApiRequest<AccountActorAccountPlayerView, AccountRegistration>({
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
 * @param payload - The `AccountActorEmailUpdateRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsPostMeEmail(requester: HttpRequester, payload: AccountActorEmailUpdateRequest, gamertag?: string): Promise<HttpResponse<EmptyMessage>> {
  let endpoint = "/api/accounts/me/email";
  
  // Make the API request
  return makeApiRequest<EmptyMessage, AccountActorEmailUpdateRequest>({
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
 * @param payload - The `AccountActorEmailUpdateConfirmation` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsPutMeEmail(requester: HttpRequester, payload: AccountActorEmailUpdateConfirmation, gamertag?: string): Promise<HttpResponse<EmptyMessage>> {
  let endpoint = "/api/accounts/me/email";
  
  // Make the API request
  return makeApiRequest<EmptyMessage, AccountActorEmailUpdateConfirmation>({
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
 * @param payload - The `SignupRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsPostSignup(requester: HttpRequester, payload: SignupRequest, gamertag?: string): Promise<HttpResponse<SignupResponse>> {
  let endpoint = "/api/accounts/signup";
  
  // Make the API request
  return makeApiRequest<SignupResponse, SignupRequest>({
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
 * @param payload - The `PasswordResetRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsPostPassword(requester: HttpRequester, payload: PasswordResetRequest, gamertag?: string): Promise<HttpResponse<EmptyMessage>> {
  let endpoint = "/api/accounts/password";
  
  // Make the API request
  return makeApiRequest<EmptyMessage, PasswordResetRequest>({
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
 * @param payload - The `PasswordResetConfirmation` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsPutPassword(requester: HttpRequester, payload: PasswordResetConfirmation, gamertag?: string): Promise<HttpResponse<EmptyMessage>> {
  let endpoint = "/api/accounts/password";
  
  // Make the API request
  return makeApiRequest<EmptyMessage, PasswordResetConfirmation>({
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
 * @param payload - The `ExternalIdentityRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsPostMeExternalIdentities(requester: HttpRequester, payload: ExternalIdentityRequest, gamertag?: string): Promise<HttpResponse<AttachExternalIdentityResponse>> {
  let endpoint = "/api/accounts/me/external-identities";
  
  // Make the API request
  return makeApiRequest<AttachExternalIdentityResponse, ExternalIdentityRequest>({
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
 * @param providerNamespace - The `providerNamespace` parameter to include in the API request.
 * @param providerService - The `providerService` parameter to include in the API request.
 * @param userId - The `userId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsDeleteMeExternalIdentities(requester: HttpRequester, providerNamespace?: string, providerService?: string, userId?: string, gamertag?: string): Promise<HttpResponse<AccountActorCommonResponse>> {
  let endpoint = "/api/accounts/me/external-identities";
  
  // Make the API request
  return makeApiRequest<AccountActorCommonResponse>({
    r: requester,
    e: endpoint,
    m: DELETE,
    q: {
      providerNamespace,
      providerService,
      userId
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
 * @param thirdParty - The `thirdParty` parameter to include in the API request.
 * @param token - The `token` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsDeleteMeThirdParty(requester: HttpRequester, thirdParty?: string, token?: string, gamertag?: string): Promise<HttpResponse<AccountActorAccountPlayerView>> {
  let endpoint = "/api/accounts/me/third-party";
  
  // Make the API request
  return makeApiRequest<AccountActorAccountPlayerView>({
    r: requester,
    e: endpoint,
    m: DELETE,
    q: {
      thirdParty,
      token
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
 * @param payload - The `RemoveDevicesRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsDeleteMeDevices(requester: HttpRequester, payload: RemoveDevicesRequest, gamertag?: string): Promise<HttpResponse<AccountActorAccountPlayerView>> {
  let endpoint = "/api/accounts/me/devices";
  
  // Make the API request
  return makeApiRequest<AccountActorAccountPlayerView, RemoveDevicesRequest>({
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
 * @param payload - The `AdminAccountUpdate` instance to use for the API request
 * @param accountId - The `accountId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsPutByAccountId(requester: HttpRequester, accountId: bigint | string, payload: AdminAccountUpdate, gamertag?: string): Promise<HttpResponse<AccountActorAccount>> {
  let endpoint = "/api/accounts/{accountId}".replace(accountIdPlaceholder, endpointEncoder(accountId));
  
  // Make the API request
  return makeApiRequest<AccountActorAccount, AdminAccountUpdate>({
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
 * @param accountId - The `accountId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsGetByAccountId(requester: HttpRequester, accountId: bigint | string, gamertag?: string): Promise<HttpResponse<AccountActorAccount>> {
  let endpoint = "/api/accounts/{accountId}".replace(accountIdPlaceholder, endpointEncoder(accountId));
  
  // Make the API request
  return makeApiRequest<AccountActorAccount>({
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
 * @param accountId - The `accountId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsDeleteByAccountId(requester: HttpRequester, accountId: bigint | string, gamertag?: string): Promise<HttpResponse<EmptyMessage>> {
  let endpoint = "/api/accounts/{accountId}".replace(accountIdPlaceholder, endpointEncoder(accountId));
  
  // Make the API request
  return makeApiRequest<EmptyMessage>({
    r: requester,
    e: endpoint,
    m: DELETE,
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
 * @param accountId - The `accountId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsPostRegisterByAccountId(requester: HttpRequester, accountId: bigint | string, payload: AccountRegistration, gamertag?: string): Promise<HttpResponse<AccountActorAccount>> {
  let endpoint = "/api/accounts/{accountId}/register".replace(accountIdPlaceholder, endpointEncoder(accountId));
  
  // Make the API request
  return makeApiRequest<AccountActorAccount, AccountRegistration>({
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
 * @param accountId - The account.
 * @param thirdParty - The provider name.
 * @param userAppId - The player's user id with that provider.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsDeleteThirdPartyByAccountId(requester: HttpRequester, accountId: bigint | string, thirdParty?: string, userAppId?: string, gamertag?: string): Promise<HttpResponse<AccountActorAccount>> {
  let endpoint = "/api/accounts/{accountId}/third-party".replace(accountIdPlaceholder, endpointEncoder(accountId));
  
  // Make the API request
  return makeApiRequest<AccountActorAccount>({
    r: requester,
    e: endpoint,
    m: DELETE,
    q: {
      thirdParty,
      userAppId
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
 * @param payload - The `ThirdPartyTransfer` instance to use for the API request
 * @param accountId - The `accountId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsPutThirdPartyByAccountId(requester: HttpRequester, accountId: bigint | string, payload: ThirdPartyTransfer, gamertag?: string): Promise<HttpResponse<AccountActorAccount>> {
  let endpoint = "/api/accounts/{accountId}/third-party".replace(accountIdPlaceholder, endpointEncoder(accountId));
  
  // Make the API request
  return makeApiRequest<AccountActorAccount, ThirdPartyTransfer>({
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
 * @param payload - The `RemoveDevicesRequest` instance to use for the API request
 * @param accountId - The `accountId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsDeleteDevicesByAccountId(requester: HttpRequester, accountId: bigint | string, payload: RemoveDevicesRequest, gamertag?: string): Promise<HttpResponse<AccountActorAccount>> {
  let endpoint = "/api/accounts/{accountId}/devices".replace(accountIdPlaceholder, endpointEncoder(accountId));
  
  // Make the API request
  return makeApiRequest<AccountActorAccount, RemoveDevicesRequest>({
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
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsGetAdminMe(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<AccountActorAccountPortalView>> {
  let endpoint = "/api/accounts/admin/me";
  
  // Make the API request
  return makeApiRequest<AccountActorAccountPortalView>({
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
 * @param cursor - The previous page's next cursor; omit it at first.
 * @param limit - How many accounts to return, at most 200.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsGetAdminUsers(requester: HttpRequester, cursor?: string, limit?: number, gamertag?: string): Promise<HttpResponse<AccountPortalViewCursorPagedResult>> {
  let endpoint = "/api/accounts/admin-users";
  
  // Make the API request
  return makeApiRequest<AccountPortalViewCursorPagedResult>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      cursor,
      limit
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
 * @param payload - The `AccountActorCreateElevatedAccountRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsPostAdminUsers(requester: HttpRequester, payload: AccountActorCreateElevatedAccountRequest, gamertag?: string): Promise<HttpResponse<AccountActorAccountPortalView>> {
  let endpoint = "/api/accounts/admin-users";
  
  // Make the API request
  return makeApiRequest<AccountActorAccountPortalView, AccountActorCreateElevatedAccountRequest>({
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
 * @param payload - The `CreateGuestRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsPost(requester: HttpRequester, payload: CreateGuestRequest, gamertag?: string): Promise<HttpResponse<AccountActorAccount>> {
  let endpoint = "/api/accounts";
  
  // Make the API request
  return makeApiRequest<AccountActorAccount, CreateGuestRequest>({
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
 * @param AppId - The `AppId` parameter to include in the API request.
 * @param DeviceId - The `DeviceId` parameter to include in the API request.
 * @param Email - The `Email` parameter to include in the API request.
 * @param PlayerId - The `PlayerId` parameter to include in the API request.
 * @param ProviderNamespace - The `ProviderNamespace` parameter to include in the API request.
 * @param ProviderService - The `ProviderService` parameter to include in the API request.
 * @param ThirdParty - The `ThirdParty` parameter to include in the API request.
 * @param UserAppId - The `UserAppId` parameter to include in the API request.
 * @param UserBusinessId - The `UserBusinessId` parameter to include in the API request.
 * @param UserId - The `UserId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsGet(requester: HttpRequester, AppId?: string, DeviceId?: string, Email?: string, PlayerId?: bigint | string, ProviderNamespace?: string, ProviderService?: string, ThirdParty?: string, UserAppId?: string, UserBusinessId?: string, UserId?: string, gamertag?: string): Promise<HttpResponse<AccountActorAccount>> {
  let endpoint = "/api/accounts";
  
  // Make the API request
  return makeApiRequest<AccountActorAccount>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      AppId,
      DeviceId,
      Email,
      PlayerId,
      ProviderNamespace,
      ProviderService,
      ThirdParty,
      UserAppId,
      UserBusinessId,
      UserId
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
 * @param payload - The `AccountActorCreateElevatedAccountRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsPostElevated(requester: HttpRequester, payload: AccountActorCreateElevatedAccountRequest, gamertag?: string): Promise<HttpResponse<AccountActorAccount>> {
  let endpoint = "/api/accounts/elevated";
  
  // Make the API request
  return makeApiRequest<AccountActorAccount, AccountActorCreateElevatedAccountRequest>({
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
 * @param email - The `email` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsGetAvailable(requester: HttpRequester, email?: string, gamertag?: string): Promise<HttpResponse<AccountActorAccountAvailableResponse>> {
  let endpoint = "/api/accounts/available";
  
  // Make the API request
  return makeApiRequest<AccountActorAccountAvailableResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      email
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
 * @param deviceId - The `deviceId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsGetAvailableDeviceId(requester: HttpRequester, deviceId?: string, gamertag?: string): Promise<HttpResponse<AccountActorAccountAvailableResponse>> {
  let endpoint = "/api/accounts/available/device-id";
  
  // Make the API request
  return makeApiRequest<AccountActorAccountAvailableResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      deviceId
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
 * @param providerNamespace - The `providerNamespace` parameter to include in the API request.
 * @param providerService - The `providerService` parameter to include in the API request.
 * @param userId - The `userId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsGetAvailableExternalIdentity(requester: HttpRequester, providerNamespace?: string, providerService?: string, userId?: string, gamertag?: string): Promise<HttpResponse<AccountActorAccountAvailableResponse>> {
  let endpoint = "/api/accounts/available/external-identity";
  
  // Make the API request
  return makeApiRequest<AccountActorAccountAvailableResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      providerNamespace,
      providerService,
      userId
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
 * @param thirdParty - The provider, such as `facebook`.
 * @param token - The provider token, verified with the provider.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsGetAvailableThirdParty(requester: HttpRequester, thirdParty?: string, token?: string, gamertag?: string): Promise<HttpResponse<AccountActorAccountAvailableResponse>> {
  let endpoint = "/api/accounts/available/third-party";
  
  // Make the API request
  return makeApiRequest<AccountActorAccountAvailableResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      thirdParty,
      token
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
 * @param query - A number is tried as a player id in this realm, then as an account id; anything
 * else is an email.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsGetFind(requester: HttpRequester, query?: string, gamertag?: string): Promise<HttpResponse<AccountActorAccount>> {
  let endpoint = "/api/accounts/find";
  
  // Make the API request
  return makeApiRequest<AccountActorAccount>({
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
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param query - As M:BeamableGateway.Controllers.AccountController.FindAccount(System.String) takes it.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsGetPii(requester: HttpRequester, query?: string, gamertag?: string): Promise<HttpResponse<AccountPiiResponse>> {
  let endpoint = "/api/accounts/pii";
  
  // Make the API request
  return makeApiRequest<AccountPiiResponse>({
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
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `RoleUpdate` instance to use for the API request
 * @param accountId - The `accountId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsPutRoleByAccountId(requester: HttpRequester, accountId: bigint | string, payload: RoleUpdate, gamertag?: string): Promise<HttpResponse<AccountActorAccount>> {
  let endpoint = "/api/accounts/{accountId}/role".replace(accountIdPlaceholder, endpointEncoder(accountId));
  
  // Make the API request
  return makeApiRequest<AccountActorAccount, RoleUpdate>({
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
 * @param accountId - The `accountId` parameter to include in the API request.
 * @param realm - The `realm` parameter to include in the API request.
 * @param role - The `role` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsDeleteRoleByAccountId(requester: HttpRequester, accountId: bigint | string, realm?: string, role?: string, gamertag?: string): Promise<HttpResponse<AccountActorAccount>> {
  let endpoint = "/api/accounts/{accountId}/role".replace(accountIdPlaceholder, endpointEncoder(accountId));
  
  // Make the API request
  return makeApiRequest<AccountActorAccount>({
    r: requester,
    e: endpoint,
    m: DELETE,
    q: {
      realm,
      role
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
 * @param accountId - The `accountId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsGetAvailableRolesByAccountId(requester: HttpRequester, accountId: bigint | string, gamertag?: string): Promise<HttpResponse<AccountActorAvailableRolesResponse>> {
  let endpoint = "/api/accounts/{accountId}/available-roles".replace(accountIdPlaceholder, endpointEncoder(accountId));
  
  // Make the API request
  return makeApiRequest<AccountActorAvailableRolesResponse>({
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
 * @param accountId - The `accountId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsGetRoleReportByAccountId(requester: HttpRequester, accountId: bigint | string, gamertag?: string): Promise<HttpResponse<AccountActorAccountRolesReport>> {
  let endpoint = "/api/accounts/{accountId}/role-report".replace(accountIdPlaceholder, endpointEncoder(accountId));
  
  // Make the API request
  return makeApiRequest<AccountActorAccountRolesReport>({
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
 * @param payload - The `EmailChange` instance to use for the API request
 * @param accountId - The `accountId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsPutEmailByAccountId(requester: HttpRequester, accountId: bigint | string, payload: EmailChange, gamertag?: string): Promise<HttpResponse<AccountActorAccount>> {
  let endpoint = "/api/accounts/{accountId}/email".replace(accountIdPlaceholder, endpointEncoder(accountId));
  
  // Make the API request
  return makeApiRequest<AccountActorAccount, EmailChange>({
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
 * @param accountId - The `accountId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsDeletePiiByAccountId(requester: HttpRequester, accountId: bigint | string, gamertag?: string): Promise<HttpResponse<AccountActorAccount>> {
  let endpoint = "/api/accounts/{accountId}/pii".replace(accountIdPlaceholder, endpointEncoder(accountId));
  
  // Make the API request
  return makeApiRequest<AccountActorAccount>({
    r: requester,
    e: endpoint,
    m: DELETE,
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
 * @param page - The page, from 1.
 * @param pageSize - Accounts per page, at most 200.
 * @param query - See M:BeamableShared.Services.Account.AccountManagementService.Search(System.String,System.Int32,System.Int32,BeamableShared.Utils.RealmInfo,System.Threading.CancellationToken).
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsGetSearch(requester: HttpRequester, page?: number, pageSize?: number, query?: string, gamertag?: string): Promise<HttpResponse<AccountActorAccountSearchResponse>> {
  let endpoint = "/api/accounts/search";
  
  // Make the API request
  return makeApiRequest<AccountActorAccountSearchResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      page,
      pageSize,
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
 * @param payload - The `MergeRequest` instance to use for the API request
 * @param accountId - The `accountId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsPutMergeByAccountId(requester: HttpRequester, accountId: bigint | string, payload: MergeRequest, gamertag?: string): Promise<HttpResponse<AccountActorAccount>> {
  let endpoint = "/api/accounts/{accountId}/merge".replace(accountIdPlaceholder, endpointEncoder(accountId));
  
  // Make the API request
  return makeApiRequest<AccountActorAccount, MergeRequest>({
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
 * @param accountId - The `accountId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function accountsDeleteAssociationsByAccountId(requester: HttpRequester, accountId: bigint | string, gamertag?: string): Promise<HttpResponse<AccountActorAccount>> {
  let endpoint = "/api/accounts/{accountId}/associations".replace(accountIdPlaceholder, endpointEncoder(accountId));
  
  // Make the API request
  return makeApiRequest<AccountActorAccount>({
    r: requester,
    e: endpoint,
    m: DELETE,
    g: gamertag,
    w: true
  });
}
