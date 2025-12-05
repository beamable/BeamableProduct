import { expect, test, vi } from 'vitest';
import { getEndlessSeed } from '@app/game/engine/seed.ts';
import answers from '@app/assets/answers.ts';

test('getEndlessSeed()', () => {
  vi.doMock('@app/assets/answers.ts', () => {
    return { default: new Array(answers.length).fill(0) };
  });

  const seed = getEndlessSeed();
  expect(seed).toBeGreaterThanOrEqual(0);
  expect(seed).toBeLessThan(answers.length);
});
