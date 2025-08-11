import { HttpRequest } from '@/network/http/types/HttpRequest';
import { BeamBase } from '@/core/BeamBase';
import { HEADERS, POST } from '@/constants';

interface makeMicroServiceRequestProps<TReq> {
  beam: BeamBase;
  serviceName: string;
  endpoint: string;
  payload?: TReq;
  withAuth: boolean;
}

/**
 * Helper function used by generated Beamable microservice client to make microservice requests.
 * @template TRes - The expected type of the response body.
 * @template TReq - The expected type of the request body.
 */
export async function makeMicroServiceRequest<TRes = any, TReq = any>(
  props: makeMicroServiceRequestProps<TReq>,
): Promise<TRes> {
  const { beam, serviceName, endpoint, payload, withAuth } = props;
  const { cid, pid, requester } = beam;
  const url = `/basic/${cid}.${pid}.micro_${serviceName}/${endpoint}`;
  const routingKey = BeamBase.env.BEAM_ROUTING_KEY;

  // Create the header parameters object
  const headers: Record<string, string> = {};
  if (routingKey) {
    headers[HEADERS.ROUTING_KEY] = routingKey;
  }

  // Create the request data
  const data: HttpRequest<TReq> = {
    method: POST,
    url,
    headers,
  };

  // Attach payload body to request data
  if (payload) data.body = payload;

  // Attach withAuth to request data
  if (withAuth) data.withAuth = withAuth;

  // Make the API request
  const { body } = await requester.request<TRes, TReq>(data);
  return body;
}
