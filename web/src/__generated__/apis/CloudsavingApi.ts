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

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `ReplaceObjectsRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postCloudsavingDataReplace(requester: HttpRequester, payload: ReplaceObjectsRequest, gamertag?: string): Promise<HttpResponse<CloudsavingBasicManifest>> {
  let endpoint = "/basic/cloudsaving/data/replace";
  
  // Make the API request
  return makeApiRequest<CloudsavingBasicManifest, ReplaceObjectsRequest>({
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
 * @param payload - The `ObjectRequests` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function deleteCloudsavingData(requester: HttpRequester, payload: ObjectRequests, gamertag?: string): Promise<HttpResponse<EmptyResponse>> {
  let endpoint = "/basic/cloudsaving/data";
  
  // Make the API request
  return makeApiRequest<EmptyResponse, ObjectRequests>({
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
 * @param payload - The `ObjectRequests` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postCloudsavingDataDownloadURL(requester: HttpRequester, payload: ObjectRequests, gamertag?: string): Promise<HttpResponse<URLSResponse>> {
  let endpoint = "/basic/cloudsaving/data/downloadURL";
  
  // Make the API request
  return makeApiRequest<URLSResponse, ObjectRequests>({
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
 * @param payload - The `ObjectRequests` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postCloudsavingDataDownloadURLFromPortal(requester: HttpRequester, payload: ObjectRequests, gamertag?: string): Promise<HttpResponse<URLSResponse>> {
  let endpoint = "/basic/cloudsaving/data/downloadURLFromPortal";
  
  // Make the API request
  return makeApiRequest<URLSResponse, ObjectRequests>({
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
 * @param payload - The `PlayerBasicCloudDataRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function putCloudsavingDataMove(requester: HttpRequester, payload: PlayerBasicCloudDataRequest, gamertag?: string): Promise<HttpResponse<CloudsavingBasicManifest>> {
  let endpoint = "/basic/cloudsaving/data/move";
  
  // Make the API request
  return makeApiRequest<CloudsavingBasicManifest, PlayerBasicCloudDataRequest>({
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
 * @param payload - The `PlayerBasicCloudDataRequest` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function putCloudsavingDataMoveFromPortal(requester: HttpRequester, payload: PlayerBasicCloudDataRequest, gamertag?: string): Promise<HttpResponse<CloudsavingBasicManifest>> {
  let endpoint = "/basic/cloudsaving/data/moveFromPortal";
  
  // Make the API request
  return makeApiRequest<CloudsavingBasicManifest, PlayerBasicCloudDataRequest>({
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
 * @param payload - The `UploadRequestsFromPortal` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postCloudsavingDataUploadURLFromPortal(requester: HttpRequester, payload: UploadRequestsFromPortal, gamertag?: string): Promise<HttpResponse<URLSResponse>> {
  let endpoint = "/basic/cloudsaving/data/uploadURLFromPortal";
  
  // Make the API request
  return makeApiRequest<URLSResponse, UploadRequestsFromPortal>({
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
 * @param payload - The `UploadRequests` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function putCloudsavingDataCommitManifest(requester: HttpRequester, payload: UploadRequests, gamertag?: string): Promise<HttpResponse<CloudsavingBasicManifest>> {
  let endpoint = "/basic/cloudsaving/data/commitManifest";
  
  // Make the API request
  return makeApiRequest<CloudsavingBasicManifest, UploadRequests>({
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
 * @param payload - The `UploadRequests` instance to use for the API request
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function postCloudsavingDataUploadURL(requester: HttpRequester, payload: UploadRequests, gamertag?: string): Promise<HttpResponse<URLSResponse>> {
  let endpoint = "/basic/cloudsaving/data/uploadURL";
  
  // Make the API request
  return makeApiRequest<URLSResponse, UploadRequests>({
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
 * @param playerId - The `playerId` parameter to include in the API request.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function getCloudsaving(requester: HttpRequester, playerId?: bigint | string, gamertag?: string): Promise<HttpResponse<CloudsavingBasicManifest>> {
  let endpoint = "/basic/cloudsaving/";
  
  // Make the API request
  return makeApiRequest<CloudsavingBasicManifest>({
    r: requester,
    e: endpoint,
    m: GET,
    q: {
      playerId
    },
    g: gamertag
  });
}
