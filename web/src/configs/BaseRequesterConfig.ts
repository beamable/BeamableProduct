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
   * A custom fetch implementation to override the native global `fetch`.
   * Useful in unit tests, or to support environments with no native fetch.
   * Example: Passing in a polyfill like `cross-fetch`, `undici`, or a logging wrapper.
   * @default globalThis.fetch
   */
  customFetch?: typeof fetch;
}
