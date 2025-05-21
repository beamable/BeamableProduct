import { describe, it, expect, afterEach } from 'vitest';
import { isBrowserEnv } from '@/utils/isBrowserEnv';

describe('isBrowserEnv()', () => {
  // Capture the original globalThis.window (if any) so we can restore it
  const originalWindow = (globalThis as any).window;

  afterEach(() => {
    // Restore globalThis.window to its original state
    if (originalWindow === undefined) {
      delete (globalThis as any).window;
    } else {
      (globalThis as any).window = originalWindow;
    }
  });

  it('returns false when window is undefined', () => {
    // simulate non-browser environment
    delete (globalThis as any).window;
    expect(isBrowserEnv()).toBe(false);
  });

  it('returns false when window exists but document is undefined', () => {
    // simulate a window without document
    (globalThis as any).window = {} as Window;
    expect(isBrowserEnv()).toBe(false);
  });

  it('returns true when window and document exist', () => {
    // simulate a full browser environment
    (globalThis as any).window = { document: {} } as any;
    expect(isBrowserEnv()).toBe(true);
  });
});
