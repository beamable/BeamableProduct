import { HttpRequest } from './HttpRequest';
import { HttpResponse } from './HttpResponse';

/**
 * Defines a service capable of sending HTTP requests and receiving typed responses.
 *
 * @interface HttpRequester
 */
export interface HttpRequester {
  /**
   * Executes an HTTP request based on the provided configuration.
   *
   * @template TRes - The expected type of the response payload.
   * @template TReq - The type of the request body.
   * @param {HttpRequest<TReq>} req - The HTTP request configuration, including URL, method, headers, body, etc.
   * @returns {Promise<HttpResponse<TRes>>} A promise that resolves with an HttpResponse containing the status, headers, and parsed data of type T.
   */
  request<TRes = any, TReq = any>(
    req: HttpRequest<TReq>,
  ): Promise<HttpResponse<TRes>>;
}
