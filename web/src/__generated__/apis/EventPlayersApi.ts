import { CommonResponse } from '@/__generated__/schemas/CommonResponse';
import { endpointEncoder } from '@/utils/endpointEncoder';
import { EventClaimRequest } from '@/__generated__/schemas/EventClaimRequest';
import { EventClaimResponse } from '@/__generated__/schemas/EventClaimResponse';
import { EventPlayerView } from '@/__generated__/schemas/EventPlayerView';
import { EventScoreRequest } from '@/__generated__/schemas/EventScoreRequest';
import { GET } from '@/constants';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpResponse } from '@/network/http/types/HttpResponse';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { objectIdPlaceholder } from '@/__generated__/apis/constants';
import { POST } from '@/constants';
import { PUT } from '@/constants';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function eventPlayersGetByObjectId(requester: HttpRequester, objectId: bigint | string, gamertag?: string): Promise<HttpResponse<EventPlayerView>> {
  let endpoint = "/object/event-players/{objectId}/".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<EventPlayerView>({
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
 * @param payload - The `EventClaimRequest` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function eventPlayersPostClaimByObjectId(requester: HttpRequester, objectId: bigint | string, payload: EventClaimRequest, gamertag?: string): Promise<HttpResponse<EventClaimResponse>> {
  let endpoint = "/object/event-players/{objectId}/claim".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<EventClaimResponse, EventClaimRequest>({
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
 * @param payload - The `EventScoreRequest` instance to use for the API request
 * @param objectId - Gamertag of the player.Underlying objectId type is integer in format int64.
 * @param gamertag - Override the Gamer Tag of the player. This is generally inferred by the auth token.
 * 
 */
export async function eventPlayersPutScoreByObjectId(requester: HttpRequester, objectId: bigint | string, payload: EventScoreRequest, gamertag?: string): Promise<HttpResponse<CommonResponse>> {
  let endpoint = "/object/event-players/{objectId}/score".replace(objectIdPlaceholder, endpointEncoder(objectId));
  
  // Make the API request
  return makeApiRequest<CommonResponse, EventScoreRequest>({
    r: requester,
    e: endpoint,
    m: PUT,
    p: payload,
    g: gamertag,
    w: true
  });
}
