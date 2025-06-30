import { HttpRequester } from '@/network/http/types/HttpRequester';
import { HttpMethod } from '@/network/http/types/HttpMethod';
import { makeQueryString } from '@/utils/makeQueryString';
import { HttpRequest } from '@/network/http/types/HttpRequest';
import { HttpResponse } from '@/network/http/types/HttpResponse';

interface makeApiRequestProps<TReq> {
  r: HttpRequester;
  e: string;
  m: HttpMethod;
  q?: Record<string, unknown>;
  p?: TReq;
  g?: string;
  w?: boolean;
}
export function makeApiRequest<TRes = any, TReq = any>(
  props: makeApiRequestProps<TReq>,
): Promise<HttpResponse<TRes>> {
  const { r, e, m, q, p, g, w } = props;

  // Create the header parameters object
  const headers: Record<string, string> = {};
  if (g != undefined) {
    headers['X-BEAM-GAMERTAG'] = g;
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
