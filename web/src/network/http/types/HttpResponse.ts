/**
 * Represents the structure of an HTTP response.
 * @template TRes - The expected type of the response payload.
 */
export interface HttpResponse<TRes = any> {
  /** The HTTP status code returned by the server (e.g., 200, 404, 500). */
  status: number;

  /** A collection of HTTP headers included in the response. */
  headers: Record<string, string>;

  /** The response payload parsed into the expected type TRes. */
  body: TRes;
}
