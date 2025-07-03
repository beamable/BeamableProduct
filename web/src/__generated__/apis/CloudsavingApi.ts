import { CloudsavingBasicManifest } from '@/__generated__/schemas/CloudsavingBasicManifest';
import { DELETE } from '@/constants';
import { EmptyResponse } from '@/__generated__/schemas/EmptyResponse';
import { GET } from '@/constants';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpResponse } from '@/network/http/types/HttpResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { ObjectRequests } from '@/__generated__/schemas/ObjectRequests';
import { PlayerBasicCloudDataRequest } from '@/__generated__/schemas/PlayerBasicCloudDataRequest';
import { POST } from '@/constants';
import { PUT } from '@/constants';
import { ReplaceObjectsRequest } from '@/__generated__/schemas/ReplaceObjectsRequest';
import { UploadRequests } from '@/__generated__/schemas/UploadRequests';
import { UploadRequestsFromPortal } from '@/__generated__/schemas/UploadRequestsFromPortal';
import { URLSResponse } from '@/__generated__/schemas/URLSResponse';

export class CloudsavingApi {
  constructor(
    private readonly r: HttpRequester
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
    let e = "/basic/cloudsaving/data/replace";
    
    // Make the API request
    return makeApiRequest<CloudsavingBasicManifest, ReplaceObjectsRequest>({
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
   * @param {ObjectRequests} payload - The `ObjectRequests` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<EmptyResponse>>} A promise containing the HttpResponse of EmptyResponse
   */
  async deleteCloudsavingData(payload: ObjectRequests, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
    let e = "/basic/cloudsaving/data";
    
    // Make the API request
    return makeApiRequest<EmptyResponse, ObjectRequests>({
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
   * @param {ObjectRequests} payload - The `ObjectRequests` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<URLSResponse>>} A promise containing the HttpResponse of URLSResponse
   */
  async postCloudsavingDataDownloadURL(payload: ObjectRequests, gamertag?: string): Promise<HttpResponse<URLSResponse>> {
    let e = "/basic/cloudsaving/data/downloadURL";
    
    // Make the API request
    return makeApiRequest<URLSResponse, ObjectRequests>({
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
   * @param {ObjectRequests} payload - The `ObjectRequests` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<URLSResponse>>} A promise containing the HttpResponse of URLSResponse
   */
  async postCloudsavingDataDownloadURLFromPortal(payload: ObjectRequests, gamertag?: string): Promise<HttpResponse<URLSResponse>> {
    let e = "/basic/cloudsaving/data/downloadURLFromPortal";
    
    // Make the API request
    return makeApiRequest<URLSResponse, ObjectRequests>({
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
   * @param {PlayerBasicCloudDataRequest} payload - The `PlayerBasicCloudDataRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CloudsavingBasicManifest>>} A promise containing the HttpResponse of CloudsavingBasicManifest
   */
  async putCloudsavingDataMove(payload: PlayerBasicCloudDataRequest, gamertag?: string): Promise<HttpResponse<CloudsavingBasicManifest>> {
    let e = "/basic/cloudsaving/data/move";
    
    // Make the API request
    return makeApiRequest<CloudsavingBasicManifest, PlayerBasicCloudDataRequest>({
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
   * @param {PlayerBasicCloudDataRequest} payload - The `PlayerBasicCloudDataRequest` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CloudsavingBasicManifest>>} A promise containing the HttpResponse of CloudsavingBasicManifest
   */
  async putCloudsavingDataMoveFromPortal(payload: PlayerBasicCloudDataRequest, gamertag?: string): Promise<HttpResponse<CloudsavingBasicManifest>> {
    let e = "/basic/cloudsaving/data/moveFromPortal";
    
    // Make the API request
    return makeApiRequest<CloudsavingBasicManifest, PlayerBasicCloudDataRequest>({
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
   * @param {UploadRequestsFromPortal} payload - The `UploadRequestsFromPortal` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<URLSResponse>>} A promise containing the HttpResponse of URLSResponse
   */
  async postCloudsavingDataUploadURLFromPortal(payload: UploadRequestsFromPortal, gamertag?: string): Promise<HttpResponse<URLSResponse>> {
    let e = "/basic/cloudsaving/data/uploadURLFromPortal";
    
    // Make the API request
    return makeApiRequest<URLSResponse, UploadRequestsFromPortal>({
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
   * @param {UploadRequests} payload - The `UploadRequests` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CloudsavingBasicManifest>>} A promise containing the HttpResponse of CloudsavingBasicManifest
   */
  async putCloudsavingDataCommitManifest(payload: UploadRequests, gamertag?: string): Promise<HttpResponse<CloudsavingBasicManifest>> {
    let e = "/basic/cloudsaving/data/commitManifest";
    
    // Make the API request
    return makeApiRequest<CloudsavingBasicManifest, UploadRequests>({
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
   * @param {UploadRequests} payload - The `UploadRequests` instance to use for the API request
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<URLSResponse>>} A promise containing the HttpResponse of URLSResponse
   */
  async postCloudsavingDataUploadURL(payload: UploadRequests, gamertag?: string): Promise<HttpResponse<URLSResponse>> {
    let e = "/basic/cloudsaving/data/uploadURL";
    
    // Make the API request
    return makeApiRequest<URLSResponse, UploadRequests>({
      r: this.r,
      e,
      m: POST,
      p: payload,
      g: gamertag,
      w: true
    });
  }
  
  /**
   * @param {bigint | string} playerId - The `playerId` parameter to include in the API request.
   * @param {string} gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
   * @returns {Promise<HttpResponse<CloudsavingBasicManifest>>} A promise containing the HttpResponse of CloudsavingBasicManifest
   */
  async getCloudsaving(playerId?: bigint | string, gamertag?: string): Promise<HttpResponse<CloudsavingBasicManifest>> {
    let e = "/basic/cloudsaving/";
    
    // Make the API request
    return makeApiRequest<CloudsavingBasicManifest>({
      r: this.r,
      e,
      m: GET,
      q: {
        playerId
      },
      g: gamertag
    });
  }
}
