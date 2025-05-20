import { TokenStorage } from '@/platform/types/TokenStorage';

export class MemoryTokenStorage implements TokenStorage {
  private token: string | null = null;

  async getToken() {
    return this.token;
  }

  async setToken(token: string) {
    this.token = token;
  }

  async removeToken() {
    this.token = null;
  }
}