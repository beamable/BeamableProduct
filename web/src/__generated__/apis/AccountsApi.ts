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
import { DeleteDevicesRequest } from '@/__generated__/schemas/DeleteDevicesRequest';
import { DeleteExternalIdentityApiRequest } from '@/__generated__/schemas/DeleteExternalIdentityApiRequest';
import { DeleteRole } from '@/__generated__/schemas/DeleteRole';
import { DeleteThirdPartyAssociation } from '@/__generated__/schemas/DeleteThirdPartyAssociation';
import { EmailUpdateConfirmation } from '@/__generated__/schemas/EmailUpdateConfirmation';
import { EmailUpdateRequest } from '@/__generated__/schemas/EmailUpdateRequest';
import { EmptyResponse } from '@/__generated__/schemas/EmptyResponse';
import { GetAdminsResponse } from '@/__generated__/schemas/GetAdminsResponse';
import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { makeQueryString } from '@/utils/makeQueryString';
import { PasswordUpdateConfirmation } from '@/__generated__/schemas/PasswordUpdateConfirmation';
import { PasswordUpdateRequest } from '@/__generated__/schemas/PasswordUpdateRequest';
import { ThirdPartyAvailableRequest } from '@/__generated__/schemas/ThirdPartyAvailableRequest';
import { TransferThirdPartyAssociation } from '@/__generated__/schemas/TransferThirdPartyAssociation';
import { UpdateRole } from '@/__generated__/schemas/UpdateRole';

export class AccountsApi {
  constructor(
    private readonly requester: HttpRequester
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
    let endpoint = "/basic/accounts/me/device";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<AccountPlayerView, DeleteDevicesRequest>({
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
   * @returns {Promise<HttpResponse<AccountPlayerView>>} A promise containing the HttpResponse of AccountPlayerView
   */
  async getAccountsMe(gamertag?: string): Promise<HttpResponse<AccountPlayerView>> {
    let endpoint = "/basic/accounts/me";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<AccountPlayerView>({
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
   * @param {AccountUpdate} payload - The `AccountUpdate` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AccountPlayerView>>} A promise containing the HttpResponse of AccountPlayerView
   */
  async putAccountMe(payload: AccountUpdate, gamertag?: string): Promise<HttpResponse<AccountPlayerView>> {
    let endpoint = "/basic/accounts/me";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<AccountPlayerView, AccountUpdate>({
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
   * @param {ThirdPartyAvailableRequest} payload - The `ThirdPartyAvailableRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AccountPlayerView>>} A promise containing the HttpResponse of AccountPlayerView
   */
  async deleteAccountMeThirdParty(payload: ThirdPartyAvailableRequest, gamertag?: string): Promise<HttpResponse<AccountPlayerView>> {
    let endpoint = "/basic/accounts/me/third-party";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<AccountPlayerView, ThirdPartyAvailableRequest>({
      url: endpoint,
      method: HttpMethod.DELETE,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {string} query - The `query` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AccountPersonallyIdentifiableInformationResponse>>} A promise containing the HttpResponse of AccountPersonallyIdentifiableInformationResponse
   */
  async getAccountsPersonallyIdentifiableInformation(query: string, gamertag?: string): Promise<HttpResponse<AccountPersonallyIdentifiableInformationResponse>> {
    let endpoint = "/basic/accounts/get-personally-identifiable-information";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      query
    });
    
    // Make the API request
    return this.requester.request<AccountPersonallyIdentifiableInformationResponse>({
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
   * @param {number} page - The `page` parameter to include in the API request.
   * @param {number} pagesize - The `pagesize` parameter to include in the API request.
   * @param {string} query - The `query` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AccountSearchResponse>>} A promise containing the HttpResponse of AccountSearchResponse
   */
  async getAccountsSearch(page: number, pagesize: number, query: string, gamertag?: string): Promise<HttpResponse<AccountSearchResponse>> {
    let endpoint = "/basic/accounts/search";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      page,
      pagesize,
      query
    });
    
    // Make the API request
    return this.requester.request<AccountSearchResponse>({
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
   * @param {EmailUpdateRequest} payload - The `EmailUpdateRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postAccountEmailUpdateInit(payload: EmailUpdateRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let endpoint = "/basic/accounts/email-update/init";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyResponse, EmailUpdateRequest>({
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
   * @param {EmailUpdateConfirmation} payload - The `EmailUpdateConfirmation` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postAccountEmailUpdateConfirm(payload: EmailUpdateConfirmation, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let endpoint = "/basic/accounts/email-update/confirm";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyResponse, EmailUpdateConfirmation>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
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
    let endpoint = "/basic/accounts/available/external_identity";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      provider_service,
      user_id,
      provider_namespace
    });
    
    // Make the API request
    return this.requester.request<AccountAvailableResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {string} thirdParty - The `thirdParty` parameter to include in the API request.
   * @param {string} token - The `token` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AccountAvailableResponse>>} A promise containing the HttpResponse of AccountAvailableResponse
   */
  async getAccountsAvailableThirdParty(thirdParty: string, token: string, gamertag?: string): Promise<HttpResponse<AccountAvailableResponse>> {
    let endpoint = "/basic/accounts/available/third-party";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      thirdParty,
      token
    });
    
    // Make the API request
    return this.requester.request<AccountAvailableResponse>({
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
   * @param {CreateElevatedAccountRequest} payload - The `CreateElevatedAccountRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AccountPortalView>>} A promise containing the HttpResponse of AccountPortalView
   */
  async postAccountAdminUser(payload: CreateElevatedAccountRequest, gamertag?: string): Promise<HttpResponse<AccountPortalView>> {
    let endpoint = "/basic/accounts/admin/admin-user";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<AccountPortalView, CreateElevatedAccountRequest>({
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
   * @param {AccountRegistration} payload - The `AccountRegistration` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AccountPlayerView>>} A promise containing the HttpResponse of AccountPlayerView
   */
  async postAccountRegister(payload: AccountRegistration, gamertag?: string): Promise<HttpResponse<AccountPlayerView>> {
    let endpoint = "/basic/accounts/register";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<AccountPlayerView, AccountRegistration>({
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
   * @returns {Promise<HttpResponse<AccountPortalView>>} A promise containing the HttpResponse of AccountPortalView
   */
  async getAccountsAdminMe(gamertag?: string): Promise<HttpResponse<AccountPortalView>> {
    let endpoint = "/basic/accounts/admin/me";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<AccountPortalView>({
      url: endpoint,
      method: HttpMethod.GET,
      headers,
      withAuth: true
    });
  }
  
  /**
   * @param {PasswordUpdateRequest} payload - The `PasswordUpdateRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postAccountPasswordUpdateInit(payload: PasswordUpdateRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let endpoint = "/basic/accounts/password-update/init";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyResponse, PasswordUpdateRequest>({
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
   * @returns {Promise<HttpResponse<GetAdminsResponse>>} A promise containing the HttpResponse of GetAdminsResponse
   */
  async getAccountsAdminUsers(gamertag?: string): Promise<HttpResponse<GetAdminsResponse>> {
    let endpoint = "/basic/accounts/admin/admin-users";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<GetAdminsResponse>({
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
   * @param {string} query - The `query` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<Account>>} A promise containing the HttpResponse of Account
   */
  async getAccountsFind(query: string, gamertag?: string): Promise<HttpResponse<Account>> {
    let endpoint = "/basic/accounts/find";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      query
    });
    
    // Make the API request
    return this.requester.request<Account>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers,
      withAuth: true
    });
  }
  
  /**
   * @param {string} deviceId - The `deviceId` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AccountAvailableResponse>>} A promise containing the HttpResponse of AccountAvailableResponse
   */
  async getAccountsAvailableDeviceId(deviceId: string, gamertag?: string): Promise<HttpResponse<AccountAvailableResponse>> {
    let endpoint = "/basic/accounts/available/device-id";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      deviceId
    });
    
    // Make the API request
    return this.requester.request<AccountAvailableResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {string} email - The `email` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AccountAvailableResponse>>} A promise containing the HttpResponse of AccountAvailableResponse
   */
  async getAccountsAvailable(email: string, gamertag?: string): Promise<HttpResponse<AccountAvailableResponse>> {
    let endpoint = "/basic/accounts/available";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      email
    });
    
    // Make the API request
    return this.requester.request<AccountAvailableResponse>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers
    });
  }
  
  /**
   * @param {PasswordUpdateConfirmation} payload - The `PasswordUpdateConfirmation` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postAccountPasswordUpdateConfirm(payload: PasswordUpdateConfirmation, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let endpoint = "/basic/accounts/password-update/confirm";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyResponse, PasswordUpdateConfirmation>({
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
   * @param {AttachExternalIdentityApiRequest} payload - The `AttachExternalIdentityApiRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AttachExternalIdentityApiResponse>>} A promise containing the HttpResponse of AttachExternalIdentityApiResponse
   */
  async postAccountExternalIdentity(payload: AttachExternalIdentityApiRequest, gamertag?: string): Promise<HttpResponse<AttachExternalIdentityApiResponse>> {
    let endpoint = "/basic/accounts/external_identity";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<AttachExternalIdentityApiResponse, AttachExternalIdentityApiRequest>({
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
   * @param {DeleteExternalIdentityApiRequest} payload - The `DeleteExternalIdentityApiRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async deleteAccountExternalIdentity(payload: DeleteExternalIdentityApiRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let endpoint = "/basic/accounts/external_identity";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CommonResponse, DeleteExternalIdentityApiRequest>({
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
   * @param {EmailUpdateRequest} payload - The `EmailUpdateRequest` instance to use for the API request
   * @param {bigint | string} objectId - AccountId of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<Account>>} A promise containing the HttpResponse of Account
   */
  async putAccountAdminEmailByObjectId(objectId: bigint | string, payload: EmailUpdateRequest, gamertag?: string): Promise<HttpResponse<Account>> {
    let endpoint = "/object/accounts/{objectId}/admin/email";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<Account, EmailUpdateRequest>({
      url: endpoint,
      method: HttpMethod.PUT,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {bigint | string} objectId - AccountId of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AvailableRolesResponse>>} A promise containing the HttpResponse of AvailableRolesResponse
   */
  async getAccountAvailableRolesByObjectId(objectId: bigint | string, gamertag?: string): Promise<HttpResponse<AvailableRolesResponse>> {
    let endpoint = "/object/accounts/{objectId}/available-roles";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<AvailableRolesResponse>({
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
   * @param {bigint | string} objectId - AccountId of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<AccountRolesReport>>} A promise containing the HttpResponse of AccountRolesReport
   */
  async getAccountRoleReportByObjectId(objectId: bigint | string, gamertag?: string): Promise<HttpResponse<AccountRolesReport>> {
    let endpoint = "/object/accounts/{objectId}/role/report";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<AccountRolesReport>({
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
   * @param {UpdateRole} payload - The `UpdateRole` instance to use for the API request
   * @param {bigint | string} objectId - AccountId of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async putAccountRoleByObjectId(objectId: bigint | string, payload: UpdateRole, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let endpoint = "/object/accounts/{objectId}/role";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyResponse, UpdateRole>({
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
   * @param {DeleteRole} payload - The `DeleteRole` instance to use for the API request
   * @param {bigint | string} objectId - AccountId of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async deleteAccountRoleByObjectId(objectId: bigint | string, payload: DeleteRole, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let endpoint = "/object/accounts/{objectId}/role";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyResponse, DeleteRole>({
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
   * @param {UpdateRole} payload - The `UpdateRole` instance to use for the API request
   * @param {bigint | string} objectId - AccountId of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async putAccountAdminScopeByObjectId(objectId: bigint | string, payload: UpdateRole, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let endpoint = "/object/accounts/{objectId}/admin/scope";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyResponse, UpdateRole>({
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
   * @param {DeleteRole} payload - The `DeleteRole` instance to use for the API request
   * @param {bigint | string} objectId - AccountId of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async deleteAccountAdminScopeByObjectId(objectId: bigint | string, payload: DeleteRole, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let endpoint = "/object/accounts/{objectId}/admin/scope";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyResponse, DeleteRole>({
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
   * @param {TransferThirdPartyAssociation} payload - The `TransferThirdPartyAssociation` instance to use for the API request
   * @param {bigint | string} objectId - AccountId of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async putAccountAdminThirdPartyByObjectId(objectId: bigint | string, payload: TransferThirdPartyAssociation, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let endpoint = "/object/accounts/{objectId}/admin/third-party";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyResponse, TransferThirdPartyAssociation>({
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
   * @param {DeleteThirdPartyAssociation} payload - The `DeleteThirdPartyAssociation` instance to use for the API request
   * @param {bigint | string} objectId - AccountId of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async deleteAccountAdminThirdPartyByObjectId(objectId: bigint | string, payload: DeleteThirdPartyAssociation, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let endpoint = "/object/accounts/{objectId}/admin/third-party";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyResponse, DeleteThirdPartyAssociation>({
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
   * @param {AccountUpdate} payload - The `AccountUpdate` instance to use for the API request
   * @param {bigint | string} objectId - AccountId of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<Account>>} A promise containing the HttpResponse of Account
   */
  async putAccountByObjectId(objectId: bigint | string, payload: AccountUpdate, gamertag?: string): Promise<HttpResponse<Account>> {
    let endpoint = "/object/accounts/{objectId}/";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<Account, AccountUpdate>({
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
   * @param {bigint | string} objectId - AccountId of the player.Underlying objectId type is integer in format int64.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<Account>>} A promise containing the HttpResponse of Account
   */
  async deleteAccountAdminForgetByObjectId(objectId: bigint | string, gamertag?: string): Promise<HttpResponse<Account>> {
    let endpoint = "/object/accounts/{objectId}/admin/forget";
    endpoint = endpoint.replace("{objectId}", encodeURIComponent(objectId.toString()));
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<Account>({
      url: endpoint,
      method: HttpMethod.DELETE,
      headers,
      withAuth: true
    });
  }
}
