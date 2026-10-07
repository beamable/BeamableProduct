import { describe, it, expect } from 'vitest';
import { makeQueryString } from '@/utils/makeQueryString';

describe('makeQueryString()', () => {
  it('should return empty string for empty queries', () => {
    expect(makeQueryString({})).toBe('');
  });

  it('should encode simple key-value pairs', () => {
    const qs = makeQueryString({ foo: 'bar', num: 123, bool: false });
    expect(qs).toBe('?foo=bar&num=123&bool=false');
  });

  it('should encode special characters', () => {
    const qs = makeQueryString({ q: 'hello world', path: '/api?test=value' });
    expect(qs).toBe('?q=hello%20world&path=%2Fapi%3Ftest%3Dvalue');
  });

  it('should skip undefined values but include null', () => {
    const qs = makeQueryString({ a: undefined, b: null, c: 'x' });
    expect(qs).toBe('?b=null&c=x');
  });

  it('should emit one repeated key per array element', () => {
    const qs = makeQueryString({ arr: [1, 2, 3] });
    expect(qs).toBe('?arr=1&arr=2&arr=3');
  });

  it('should encode each array element on its own', () => {
    const qs = makeQueryString({ states: ['Granted', 'a,b c'], x: 'y' });
    expect(qs).toBe('?states=Granted&states=a%2Cb%20c&x=y');
  });

  it('should skip undefined array elements but keep null ones', () => {
    const qs = makeQueryString({ arr: ['a', undefined, null, 'b'] });
    expect(qs).toBe('?arr=a&arr=null&arr=b');
  });

  it('should drop an empty array entirely', () => {
    expect(makeQueryString({ arr: [], c: 'x' })).toBe('?c=x');
    expect(makeQueryString({ arr: [] })).toBe('');
  });

  it('should return empty string for all undefined values', () => {
    const qs = makeQueryString({ a: undefined, b: undefined });
    expect(qs).toBe('');
  });
});
