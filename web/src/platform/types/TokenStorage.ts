/**
 * Abstraction for managing a persisted authentication token.
 *
 * @interface TokenStorage
 */
export abstract class TokenStorage {
  protected accessToken: string | null = null;
  protected refreshToken: string | null = null;
  protected expiresIn: number | null = null;

  /**
   * Retrieves the stored access token, or `null` if none.
   * @return {Promise<string | null>}
   */
  abstract getAccessToken(): Promise<string | null>;

  /**
   * Persists the provided access token.
   * @param {string} token - The access token to store.
   * @return {Promise<void>}
   */
  abstract setAccessToken(token: string): Promise<void>;

  /**
   *  * Removes any stored access token.
   * @return {Promise<void>}
   */
  abstract removeAccessToken(): Promise<void>;

  /**
   * Retrieves the stored refresh token, or `null` if none.
   * @return {Promise<string | null>}
   */
  abstract getRefreshToken(): Promise<string | null>;

  /**
   * Persists the provided refresh token.
   * @param {string} token - The refresh token to store.
   * @return {Promise<void>}
   */
  abstract setRefreshToken(token: string): Promise<void>;

  /**
   * Removes any stored refresh token.
   * @return {Promise<void>}
   */
  abstract removeRefreshToken(): Promise<void>;

  /**
   * Retrieves the stored expiresIn, or `null` if none.
   * @return {Promise<number | null>}
   */
  abstract getExpiresIn(): Promise<number | null>;

  /**
   * Persists the provided expiresIn
   * @param {number} expiresIn - The expiresIn to store.
   * @return {Promise<void>}
   */
  abstract setExpiresIn(expiresIn: number): Promise<void>;

  /**
   * Removes any stored expiresIn.
   * @return {Promise<void>}
   */
  abstract removeExpiresIn(): Promise<void>;

  /** Clean up BroadcastChannel and storage listener in the case of browser environment (e.g., on logout). */
  abstract dispose(): void;

  /** True if the token has already expired OR will expire within the next 24 hours. */
  get isExpired(): boolean {
    if (this.expiresIn === null || isNaN(this.expiresIn)) return true;

    const oneDayInMilliseconds = 24 * 60 * 60 * 1000;
    // Consider token expired when the current time is later than (expiry − 1 day).
    return Date.now() >= this.expiresIn - oneDayInMilliseconds;
  }
}
