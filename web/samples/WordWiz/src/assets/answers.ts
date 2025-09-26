// Convert answers.txt into an array of words for use across the app
// The file is imported as raw text via Vite's ?raw import.
import answersRaw from '@app/assets/answers.txt?raw';

// Normalize: trim, filter empty, and lowercase (dictionary is case-insensitive)
export const answers: string[] = answersRaw
  .split(/\r?\n/)
  .map((line) => line.trim())
  .filter((line) => line.length > 0)
  .map((line) => line.toLowerCase());

export default answers;
