export interface TokenData {
  accessToken: string | null;
  refreshToken: string | null;
  expiresIn: number | null;
}

/**
 * Abstraction for managing a persisted authentication token.
 */
export abstract class TokenStorage {
  protected accessToken: string | null = null;
  protected refreshToken: string | null = null;
  protected expiresIn: number | null = null;

  /**
   * Asynchronously loads persisted tokens into memory.
   *
   * Called once by the SDK at the start of `connect()`, before any synchronous
   * read of {@link isExpired}. Implementations backed by a synchronous store
   * (browser localStorage, Node filesystem) load in their constructor and leave
   * this a no-op; implementations backed by an asynchronous store (e.g. React
   * Native AsyncStorage) override it to load their tokens here. Must be
   * idempotent — the SDK may call it more than once.
   */
  async hydrate(): Promise<void> {}

  /** Retrieves the stored token data. */
  abstract getTokenData(): Promise<TokenData>;

  /**
   * Updates the stored token data. Fields not provided are left unchanged.
   * Set a field to `null` to clear it.
   * @remarks When setting the `expiresIn`, use the raw `expires_in` from the token response plus Date.now() to compute the absolute expiry time.
   */
  abstract setTokenData(data: Partial<TokenData>): Promise<this>;

  /** Clears all stored tokens and expiry information. */
  abstract clear(): void;

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
