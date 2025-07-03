import { describe, it, expect, afterEach } from 'vitest';
import { getUserDeviceAndPlatform } from '@/utils/getUserDeviceAndPlatform';

const originalWindow = (globalThis as any).window;
const originalNavigator = (globalThis as any).navigator;

afterEach(() => {
  if (originalWindow === undefined) {
    delete (globalThis as any).window;
  } else {
    (globalThis as any).window = originalWindow;
  }
  if (originalNavigator === undefined) {
    delete (globalThis as any).navigator;
  } else {
    (globalThis as any).navigator = originalNavigator;
  }
});

describe('getUserDeviceAndPlatform', () => {
  it('returns node platform when window or navigator is undefined', () => {
    delete (globalThis as any).window;
    delete (globalThis as any).navigator;
    expect(getUserDeviceAndPlatform()).toEqual({
      deviceType: 'Desktop',
      platform: 'Node',
    });
  });

  describe('using userAgentData when available', () => {
    it('detects mobile when userAgentData.mobile is true', () => {
      (globalThis as any).window = {} as any;
      (globalThis as any).navigator = {
        userAgent: 'Chrome',
        userAgentData: { mobile: true },
      } as any;
      expect(getUserDeviceAndPlatform()).toEqual({
        deviceType: 'Mobile',
        platform: 'Chrome',
      });
    });

    it('detects desktop when userAgentData.mobile is false', () => {
      (globalThis as any).window = {} as any;
      (globalThis as any).navigator = {
        userAgent: 'Chrome',
        userAgentData: { mobile: false },
      } as any;
      expect(getUserDeviceAndPlatform()).toEqual({
        deviceType: 'Desktop',
        platform: 'Chrome',
      });
    });
  });

  describe('fallback to old API', () => {
    it('detects tablet for iPad UA', () => {
      (globalThis as any).window = {} as any;
      (globalThis as any).navigator = {
        userAgent: 'iPad',
        maxTouchPoints: 0,
      } as any;
      expect(getUserDeviceAndPlatform()).toEqual({
        deviceType: 'Tablet',
        platform: 'Web',
      });
    });

    it('detects tablet for touch Mac without iPad UA', () => {
      (globalThis as any).window = {} as any;
      (globalThis as any).navigator = {
        userAgent: 'Macintosh',
        maxTouchPoints: 2,
      } as any;
      expect(getUserDeviceAndPlatform()).toEqual({
        deviceType: 'Tablet',
        platform: 'Web',
      });
    });

    it('detects mobile for Android with Mobile', () => {
      (globalThis as any).window = {} as any;
      (globalThis as any).navigator = {
        userAgent: 'Android Mobile',
        maxTouchPoints: 0,
      } as any;
      expect(getUserDeviceAndPlatform()).toEqual({
        deviceType: 'Mobile',
        platform: 'Web',
      });
    });

    it('detects tablet for Android without Mobile', () => {
      (globalThis as any).window = {} as any;
      (globalThis as any).navigator = {
        userAgent: 'Android',
        maxTouchPoints: 0,
      } as any;
      expect(getUserDeviceAndPlatform()).toEqual({
        deviceType: 'Tablet',
        platform: 'Web',
      });
    });

    it('detects mobile for iPhone', () => {
      (globalThis as any).window = {} as any;
      (globalThis as any).navigator = {
        userAgent: 'iPhone',
        maxTouchPoints: 0,
      } as any;
      expect(getUserDeviceAndPlatform()).toEqual({
        deviceType: 'Mobile',
        platform: 'Web',
      });
    });

    it('defaults to desktop when no device patterns match', () => {
      (globalThis as any).window = {} as any;
      (globalThis as any).navigator = {
        userAgent: 'Windows NT',
        maxTouchPoints: 0,
      } as any;
      expect(getUserDeviceAndPlatform()).toEqual({
        deviceType: 'Desktop',
        platform: 'Web',
      });
    });
  });

  describe('platform detection', () => {
    const platforms: Array<[string, string]> = [
      ['OPR', 'Opera'],
      ['Opera', 'Opera'],
      ['Edg', 'Edge'],
      ['Edge', 'Edge'],
      ['Chrome', 'Chrome'],
      ['Firefox', 'Firefox'],
      ['Safari', 'Safari'],
      ['SamsungBrowser', 'Samsung Browser'],
      ['MSIE', 'Internet Explorer'],
      ['Trident', 'Internet Explorer'],
      ['Unknown', 'Web'],
    ];

    platforms.forEach(([uaFragment, expectedPlatform]) => {
      it(`detects platform ${expectedPlatform} for UA containing ${uaFragment}`, () => {
        (globalThis as any).window = {} as any;
        (globalThis as any).navigator = {
          userAgent: uaFragment,
          maxTouchPoints: 0,
        } as any;
        expect(getUserDeviceAndPlatform().platform).toBe(expectedPlatform);
      });
    });
  });
});
