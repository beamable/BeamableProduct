/**
 * Abstraction for managing a persisted authentication token.
 *
 * @interface TokenStorage
 */
export interface TokenStorage {
  /**
   * Retrieves the stored token.
   *
   * @returns A promise that resolves with the token string, or `null` if no token is stored.
   */
  getToken(): Promise<string | null>;

  /**
   * Persists the provided token.
   *
   * @param token - The authentication token to store.
   * @returns A promise that resolves once the token has been saved.
   */
  setToken(token: string): Promise<void>;

  /**
   * Removes any stored token.
   *
   * @returns A promise that resolves once the token has been cleared.
   */
  removeToken(): Promise<void>;
}
