/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { aliasPlaceholder } from '@/__generated__/apis/constants';
import { customerIdPlaceholder } from '@/__generated__/apis/constants';
import { DELETE } from '@/constants';
import { destinationRealmIdPlaceholder } from '@/__generated__/apis/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { gameIdPlaceholder } from '@/__generated__/apis/constants';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { PATCH } from '@/constants';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import { realmIdPlaceholder } from '@/__generated__/apis/constants';
import type { ApiCustomersActivatePutCustomerResponse } from '@/__generated__/schemas/ApiCustomersActivatePutCustomerResponse';
import type { CreateRealmRequest } from '@/__generated__/schemas/CreateRealmRequest';
import type { CustomerActorAliasAvailableResponse } from '@/__generated__/schemas/CustomerActorAliasAvailableResponse';
import type { CustomerActorCustomer } from '@/__generated__/schemas/CustomerActorCustomer';
import type { CustomerActorCustomersResponse } from '@/__generated__/schemas/CustomerActorCustomersResponse';
import type { CustomerActorCustomerView } from '@/__generated__/schemas/CustomerActorCustomerView';
import type { CustomerActorNewCustomerRequest } from '@/__generated__/schemas/CustomerActorNewCustomerRequest';
import type { CustomerActorNewCustomerResponse } from '@/__generated__/schemas/CustomerActorNewCustomerResponse';
import type { CustomerActorNewGameRequest } from '@/__generated__/schemas/CustomerActorNewGameRequest';
import type { CustomerActorPromoteRealmRequest } from '@/__generated__/schemas/CustomerActorPromoteRealmRequest';
import type { CustomerActorPromoteRealmResponse } from '@/__generated__/schemas/CustomerActorPromoteRealmResponse';
import type { CustomerActorRealmConfigResponse } from '@/__generated__/schemas/CustomerActorRealmConfigResponse';
import type { CustomerActorRealmConfigSaveRequest } from '@/__generated__/schemas/CustomerActorRealmConfigSaveRequest';
import type { CustomerActorRealmConfiguration } from '@/__generated__/schemas/CustomerActorRealmConfiguration';
import type { CustomerActorUpdateGameHierarchyRequest } from '@/__generated__/schemas/CustomerActorUpdateGameHierarchyRequest';
import type { EmptyMessage } from '@/__generated__/schemas/EmptyMessage';
import type { GetGamesResponse } from '@/__generated__/schemas/GetGamesResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { RealmConfigChangeRequest } from '@/__generated__/schemas/RealmConfigChangeRequest';
import type { RealmView } from '@/__generated__/schemas/RealmView';
import type { RenameRealmRequest } from '@/__generated__/schemas/RenameRealmRequest';
import type { StripeSubscriptionResponse } from '@/__generated__/schemas/StripeSubscriptionResponse';
import type { UpdateRealmRequest } from '@/__generated__/schemas/UpdateRealmRequest';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `CustomerActorNewCustomerRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPost(requester: HttpRequester, payload: CustomerActorNewCustomerRequest, gamertag?: string): Promise<HttpResponse<CustomerActorNewCustomerResponse>> {
  let endpoint = "/api/customers";
  
  // Make the API request
  return makeApiRequest<CustomerActorNewCustomerResponse, CustomerActorNewCustomerRequest>({
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
 * @param showHiddenRealms - Whether to include hidden realms in the response.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGet(requester: HttpRequester, showHiddenRealms?: boolean, gamertag?: string): Promise<HttpResponse<CustomerActorCustomersResponse>> {
  let endpoint = "/api/customers";
  
  // Make the API request
  return makeApiRequest<CustomerActorCustomersResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      showHiddenRealms
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
 * @param payload - The `CustomerActorNewCustomerRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostVerify(requester: HttpRequester, payload: CustomerActorNewCustomerRequest, gamertag?: string): Promise<HttpResponse<CustomerActorNewCustomerResponse>> {
  let endpoint = "/api/customers/verify";
  
  // Make the API request
  return makeApiRequest<CustomerActorNewCustomerResponse, CustomerActorNewCustomerRequest>({
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
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPutActivate(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<ApiCustomersActivatePutCustomerResponse>> {
  let endpoint = "/api/customers/activate";
  
  // Make the API request
  return makeApiRequest<ApiCustomersActivatePutCustomerResponse>({
    r: requester,
    e: endpoint,
    m: PUT,
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
 * @param customerId - The customer ID to look up.
 * @param showHiddenRealms - Whether to include hidden realms in the response.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetByCustomerId(requester: HttpRequester, customerId: string, showHiddenRealms?: boolean, gamertag?: string): Promise<HttpResponse<CustomerActorCustomerView>> {
  let endpoint = "/api/customers/{customerId}".replace(customerIdPlaceholder, endpointEncoder(customerId));
  
  // Make the API request
  return makeApiRequest<CustomerActorCustomerView>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      showHiddenRealms
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
 * @param customerId - The customer ID to look up.
 * @param showHiddenRealms - Whether to include hidden realms in the response.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetAdminViewByCustomerId(requester: HttpRequester, customerId: string, showHiddenRealms?: boolean, gamertag?: string): Promise<HttpResponse<CustomerActorCustomer>> {
  let endpoint = "/api/customers/{customerId}/admin-view".replace(customerIdPlaceholder, endpointEncoder(customerId));
  
  // Make the API request
  return makeApiRequest<CustomerActorCustomer>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      showHiddenRealms
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
 * @param customerId - ID of the customer.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetStripeSubscriptionByCustomerId(requester: HttpRequester, customerId: string, gamertag?: string): Promise<HttpResponse<StripeSubscriptionResponse>> {
  let endpoint = "/api/customers/{customerId}/stripe/subscription".replace(customerIdPlaceholder, endpointEncoder(customerId));
  
  // Make the API request
  return makeApiRequest<StripeSubscriptionResponse>({
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
 * @param payload - The `UpdateRealmRequest` instance to use for the API request
 * @param customerId - ID of the customer that owns the realm.
 * @param realmId - ID of the realm to update.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPutRealms(requester: HttpRequester, customerId: string, realmId: string, payload: UpdateRealmRequest, gamertag?: string): Promise<HttpResponse<EmptyMessage>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
  // Make the API request
  return makeApiRequest<EmptyMessage, UpdateRealmRequest>({
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
 * @param customerId - ID of the customer.
 * @param realmId - ID of the realm to retrieve.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetRealms(requester: HttpRequester, customerId: string, realmId: string, gamertag?: string): Promise<HttpResponse<RealmView>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
  // Make the API request
  return makeApiRequest<RealmView>({
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
 * @param customerId - ID of the customer that owns the realm.
 * @param realmId - ID of the realm to archive.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersDeleteRealms(requester: HttpRequester, customerId: string, realmId: string, gamertag?: string): Promise<HttpResponse<EmptyMessage>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
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
 * @param customerId - ID of the customer to retrieve config for.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetConfigByCustomerId(requester: HttpRequester, customerId: string, gamertag?: string): Promise<HttpResponse<CustomerActorRealmConfigResponse>> {
  let endpoint = "/api/customers/{customerId}/config".replace(customerIdPlaceholder, endpointEncoder(customerId));
  
  // Make the API request
  return makeApiRequest<CustomerActorRealmConfigResponse>({
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
 * @param customerId - ID of the customer.
 * @param showHiddenRealms - Whether to include hidden realms.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetGamesByCustomerId(requester: HttpRequester, customerId: string, showHiddenRealms?: boolean, gamertag?: string): Promise<HttpResponse<GetGamesResponse>> {
  let endpoint = "/api/customers/{customerId}/games".replace(customerIdPlaceholder, endpointEncoder(customerId));
  
  // Make the API request
  return makeApiRequest<GetGamesResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      showHiddenRealms
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
 * @param payload - The `CustomerActorNewGameRequest` instance to use for the API request
 * @param customerId - ID of the customer to create the game under.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostGamesByCustomerId(requester: HttpRequester, customerId: string, payload: CustomerActorNewGameRequest, gamertag?: string): Promise<HttpResponse<RealmView>> {
  let endpoint = "/api/customers/{customerId}/games".replace(customerIdPlaceholder, endpointEncoder(customerId));
  
  // Make the API request
  return makeApiRequest<RealmView, CustomerActorNewGameRequest>({
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
 * @param customerId - ID of the customer.
 * @param gameId - ID of the game realm to retrieve realms for.
 * @param showHiddenRealms - Whether to include hidden realms.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetGames(requester: HttpRequester, customerId: string, gameId: string, showHiddenRealms?: boolean, gamertag?: string): Promise<HttpResponse<GetGamesResponse>> {
  let endpoint = "/api/customers/{customerId}/games/{gameId}".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(gameIdPlaceholder, endpointEncoder(gameId));
  
  // Make the API request
  return makeApiRequest<GetGamesResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      showHiddenRealms
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
 * @param payload - The `CustomerActorUpdateGameHierarchyRequest` instance to use for the API request
 * @param customerId - ID of the customer.
 * @param gameId - ID of the game realm to update.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPutGames(requester: HttpRequester, customerId: string, gameId: string, payload: CustomerActorUpdateGameHierarchyRequest, gamertag?: string): Promise<HttpResponse<EmptyMessage>> {
  let endpoint = "/api/customers/{customerId}/games/{gameId}".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(gameIdPlaceholder, endpointEncoder(gameId));
  
  // Make the API request
  return makeApiRequest<EmptyMessage, CustomerActorUpdateGameHierarchyRequest>({
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
 * @param payload - The `CreateRealmRequest` instance to use for the API request
 * @param customerId - ID of the customer to create the realm under.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostRealmsByCustomerId(requester: HttpRequester, customerId: string, payload: CreateRealmRequest, gamertag?: string): Promise<HttpResponse<EmptyMessage>> {
  let endpoint = "/api/customers/{customerId}/realms".replace(customerIdPlaceholder, endpointEncoder(customerId));
  
  // Make the API request
  return makeApiRequest<EmptyMessage, CreateRealmRequest>({
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
 * @param payload - The `RenameRealmRequest` instance to use for the API request
 * @param customerId - ID of the customer that owns the realm.
 * @param realmId - ID of the realm to rename.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPutRealmsRename(requester: HttpRequester, customerId: string, realmId: string, payload: RenameRealmRequest, gamertag?: string): Promise<HttpResponse<EmptyMessage>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/rename".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
  // Make the API request
  return makeApiRequest<EmptyMessage, RenameRealmRequest>({
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
 * @param customerId - ID of the customer.
 * @param realmId - ID of the realm to retrieve config for.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetRealmsConfig(requester: HttpRequester, customerId: string, realmId: string, gamertag?: string): Promise<HttpResponse<CustomerActorRealmConfigResponse>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/config".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
  // Make the API request
  return makeApiRequest<CustomerActorRealmConfigResponse>({
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
 * @param payload - The `CustomerActorRealmConfigSaveRequest` instance to use for the API request
 * @param customerId - ID of the customer.
 * @param realmId - ID of the realm to update.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPutRealmsConfig(requester: HttpRequester, customerId: string, realmId: string, payload: CustomerActorRealmConfigSaveRequest, gamertag?: string): Promise<HttpResponse<EmptyMessage>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/config".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
  // Make the API request
  return makeApiRequest<EmptyMessage, CustomerActorRealmConfigSaveRequest>({
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
 * @param payload - The `RealmConfigChangeRequest` instance to use for the API request
 * @param customerId - ID of the customer.
 * @param realmId - ID of the realm to update.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPatchRealmsConfig(requester: HttpRequester, customerId: string, realmId: string, payload: RealmConfigChangeRequest, gamertag?: string): Promise<HttpResponse<EmptyMessage>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/config".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
  // Make the API request
  return makeApiRequest<EmptyMessage, RealmConfigChangeRequest>({
    r: requester,
    e: endpoint,
    m: PATCH,
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
 * @param customerId - ID of the customer.
 * @param realmId - ID of the realm.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetRealmsClientDefaults(requester: HttpRequester, customerId: string, realmId: string, gamertag?: string): Promise<HttpResponse<CustomerActorRealmConfiguration>> {
  let endpoint = "/api/customers/{customerId}/realms/{realmId}/client-defaults".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(realmIdPlaceholder, endpointEncoder(realmId));
  
  // Make the API request
  return makeApiRequest<CustomerActorRealmConfiguration>({
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
 * @param payload - The `CustomerActorPromoteRealmRequest` instance to use for the API request
 * @param customerId - ID of the customer.
 * @param destinationRealmId - ID of the realm to promote content into.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersPostRealmsPromotion(requester: HttpRequester, customerId: string, destinationRealmId: string, payload: CustomerActorPromoteRealmRequest, gamertag?: string): Promise<HttpResponse<CustomerActorPromoteRealmResponse>> {
  let endpoint = "/api/customers/{customerId}/realms/{destinationRealmId}/promotion".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(destinationRealmIdPlaceholder, endpointEncoder(destinationRealmId));
  
  // Make the API request
  return makeApiRequest<CustomerActorPromoteRealmResponse, CustomerActorPromoteRealmRequest>({
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
 * @param customerId - ID of the customer.
 * @param destinationRealmId - ID of the destination realm.
 * @param contentIds - Comma-separated list of content IDs to filter by.
 * @param promotables - Comma-separated list of promotable types to include.
 * @param sourceRealmId - ID of the source realm.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetRealmsPromotion(requester: HttpRequester, customerId: string, destinationRealmId: string, contentIds?: string, promotables?: string, sourceRealmId?: string, gamertag?: string): Promise<HttpResponse<CustomerActorPromoteRealmResponse>> {
  let endpoint = "/api/customers/{customerId}/realms/{destinationRealmId}/promotion".replace(customerIdPlaceholder, endpointEncoder(customerId)).replace(destinationRealmIdPlaceholder, endpointEncoder(destinationRealmId));
  
  // Make the API request
  return makeApiRequest<CustomerActorPromoteRealmResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      contentIds,
      promotables,
      sourceRealmId
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
 * @param alias - The alias to check.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function customersGetAliasesByAlias(requester: HttpRequester, alias: string, gamertag?: string): Promise<HttpResponse<CustomerActorAliasAvailableResponse>> {
  let endpoint = "/api/customers/aliases/{alias}".replace(aliasPlaceholder, endpointEncoder(alias));
  
  // Make the API request
  return makeApiRequest<CustomerActorAliasAvailableResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    g: gamertag,
    w: true
  });
}
