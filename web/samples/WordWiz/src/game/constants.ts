export const WORD_LENGTH = 5;
export const MAX_ATTEMPTS = 6;
export const TILE_FLIP_DURATION = 250;
export const FLIP_STAGGER = 100;
export const RESULT_REVEAL_DELAY = 500;
export const KEYBOARD_ROW = [
  ['q', 'w', 'e', 'r', 't', 'y', 'u', 'i', 'o', 'p'],
  ['a', 's', 'd', 'f', 'g', 'h', 'j', 'k', 'l'],
  ['z', 'x', 'c', 'v', 'b', 'n', 'm'],
];
export const LETTERS = Array.from({ length: 26 }, (_, i) =>
  String.fromCharCode(65 + i),
);
export const TIPS = 'Green = Correct • Amber = Present • Dark = Absent';
export const DAILY_STREAK = 'DAILY_STREAK';
export const ENDLESS_STREAK = 'ENDLESS_STREAK';
