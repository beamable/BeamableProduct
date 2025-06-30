import { describe, it, expect, afterEach, vi } from 'vitest';
import { generateTag } from '@/utils/generateTag';

describe('generateTag()', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('returns a default 8-character alphanumeric tag', () => {
    const tag = generateTag();
    expect(tag.length).toBe(8);
    expect(/^[A-Za-z0-9]+$/.test(tag)).toBe(true);
  });

  it('returns a tag of specified length', () => {
    const length = 16;
    const tag = generateTag(length);
    expect(tag.length).toBe(length);
    expect(/^[A-Za-z0-9]+$/.test(tag)).toBe(true);
  });

  it('returns an empty string when length is 0', () => {
    expect(generateTag(0)).toBe('');
  });
});
