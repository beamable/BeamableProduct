import { TokenStorage } from '@/platform/types/TokenStorage';

/**
 * TokenStorage implementation for Node.js environments.
 * Stores tokens in memory for the duration of the process.
 */
export class NodeTokenStorage extends TokenStorage {
  async getAccessToken(): Promise<string | null> {
    return this.accessToken;
  }

  async setAccessToken(token: string): Promise<void> {
    this.accessToken = token;
  }

  async removeAccessToken(): Promise<void> {
    this.accessToken = null;
  }

  async getRefreshToken(): Promise<string | null> {
    return this.refreshToken;
  }

  async setRefreshToken(token: string): Promise<void> {
    this.refreshToken = token;
  }

  async removeRefreshToken(): Promise<void> {
    this.refreshToken = null;
  }

  async getExpiresIn(): Promise<number | null> {
    return this.expiresIn;
  }

  async setExpiresIn(expiresIn: number): Promise<void> {
    this.expiresIn = expiresIn;
  }

  async removeExpiresIn(): Promise<void> {
    this.expiresIn = null;
  }

  dispose() {}
}
