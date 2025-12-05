import { describe, expect, it, beforeEach, afterEach, vi } from 'vitest';
import { TokenStorage } from '@/platform/types/TokenStorage';

class DummyStorage extends TokenStorage {
  async getTokenData() {
    return {
      accessToken: this.accessToken,
      refreshToken: this.refreshToken,
      expiresIn: this.expiresIn,
    };
  }
  async setTokenData(
    data: Partial<{
      accessToken: string | null;
      refreshToken: string | null;
      expiresIn: number | null;
    }>,
  ) {
    if ('accessToken' in data) this.accessToken = data.accessToken ?? null;
    if ('refreshToken' in data) this.refreshToken = data.refreshToken ?? null;
    if ('expiresIn' in data) this.expiresIn = data.expiresIn ?? null;
    return this;
  }
  clear() {
    this.accessToken = null;
    this.refreshToken = null;
    this.expiresIn = null;
  }
  dispose() {}
}

describe('TokenStorage', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2020-01-01T00:00:00.000Z').getTime());
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('returns true when expiresIn is null or NaN', () => {
    const storage = new DummyStorage();
    expect(storage.isExpired).toBe(true);
    storage.setTokenData({ expiresIn: NaN });
    expect(storage.isExpired).toBe(true);
  });

  it('returns false when expiresIn is more than one day in the future', () => {
    const storage = new DummyStorage();
    const twoDays = 2 * 24 * 60 * 60 * 1000;
    storage.setTokenData({ expiresIn: Date.now() + twoDays });
    expect(storage.isExpired).toBe(false);
  });

  it('returns true when expiresIn is less than one day in the future', () => {
    const storage = new DummyStorage();
    const twelveHours = 12 * 60 * 60 * 1000;
    storage.setTokenData({ expiresIn: Date.now() + twelveHours });
    expect(storage.isExpired).toBe(true);
  });
});
