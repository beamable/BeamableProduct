import { expect, test } from 'vitest';
import {
  canSubmit,
  isAllowedWord,
  isAlphabetic,
  isLengthOk,
} from '@app/game/engine/validators.ts';
import allowedWords from '@app/assets/allowedWords.ts';

test('isLengthOk()', () => {
  const correct = 'ALLEY';
  const wrong = 'FOOD';

  expect(isLengthOk(correct)).toBe(true);
  expect(isLengthOk(wrong)).toBe(false);
});

test('isAlphabetic()', () => {
  const correct = 'ALLEY';
  const wrong = 'FOOD1';

  expect(isAlphabetic(correct)).toBe(true);
  expect(isAlphabetic(wrong)).toBe(false);
});

test('isAllowedWord()', () => {
  const correct = 'ARGON';
  const wrong = 'FOOD';

  expect(isAllowedWord(correct, allowedWords)).toBe(true);
  expect(isAllowedWord(wrong, allowedWords)).toBe(false);
});

test('canSubmit()', () => {
  const correct = 'ALLEY';
  const wrong = 'FOOD';

  expect(canSubmit(correct)).toBe(true);
  expect(canSubmit(wrong)).toBe(false);
});
