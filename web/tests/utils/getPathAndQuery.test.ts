import { describe, it, expect } from 'vitest';
import { getPathAndQuery } from '@/utils/getPathAndQuery';

describe('getPathAndQuery', () => {
  it('should strip host, leaving path and query for absolute HTTP URLs', () => {
    const url = 'https://example.com/foo/bar?x=1';
    expect(getPathAndQuery(url)).toBe('/foo/bar?x=1');
  });

  it('should leave a relative path unchanged', () => {
    const relative = '/just/a/path?y=2#frag';
    expect(getPathAndQuery(relative)).toBe(relative);
  });

  it('should return the input as-is when the string is not a valid URL', () => {
    const invalid = 'not a url';
    expect(getPathAndQuery(invalid)).toBe(invalid);
  });

  it('should handle URLs with ports correctly', () => {
    const url = 'http://localhost:3000/test/path';
    expect(getPathAndQuery(url)).toBe('/test/path');
  });

  it('should strip credentials in URL and return just the path', () => {
    const url = 'https://user:pass@example.com/secure?token=abc';
    expect(getPathAndQuery(url)).toBe('/secure?token=abc');
  });

  it('should handle URLs with no path (root)', () => {
    const url = 'https://domain.com';
    expect(getPathAndQuery(url)).toBe('/');
  });
});
