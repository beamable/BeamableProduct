import { HttpRequest } from '@/network/http/types/HttpRequest';
import { BeamBase } from '@/core/BeamBase';
import { BeamMicroServiceHost } from '@/core/BeamMicroServiceClient';
import { HEADERS, POST } from '@/constants';

interface makeMicroServiceRequestProps<TReq> {
  host: BeamMicroServiceHost;
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
  const { host, serviceName, endpoint, payload, withAuth } = props;
  const { cid, microServiceScope, requester } = host;
  // `microServiceScope` is the realm pid for a realm SDK, or the zone zid for a
  // zone SDK — so a zone extension calling a zone-running service routes to
  // `/basic/{cid}.{zid}.micro_{service}/...`, the zone analog of `{cid}.{pid}`.
  const scope = `${cid}.${microServiceScope}`;
  const url = `/basic/${scope}.micro_${serviceName}/${endpoint}`;
  const routingKey = BeamBase.env.BEAM_ROUTING_KEY;

  // Create the header parameters object. Pin the scope to the routed target so
  // a zone SDK — whose requester defaults to a cid-only scope for its
  // customer-directory calls — still scopes microservice calls to `cid.zid`.
  const headers: Record<string, string> = {
    [HEADERS.BEAM_SCOPE]: scope,
  };
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
