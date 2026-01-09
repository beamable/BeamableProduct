import { WORD_LENGTH } from '@app/game/constants.ts';
import allowedWordsDefault from '@app/assets/allowedWords.ts';
import answersDefault from '@app/assets/answers.ts';

export function isLengthOk(word: string) {
  return word.length === WORD_LENGTH;
}

export function isAlphabetic(word: string) {
  return /^[A-Z]+$/.test(word);
}

// If an explicit list is provided, use it. Otherwise, use the default allowed words loaded from allowed.txt
export function isAllowedWord(word: string, allowedWords?: string[]) {
  const list = allowedWords ?? allowedWordsDefault.concat(answersDefault);
  return list.includes(word.toLowerCase());
}

export function canSubmit(word: string) {
  return isLengthOk(word) && isAlphabetic(word);
}
