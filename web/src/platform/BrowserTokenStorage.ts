import { TokenStorage } from '@/platform/types/TokenStorage';

const BASE_ACCESS_TOKEN_KEY = 'beam_access_token';
const BASE_REFRESH_TOKEN_KEY = 'beam_refresh_token';
const BASE_BROADCAST_CHANNEL = 'beam_token_storage';

/**
 * TokenStorage implementation for browser environments.
 * Stores tokens in memory and localStorage, and synchronizes across tabs.
 */
export class BrowserTokenStorage implements TokenStorage {
  private readonly prefix: string;
  private accessToken: string | null;
  private refreshToken: string | null;
  private bc: BroadcastChannel | null = null;

  constructor(tag: string) {
    this.prefix = `${tag}_`;
    this.accessToken = localStorage.getItem(this.accessTokenKey);
    this.refreshToken = localStorage.getItem(this.refreshTokenKey);

    if (typeof BroadcastChannel !== 'undefined') {
      this.bc = new BroadcastChannel(this.broadcastChannelName);
      this.bc.onmessage = (event) => {
        const { type, token } = event.data as {
          type: 'access' | 'refresh';
          token: string | null;
        };

        if (type === 'access') {
          this.accessToken = token;
          if (token == null) {
            localStorage.removeItem(this.accessTokenKey);
          } else {
            localStorage.setItem(this.accessTokenKey, token);
          }
        } else {
          this.refreshToken = token;
          if (token == null) {
            localStorage.removeItem(this.refreshTokenKey);
          } else {
            localStorage.setItem(this.refreshTokenKey, token);
          }
        }
      };
    }

    window.addEventListener('storage', (e) => {
      if (e.key === this.accessTokenKey) {
        this.accessToken = e.newValue;
      } else if (e.key === this.refreshTokenKey) {
        this.refreshToken = e.newValue;
      }
    });
  }

  async getAccessToken(): Promise<string | null> {
    return this.accessToken;
  }

  async setAccessToken(token: string): Promise<void> {
    this.accessToken = token;
    localStorage.setItem(this.accessTokenKey, token);
    this.bc?.postMessage({ type: 'access', token });
  }

  async removeAccessToken(): Promise<void> {
    this.accessToken = null;
    localStorage.removeItem(this.accessTokenKey);
    this.bc?.postMessage({ type: 'access', token: null });
  }

  async getRefreshToken(): Promise<string | null> {
    return this.refreshToken;
  }

  async setRefreshToken(token: string): Promise<void> {
    this.refreshToken = token;
    localStorage.setItem(this.refreshTokenKey, token);
    this.bc?.postMessage({ type: 'refresh', token });
  }

  async removeRefreshToken(): Promise<void> {
    this.refreshToken = null;
    localStorage.removeItem(this.refreshTokenKey);
    this.bc?.postMessage({ type: 'refresh', token: null });
  }

  get accessTokenKey() {
    return this.prefix + BASE_ACCESS_TOKEN_KEY;
  }

  get refreshTokenKey() {
    return this.prefix + BASE_REFRESH_TOKEN_KEY;
  }

  get broadcastChannelName() {
    return this.prefix + BASE_BROADCAST_CHANNEL;
  }
}
