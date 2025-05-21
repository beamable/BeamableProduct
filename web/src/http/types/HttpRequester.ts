import { HttpRequest } from './HttpRequest';
import { HttpResponse } from './HttpResponse';

/**
 * A pluggable HTTP client abstraction that can send requests and receive typed responses.
 * @interface HttpRequester
 */
export interface HttpRequester {
  /**
   * Sends an HTTP request and returns a typed response.
   * @template TRes - The expected type of the response body.
   * @template TReq - The type of the request payload.
   * @param {HttpRequest<TReq>} req - Configuration for the HTTP request,
   *   including URL, method, headers, optional body, etc.
   * @returns {Promise<HttpResponse<TRes>>} A promise that resolves with
   *   an HttpResponse containing status, headers, and the parsed body as TRes.
   */
  request<TRes = any, TReq = any>(
    req: HttpRequest<TReq>,
  ): Promise<HttpResponse<TRes>>;

  /**
   * Overrides the base URL used for all subsequent requests.
   * @param {string} url - The new base URL to prepend to request paths.
   */
  setBaseUrl(url: string): void;

  /**
   * Sets or removes a default header included on every request.
   * @param {string} key - The header name.
   * @param {string | undefined} value - The header value; if undefined, the header is removed.
   */
  setDefaultHeader(key: string, value?: string): void;

  /**
   * Sets the token provider callback used by custom HTTP requesters to obtain the authorization token.
   * The provided function will be invoked whenever a token is needed, allowing custom requester
   * implementation to retrieve the current token and include it in the
   * `Authorization` header of each request that requires auth.
   * @param provider - A function that returns the authorization token, either directly as a string
   *                   or asynchronously as a Promise that resolves to a string.
   */
  setTokenProvider(provider: () => Promise<string> | string): void;
}
