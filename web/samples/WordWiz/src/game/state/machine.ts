import { GameEvent, GameState, Letter, LetterMark } from '@app/game/types.ts';
import { LETTERS } from '@app/game/constants.ts';
import { guessEvaluator } from '@app/game/engine/guessEvaluator.ts';

export function stateMachine(state: GameState, event: GameEvent) {
  const currentRow = state.row;
  const maxAttempts = state.maxAttempts;
  const gridRow = [...state.grid[currentRow]];

  switch (event.type) {
    case 'INPUT_LETTER': {
      if (state.status === 'typing') {
        if (gridRow.length < state.wordLength) {
          state.grid[currentRow] = [...gridRow, { letter: event.letter }];
        }
      }
      return state;
    }
    case 'DELETE': {
      if (state.status === 'typing') {
        if (gridRow.length > 0) {
          gridRow.pop();
          state.grid[currentRow] = [...gridRow];
        }
      }
      return state;
    }
    case 'SUBMIT': {
      if (state.status === 'typing') {
        state.isSubmitting = true;
        state.status = 'validating';
        const lastResult = guessEvaluator(
          state.currentAnswer,
          gridRow.map((tile) => tile.letter),
        );
        state.lastResult = lastResult;
        state.grid[currentRow] = state.grid[currentRow].map((tile, i) => ({
          ...tile,
          mark: lastResult.marks[i],
        }));
        state.status = 'revealing';
      }
      return state;
    }
    case 'REVEAL_DONE': {
      state.isSubmitting = false;
      const lastResult = state.lastResult;
      if (!lastResult) return state;

      if (lastResult.isWin) {
        state.status = 'round_end_win';
      } else if (currentRow === maxAttempts - 1) {
        state.status = 'round_end_loss';
      } else {
        state.status = 'typing';
        state.row += 1;
      }
      return state;
    }
    case 'NEXT_ROUND': {
      state = {
        ...state,
        row: 0,
        grid: Array.from({ length: state.maxAttempts }, () => []),
        keyboard: LETTERS.reduce(
          (acc, cur) => ({
            ...acc,
            [cur]: undefined,
          }),
          {} as Record<Letter, LetterMark | undefined>,
        ),
        answerSeed: event.answerSeed,
        currentAnswer: event.currentAnswer,
        status: 'typing',
        lastResult: undefined,
      };
      return state;
    }
    case 'MODE_CHANGE': {
      if (state.status === 'idle' || state.status === 'typing') {
        state.mode = event.mode;
        state.status = 'typing';
        state.answerSeed = event.answerSeed;
        state.currentAnswer = event.currentAnswer;
      }
      return state;
    }
    default:
      return state;
  }
}
