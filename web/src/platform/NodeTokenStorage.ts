import { TokenStorage } from '@/platform/types/TokenStorage';

/**
 * TokenStorage implementation for Node.js environments.
 * Stores tokens in memory for the duration of the process.
 */
export class NodeTokenStorage implements TokenStorage {
  private accessToken: string | null = null;
  private refreshToken: string | null = null;

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
}
