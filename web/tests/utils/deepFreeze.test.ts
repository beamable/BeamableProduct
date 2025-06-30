import { describe, it, expect } from 'vitest';
import { deepFreeze } from '@/utils/deepFreeze';

describe('deepFreeze()', () => {
  it('should return non-objects as-is', () => {
    expect(deepFreeze(42)).toBe(42);
    expect(deepFreeze('foo')).toBe('foo');
    expect(deepFreeze(null)).toBeNull();
    const fn = () => {};
    expect(deepFreeze(fn)).toBe(fn);
  });

  it('should freeze a flat object', () => {
    const o = { a: 1, b: 'x' };
    const frozen = deepFreeze(o);
    expect(Object.isFrozen(frozen)).toBe(true);
    // in strict mode, assignment throws
    expect(() => {
      // @ts-ignore
      frozen.a = 2;
    }).toThrow(TypeError);
  });

  it('should freeze nested objects', () => {
    const nested = {
      a: { x: 1 },
      b: [2, 3, { y: 4 }],
    };
    deepFreeze(nested);
    expect(Object.isFrozen(nested)).toBe(true);
    expect(Object.isFrozen(nested.a)).toBe(true);
    expect(Object.isFrozen(nested.b)).toBe(true);
    expect(Object.isFrozen(nested.b[2])).toBe(true);

    // try mutating deep property
    expect(() => {
      // @ts-ignore
      nested.a.x = 9;
    }).toThrow(TypeError);

    expect(() => {
      // @ts-ignore
      nested.b[2].y = 5;
    }).toThrow(TypeError);
  });

  it('should handle symbol properties', () => {
    const sym = Symbol('foo');
    const o = { [sym]: { inner: 'bar' } };
    deepFreeze(o);
    expect(Object.isFrozen(o)).toBe(true);
    // @ts-ignore
    expect(Object.isFrozen(o[sym])).toBe(true);
    expect(() => {
      // @ts-ignore
      o[sym].inner = 'baz';
    }).toThrow(TypeError);
  });
});
