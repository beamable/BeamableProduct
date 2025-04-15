import { describe, it, expect } from 'vitest';
import { print } from '../src';

describe('print function', () => {
  it('should return a print message', () => {
    expect(print()).toBe('Welcome to Beam SDK');
  });
});
