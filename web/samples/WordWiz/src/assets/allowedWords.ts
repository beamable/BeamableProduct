// Convert allowed.txt into an array of words for use across the app
// The file is imported as raw text via Vite's ?raw import.
import allowedRaw from '@app/assets/allowed.txt?raw';

// Normalize: trim, filter empty, and lowercase (dictionary is case-insensitive)
export const allowedWords: string[] = allowedRaw
  .split(/\r?\n/)
  .map((line) => line.trim())
  .filter((line) => line.length > 0)
  .map((line) => line.toLowerCase());

export default allowedWords;
