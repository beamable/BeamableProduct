import { describe, it, expect } from 'vitest';
import { Setup } from '../setup.mjs';

describe('compareVersions', () => {
  it('should return 0 for equal versions', () => {
    expect(Setup.compareVersions('16.13.0', '16.13.0')).toBe(0);
  });

  it('should return 1 when the first version is greater', () => {
    expect(Setup.compareVersions('16.13.1', '16.13.0')).toBe(1);
  });

  it('should return -1 when the first version is lower', () => {
    expect(Setup.compareVersions('14.0.0', '16.13.0')).toBe(-1);
  });
});
