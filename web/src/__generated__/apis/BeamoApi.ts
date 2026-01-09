/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { DELETE } from '@/constants';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { manifestIdPlaceholder } from '@/__generated__/apis/constants';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import { queryIdPlaceholder } from '@/__generated__/apis/constants';
import { serviceNamePlaceholder } from '@/__generated__/apis/constants';
import { storageObjectNamePlaceholder } from '@/__generated__/apis/constants';
import type { BeamoBasicGetManifestsResponse } from '@/__generated__/schemas/BeamoBasicGetManifestsResponse';
import type { BeamoBasicManifestChecksums } from '@/__generated__/schemas/BeamoBasicManifestChecksums';
import type { BeamoV2ApiBeamoServicesLogsQueryDeleteBeamoResponse } from '@/__generated__/schemas/BeamoV2ApiBeamoServicesLogsQueryDeleteBeamoResponse';
import type { BeamoV2ConnectionStringResponse } from '@/__generated__/schemas/BeamoV2ConnectionStringResponse';
import type { BeamoV2DeleteRegistrationRequest } from '@/__generated__/schemas/BeamoV2DeleteRegistrationRequest';
import type { BeamoV2EmptyMessage } from '@/__generated__/schemas/BeamoV2EmptyMessage';
import type { BeamoV2FederationRegistrationResponse } from '@/__generated__/schemas/BeamoV2FederationRegistrationResponse';
import type { BeamoV2GetManifestsResponse } from '@/__generated__/schemas/BeamoV2GetManifestsResponse';
import type { BeamoV2GetMetricsRequest } from '@/__generated__/schemas/BeamoV2GetMetricsRequest';
import type { BeamoV2GetServiceSecretResponse } from '@/__generated__/schemas/BeamoV2GetServiceSecretResponse';
import type { BeamoV2GetStatusResponse } from '@/__generated__/schemas/BeamoV2GetStatusResponse';
import type { BeamoV2GetTemplatesResponse } from '@/__generated__/schemas/BeamoV2GetTemplatesResponse';
import type { BeamoV2Manifest } from '@/__generated__/schemas/BeamoV2Manifest';
import type { BeamoV2ManifestChecksum } from '@/__generated__/schemas/BeamoV2ManifestChecksum';
import type { BeamoV2PostManifestRequest } from '@/__generated__/schemas/BeamoV2PostManifestRequest';
import type { BeamoV2PromoteBeamoManifestRequest } from '@/__generated__/schemas/BeamoV2PromoteBeamoManifestRequest';
import type { BeamoV2QueryResponse } from '@/__generated__/schemas/BeamoV2QueryResponse';
import type { BeamoV2ServiceRegistrationQuery } from '@/__generated__/schemas/BeamoV2ServiceRegistrationQuery';
import type { BeamoV2ServiceRegistrationRequest } from '@/__generated__/schemas/BeamoV2ServiceRegistrationRequest';
import type { BeamoV2ServiceRegistrationResponse } from '@/__generated__/schemas/BeamoV2ServiceRegistrationResponse';
import type { BeamoV2SignedRequest } from '@/__generated__/schemas/BeamoV2SignedRequest';
import type { BeamoV2StartServiceLogsRequest } from '@/__generated__/schemas/BeamoV2StartServiceLogsRequest';
import type { BeamoV2StoragePerformance } from '@/__generated__/schemas/BeamoV2StoragePerformance';
import type { BeamoV2UriResponse } from '@/__generated__/schemas/BeamoV2UriResponse';
import type { CommitImageRequest } from '@/__generated__/schemas/CommitImageRequest';
import type { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import type { ConnectionString } from '@/__generated__/schemas/ConnectionString';
import type { EmptyResponse } from '@/__generated__/schemas/EmptyResponse';
import type { GetCurrentManifestResponse } from '@/__generated__/schemas/GetCurrentManifestResponse';
import type { GetElasticContainerRegistryURI } from '@/__generated__/schemas/GetElasticContainerRegistryURI';
import type { GetLambdaURI } from '@/__generated__/schemas/GetLambdaURI';
import type { GetLogsInsightUrlRequest } from '@/__generated__/schemas/GetLogsInsightUrlRequest';
import type { GetLogsUrlRequest } from '@/__generated__/schemas/GetLogsUrlRequest';
import type { GetManifestResponse } from '@/__generated__/schemas/GetManifestResponse';
import type { GetMetricsUrlRequest } from '@/__generated__/schemas/GetMetricsUrlRequest';
import type { GetServiceURLsRequest } from '@/__generated__/schemas/GetServiceURLsRequest';
import type { GetSignedUrlResponse } from '@/__generated__/schemas/GetSignedUrlResponse';
import type { GetStatusResponse } from '@/__generated__/schemas/GetStatusResponse';
import type { GetTemplatesResponse } from '@/__generated__/schemas/GetTemplatesResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { LambdaResponse } from '@/__generated__/schemas/LambdaResponse';
import type { MicroserviceRegistrationRequest } from '@/__generated__/schemas/MicroserviceRegistrationRequest';
import type { MicroserviceRegistrationsQuery } from '@/__generated__/schemas/MicroserviceRegistrationsQuery';
import type { MicroserviceRegistrationsResponse } from '@/__generated__/schemas/MicroserviceRegistrationsResponse';
import type { MicroserviceSecretResponse } from '@/__generated__/schemas/MicroserviceSecretResponse';
import type { PerformanceResponse } from '@/__generated__/schemas/PerformanceResponse';
import type { PostManifestRequest } from '@/__generated__/schemas/PostManifestRequest';
import type { PostManifestResponse } from '@/__generated__/schemas/PostManifestResponse';
import type { PreSignedUrlsResponse } from '@/__generated__/schemas/PreSignedUrlsResponse';
import type { PullBeamoManifestRequest } from '@/__generated__/schemas/PullBeamoManifestRequest';
import type { Query } from '@/__generated__/schemas/Query';
import type { SupportedFederationsResponse } from '@/__generated__/schemas/SupportedFederationsResponse';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `BeamoV2PostManifestRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoPostManifests(requester: HttpRequester, payload: BeamoV2PostManifestRequest, gamertag?: string): Promise<HttpResponse<BeamoV2ManifestChecksum>> {
  let endpoint = "/api/beamo/manifests";
  
  // Make the API request
  return makeApiRequest<BeamoV2ManifestChecksum, BeamoV2PostManifestRequest>({
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
 * @param archived - The `archived` parameter to include in the API request.
 * @param limit - The `limit` parameter to include in the API request.
 * @param offset - The `offset` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoGetManifests(requester: HttpRequester, archived?: boolean, limit?: number, offset?: number, gamertag?: string): Promise<HttpResponse<BeamoV2GetManifestsResponse>> {
  let endpoint = "/api/beamo/manifests";
  
  // Make the API request
  return makeApiRequest<BeamoV2GetManifestsResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      archived,
      limit,
      offset
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
 * @param manifestId - The `manifestId` parameter to include in the API request.
 * @param archived - The `archived` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoGetManifestsByManifestId(requester: HttpRequester, manifestId: string, archived?: boolean, gamertag?: string): Promise<HttpResponse<BeamoV2Manifest>> {
  let endpoint = "/api/beamo/manifests/{manifestId}".replace(manifestIdPlaceholder, endpointEncoder(manifestId));
  
  // Make the API request
  return makeApiRequest<BeamoV2Manifest>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      archived
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
 * @param archived - The `archived` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoGetManifestsCurrent(requester: HttpRequester, archived?: boolean, gamertag?: string): Promise<HttpResponse<BeamoV2Manifest>> {
  let endpoint = "/api/beamo/manifests/current";
  
  // Make the API request
  return makeApiRequest<BeamoV2Manifest>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      archived
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
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoPostManifestsCurrent(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<BeamoV2EmptyMessage>> {
  let endpoint = "/api/beamo/manifests/current";
  
  // Make the API request
  return makeApiRequest<BeamoV2EmptyMessage>({
    r: requester,
    e: endpoint,
    m: POST,
    g: gamertag,
    w: true
  });
}

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @deprecated
 * This API method is deprecated and may be removed in future versions.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `BeamoV2PromoteBeamoManifestRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoPostManifestsPromote(requester: HttpRequester, payload: BeamoV2PromoteBeamoManifestRequest, gamertag?: string): Promise<HttpResponse<BeamoV2EmptyMessage>> {
  let endpoint = "/api/beamo/manifests/promote";
  
  // Make the API request
  return makeApiRequest<BeamoV2EmptyMessage, BeamoV2PromoteBeamoManifestRequest>({
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
export async function beamoGetTemplates(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<BeamoV2GetTemplatesResponse>> {
  let endpoint = "/api/beamo/templates";
  
  // Make the API request
  return makeApiRequest<BeamoV2GetTemplatesResponse>({
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
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoGetStatus(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<BeamoV2GetStatusResponse>> {
  let endpoint = "/api/beamo/status";
  
  // Make the API request
  return makeApiRequest<BeamoV2GetStatusResponse>({
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
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoGetRegistryUri(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<BeamoV2UriResponse>> {
  let endpoint = "/api/beamo/registry-uri";
  
  // Make the API request
  return makeApiRequest<BeamoV2UriResponse>({
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
 * @param payload - The `BeamoV2ServiceRegistrationQuery` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoPostServicesRegistrations(requester: HttpRequester, payload: BeamoV2ServiceRegistrationQuery, gamertag?: string): Promise<HttpResponse<BeamoV2ServiceRegistrationResponse>> {
  let endpoint = "/api/beamo/services/registrations";
  
  // Make the API request
  return makeApiRequest<BeamoV2ServiceRegistrationResponse, BeamoV2ServiceRegistrationQuery>({
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
 * @param payload - The `BeamoV2ServiceRegistrationQuery` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoPostServicesFederation(requester: HttpRequester, payload: BeamoV2ServiceRegistrationQuery, gamertag?: string): Promise<HttpResponse<BeamoV2FederationRegistrationResponse>> {
  let endpoint = "/api/beamo/services/federation";
  
  // Make the API request
  return makeApiRequest<BeamoV2FederationRegistrationResponse, BeamoV2ServiceRegistrationQuery>({
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
 * @param payload - The `BeamoV2ServiceRegistrationRequest` instance to use for the API request
 * @param serviceName - The `serviceName` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoPutServicesFederationTrafficByServiceName(requester: HttpRequester, serviceName: string, payload: BeamoV2ServiceRegistrationRequest, gamertag?: string): Promise<HttpResponse<BeamoV2EmptyMessage>> {
  let endpoint = "/api/beamo/services/{serviceName}/federation/traffic".replace(serviceNamePlaceholder, endpointEncoder(serviceName));
  
  // Make the API request
  return makeApiRequest<BeamoV2EmptyMessage, BeamoV2ServiceRegistrationRequest>({
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
 * @param payload - The `BeamoV2DeleteRegistrationRequest` instance to use for the API request
 * @param serviceName - The `serviceName` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoDeleteServicesFederationTrafficByServiceName(requester: HttpRequester, serviceName: string, payload: BeamoV2DeleteRegistrationRequest, gamertag?: string): Promise<HttpResponse<BeamoV2EmptyMessage>> {
  let endpoint = "/api/beamo/services/{serviceName}/federation/traffic".replace(serviceNamePlaceholder, endpointEncoder(serviceName));
  
  // Make the API request
  return makeApiRequest<BeamoV2EmptyMessage, BeamoV2DeleteRegistrationRequest>({
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
 * @param payload - The `BeamoV2GetMetricsRequest` instance to use for the API request
 * @param serviceName - The `serviceName` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoPostServicesMetricsRequestByServiceName(requester: HttpRequester, serviceName: string, payload: BeamoV2GetMetricsRequest, gamertag?: string): Promise<HttpResponse<BeamoV2SignedRequest>> {
  let endpoint = "/api/beamo/services/{serviceName}/metrics-request".replace(serviceNamePlaceholder, endpointEncoder(serviceName));
  
  // Make the API request
  return makeApiRequest<BeamoV2SignedRequest, BeamoV2GetMetricsRequest>({
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
 * @param payload - The `BeamoV2StartServiceLogsRequest` instance to use for the API request
 * @param serviceName - The `serviceName` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoPostServicesLogsQueryByServiceName(requester: HttpRequester, serviceName: string, payload: BeamoV2StartServiceLogsRequest, gamertag?: string): Promise<HttpResponse<BeamoV2QueryResponse>> {
  let endpoint = "/api/beamo/services/{serviceName}/logs/query".replace(serviceNamePlaceholder, endpointEncoder(serviceName));
  
  // Make the API request
  return makeApiRequest<BeamoV2QueryResponse, BeamoV2StartServiceLogsRequest>({
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
 * @param queryId - The `queryId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoDeleteServicesLogsQueryByQueryId(requester: HttpRequester, queryId: string, gamertag?: string): Promise<HttpResponse<BeamoV2ApiBeamoServicesLogsQueryDeleteBeamoResponse>> {
  let endpoint = "/api/beamo/services/logs/query/{queryId}".replace(queryIdPlaceholder, endpointEncoder(queryId));
  
  // Make the API request
  return makeApiRequest<BeamoV2ApiBeamoServicesLogsQueryDeleteBeamoResponse>({
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
 * @param queryId - The `queryId` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoGetServicesLogsQueryByQueryId(requester: HttpRequester, queryId: string, gamertag?: string): Promise<HttpResponse<BeamoV2SignedRequest>> {
  let endpoint = "/api/beamo/services/logs/query/{queryId}".replace(queryIdPlaceholder, endpointEncoder(queryId));
  
  // Make the API request
  return makeApiRequest<BeamoV2SignedRequest>({
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
 * @deprecated
 * This API method is deprecated and may be removed in future versions.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoGetServicesSecret(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<BeamoV2GetServiceSecretResponse>> {
  let endpoint = "/api/beamo/services/secret";
  
  // Make the API request
  return makeApiRequest<BeamoV2GetServiceSecretResponse>({
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
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoGetStorageConnection(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<BeamoV2ConnectionStringResponse>> {
  let endpoint = "/api/beamo/storage/connection";
  
  // Make the API request
  return makeApiRequest<BeamoV2ConnectionStringResponse>({
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
 * @param storageObjectName - The `storageObjectName` parameter to include in the API request.
 * @param EndTime - The `EndTime` parameter to include in the API request.
 * @param Granularity - The `Granularity` parameter to include in the API request.
 * @param Period - The `Period` parameter to include in the API request.
 * @param StartTime - The `StartTime` parameter to include in the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function beamoGetStoragePerformanceByStorageObjectName(requester: HttpRequester, storageObjectName: string, EndTime?: Date, Granularity?: string, Period?: string, StartTime?: Date, gamertag?: string): Promise<HttpResponse<BeamoV2StoragePerformance>> {
  let endpoint = "/api/beamo/storage/{storageObjectName}/performance".replace(storageObjectNamePlaceholder, endpointEncoder(storageObjectName));
  
  // Make the API request
  return makeApiRequest<BeamoV2StoragePerformance>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      EndTime,
      Granularity,
      Period,
      StartTime
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
 * @param payload - The `MicroserviceRegistrationsQuery` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function beamoPostMicroserviceRegistrationsBasic(requester: HttpRequester, payload: MicroserviceRegistrationsQuery, gamertag?: string): Promise<HttpResponse<MicroserviceRegistrationsResponse>> {
  let endpoint = "/basic/beamo/microservice/registrations";
  
  // Make the API request
  return makeApiRequest<MicroserviceRegistrationsResponse, MicroserviceRegistrationsQuery>({
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
 * @param payload - The `MicroserviceRegistrationRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function beamoPutMicroserviceFederationTrafficBasic(requester: HttpRequester, payload: MicroserviceRegistrationRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/basic/beamo/microservice/federation/traffic";
  
  // Make the API request
  return makeApiRequest<CommonResponse, MicroserviceRegistrationRequest>({
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
 * @param payload - The `MicroserviceRegistrationRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function beamoDeleteMicroserviceFederationTrafficBasic(requester: HttpRequester, payload: MicroserviceRegistrationRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/basic/beamo/microservice/federation/traffic";
  
  // Make the API request
  return makeApiRequest<CommonResponse, MicroserviceRegistrationRequest>({
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
 * @param payload - The `GetServiceURLsRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function beamoPostImageUrlsBasic(requester: HttpRequester, payload: GetServiceURLsRequest, gamertag?: string): Promise<HttpResponse<PreSignedUrlsResponse>> {
  let endpoint = "/basic/beamo/image/urls";
  
  // Make the API request
  return makeApiRequest<PreSignedUrlsResponse, GetServiceURLsRequest>({
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
 * @param payload - The `GetMetricsUrlRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function beamoPostMetricsUrlBasic(requester: HttpRequester, payload: GetMetricsUrlRequest, gamertag?: string): Promise<HttpResponse<GetSignedUrlResponse>> {
  let endpoint = "/basic/beamo/metricsUrl";
  
  // Make the API request
  return makeApiRequest<GetSignedUrlResponse, GetMetricsUrlRequest>({
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
export async function beamoGetMicroserviceSecretBasic(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<MicroserviceSecretResponse>> {
  let endpoint = "/basic/beamo/microservice/secret";
  
  // Make the API request
  return makeApiRequest<MicroserviceSecretResponse>({
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
 * @param payload - The `Query` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function beamoPostQueryLogsResultBasic(requester: HttpRequester, payload: Query, gamertag?: string): Promise<HttpResponse<GetSignedUrlResponse>> {
  let endpoint = "/basic/beamo/queryLogs/result";
  
  // Make the API request
  return makeApiRequest<GetSignedUrlResponse, Query>({
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
 * @param granularity - The `granularity` parameter to include in the API request.
 * @param storageObjectName - The `storageObjectName` parameter to include in the API request.
 * @param endDate - The `endDate` parameter to include in the API request.
 * @param period - The `period` parameter to include in the API request.
 * @param startDate - The `startDate` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function beamoGetStoragePerformanceBasic(requester: HttpRequester, granularity: string, storageObjectName: string, endDate?: string, period?: string, startDate?: string, gamertag?: string): Promise<HttpResponse<PerformanceResponse>> {
  let endpoint = "/basic/beamo/storage/performance";
  
  // Make the API request
  return makeApiRequest<PerformanceResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      granularity,
      storageObjectName,
      endDate,
      period,
      startDate
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
 * @param archived - The `archived` parameter to include in the API request.
 * @param limit - The `limit` parameter to include in the API request.
 * @param offset - The `offset` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function beamoGetManifestsBasic(requester: HttpRequester, archived?: boolean, limit?: number, offset?: number, gamertag?: string): Promise<HttpResponse<BeamoBasicGetManifestsResponse>> {
  let endpoint = "/basic/beamo/manifests";
  
  // Make the API request
  return makeApiRequest<BeamoBasicGetManifestsResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      archived,
      limit,
      offset
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
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function beamoGetTemplatesBasic(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<GetTemplatesResponse>> {
  let endpoint = "/basic/beamo/templates";
  
  // Make the API request
  return makeApiRequest<GetTemplatesResponse>({
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
 * @param payload - The `GetLogsInsightUrlRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function beamoPostQueryLogsBasic(requester: HttpRequester, payload: GetLogsInsightUrlRequest, gamertag?: string): Promise<HttpResponse<Query>> {
  let endpoint = "/basic/beamo/queryLogs";
  
  // Make the API request
  return makeApiRequest<Query, GetLogsInsightUrlRequest>({
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
 * @param payload - The `Query` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function beamoDeleteQueryLogsBasic(requester: HttpRequester, payload: Query, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/basic/beamo/queryLogs";
  
  // Make the API request
  return makeApiRequest<CommonResponse, Query>({
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
 * @param payload - The `GetLogsUrlRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function beamoPostLogsUrlBasic(requester: HttpRequester, payload: GetLogsUrlRequest, gamertag?: string): Promise<HttpResponse<GetSignedUrlResponse>> {
  let endpoint = "/basic/beamo/logsUrl";
  
  // Make the API request
  return makeApiRequest<GetSignedUrlResponse, GetLogsUrlRequest>({
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
 * @param payload - The `CommitImageRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function beamoPutImageCommitBasic(requester: HttpRequester, payload: CommitImageRequest, gamertag?: string): Promise<HttpResponse<LambdaResponse>> {
  let endpoint = "/basic/beamo/image/commit";
  
  // Make the API request
  return makeApiRequest<LambdaResponse, CommitImageRequest>({
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
export async function beamoGetUploadAPIBasic(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<GetLambdaURI>> {
  let endpoint = "/basic/beamo/uploadAPI";
  
  // Make the API request
  return makeApiRequest<GetLambdaURI>({
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
export async function beamoGetStatusBasic(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<GetStatusResponse>> {
  let endpoint = "/basic/beamo/status";
  
  // Make the API request
  return makeApiRequest<GetStatusResponse>({
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
 * @param archived - The `archived` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function beamoGetManifestCurrentBasic(requester: HttpRequester, archived?: boolean, gamertag?: string): Promise<HttpResponse<GetCurrentManifestResponse>> {
  let endpoint = "/basic/beamo/manifest/current";
  
  // Make the API request
  return makeApiRequest<GetCurrentManifestResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      archived
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
 * @param payload - The `PullBeamoManifestRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function beamoPostManifestPullBasic(requester: HttpRequester, payload: PullBeamoManifestRequest, gamertag?: string): Promise<HttpResponse<BeamoBasicManifestChecksums>> {
  let endpoint = "/basic/beamo/manifest/pull";
  
  // Make the API request
  return makeApiRequest<BeamoBasicManifestChecksums, PullBeamoManifestRequest>({
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
export async function beamoGetRegistryBasic(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<GetElasticContainerRegistryURI>> {
  let endpoint = "/basic/beamo/registry";
  
  // Make the API request
  return makeApiRequest<GetElasticContainerRegistryURI>({
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
export async function beamoPostManifestDeployBasic(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/basic/beamo/manifest/deploy";
  
  // Make the API request
  return makeApiRequest<EmptyResponse>({
    r: requester,
    e: endpoint,
    m: POST,
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
 * @param payload - The `MicroserviceRegistrationsQuery` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function beamoPostMicroserviceFederationBasic(requester: HttpRequester, payload: MicroserviceRegistrationsQuery, gamertag?: string): Promise<HttpResponse<SupportedFederationsResponse>> {
  let endpoint = "/basic/beamo/microservice/federation";
  
  // Make the API request
  return makeApiRequest<SupportedFederationsResponse, MicroserviceRegistrationsQuery>({
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
export async function beamoGetStorageConnectionBasic(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<ConnectionString>> {
  let endpoint = "/basic/beamo/storage/connection";
  
  // Make the API request
  return makeApiRequest<ConnectionString>({
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
 * @param id - The `id` parameter to include in the API request.
 * @param archived - The `archived` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function beamoGetManifestBasic(requester: HttpRequester, id: string, archived?: boolean, gamertag?: string): Promise<HttpResponse<GetManifestResponse>> {
  let endpoint = "/basic/beamo/manifest";
  
  // Make the API request
  return makeApiRequest<GetManifestResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      id,
      archived
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
 * @param payload - The `PostManifestRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function beamoPostManifestBasic(requester: HttpRequester, payload: PostManifestRequest, gamertag?: string): Promise<HttpResponse<PostManifestResponse>> {
  let endpoint = "/basic/beamo/manifest";
  
  // Make the API request
  return makeApiRequest<PostManifestResponse, PostManifestRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}
