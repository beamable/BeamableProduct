import { CloudsavingBasicManifest } from '@/__generated__/schemas/CloudsavingBasicManifest';
import { EmptyResponse } from '@/__generated__/schemas/EmptyResponse';
import { HttpMethod } from '@/http/types/HttpMethod';
import { HttpRequester } from '@/http/types/HttpRequester';
import { HttpResponse } from '@/http/types/HttpResponse';
import { makeQueryString } from '@/utils/makeQueryString';
import { ObjectRequests } from '@/__generated__/schemas/ObjectRequests';
import { PlayerBasicCloudDataRequest } from '@/__generated__/schemas/PlayerBasicCloudDataRequest';
import { ReplaceObjectsRequest } from '@/__generated__/schemas/ReplaceObjectsRequest';
import { UploadRequests } from '@/__generated__/schemas/UploadRequests';
import { UploadRequestsFromPortal } from '@/__generated__/schemas/UploadRequestsFromPortal';
import { URLSResponse } from '@/__generated__/schemas/URLSResponse';

export class CloudsavingApi {
  constructor(
    private readonly requester: HttpRequester
  ) {
  }
  
  /**
   * @remarks
   * **Authentication:**
   * This method requires a valid bearer token in the `Authorization` header.
   * 
   * @param {ReplaceObjectsRequest} payload - The `ReplaceObjectsRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CloudsavingBasicManifest>>} A promise containing the HttpResponse of CloudsavingBasicManifest
   */
  async postCloudsavingDataReplace(payload: ReplaceObjectsRequest, gamertag?: string): Promise<HttpResponse<CloudsavingBasicManifest>> {
    let endpoint = "/basic/cloudsaving/data/replace";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CloudsavingBasicManifest, ReplaceObjectsRequest>({
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
   * @param {ObjectRequests} payload - The `ObjectRequests` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async deleteCloudsavingData(payload: ObjectRequests, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let endpoint = "/basic/cloudsaving/data";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<EmptyResponse, ObjectRequests>({
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
   * @param {ObjectRequests} payload - The `ObjectRequests` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<URLSResponse>>} A promise containing the HttpResponse of URLSResponse
   */
  async postCloudsavingDataDownloadURL(payload: ObjectRequests, gamertag?: string): Promise<HttpResponse<URLSResponse>> {
    let endpoint = "/basic/cloudsaving/data/downloadURL";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<URLSResponse, ObjectRequests>({
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
   * @param {ObjectRequests} payload - The `ObjectRequests` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<URLSResponse>>} A promise containing the HttpResponse of URLSResponse
   */
  async postCloudsavingDataDownloadURLFromPortal(payload: ObjectRequests, gamertag?: string): Promise<HttpResponse<URLSResponse>> {
    let endpoint = "/basic/cloudsaving/data/downloadURLFromPortal";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<URLSResponse, ObjectRequests>({
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
   * @param {PlayerBasicCloudDataRequest} payload - The `PlayerBasicCloudDataRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CloudsavingBasicManifest>>} A promise containing the HttpResponse of CloudsavingBasicManifest
   */
  async putCloudsavingDataMove(payload: PlayerBasicCloudDataRequest, gamertag?: string): Promise<HttpResponse<CloudsavingBasicManifest>> {
    let endpoint = "/basic/cloudsaving/data/move";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CloudsavingBasicManifest, PlayerBasicCloudDataRequest>({
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
   * @param {PlayerBasicCloudDataRequest} payload - The `PlayerBasicCloudDataRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CloudsavingBasicManifest>>} A promise containing the HttpResponse of CloudsavingBasicManifest
   */
  async putCloudsavingDataMoveFromPortal(payload: PlayerBasicCloudDataRequest, gamertag?: string): Promise<HttpResponse<CloudsavingBasicManifest>> {
    let endpoint = "/basic/cloudsaving/data/moveFromPortal";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CloudsavingBasicManifest, PlayerBasicCloudDataRequest>({
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
   * @param {UploadRequestsFromPortal} payload - The `UploadRequestsFromPortal` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<URLSResponse>>} A promise containing the HttpResponse of URLSResponse
   */
  async postCloudsavingDataUploadURLFromPortal(payload: UploadRequestsFromPortal, gamertag?: string): Promise<HttpResponse<URLSResponse>> {
    let endpoint = "/basic/cloudsaving/data/uploadURLFromPortal";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<URLSResponse, UploadRequestsFromPortal>({
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
   * @param {UploadRequests} payload - The `UploadRequests` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CloudsavingBasicManifest>>} A promise containing the HttpResponse of CloudsavingBasicManifest
   */
  async putCloudsavingDataCommitManifest(payload: UploadRequests, gamertag?: string): Promise<HttpResponse<CloudsavingBasicManifest>> {
    let endpoint = "/basic/cloudsaving/data/commitManifest";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<CloudsavingBasicManifest, UploadRequests>({
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
   * @param {UploadRequests} payload - The `UploadRequests` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<URLSResponse>>} A promise containing the HttpResponse of URLSResponse
   */
  async postCloudsavingDataUploadURL(payload: UploadRequests, gamertag?: string): Promise<HttpResponse<URLSResponse>> {
    let endpoint = "/basic/cloudsaving/data/uploadURL";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Make the API request
    return this.requester.request<URLSResponse, UploadRequests>({
      url: endpoint,
      method: HttpMethod.POST,
      headers,
      body: payload,
      withAuth: true
    });
  }
  
  /**
   * @param {bigint | string} playerId - The `playerId` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CloudsavingBasicManifest>>} A promise containing the HttpResponse of CloudsavingBasicManifest
   */
  async getCloudsaving(playerId?: bigint | string, gamertag?: string): Promise<HttpResponse<CloudsavingBasicManifest>> {
    let endpoint = "/basic/cloudsaving/";
    
    // Create the header parameters object
    const headers: Record<string, string> = {};
    if (gamertag != undefined) {
      headers['X-BEAM-GAMERTAG'] = gamertag;
    }
    
    // Create the query string from the query parameters
    const queryString = makeQueryString({
      playerId
    });
    
    // Make the API request
    return this.requester.request<CloudsavingBasicManifest>({
      url: endpoint.concat(queryString),
      method: HttpMethod.GET,
      headers
    });
  }
}
