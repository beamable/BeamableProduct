import { GuessResult, Letter, LetterMark, Tile } from '@app/game/types.ts';

export function guessEvaluator(
  answer: Letter[],
  guess: Letter[],
  wordLength = 5,
): GuessResult {
  const tiles: Tile[] = [];
  const marks: LetterMark[] = [];

  for (let i = 0; i <= wordLength - 1; i++) {
    tiles.push({ letter: guess[i], mark: undefined });
    marks.push('absent');
  }

  const answerCounts = answer.reduce(
    (letterMap, currentLetter) => {
      if (currentLetter in letterMap) {
        letterMap[currentLetter] = letterMap[currentLetter] + 1;
      } else {
        letterMap[currentLetter] = 1;
      }

      return letterMap;
    },
    {} as Record<Letter, number>,
  );

  // First pass (correct):
  for (let i = 0; i <= wordLength - 1; i++) {
    const guessLetter = guess[i];
    const answerLetter = answer[i];
    if (guessLetter === answerLetter) {
      marks[i] = 'correct';
      tiles[i].mark = marks[i];
      answerCounts[guessLetter] = answerCounts[guessLetter] - 1;
    }
  }

  // Second pass (present/absent):
  for (let i = 0; i <= wordLength - 1; i++) {
    if (marks[i] === 'correct') continue;

    const guessLetter = guess[i];
    if (answerCounts[guessLetter] > 0) {
      marks[i] = 'present';
      tiles[i].mark = marks[i];
      answerCounts[guessLetter] = answerCounts[guessLetter] - 1;
    } else {
      marks[i] = 'absent';
      tiles[i].mark = marks[i];
    }
  }

  return { tiles, marks, isWin: marks.every((mark) => mark === 'correct') };
}
