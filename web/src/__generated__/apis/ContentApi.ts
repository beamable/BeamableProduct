import { ArchiveOrUnarchiveManifestsRequest } from '@/__generated__/schemas/ArchiveOrUnarchiveManifestsRequest';
import { ClientManifestJsonResponse } from '@/__generated__/schemas/ClientManifestJsonResponse';
import { ClientManifestResponse } from '@/__generated__/schemas/ClientManifestResponse';
import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { ContentBasicGetManifestsResponse } from '@/__generated__/schemas/ContentBasicGetManifestsResponse';
import { ContentBasicManifest } from '@/__generated__/schemas/ContentBasicManifest';
import { ContentBasicManifestChecksum } from '@/__generated__/schemas/ContentBasicManifestChecksum';
import { ContentBasicManifestChecksums } from '@/__generated__/schemas/ContentBasicManifestChecksums';
import { ContentOrText } from '@/__generated__/schemas/ContentOrText';
import { DELETE } from '@/constants';
import { DeleteLocalizationRequest } from '@/__generated__/schemas/DeleteLocalizationRequest';
import { EmptyResponse } from '@/__generated__/schemas/EmptyResponse';
import { GET } from '@/constants';
import { GetLocalizationsResponse } from '@/__generated__/schemas/GetLocalizationsResponse';
import { GetManifestHistoryResponse } from '@/__generated__/schemas/GetManifestHistoryResponse';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpResponse } from '@/network/http/types/HttpResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import { PullAllManifestsRequest } from '@/__generated__/schemas/PullAllManifestsRequest';
import { PullManifestRequest } from '@/__generated__/schemas/PullManifestRequest';
import { PUT } from '@/constants';
import { PutLocalizationsRequest } from '@/__generated__/schemas/PutLocalizationsRequest';
import { RepeatManifestRequest } from '@/__generated__/schemas/RepeatManifestRequest';
import { SaveBinaryRequest } from '@/__generated__/schemas/SaveBinaryRequest';
import { SaveBinaryResponse } from '@/__generated__/schemas/SaveBinaryResponse';
import { SaveContentRequest } from '@/__generated__/schemas/SaveContentRequest';
import { SaveContentResponse } from '@/__generated__/schemas/SaveContentResponse';
import { SaveManifestRequest } from '@/__generated__/schemas/SaveManifestRequest';
import { SaveTextRequest } from '@/__generated__/schemas/SaveTextRequest';
import { SaveTextResponse } from '@/__generated__/schemas/SaveTextResponse';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `ArchiveOrUnarchiveManifestsRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postContentManifestsUnarchive(requester: HttpRequester, payload: ArchiveOrUnarchiveManifestsRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/basic/content/manifests/unarchive";
  
  // Make the API request
  return makeApiRequest<EmptyResponse, ArchiveOrUnarchiveManifestsRequest>({
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
 * @param payload - The `PullManifestRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postContentManifestPull(requester: HttpRequester, payload: PullManifestRequest, gamertag?: string): Promise<HttpResponse<ContentBasicManifest>> {
  let endpoint = "/basic/content/manifest/pull";
  
  // Make the API request
  return makeApiRequest<ContentBasicManifest, PullManifestRequest>({
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
 * @param id - The `id` parameter to include in the API request.
 * @param limit - The `limit` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getContentManifestHistory(requester: HttpRequester, id?: string, limit?: number, gamertag?: string): Promise<HttpResponse<GetManifestHistoryResponse>> {
  let endpoint = "/basic/content/manifest/history";
  
  // Make the API request
  return makeApiRequest<GetManifestHistoryResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      id,
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
 * @param payload - The `SaveBinaryRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postContentBinary(requester: HttpRequester, payload: SaveBinaryRequest, gamertag?: string): Promise<HttpResponse<SaveBinaryResponse>> {
  let endpoint = "/basic/content/binary";
  
  // Make the API request
  return makeApiRequest<SaveBinaryResponse, SaveBinaryRequest>({
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
 * @param payload - The `PullAllManifestsRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postContentManifestsPull(requester: HttpRequester, payload: PullAllManifestsRequest, gamertag?: string): Promise<HttpResponse<ContentBasicManifestChecksums>> {
  let endpoint = "/basic/content/manifests/pull";
  
  // Make the API request
  return makeApiRequest<ContentBasicManifestChecksums, PullAllManifestsRequest>({
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
 * @param contentId - The `contentId` parameter to include in the API request.
 * @param version - The `version` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getContent(requester: HttpRequester, contentId: string, version: string, gamertag?: string): Promise<HttpResponse<ContentOrText>> {
  let endpoint = "/basic/content/content";
  
  // Make the API request
  return makeApiRequest<ContentOrText>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      contentId,
      version
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
export async function getContentLocalizations(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<GetLocalizationsResponse>> {
  let endpoint = "/basic/content/localizations";
  
  // Make the API request
  return makeApiRequest<GetLocalizationsResponse>({
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
 * @param payload - The `PutLocalizationsRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function putContentLocalizations(requester: HttpRequester, payload: PutLocalizationsRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/basic/content/localizations";
  
  // Make the API request
  return makeApiRequest<CommonResponse, PutLocalizationsRequest>({
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
 * @param payload - The `DeleteLocalizationRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function deleteContentLocalizations(requester: HttpRequester, payload: DeleteLocalizationRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/basic/content/localizations";
  
  // Make the API request
  return makeApiRequest<CommonResponse, DeleteLocalizationRequest>({
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
 * @param payload - The `SaveTextRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postContentText(requester: HttpRequester, payload: SaveTextRequest, gamertag?: string): Promise<HttpResponse<SaveTextResponse>> {
  let endpoint = "/basic/content/text";
  
  // Make the API request
  return makeApiRequest<SaveTextResponse, SaveTextRequest>({
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
 * @param uid - The `uid` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getContentManifestExact(requester: HttpRequester, uid: string, gamertag?: string): Promise<HttpResponse<ContentBasicManifest>> {
  let endpoint = "/basic/content/manifest/exact";
  
  // Make the API request
  return makeApiRequest<ContentBasicManifest>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      uid
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
 * @param id - ID of the content manifest
 * @param uid - UID of the content manifest
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getContentManifest(requester: HttpRequester, id?: string, uid?: string, gamertag?: string): Promise<HttpResponse<ContentBasicManifest>> {
  let endpoint = "/basic/content/manifest";
  
  // Make the API request
  return makeApiRequest<ContentBasicManifest>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      id,
      uid
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
 * @param payload - The `SaveManifestRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postContentManifest(requester: HttpRequester, payload: SaveManifestRequest, gamertag?: string): Promise<HttpResponse<ContentBasicManifest>> {
  let endpoint = "/basic/content/manifest";
  
  // Make the API request
  return makeApiRequest<ContentBasicManifest, SaveManifestRequest>({
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
 * @param payload - The `ArchiveOrUnarchiveManifestsRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postContentManifestsArchive(requester: HttpRequester, payload: ArchiveOrUnarchiveManifestsRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/basic/content/manifests/archive";
  
  // Make the API request
  return makeApiRequest<EmptyResponse, ArchiveOrUnarchiveManifestsRequest>({
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
 * @param payload - The `SaveContentRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postContent(requester: HttpRequester, payload: SaveContentRequest, gamertag?: string): Promise<HttpResponse<SaveContentResponse>> {
  let endpoint = "/basic/content/";
  
  // Make the API request
  return makeApiRequest<SaveContentResponse, SaveContentRequest>({
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
 * @param id - ID of the content manifest
 * @param uid - UID of the content manifest
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getContentManifestPublic(requester: HttpRequester, id?: string, uid?: string, gamertag?: string): Promise<HttpResponse<ClientManifestResponse>> {
  let endpoint = "/basic/content/manifest/public";
  
  // Make the API request
  return makeApiRequest<ClientManifestResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      id,
      uid
    },
    g: gamertag
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param id - Content ID of the content manifest
 * @param uid - UID of the content manifest
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getContentManifestPublicJson(requester: HttpRequester, id?: string, uid?: string, gamertag?: string): Promise<HttpResponse<ClientManifestJsonResponse>> {
  let endpoint = "/basic/content/manifest/public/json";
  
  // Make the API request
  return makeApiRequest<ClientManifestJsonResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      id,
      uid
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
 * @param payload - The `RepeatManifestRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function putContentManifestRepeat(requester: HttpRequester, payload: RepeatManifestRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/basic/content/manifest/repeat";
  
  // Make the API request
  return makeApiRequest<CommonResponse, RepeatManifestRequest>({
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
 * @param id - Content ID of the content manifest
 * @param uid - UID of the content manifest
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getContentManifestPrivateJson(requester: HttpRequester, id?: string, uid?: string, gamertag?: string): Promise<HttpResponse<ClientManifestJsonResponse>> {
  let endpoint = "/basic/content/manifest/private/json";
  
  // Make the API request
  return makeApiRequest<ClientManifestJsonResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      id,
      uid
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
 * @param id - ID of the content manifest
 * @param uid - UID of the content manifest
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getContentManifestPrivate(requester: HttpRequester, id?: string, uid?: string, gamertag?: string): Promise<HttpResponse<ClientManifestResponse>> {
  let endpoint = "/basic/content/manifest/private";
  
  // Make the API request
  return makeApiRequest<ClientManifestResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      id,
      uid
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
export async function getContentManifestChecksums(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<ContentBasicManifestChecksums>> {
  let endpoint = "/basic/content/manifest/checksums";
  
  // Make the API request
  return makeApiRequest<ContentBasicManifestChecksums>({
    r: requester,
    e: endpoint,
    m: GET,
    g: gamertag,
    w: true
  });
}

/**
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param id - ID of the content manifest
 * @param uid - UID of the content manifest
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getContentManifestChecksum(requester: HttpRequester, id?: string, uid?: string, gamertag?: string): Promise<HttpResponse<ContentBasicManifestChecksum>> {
  let endpoint = "/basic/content/manifest/checksum";
  
  // Make the API request
  return makeApiRequest<ContentBasicManifestChecksum>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      id,
      uid
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
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getContentManifests(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<ContentBasicGetManifestsResponse>> {
  let endpoint = "/basic/content/manifests";
  
  // Make the API request
  return makeApiRequest<ContentBasicGetManifestsResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    g: gamertag,
    w: true
  });
}
