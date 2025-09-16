import { TokenStorage, type TokenData } from '@/platform/types/TokenStorage';

const BASE_ACCESS_TOKEN_KEY = 'beam_access_token';
const BASE_REFRESH_TOKEN_KEY = 'beam_refresh_token';
const BASE_EXPIRES_IN_KEY = 'beam_expires_in';
const BASE_BROADCAST_CHANNEL = 'beam_token_storage';

/** Payload interface for BroadcastChannel messages. */
interface TokenMessage {
  type: 'access' | 'refresh' | 'expiresIn';
  value: string | null;
}

/**
 * TokenStorage implementation for browser environments.
 * Stores tokens in memory and localStorage, and synchronizes across tabs.
 */
export class BrowserTokenStorage extends TokenStorage {
  private readonly prefix: string;
  private bc: BroadcastChannel | null = null;
  private readonly storageListener: (e: StorageEvent) => void;

  constructor(pid: string, tag?: string) {
    super();
    this.prefix = tag ? `${tag}_${pid}_` : `${pid}_`;

    // Initialize in-memory values from localStorage,
    this.accessToken = localStorage.getItem(this.accessTokenKey);
    this.refreshToken = localStorage.getItem(this.refreshTokenKey);

    const rawExpires = localStorage.getItem(this.expiresInKey);
    this.expiresIn = rawExpires !== null ? Number(rawExpires) : null;

    // Set up cross-tab sync via BroadcastChannel, if supported
    if (typeof BroadcastChannel !== 'undefined') {
      this.bc = new BroadcastChannel(this.broadcastChannelName);
      this.bc.onmessage = (event) => {
        const data = event.data as TokenMessage | null;
        if (!data) return;

        const { type, value } = data;
        switch (type) {
          case 'access':
            this.accessToken = value;
            if (value === null) {
              localStorage.removeItem(this.accessTokenKey);
            } else {
              localStorage.setItem(this.accessTokenKey, value);
            }
            break;

          case 'refresh':
            this.refreshToken = value;
            if (value === null) {
              localStorage.removeItem(this.refreshTokenKey);
            } else {
              localStorage.setItem(this.refreshTokenKey, value);
            }
            break;

          case 'expiresIn':
            if (value === null) {
              this.expiresIn = null;
              localStorage.removeItem(this.expiresInKey);
            } else {
              this.expiresIn = Number(value);
              localStorage.setItem(this.expiresInKey, value);
            }
            break;
        }
      };
      this.bc.onmessageerror = (err) =>
        console.error('BroadcastChannel error:', err);
    }

    // Listen to other tabs localStorage changes
    this.storageListener = (e: StorageEvent) => {
      if (e.key === this.accessTokenKey) {
        this.accessToken = e.newValue;
      } else if (e.key === this.refreshTokenKey) {
        this.refreshToken = e.newValue;
      } else if (e.key === this.expiresInKey) {
        this.expiresIn = e.newValue !== null ? Number(e.newValue) : null;
      }
    };
    window.addEventListener('storage', this.storageListener);
  }

  async getTokenData(): Promise<TokenData> {
    return {
      accessToken: this.accessToken,
      refreshToken: this.refreshToken,
      expiresIn: this.expiresIn,
    };
  }

  async setTokenData(data: Partial<TokenData>): Promise<this> {
    if ('accessToken' in data) {
      this.accessToken = data.accessToken ?? null;
      if (this.accessToken === null) {
        localStorage.removeItem(this.accessTokenKey);
      } else {
        localStorage.setItem(this.accessTokenKey, this.accessToken);
      }
      this.bc?.postMessage({ type: 'access', value: this.accessToken });
    }

    if ('refreshToken' in data) {
      this.refreshToken = data.refreshToken ?? null;
      if (this.refreshToken === null) {
        localStorage.removeItem(this.refreshTokenKey);
      } else {
        localStorage.setItem(this.refreshTokenKey, this.refreshToken);
      }
      this.bc?.postMessage({ type: 'refresh', value: this.refreshToken });
    }

    if ('expiresIn' in data) {
      this.expiresIn = data.expiresIn ?? null;
      if (this.expiresIn === null) {
        localStorage.removeItem(this.expiresInKey);
        this.bc?.postMessage({ type: 'expiresIn', value: null });
      } else {
        localStorage.setItem(this.expiresInKey, String(this.expiresIn));
        this.bc?.postMessage({
          type: 'expiresIn',
          value: String(this.expiresIn),
        });
      }
    }

    return this;
  }

  async clear(): Promise<void> {
    // Remove all 'access token', 'refresh token' and 'expires in' entries from localStorage regardless of prefix
    const keys: string[] = [];
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (
        key !== null &&
        (key.endsWith(BASE_ACCESS_TOKEN_KEY) ||
          key.endsWith(BASE_REFRESH_TOKEN_KEY) ||
          key.endsWith(BASE_EXPIRES_IN_KEY))
      ) {
        keys.push(key);
      }
    }
    keys.forEach((key) => localStorage.removeItem(key));

    // Reset in-memory values
    this.accessToken = null;
    this.refreshToken = null;
    this.expiresIn = null;

    // Broadcast removal to other tabs for this prefix
    this.bc?.postMessage({ type: 'access', value: null });
    this.bc?.postMessage({ type: 'refresh', value: null });
    this.bc?.postMessage({ type: 'expiresIn', value: null });
  }

  dispose(): void {
    this.bc?.close();
    window.removeEventListener('storage', this.storageListener);
  }

  private get accessTokenKey(): string {
    return this.prefix + BASE_ACCESS_TOKEN_KEY;
  }

  private get refreshTokenKey(): string {
    return this.prefix + BASE_REFRESH_TOKEN_KEY;
  }

  private get expiresInKey(): string {
    return this.prefix + BASE_EXPIRES_IN_KEY;
  }

  private get broadcastChannelName(): string {
    return this.prefix + BASE_BROADCAST_CHANNEL;
  }
}
