import { pause } from './async-utils';

export async function prompt(question: string, attempts: number = 1, delay: number = 10) {
  let answer;

  while (!answer && --attempts) {
    answer = window.prompt(question, '') || null;

    if (arguments.length > 2) {
      await pause(delay);
    }
  }

  if (!answer) {
    throw new Error(`[required]: ${question}`);
  }

  return answer;
}
