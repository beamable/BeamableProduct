import { expect, test } from 'vitest';
import { guessEvaluator } from '@app/game/engine/guessEvaluator.ts';
import { Letter } from '@app/game/types.ts';

test('answer=APPLE, guess=ALLEY', () => {
  const answer = 'APPLE'.split('').map((ch) => ch as Letter);
  const guess = 'ALLEY'.split('').map((ch) => ch as Letter);
  const result = guessEvaluator(answer, guess);

  expect(result.marks).toEqual([
    'correct',
    'present',
    'absent',
    'present',
    'absent',
  ]);
  expect(result.isWin).toBe(false);
});

test('answer=LEVEL, guess=LEMON', () => {
  const answer = 'LEVEL'.split('').map((ch) => ch as Letter);
  const guess = 'LEMON'.split('').map((ch) => ch as Letter);
  const result = guessEvaluator(answer, guess);

  expect(result.marks).toEqual([
    'correct',
    'correct',
    'absent',
    'absent',
    'absent',
  ]);
  expect(result.isWin).toBe(false);
});

test('answer=ABBEY, guess=BABES', () => {
  const answer = 'ABBEY'.split('').map((ch) => ch as Letter);
  const guess = 'BABES'.split('').map((ch) => ch as Letter);
  const result = guessEvaluator(answer, guess);

  expect(result.marks).toEqual([
    'present',
    'present',
    'correct',
    'correct',
    'absent',
  ]);
  expect(result.isWin).toBe(false);
});

test('answer=CRANE, guess=CRANE', () => {
  const answer = 'CRANE'.split('').map((ch) => ch as Letter);
  const guess = 'CRANE'.split('').map((ch) => ch as Letter);
  const result = guessEvaluator(answer, guess);

  expect(result.marks).toEqual([
    'correct',
    'correct',
    'correct',
    'correct',
    'correct',
  ]);
  expect(result.isWin).toBe(true);
});

test('answer=PLATE, guess=BRING', () => {
  const answer = 'PLATE'.split('').map((ch) => ch as Letter);
  const guess = 'BRING'.split('').map((ch) => ch as Letter);
  const result = guessEvaluator(answer, guess);

  expect(result.marks).toEqual([
    'absent',
    'absent',
    'absent',
    'absent',
    'absent',
  ]);
  expect(result.isWin).toBe(false);
});
