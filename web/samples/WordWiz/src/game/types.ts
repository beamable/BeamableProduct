export type GameMode = 'daily' | 'endless';
export type LetterMark = 'correct' | 'present' | 'absent';
export type Tile = { letter: Letter; mark?: LetterMark };
export type GuessResult = {
  tiles: Tile[];
  marks: LetterMark[];
  isWin: boolean;
};
export type KeyboardState = Record<Letter, LetterMark | undefined>;
export type GameState = {
  mode: GameMode;
  row: number;
  grid: Tile[][];
  keyboard: KeyboardState;
  currentAnswer: Letter[];
  answerSeed: number;
  wordLength: number;
  maxAttempts: number;
  isSubmitting: boolean;
  status: GameStateStatus;
  lastResult?: GuessResult;
};
export type GameStateStatus =
  | 'idle'
  | 'typing'
  | 'validating'
  | 'revealing'
  | 'round_end_win'
  | 'round_end_loss'
  | 'ready_next';
export type GameEvent =
  | { type: 'INPUT_LETTER'; letter: Letter }
  | { type: 'DELETE' }
  | { type: 'SUBMIT' }
  | { type: 'REVEAL_DONE' }
  | { type: 'NEXT_ROUND'; answerSeed: number; currentAnswer: Letter[] }
  | {
      type: 'MODE_CHANGE';
      mode: GameMode;
      answerSeed: number;
      currentAnswer: Letter[];
    };
export type Letter =
  | 'A'
  | 'B'
  | 'C'
  | 'D'
  | 'E'
  | 'F'
  | 'G'
  | 'H'
  | 'I'
  | 'J'
  | 'K'
  | 'L'
  | 'M'
  | 'N'
  | 'O'
  | 'P'
  | 'Q'
  | 'R'
  | 'S'
  | 'T'
  | 'U'
  | 'V'
  | 'W'
  | 'X'
  | 'Y'
  | 'Z';
