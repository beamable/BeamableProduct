import { TokenStorage } from '@/platform/types/TokenStorage';

export class BrowserTokenStorage implements TokenStorage {
  private key = 'beam:token';

  async getToken() {
    return localStorage.getItem(this.key);
  }

  async setToken(token: string) {
    localStorage.setItem(this.key, token);
  }

  async removeToken() {
    localStorage.removeItem(this.key);
  }
}