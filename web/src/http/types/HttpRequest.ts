import { HttpMethod } from '@/http/types/HttpMethod';

/**
 * Describes the configuration for an HTTP request.
 * @template TReq - The expected type of the request body.
 */
export interface HttpRequest<TReq = any> {
  /** The full URL to which the HTTP request will be sent. */
  url: string;

  /**
   * The HTTP method to use for the request (e.g., 'GET', 'POST', 'PUT', 'PATCH', 'DELETE').
   * If omitted, the requester will assume a default (often 'GET').
   */
  method?: HttpMethod;

  /** A collection of HTTP headers to include with the request. */
  headers?: Record<string, string>;

  /** The payload to send with the request, of type TReq. */
  body?: TReq;

  /**
   * Whether to automatically include an authorization token (e.g., Bearer token)
   * in the request headers. Implementations should respect this flag.
   */
  withAuth?: boolean;
}
