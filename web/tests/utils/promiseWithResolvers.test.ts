import { describe, expect, it } from 'vitest';
import { promiseWithResolvers } from '@/utils/promiseWithResolvers';

describe('promiseWithResolvers()', () => {
  it('resolves with the provided value', async () => {
    const { promise, resolve } = promiseWithResolvers<number>();
    const value = 42;
    resolve(value);
    await expect(promise).resolves.toBe(value);
  });

  it('rejects with the provided reason', async () => {
    const { promise, reject } = promiseWithResolvers<number>();
    const error = new Error('test error');
    reject(error);
    await expect(promise).rejects.toBe(error);
  });
});
