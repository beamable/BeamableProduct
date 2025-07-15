import { HttpRequest } from './HttpRequest';
import { HttpResponse } from './HttpResponse';

/** A pluggable HTTP client abstraction that can send requests and receive typed responses. */
export interface HttpRequester {
  /**
   * Sends an HTTP request and returns a typed response.
   * @template TRes - The expected type of the response body.
   * @template TReq - The type of the request payload.
   * @param req - Configuration for the HTTP request, including URL, method, headers, optional body, etc.
   * @returns A promise that resolves with an HttpResponse containing status, headers, and the parsed body as TRes.
   */
  request<TRes = any, TReq = any>(
    req: HttpRequest<TReq>,
  ): Promise<HttpResponse<TRes>>;

  /** Overrides the base URL used for all subsequent requests. */
  set baseUrl(url: string);

  /** Sets the default headers to include on every request. */
  set defaultHeaders(header: Record<string, string>);
}
