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
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/constants';
import { PasswordUpdateConfirmation } from '@/__generated__/schemas/PasswordUpdateConfirmation';
import { PasswordUpdateRequest } from '@/__generated__/schemas/PasswordUpdateRequest';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import { ThirdPartyAvailableRequest } from '@/__generated__/schemas/ThirdPartyAvailableRequest';
import { TransferThirdPartyAssociation } from '@/__generated__/schemas/TransferThirdPartyAssociation';
import { UpdateRole } from '@/__generated__/schemas/UpdateRole';

export class AccountsApi {
  constructor(
    private readonly r: HttpRequester
  ) {
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {DeleteDevicesRequest} payload - The `DeleteDevicesRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AccountPlayerView>>} A promise containing the HttpResponse of AccountPlayerView
   */
  async deleteAccountMeDevice(payload: DeleteDevicesRequest, gamertag?: string): Promise<HttpResponse<AccountPlayerView>> {
    let e = "/basic/accounts/me/device";
    
    // Make the API request
    return makeApiRequest<AccountPlayerView, DeleteDevicesRequest>({
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
   * @returns {Promise<HttpResponse<AccountPlayerView>>} A promise containing the HttpResponse of AccountPlayerView
   */
  async getAccountsMe(gamertag?: string): Promise<HttpResponse<AccountPlayerView>> {
    let e = "/basic/accounts/me";
    
    // Make the API request
    return makeApiRequest<AccountPlayerView>({
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
   * @param {AccountUpdate} payload - The `AccountUpdate` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AccountPlayerView>>} A promise containing the HttpResponse of AccountPlayerView
   */
  async putAccountMe(payload: AccountUpdate, gamertag?: string): Promise<HttpResponse<AccountPlayerView>> {
    let e = "/basic/accounts/me";
    
    // Make the API request
    return makeApiRequest<AccountPlayerView, AccountUpdate>({
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
   * @param {ThirdPartyAvailableRequest} payload - The `ThirdPartyAvailableRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AccountPlayerView>>} A promise containing the HttpResponse of AccountPlayerView
   */
  async deleteAccountMeThirdParty(payload: ThirdPartyAvailableRequest, gamertag?: string): Promise<HttpResponse<AccountPlayerView>> {
    let e = "/basic/accounts/me/third-party";
    
    // Make the API request
    return makeApiRequest<AccountPlayerView, ThirdPartyAvailableRequest>({
      r: this.r,
      e,
      m: DELETE,
      p: payload,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {string} query - The `query` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AccountPersonallyIdentifiableInformationResponse>>} A promise containing the HttpResponse of AccountPersonallyIdentifiableInformationResponse
   */
  async getAccountsPersonallyIdentifiableInformation(query: string, gamertag?: string): Promise<HttpResponse<AccountPersonallyIdentifiableInformationResponse>> {
    let e = "/basic/accounts/get-personally-identifiable-information";
    
    // Make the API request
    return makeApiRequest<AccountPersonallyIdentifiableInformationResponse>({
      r: this.r,
      e,
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
   * @param {number} page - The `page` parameter to include in the API request.
   * @param {number} pagesize - The `pagesize` parameter to include in the API request.
   * @param {string} query - The `query` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AccountSearchResponse>>} A promise containing the HttpResponse of AccountSearchResponse
   */
  async getAccountsSearch(page: number, pagesize: number, query: string, gamertag?: string): Promise<HttpResponse<AccountSearchResponse>> {
    let e = "/basic/accounts/search";
    
    // Make the API request
    return makeApiRequest<AccountSearchResponse>({
      r: this.r,
      e,
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
   * @param {EmailUpdateRequest} payload - The `EmailUpdateRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postAccountEmailUpdateInit(payload: EmailUpdateRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let e = "/basic/accounts/email-update/init";
    
    // Make the API request
    return makeApiRequest<EmptyResponse, EmailUpdateRequest>({
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
   * @param {EmailUpdateConfirmation} payload - The `EmailUpdateConfirmation` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postAccountEmailUpdateConfirm(payload: EmailUpdateConfirmation, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let e = "/basic/accounts/email-update/confirm";
    
    // Make the API request
    return makeApiRequest<EmptyResponse, EmailUpdateConfirmation>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {string} provider_service - The `provider_service` parameter to include in the API request.
   * @param {string} user_id - The `user_id` parameter to include in the API request.
   * @param {string} provider_namespace - The `provider_namespace` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AccountAvailableResponse>>} A promise containing the HttpResponse of AccountAvailableResponse
   */
  async getAccountsAvailableExternalIdentity(provider_service: string, user_id: string, provider_namespace?: string, gamertag?: string): Promise<HttpResponse<AccountAvailableResponse>> {
    let e = "/basic/accounts/available/external_identity";
    
    // Make the API request
    return makeApiRequest<AccountAvailableResponse>({
      r: this.r,
      e,
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
   * @param {string} thirdParty - The `thirdParty` parameter to include in the API request.
   * @param {string} token - The `token` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AccountAvailableResponse>>} A promise containing the HttpResponse of AccountAvailableResponse
   */
  async getAccountsAvailableThirdParty(thirdParty: string, token: string, gamertag?: string): Promise<HttpResponse<AccountAvailableResponse>> {
    let e = "/basic/accounts/available/third-party";
    
    // Make the API request
    return makeApiRequest<AccountAvailableResponse>({
      r: this.r,
      e,
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
   * @param {CreateElevatedAccountRequest} payload - The `CreateElevatedAccountRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AccountPortalView>>} A promise containing the HttpResponse of AccountPortalView
   */
  async postAccountAdminUser(payload: CreateElevatedAccountRequest, gamertag?: string): Promise<HttpResponse<AccountPortalView>> {
    let e = "/basic/accounts/admin/admin-user";
    
    // Make the API request
    return makeApiRequest<AccountPortalView, CreateElevatedAccountRequest>({
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
   * @param {AccountRegistration} payload - The `AccountRegistration` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AccountPlayerView>>} A promise containing the HttpResponse of AccountPlayerView
   */
  async postAccountRegister(payload: AccountRegistration, gamertag?: string): Promise<HttpResponse<AccountPlayerView>> {
    let e = "/basic/accounts/register";
    
    // Make the API request
    return makeApiRequest<AccountPlayerView, AccountRegistration>({
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
   * @returns {Promise<HttpResponse<AccountPortalView>>} A promise containing the HttpResponse of AccountPortalView
   */
  async getAccountsAdminMe(gamertag?: string): Promise<HttpResponse<AccountPortalView>> {
    let e = "/basic/accounts/admin/me";
    
    // Make the API request
    return makeApiRequest<AccountPortalView>({
      r: this.r,
      e,
      m: GET,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {PasswordUpdateRequest} payload - The `PasswordUpdateRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postAccountPasswordUpdateInit(payload: PasswordUpdateRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let e = "/basic/accounts/password-update/init";
    
    // Make the API request
    return makeApiRequest<EmptyResponse, PasswordUpdateRequest>({
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
   * @returns {Promise<HttpResponse<GetAdminsResponse>>} A promise containing the HttpResponse of GetAdminsResponse
   */
  async getAccountsAdminUsers(gamertag?: string): Promise<HttpResponse<GetAdminsResponse>> {
    let e = "/basic/accounts/admin/admin-users";
    
    // Make the API request
    return makeApiRequest<GetAdminsResponse>({
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
   * @param {string} query - The `query` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<Account>>} A promise containing the HttpResponse of Account
   */
  async getAccountsFind(query: string, gamertag?: string): Promise<HttpResponse<Account>> {
    let e = "/basic/accounts/find";
    
    // Make the API request
    return makeApiRequest<Account>({
      r: this.r,
      e,
      m: GET,
      q: {
        query
      },
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {string} deviceId - The `deviceId` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AccountAvailableResponse>>} A promise containing the HttpResponse of AccountAvailableResponse
   */
  async getAccountsAvailableDeviceId(deviceId: string, gamertag?: string): Promise<HttpResponse<AccountAvailableResponse>> {
    let e = "/basic/accounts/available/device-id";
    
    // Make the API request
    return makeApiRequest<AccountAvailableResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        deviceId
      },
      g: gamertag
    });
  }
  
  /**
   * @param {string} email - The `email` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AccountAvailableResponse>>} A promise containing the HttpResponse of AccountAvailableResponse
   */
  async getAccountsAvailable(email: string, gamertag?: string): Promise<HttpResponse<AccountAvailableResponse>> {
    let e = "/basic/accounts/available";
    
    // Make the API request
    return makeApiRequest<AccountAvailableResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        email
      },
      g: gamertag
    });
  }
  
  /**
   * @param {PasswordUpdateConfirmation} payload - The `PasswordUpdateConfirmation` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postAccountPasswordUpdateConfirm(payload: PasswordUpdateConfirmation, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let e = "/basic/accounts/password-update/confirm";
    
    // Make the API request
    return makeApiRequest<EmptyResponse, PasswordUpdateConfirmation>({
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
   * @param {AttachExternalIdentityApiRequest} payload - The `AttachExternalIdentityApiRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AttachExternalIdentityApiResponse>>} A promise containing the HttpResponse of AttachExternalIdentityApiResponse
   */
  async postAccountExternalIdentity(payload: AttachExternalIdentityApiRequest, gamertag?: string): Promise<HttpResponse<AttachExternalIdentityApiResponse>> {
    let e = "/basic/accounts/external_identity";
    
    // Make the API request
    return makeApiRequest<AttachExternalIdentityApiResponse, AttachExternalIdentityApiRequest>({
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
   * @param {DeleteExternalIdentityApiRequest} payload - The `DeleteExternalIdentityApiRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async deleteAccountExternalIdentity(payload: DeleteExternalIdentityApiRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/basic/accounts/external_identity";
    
    // Make the API request
    return makeApiRequest<CommonResponse, DeleteExternalIdentityApiRequest>({
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
   * @param {EmailUpdateRequest} payload - The `EmailUpdateRequest` instance to use for the API request
   * @param {bigint | string} objectId - AccountId of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<Account>>} A promise containing the HttpResponse of Account
   */
  async putAccountAdminEmailByObjectId(objectId: bigint | string, payload: EmailUpdateRequest, gamertag?: string): Promise<HttpResponse<Account>> {
    let e = "/object/accounts/{objectId}/admin/email".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<Account, EmailUpdateRequest>({
      r: this.r,
      e,
      m: PUT,
      p: payload,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {bigint | string} objectId - AccountId of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AvailableRolesResponse>>} A promise containing the HttpResponse of AvailableRolesResponse
   */
  async getAccountAvailableRolesByObjectId(objectId: bigint | string, gamertag?: string): Promise<HttpResponse<AvailableRolesResponse>> {
    let e = "/object/accounts/{objectId}/available-roles".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<AvailableRolesResponse>({
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
   * @param {bigint | string} objectId - AccountId of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AccountRolesReport>>} A promise containing the HttpResponse of AccountRolesReport
   */
  async getAccountRoleReportByObjectId(objectId: bigint | string, gamertag?: string): Promise<HttpResponse<AccountRolesReport>> {
    let e = "/object/accounts/{objectId}/role/report".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<AccountRolesReport>({
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
   * @param {UpdateRole} payload - The `UpdateRole` instance to use for the API request
   * @param {bigint | string} objectId - AccountId of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async putAccountRoleByObjectId(objectId: bigint | string, payload: UpdateRole, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let e = "/object/accounts/{objectId}/role".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<EmptyResponse, UpdateRole>({
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
   * @param {DeleteRole} payload - The `DeleteRole` instance to use for the API request
   * @param {bigint | string} objectId - AccountId of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async deleteAccountRoleByObjectId(objectId: bigint | string, payload: DeleteRole, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let e = "/object/accounts/{objectId}/role".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<EmptyResponse, DeleteRole>({
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
   * @param {UpdateRole} payload - The `UpdateRole` instance to use for the API request
   * @param {bigint | string} objectId - AccountId of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async putAccountAdminScopeByObjectId(objectId: bigint | string, payload: UpdateRole, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let e = "/object/accounts/{objectId}/admin/scope".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<EmptyResponse, UpdateRole>({
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
   * @param {DeleteRole} payload - The `DeleteRole` instance to use for the API request
   * @param {bigint | string} objectId - AccountId of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async deleteAccountAdminScopeByObjectId(objectId: bigint | string, payload: DeleteRole, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let e = "/object/accounts/{objectId}/admin/scope".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<EmptyResponse, DeleteRole>({
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
   * @param {TransferThirdPartyAssociation} payload - The `TransferThirdPartyAssociation` instance to use for the API request
   * @param {bigint | string} objectId - AccountId of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async putAccountAdminThirdPartyByObjectId(objectId: bigint | string, payload: TransferThirdPartyAssociation, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let e = "/object/accounts/{objectId}/admin/third-party".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<EmptyResponse, TransferThirdPartyAssociation>({
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
   * @param {DeleteThirdPartyAssociation} payload - The `DeleteThirdPartyAssociation` instance to use for the API request
   * @param {bigint | string} objectId - AccountId of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async deleteAccountAdminThirdPartyByObjectId(objectId: bigint | string, payload: DeleteThirdPartyAssociation, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let e = "/object/accounts/{objectId}/admin/third-party".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<EmptyResponse, DeleteThirdPartyAssociation>({
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
   * @param {AccountUpdate} payload - The `AccountUpdate` instance to use for the API request
   * @param {bigint | string} objectId - AccountId of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<Account>>} A promise containing the HttpResponse of Account
   */
  async putAccountByObjectId(objectId: bigint | string, payload: AccountUpdate, gamertag?: string): Promise<HttpResponse<Account>> {
    let e = "/object/accounts/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<Account, AccountUpdate>({
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
   * @param {bigint | string} objectId - AccountId of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<Account>>} A promise containing the HttpResponse of Account
   */
  async deleteAccountAdminForgetByObjectId(objectId: bigint | string, gamertag?: string): Promise<HttpResponse<Account>> {
    let e = "/object/accounts/{objectId}/admin/forget".replace(objectIdPlaceholder, endpointEncoder(objectId));
    
    // Make the API request
    return makeApiRequest<Account>({
      r: this.r,
      e,
      m: DELETE,
      g: gamertag,
      w: true
    });
  }
}
