import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpMethod } from '@/network/http/types/HttpMethod';
import { makeQueryString } from '@/utils/makeQueryString';
import { HttpRequest } from '@/network/http/types/HttpRequest';
import { HttpResponse } from '@/network/http/types/HttpResponse';
import { HEADERS } from '@/constants';

interface makeApiRequestProps<TReq> {
  r: HttpRequester; // The HTTP requester instance to use for making the API request
  e: string; // The endpoint URL to which the request will be sent
  m: HttpMethod; // The HTTP method to use for the request (e.g., GET, POST, PUT, DELETE)
  q?: Record<string, unknown>; // Optional query parameters to include in the request URL
  p?: TReq; // Optional payload to include in the request body (for POST, PUT, etc.)
  g?: string; // Optional gamertag to include in the request headers
  w?: boolean; // Optional flag to indicate whether to include authentication in the request
}

/**
 * Helper function used by generated Beamable API functions to make API requests.
 * @template TRes - The expected type of the response body.
 * @template TReq - The expected type of the request body.
 */
export function makeApiRequest<TRes = any, TReq = any>(
  props: makeApiRequestProps<TReq>,
): Promise<HttpResponse<TRes>> {
  // Abbreviate prop names to shrink bundle size (bundlers won’t minify object keys)
  // r: requester, e: endpoint, m: method, q: query, p: payload, g: gamertag, w: withAuth
  const { r, e, m, q, p, g, w } = props;

  // Create the header parameters object
  const headers: Record<string, string> = {};
  if (g != undefined) {
    headers[HEADERS.GAMERTAG] = g;
  }

  // Create the query string from the query parameters
  const queryString = q ? makeQueryString(q) : '';

  // Create the request data
  const data: HttpRequest<TReq> = {
    url: e.concat(queryString),
    method: m,
    headers,
  };

  // Attach payload body to request data
  if (p) data.body = p;

  // Attach withAuth to request data
  if (w !== undefined) data.withAuth = w;

  // Make the API request
  return r.request<TRes, TReq>(data);
}
