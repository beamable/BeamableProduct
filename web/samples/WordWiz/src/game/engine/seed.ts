import answers from '@app/assets/answers.ts';

export function getEndlessSeed() {
  // generate a random number between 0 and answersLength
  return Math.floor(Math.random() * answers.length);
}
