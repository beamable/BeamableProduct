import { describe, expect, it, beforeEach, afterEach, vi } from 'vitest';
import { TokenStorage } from '@/platform/types/TokenStorage';

class DummyStorage extends TokenStorage {
  async getAccessToken() {
    return this.accessToken;
  }
  async setAccessToken(token: string) {
    this.accessToken = token;
  }
  async removeAccessToken() {
    this.accessToken = null;
  }
  async getRefreshToken() {
    return this.refreshToken;
  }
  async setRefreshToken(token: string) {
    this.refreshToken = token;
  }
  async removeRefreshToken() {
    this.refreshToken = null;
  }
  async getExpiresIn() {
    return this.expiresIn;
  }
  async setExpiresIn(expiresIn: number) {
    this.expiresIn = expiresIn;
  }
  async removeExpiresIn() {
    this.expiresIn = null;
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
    storage.setExpiresIn(NaN);
    expect(storage.isExpired).toBe(true);
  });

  it('returns false when expiresIn is more than one day in the future', () => {
    const storage = new DummyStorage();
    const twoDays = 2 * 24 * 60 * 60 * 1000;
    storage.setExpiresIn(Date.now() + twoDays);
    expect(storage.isExpired).toBe(false);
  });

  it('returns true when expiresIn is less than one day in the future', () => {
    const storage = new DummyStorage();
    const twelveHours = 12 * 60 * 60 * 1000;
    storage.setExpiresIn(Date.now() + twelveHours);
    expect(storage.isExpired).toBe(true);
  });
});
