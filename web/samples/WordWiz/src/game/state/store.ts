import {
  GameEvent,
  GameMode,
  GameState,
  GuessResult,
  Letter,
  LetterMark,
} from '@app/game/types.ts';
import {
  FLIP_STAGGER,
  LETTERS,
  RESULT_REVEAL_DELAY,
  TILE_FLIP_DURATION,
} from '@app/game/constants.ts';
import { canSubmit, isAllowedWord } from '@app/game/engine/validators.ts';
import { stateMachine } from '@app/game/state/machine.ts';

interface GameStoreOptions {
  mode: GameMode;
  answerSeed: number;
  wordLength: number;
  maxAttempts: number;
  getAnswerBySeed(seed: number): Letter[];
  onShakeGridRow?(this: void, row: number, reason: string): void;
  onFlipTile?(this: void, row: number, col: number, mark: LetterMark): void;
  onRowRevealStart?(this: void, row: number): void;
  onRowRevealEnd?(this: void, row: number, result: GuessResult): void;
  onGameEnd?(this: void, state: GameState): void;
}

export interface GameStoreCallbacks {
  onShakeGridRow?(this: void, row: number, reason: string): void;
  onFlipTile?(this: void, row: number, col: number, mark: LetterMark): void;
  onRowRevealStart?(this: void, row: number): void;
  onRowRevealEnd?(this: void, row: number, result: GuessResult): void;
  onGameEnd?(this: void, state: GameState): void;
}

export interface GameStore {
  stats: Record<string, string>;
  getState(): GameState;
  dispatch(event: GameEvent): void;
  subscribe(fn: (state: GameState) => void): void;
  inputLetter(letter: Letter): void;
  deleteLetter(): void;
  submit(): void;
  nextRound(answerSeed: number): void;
  changeMode(mode: GameMode, answerSeed: number): void;
  setCallback(callback: GameStoreCallbacks): void;
  reset(): void;
}

function buildInitialState(options: GameStoreOptions): GameState {
  return {
    mode: options.mode,
    row: 0,
    grid: Array.from({ length: options.maxAttempts }, () => []),
    keyboard: LETTERS.reduce(
      (acc, cur) => ({ ...acc, [cur]: undefined }),
      {} as Record<Letter, LetterMark | undefined>,
    ),
    currentAnswer: options.getAnswerBySeed(options.answerSeed),
    answerSeed: options.answerSeed,
    wordLength: options.wordLength,
    maxAttempts: options.maxAttempts,
    isSubmitting: false,
    status: 'idle',
    lastResult: undefined,
  };
}

export function createGameStore(options: GameStoreOptions): GameStore {
  let state = buildInitialState(options);
  let subscribers: ((state: GameState) => void)[] = [];

  const store: GameStore = {
    stats: {},
    getState() {
      return state;
    },
    dispatch(event: GameEvent) {
      state = stateMachine(state, event);
      subscribers.forEach((fn) => fn(state));
    },
    subscribe(fn: (state: GameState) => void) {
      subscribers.push(fn);
    },
    inputLetter(letter: Letter) {
      store.dispatch({ type: 'INPUT_LETTER', letter });
    },
    deleteLetter() {
      store.dispatch({ type: 'DELETE' });
    },
    submit() {
      if (state.isSubmitting) return;

      const gridRow = state.grid[state.row];
      const rowWord = gridRow.map((tile) => tile.letter).join('');

      if (!canSubmit(rowWord)) {
        options.onShakeGridRow?.(state.row, 'Not enough letters');
        return;
      }

      if (!isAllowedWord(rowWord)) {
        options.onShakeGridRow?.(state.row, 'Not in word list');
        return;
      }

      store.dispatch({ type: 'SUBMIT' });

      if (state.status !== 'revealing') return;

      // Reveal tiles
      const row = state.row;
      options.onRowRevealStart?.(row);
      for (let col = 0; col <= options.wordLength - 1; col++) {
        setTimeout(() => {
          const tileLetter = state.grid[row][col].letter;
          const keyboardLetterMark = state.keyboard[tileLetter];
          const tileLetterMark = state.grid[row][col].mark;

          options.onFlipTile?.(state.row, col, tileLetterMark ?? 'absent');

          // Update keyboard state with priority: correct > present > absent
          state.keyboard = {
            ...state.keyboard,
            [state.grid[row][col].letter]:
              keyboardLetterMark === 'correct'
                ? keyboardLetterMark
                : keyboardLetterMark === 'present' &&
                    tileLetterMark === 'absent'
                  ? keyboardLetterMark
                  : tileLetterMark,
          };

          if (col !== options.wordLength - 1) return;

          // Last tile, schedule REVEAL_DONE event
          setTimeout(() => {
            options.onRowRevealEnd?.(row, state.lastResult!);
            store.dispatch({ type: 'REVEAL_DONE' });

            const isNotRoundEnd =
              state.status !== 'round_end_win' &&
              state.status !== 'round_end_loss';
            if (isNotRoundEnd) return;

            setTimeout(() => {
              options.onGameEnd?.(state);
            }, RESULT_REVEAL_DELAY);
          }, TILE_FLIP_DURATION + FLIP_STAGGER); // FLIP_STAGGER as buffer
        }, col * FLIP_STAGGER);
      }
    },
    nextRound(answerSeed: number) {
      store.dispatch({
        type: 'NEXT_ROUND',
        currentAnswer: options.getAnswerBySeed(answerSeed),
        answerSeed,
      });
    },
    changeMode(mode: GameMode, answerSeed: number) {
      store.dispatch({
        type: 'MODE_CHANGE',
        currentAnswer: options.getAnswerBySeed(answerSeed),
        mode,
        answerSeed,
      });
    },
    setCallback(callbacks: GameStoreCallbacks) {
      if (callbacks.onShakeGridRow) {
        options.onShakeGridRow = callbacks.onShakeGridRow;
      }
      if (callbacks.onFlipTile) {
        options.onFlipTile = callbacks.onFlipTile;
      }
      if (callbacks.onRowRevealStart) {
        options.onRowRevealStart = callbacks.onRowRevealStart;
      }
      if (callbacks.onRowRevealEnd) {
        options.onRowRevealEnd = callbacks.onRowRevealEnd;
      }
      if (callbacks.onGameEnd) {
        options.onGameEnd = callbacks.onGameEnd;
      }
    },
    reset() {
      state = buildInitialState(options);
      subscribers = [];
    },
  };

  return store;
}
