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
import { endpointEncoder } from '@/utils/endpointEncoder';
import { GET } from '@/constants';
import { GetLocalizationsResponse } from '@/__generated__/schemas/GetLocalizationsResponse';
import { GetManifestHistoryResponse } from '@/__generated__/schemas/GetManifestHistoryResponse';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/constants';
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

export class ContentApi {
  constructor(
    private readonly r: HttpRequester
  ) {
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {ArchiveOrUnarchiveManifestsRequest} payload - The `ArchiveOrUnarchiveManifestsRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postContentManifestsUnarchive(payload: ArchiveOrUnarchiveManifestsRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let e = "/basic/content/manifests/unarchive";
    
    // Make the API request
    return makeApiRequest<EmptyResponse, ArchiveOrUnarchiveManifestsRequest>({
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
   * @param {PullManifestRequest} payload - The `PullManifestRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ContentBasicManifest>>} A promise containing the HttpResponse of ContentBasicManifest
   */
  async postContentManifestPull(payload: PullManifestRequest, gamertag?: string): Promise<HttpResponse<ContentBasicManifest>> {
    let e = "/basic/content/manifest/pull";
    
    // Make the API request
    return makeApiRequest<ContentBasicManifest, PullManifestRequest>({
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
   * @param {string} id - The `id` parameter to include in the API request.
   * @param {number} limit - The `limit` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetManifestHistoryResponse>>} A promise containing the HttpResponse of GetManifestHistoryResponse
   */
  async getContentManifestHistory(id?: string, limit?: number, gamertag?: string): Promise<HttpResponse<GetManifestHistoryResponse>> {
    let e = "/basic/content/manifest/history";
    
    // Make the API request
    return makeApiRequest<GetManifestHistoryResponse>({
      r: this.r,
      e,
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
   * @param {SaveBinaryRequest} payload - The `SaveBinaryRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<SaveBinaryResponse>>} A promise containing the HttpResponse of SaveBinaryResponse
   */
  async postContentBinary(payload: SaveBinaryRequest, gamertag?: string): Promise<HttpResponse<SaveBinaryResponse>> {
    let e = "/basic/content/binary";
    
    // Make the API request
    return makeApiRequest<SaveBinaryResponse, SaveBinaryRequest>({
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
   * @param {PullAllManifestsRequest} payload - The `PullAllManifestsRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ContentBasicManifestChecksums>>} A promise containing the HttpResponse of ContentBasicManifestChecksums
   */
  async postContentManifestsPull(payload: PullAllManifestsRequest, gamertag?: string): Promise<HttpResponse<ContentBasicManifestChecksums>> {
    let e = "/basic/content/manifests/pull";
    
    // Make the API request
    return makeApiRequest<ContentBasicManifestChecksums, PullAllManifestsRequest>({
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
   * @param {string} contentId - The `contentId` parameter to include in the API request.
   * @param {string} version - The `version` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ContentOrText>>} A promise containing the HttpResponse of ContentOrText
   */
  async getContent(contentId: string, version: string, gamertag?: string): Promise<HttpResponse<ContentOrText>> {
    let e = "/basic/content/content";
    
    // Make the API request
    return makeApiRequest<ContentOrText>({
      r: this.r,
      e,
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
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<GetLocalizationsResponse>>} A promise containing the HttpResponse of GetLocalizationsResponse
   */
  async getContentLocalizations(gamertag?: string): Promise<HttpResponse<GetLocalizationsResponse>> {
    let e = "/basic/content/localizations";
    
    // Make the API request
    return makeApiRequest<GetLocalizationsResponse>({
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
   * @param {PutLocalizationsRequest} payload - The `PutLocalizationsRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putContentLocalizations(payload: PutLocalizationsRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/basic/content/localizations";
    
    // Make the API request
    return makeApiRequest<CommonResponse, PutLocalizationsRequest>({
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
   * @param {DeleteLocalizationRequest} payload - The `DeleteLocalizationRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async deleteContentLocalizations(payload: DeleteLocalizationRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/basic/content/localizations";
    
    // Make the API request
    return makeApiRequest<CommonResponse, DeleteLocalizationRequest>({
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
   * @param {SaveTextRequest} payload - The `SaveTextRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<SaveTextResponse>>} A promise containing the HttpResponse of SaveTextResponse
   */
  async postContentText(payload: SaveTextRequest, gamertag?: string): Promise<HttpResponse<SaveTextResponse>> {
    let e = "/basic/content/text";
    
    // Make the API request
    return makeApiRequest<SaveTextResponse, SaveTextRequest>({
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
   * @param {string} uid - The `uid` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ContentBasicManifest>>} A promise containing the HttpResponse of ContentBasicManifest
   */
  async getContentManifestExact(uid: string, gamertag?: string): Promise<HttpResponse<ContentBasicManifest>> {
    let e = "/basic/content/manifest/exact";
    
    // Make the API request
    return makeApiRequest<ContentBasicManifest>({
      r: this.r,
      e,
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
   * @param {string} id - ID of the content manifest
   * @param {string} uid - UID of the content manifest
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ContentBasicManifest>>} A promise containing the HttpResponse of ContentBasicManifest
   */
  async getContentManifest(id?: string, uid?: string, gamertag?: string): Promise<HttpResponse<ContentBasicManifest>> {
    let e = "/basic/content/manifest";
    
    // Make the API request
    return makeApiRequest<ContentBasicManifest>({
      r: this.r,
      e,
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
   * @param {SaveManifestRequest} payload - The `SaveManifestRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ContentBasicManifest>>} A promise containing the HttpResponse of ContentBasicManifest
   */
  async postContentManifest(payload: SaveManifestRequest, gamertag?: string): Promise<HttpResponse<ContentBasicManifest>> {
    let e = "/basic/content/manifest";
    
    // Make the API request
    return makeApiRequest<ContentBasicManifest, SaveManifestRequest>({
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
   * @param {ArchiveOrUnarchiveManifestsRequest} payload - The `ArchiveOrUnarchiveManifestsRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async postContentManifestsArchive(payload: ArchiveOrUnarchiveManifestsRequest, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let e = "/basic/content/manifests/archive";
    
    // Make the API request
    return makeApiRequest<EmptyResponse, ArchiveOrUnarchiveManifestsRequest>({
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
   * @param {SaveContentRequest} payload - The `SaveContentRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<SaveContentResponse>>} A promise containing the HttpResponse of SaveContentResponse
   */
  async postContent(payload: SaveContentRequest, gamertag?: string): Promise<HttpResponse<SaveContentResponse>> {
    let e = "/basic/content/";
    
    // Make the API request
    return makeApiRequest<SaveContentResponse, SaveContentRequest>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {string} id - ID of the content manifest
   * @param {string} uid - UID of the content manifest
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ClientManifestResponse>>} A promise containing the HttpResponse of ClientManifestResponse
   */
  async getContentManifestPublic(id?: string, uid?: string, gamertag?: string): Promise<HttpResponse<ClientManifestResponse>> {
    let e = "/basic/content/manifest/public";
    
    // Make the API request
    return makeApiRequest<ClientManifestResponse>({
      r: this.r,
      e,
      m: GET,
      q: {
        id,
        uid
      },
      g: gamertag
    });
  }
  
  /**
   * @param {string} id - Content ID of the content manifest
   * @param {string} uid - UID of the content manifest
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ClientManifestJsonResponse>>} A promise containing the HttpResponse of ClientManifestJsonResponse
   */
  async getContentManifestPublicJson(id?: string, uid?: string, gamertag?: string): Promise<HttpResponse<ClientManifestJsonResponse>> {
    let e = "/basic/content/manifest/public/json";
    
    // Make the API request
    return makeApiRequest<ClientManifestJsonResponse>({
      r: this.r,
      e,
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
   * @param {RepeatManifestRequest} payload - The `RepeatManifestRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CommonResponse>>} A promise containing the HttpResponse of CommonResponse
   */
  async putContentManifestRepeat(payload: RepeatManifestRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
    let e = "/basic/content/manifest/repeat";
    
    // Make the API request
    return makeApiRequest<CommonResponse, RepeatManifestRequest>({
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
   * @param {string} id - Content ID of the content manifest
   * @param {string} uid - UID of the content manifest
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ClientManifestJsonResponse>>} A promise containing the HttpResponse of ClientManifestJsonResponse
   */
  async getContentManifestPrivateJson(id?: string, uid?: string, gamertag?: string): Promise<HttpResponse<ClientManifestJsonResponse>> {
    let e = "/basic/content/manifest/private/json";
    
    // Make the API request
    return makeApiRequest<ClientManifestJsonResponse>({
      r: this.r,
      e,
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
   * @param {string} id - ID of the content manifest
   * @param {string} uid - UID of the content manifest
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ClientManifestResponse>>} A promise containing the HttpResponse of ClientManifestResponse
   */
  async getContentManifestPrivate(id?: string, uid?: string, gamertag?: string): Promise<HttpResponse<ClientManifestResponse>> {
    let e = "/basic/content/manifest/private";
    
    // Make the API request
    return makeApiRequest<ClientManifestResponse>({
      r: this.r,
      e,
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
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ContentBasicManifestChecksums>>} A promise containing the HttpResponse of ContentBasicManifestChecksums
   */
  async getContentManifestChecksums(gamertag?: string): Promise<HttpResponse<ContentBasicManifestChecksums>> {
    let e = "/basic/content/manifest/checksums";
    
    // Make the API request
    return makeApiRequest<ContentBasicManifestChecksums>({
      r: this.r,
      e,
      m: GET,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {string} id - ID of the content manifest
   * @param {string} uid - UID of the content manifest
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ContentBasicManifestChecksum>>} A promise containing the HttpResponse of ContentBasicManifestChecksum
   */
  async getContentManifestChecksum(id?: string, uid?: string, gamertag?: string): Promise<HttpResponse<ContentBasicManifestChecksum>> {
    let e = "/basic/content/manifest/checksum";
    
    // Make the API request
    return makeApiRequest<ContentBasicManifestChecksum>({
      r: this.r,
      e,
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
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<ContentBasicGetManifestsResponse>>} A promise containing the HttpResponse of ContentBasicGetManifestsResponse
   */
  async getContentManifests(gamertag?: string): Promise<HttpResponse<ContentBasicGetManifestsResponse>> {
    let e = "/basic/content/manifests";
    
    // Make the API request
    return makeApiRequest<ContentBasicGetManifestsResponse>({
      r: this.r,
      e,
      m: GET,
      g: gamertag,
      w: true
    });
  }
}
