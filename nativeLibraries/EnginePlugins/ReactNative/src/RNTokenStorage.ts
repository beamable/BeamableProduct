import AsyncStorage from '@react-native-async-storage/async-storage';
import type { TokenData } from '@beamable/sdk';

const ACCESS = 'access_token';
const REFRESH = 'refresh_token';
const EXPIRES = 'expires_in';
const ONE_DAY_MS = 24 * 60 * 60 * 1000;

/**
 * React Native implementation of the SDK's `TokenStorage`, backed by
 * AsyncStorage. This is the SDK's officially supported extension point for
 * custom persistence (see the SDK README "Custom token storage").
 *
 * The SDK's `TokenStorage` is an abstract class with `protected` members and
 * is exported type-only, so it can be neither `extends`-ed nor `implements`-ed
 * externally. We replicate its public shape (including `isExpired`) here as a
 * plain class and cast to `TokenStorage` at the `Beam.init()` call site — the
 * SDK only ever calls the public methods/getter below.
 *
 * Use the async `create()` factory so persisted tokens are loaded into memory
 * before `Beam.init()` reads `isExpired` synchronously.
 */
export class RNTokenStorage {
  private accessToken: string | null = null;
  private refreshToken: string | null = null;
  private expiresIn: number | null = null;
  private readonly prefix: string;

  private constructor(pid: string, tag?: string) {
    this.prefix = tag ? `${tag}_${pid}_` : `${pid}_`;
  }

  static async create(pid: string, tag?: string): Promise<RNTokenStorage> {
    const storage = new RNTokenStorage(pid, tag);
    const entries = await AsyncStorage.multiGet([
      storage.key(ACCESS),
      storage.key(REFRESH),
      storage.key(EXPIRES),
    ]);
    const map = Object.fromEntries(entries) as Record<string, string | null>;
    storage.accessToken = map[storage.key(ACCESS)] ?? null;
    storage.refreshToken = map[storage.key(REFRESH)] ?? null;
    const rawExpires = map[storage.key(EXPIRES)];
    storage.expiresIn = rawExpires != null ? Number(rawExpires) : null;
    return storage;
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
      await this.persist(ACCESS, this.accessToken);
    }
    if ('refreshToken' in data) {
      this.refreshToken = data.refreshToken ?? null;
      await this.persist(REFRESH, this.refreshToken);
    }
    if ('expiresIn' in data) {
      this.expiresIn = data.expiresIn ?? null;
      await this.persist(
        EXPIRES,
        this.expiresIn == null ? null : String(this.expiresIn),
      );
    }
    return this;
  }

  async clear(): Promise<void> {
    this.accessToken = null;
    this.refreshToken = null;
    this.expiresIn = null;
    await AsyncStorage.multiRemove([
      this.key(ACCESS),
      this.key(REFRESH),
      this.key(EXPIRES),
    ]);
  }

  dispose(): void {
    // No background resources (BroadcastChannel / listeners) to release in RN.
  }

  /** True if the token is missing, already expired, or expires within 24h. */
  get isExpired(): boolean {
    if (this.expiresIn === null || isNaN(this.expiresIn)) return true;
    return Date.now() >= this.expiresIn - ONE_DAY_MS;
  }

  private key(base: string): string {
    return `beam_${this.prefix}${base}`;
  }

  private async persist(base: string, value: string | null): Promise<void> {
    if (value === null) await AsyncStorage.removeItem(this.key(base));
    else await AsyncStorage.setItem(this.key(base), value);
  }
}
