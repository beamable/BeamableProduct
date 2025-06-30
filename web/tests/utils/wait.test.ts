import { describe, expect, it, afterEach, vi } from 'vitest';
import { wait } from '@/utils/wait';

describe('wait()', () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it('resolves after the specified delay', async () => {
    vi.useFakeTimers();
    const delay = 100;
    let resolved = false;
    wait(delay).then(() => {
      resolved = true;
    });
    vi.advanceTimersByTime(delay - 1);
    await Promise.resolve();
    expect(resolved).toBe(false);
    vi.advanceTimersByTime(1);
    await Promise.resolve();
    expect(resolved).toBe(true);
  });

  it('rejects if the signal is aborted before the delay', async () => {
    vi.useFakeTimers();
    const controller = new AbortController();
    const delay = 100;
    let rejected = false;
    wait(delay, controller.signal).catch(() => {
      rejected = true;
    });
    controller.abort();
    vi.advanceTimersByTime(delay);
    await Promise.resolve();
    expect(rejected).toBe(true);
  });
});
