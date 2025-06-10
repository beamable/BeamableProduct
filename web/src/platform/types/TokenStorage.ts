/**
 * Abstraction for managing a persisted authentication token.
 *
 * @interface TokenStorage
 */
export interface TokenStorage {
  /**
   * Retrieves the stored access token, or `null` if none.
   * @return {Promise<string | null>}
   */
  getAccessToken(): Promise<string | null>;

  /**
   * Persists the provided access token.
   * @param {string} token - The access token to store.
   * @return {Promise<void>}
   */
  setAccessToken(token: string): Promise<void>;

  /**
   *  * Removes any stored access token.
   * @return {Promise<void>}
   */
  removeAccessToken(): Promise<void>;

  /**
   * Retrieves the stored refresh token, or `null` if none.
   * @return {Promise<string | null>}
   */
  getRefreshToken(): Promise<string | null>;

  /**
   * Persists the provided refresh token.
   * @param {string} token - The refresh token to store.
   * @return {Promise<void>}
   */
  setRefreshToken(token: string): Promise<void>;

  /**
   * Removes any stored refresh token.
   * @return {Promise<void>}
   */
  removeRefreshToken(): Promise<void>;
}
