import { expect, test, vi, beforeEach, afterEach } from 'vitest';
import { createGameStore } from '@app/game/state/store.ts';
import { LETTERS, MAX_ATTEMPTS, WORD_LENGTH } from '@app/game/constants.ts';
import { Letter, LetterMark } from '@app/game/types.ts';
import answers from '@app/assets/answers.ts';

beforeEach(() => {
  vi.useFakeTimers();
});

afterEach(() => {
  vi.useRealTimers();
});

test('Basic typing and guard rails', () => {
  const store = createGameStore({
    mode: 'endless',
    answerSeed: 0,
    wordLength: WORD_LENGTH,
    maxAttempts: MAX_ATTEMPTS,
    getAnswerBySeed(seed: number): Letter[] {
      return answers[seed]
        .split('')
        .map((letter) => letter.toUpperCase() as Letter);
    },
  });
  store.nextRound(0);

  const expected: Letter[] = ['A', 'P', 'P', 'L', 'E'];
  const input: Letter[] = ['A', 'P', 'P', 'L', 'E', 'S'];
  input.forEach((letter) => store.inputLetter(letter));

  const state = store.getState();
  const currentRow = state.row;
  const gridRow = [...state.grid[currentRow]];

  expect(gridRow.map((tile) => tile.letter)).toEqual(expected);

  store.deleteLetter();
  store.deleteLetter();
  expect(state.grid[currentRow].length).toBe(3);

  store.submit();
  expect(state.status).not.toBe('validating');
  expect(state.status).toBe('typing');
});

test('Valid submit → reveal → continue typing', () => {
  const handleFlipTile = vi.fn();
  const handleRevealStart = vi.fn();
  const handleRevealEnd = vi.fn();
  const store = createGameStore({
    mode: 'endless',
    answerSeed: 0,
    wordLength: WORD_LENGTH,
    maxAttempts: MAX_ATTEMPTS,
    getAnswerBySeed(seed: number): Letter[] {
      return answers[seed]
        .split('')
        .map((letter) => letter.toUpperCase() as Letter);
    },
    onFlipTile: handleFlipTile,
    onRowRevealStart: handleRevealStart,
    onRowRevealEnd: handleRevealEnd,
  });
  store.nextRound(0);

  const input: Letter[] = ['A', 'B', 'A', 'C', 'K'];
  const expectedKeyboard = LETTERS.reduce(
    (acc, cur) => ({ ...acc, [cur]: undefined }),
    {} as Record<Letter, LetterMark | undefined>,
  );

  input.forEach((letter) => store.inputLetter(letter));
  store.submit();

  expect(store.getState().status).toBe('revealing');
  expect(handleRevealStart).toBeCalledTimes(1);
  expect(handleRevealStart).toBeCalledWith(store.getState().row);

  vi.runAllTimers();
  input.forEach((letter) => (expectedKeyboard[letter] = 'correct'));

  expect(handleFlipTile).toBeCalledTimes(5);
  expect(handleRevealEnd).toBeCalledTimes(1);
  expect(handleRevealEnd).toBeCalledWith(
    store.getState().row,
    store.getState().lastResult,
  );
  expect(store.getState().status).toBe('round_end_win');
  expect(store.getState().keyboard).toEqual(expectedKeyboard);
});

test('Loss after max attempts', () => {
  const store = createGameStore({
    mode: 'endless',
    answerSeed: 0,
    wordLength: WORD_LENGTH,
    maxAttempts: MAX_ATTEMPTS,
    getAnswerBySeed(seed: number): Letter[] {
      return answers[seed]
        .split('')
        .map((letter) => letter.toUpperCase() as Letter);
    },
  });
  store.nextRound(0);

  const input: Letter[] = ['A', 'P', 'P', 'L', 'E'];
  for (let attempt = 0; attempt < MAX_ATTEMPTS; attempt++) {
    input.forEach((letter) => store.inputLetter(letter));
    store.submit();
    vi.runAllTimers();
  }

  expect(store.getState().status).toBe('round_end_loss');
});

test('Next round resets correctly', () => {
  const store = createGameStore({
    mode: 'endless',
    answerSeed: 0,
    wordLength: WORD_LENGTH,
    maxAttempts: MAX_ATTEMPTS,
    getAnswerBySeed(seed: number): Letter[] {
      return answers[seed]
        .split('')
        .map((letter) => letter.toUpperCase() as Letter);
    },
  });
  store.nextRound(0);

  const state = store.getState();
  expect(state.status).toBe('typing');
  expect(state.row).toBe(0);
  expect(state.grid.every((row) => row.length === 0)).toBe(true);
  expect(
    Object.values(state.keyboard).every((mark) => mark === undefined),
  ).toBe(true);
});

test('Re-entrancy locks', () => {
  const store = createGameStore({
    mode: 'endless',
    answerSeed: 0,
    wordLength: WORD_LENGTH,
    maxAttempts: MAX_ATTEMPTS,
    getAnswerBySeed(seed: number): Letter[] {
      return answers[seed]
        .split('')
        .map((letter) => letter.toUpperCase() as Letter);
    },
  });
  store.nextRound(0);

  const input: Letter[] = ['A', 'P', 'P', 'L', 'E'];
  const currentRow = store.getState().row;
  input.forEach((letter) => store.inputLetter(letter));
  store.submit();

  expect(store.getState().status).toBe('revealing');

  // These should be no-ops:
  store.inputLetter('A');
  expect(store.getState().status).toBe('revealing');
  expect(store.getState().grid[currentRow].length).toBe(5);

  store.deleteLetter();
  expect(store.getState().status).toBe('revealing');
  expect(store.getState().grid[currentRow].length).toBe(5);

  store.submit();
  expect(store.getState().status).toBe('revealing');
  expect(store.getState().grid[currentRow].length).toBe(5);

  vi.runAllTimers();
});

test('Mode change while idle/typing', () => {
  const store = createGameStore({
    mode: 'endless',
    answerSeed: 0,
    wordLength: WORD_LENGTH,
    maxAttempts: MAX_ATTEMPTS,
    getAnswerBySeed(seed: number): Letter[] {
      return answers[seed]
        .split('')
        .map((letter) => letter.toUpperCase() as Letter);
    },
  });
  expect(store.getState().mode).toBe('endless');

  store.dispatch({
    type: 'MODE_CHANGE',
    mode: 'daily',
    answerSeed: 0,
    currentAnswer: ['A', 'P', 'P', 'L', 'E'],
  });
  expect(store.getState().mode).toBe('daily');
  expect(store.getState().answerSeed).toBe(0);

  store.nextRound(0);
  expect(store.getState().status).toBe('typing');

  store.dispatch({
    type: 'MODE_CHANGE',
    mode: 'endless',
    answerSeed: 0,
    currentAnswer: ['A', 'P', 'P', 'L', 'E'],
  });
  expect(store.getState().mode).toBe('endless');
  expect(store.getState().answerSeed).toBe(0);
});
