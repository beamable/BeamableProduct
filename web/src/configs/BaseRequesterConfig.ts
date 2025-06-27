/**
 * Configuration options for a Fetch-based HTTP requester.
 * Includes settings for timeouts, base URL, default headers,
 * credential handling, custom fetch implementation, and token provisioning.
 */
export interface BaseRequesterConfig {
  /**
   * Timeout duration in milliseconds for each request.
   * If the request takes longer than this time, it will be aborted.
   * @default 10000 (10 seconds)
   */
  timeout?: number;

  /**
   * A base URL that will be prepended to all relative request URLs.
   * Example:
   * - baseUrl: 'https://api.beamable.com'
   * - request.url: `/api/auth/refresh-token`
   * - Final URL: 'https://api.beamable.com/api/auth/refresh-token'
   */
  baseUrl?: string;

  /**
   * Default headers that will be merged into every request made by the requester.
   * Can be overridden per request by explicitly setting the same header key.
   * Example: `Authorization`, `Content-Type`, `Accept`, etc.
   */
  defaultHeaders?: Record<string, string>;

  /**
   * Enables sending of cookies and credentials in cross-origin requests
   * when running in the browser. This maps to the native fetch `credentials` option:
   * - true  → `credentials: 'include'` (send credentials on all requests)
   * - false → `credentials: 'same-origin'` (send only on same-origin)
   * Note: This setting is **only relevant in browsers**. It is ignored in Node.js.
   * @default false
   */
  withCredentials?: boolean;

  /**
   * A custom fetch implementation to override the native global `fetch`.
   * Useful in unit tests, or to support environments with no native fetch.
   * Example: Passing in a polyfill like `cross-fetch`, `undici`, or a logging wrapper.
   * @default globalThis.fetch
   */
  customFetch?: typeof fetch;

  /**
   * A function that provides an authentication token to include in the
   * `Authorization` header of each request that requires auth.
   * Can return a string directly or a Promise resolving to a string.
   */
  tokenProvider?: () => Promise<string> | string;
}
