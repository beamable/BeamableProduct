import { describe, it, expect } from 'vitest';
import { endpointEncoder } from '@/utils/endpointEncoder';

describe('endpointEncoder()', () => {
  it('returns unmodified alphanumeric strings', () => {
    expect(endpointEncoder('abc123')).toBe('abc123');
  });

  it('encodes spaces and reserved URL characters', () => {
    expect(endpointEncoder('a b/c?d&e')).toBe('a%20b%2Fc%3Fd%26e');
  });

  it('stringifies and encodes bigint values', () => {
    expect(endpointEncoder(12345678901234567890n)).toBe('12345678901234567890');
    expect(endpointEncoder(-42n)).toBe('-42');
  });
});
