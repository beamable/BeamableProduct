import { TokenStorage, type TokenData } from '@/platform/types/TokenStorage';
import AsyncStorage from '@react-native-async-storage/async-storage';

const BASE_ACCESS_TOKEN_KEY = 'beam_access_token';
const BASE_REFRESH_TOKEN_KEY = 'beam_refresh_token';
const BASE_EXPIRES_IN_KEY = 'beam_expires_in';

/**
 * TokenStorage implementation for React Native environments, backed by
 * AsyncStorage. Tokens are cached in memory and persisted so a guest (or
 * signed-in) session survives across app launches.
 *
 * AsyncStorage is asynchronous, but `Beam.connect()` reads {@link isExpired}
 * synchronously, so the constructor performs no I/O — {@link hydrate} loads the
 * persisted tokens into memory and the SDK awaits it before that synchronous
 * read. `hydrate()` is idempotent (a single load promise is cached).
 */
export class ReactNativeTokenStorage extends TokenStorage {
  private readonly prefix: string;
  private hydratePromise: Promise<void> | null = null;

  constructor(pid: string, tag?: string) {
    super();
    this.prefix = tag ? `${tag}_${pid}_` : `${pid}_`;
  }

  async hydrate(): Promise<void> {
    if (!this.hydratePromise) {
      this.hydratePromise = (async () => {
        const entries = await AsyncStorage.multiGet([
          this.accessTokenKey,
          this.refreshTokenKey,
          this.expiresInKey,
        ]);
        const map = Object.fromEntries(entries) as Record<
          string,
          string | null
        >;
        this.accessToken = map[this.accessTokenKey] ?? null;
        this.refreshToken = map[this.refreshTokenKey] ?? null;
        const rawExpires = map[this.expiresInKey];
        this.expiresIn = rawExpires != null ? Number(rawExpires) : null;
      })();
    }
    return this.hydratePromise;
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
      await this.persist(this.accessTokenKey, this.accessToken);
    }
    if ('refreshToken' in data) {
      this.refreshToken = data.refreshToken ?? null;
      await this.persist(this.refreshTokenKey, this.refreshToken);
    }
    if ('expiresIn' in data) {
      this.expiresIn = data.expiresIn ?? null;
      await this.persist(
        this.expiresInKey,
        this.expiresIn == null ? null : String(this.expiresIn),
      );
    }
    return this;
  }

  async clear(): Promise<void> {
    // Remove all token entries regardless of prefix (mirrors the browser/Node storages).
    const allKeys = await AsyncStorage.getAllKeys();
    const tokenKeys = allKeys.filter(
      (key) =>
        key.endsWith(BASE_ACCESS_TOKEN_KEY) ||
        key.endsWith(BASE_REFRESH_TOKEN_KEY) ||
        key.endsWith(BASE_EXPIRES_IN_KEY),
    );
    if (tokenKeys.length > 0) await AsyncStorage.multiRemove(tokenKeys);

    // Reset in-memory values.
    this.accessToken = null;
    this.refreshToken = null;
    this.expiresIn = null;
  }

  dispose(): void {
    // No background resources (BroadcastChannel / listeners) to release in RN.
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

  private async persist(key: string, value: string | null): Promise<void> {
    if (value === null) await AsyncStorage.removeItem(key);
    else await AsyncStorage.setItem(key, value);
  }
}
