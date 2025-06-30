import { describe, expect, it } from 'vitest';
import { BeamJsonUtils } from '@/utils/BeamJsonUtils';

describe('beamReplacer', () => {
  it('serializes BigInt as string', () => {
    const obj = { id: BigInt(123) };
    const json = JSON.stringify(obj, BeamJsonUtils.replacer);
    expect(json).toContain('"123"');
  });

  it('serializes Date as ISO string', () => {
    const date = new Date('2023-01-01T12:34:56.789Z');
    const obj = { ts: date };
    const json = JSON.stringify(obj, BeamJsonUtils.replacer);
    expect(json).toContain(`"${date.toISOString()}"`);
  });

  it('leaves other types untouched', () => {
    const obj = {
      num: 42,
      str: 'foo',
      bool: true,
      nul: null,
      arr: [1, 2, 3],
      obj: {},
    };
    const json = JSON.stringify(obj, BeamJsonUtils.replacer);
    expect(json).toContain('"num":42');
    expect(json).toContain('"str":"foo"');
    expect(json).toContain('"bool":true');
    expect(json).toContain('"nul":null');
    expect(json).toContain('"arr":[1,2,3]');
    expect(json).toContain('"obj":{}');
  });
});

describe('beamReviver', () => {
  it('parses ISO date strings into Date', () => {
    const iso = '2025-01-01T12:34:56.789Z';
    const result = JSON.parse(`{"date":"${iso}"}`, BeamJsonUtils.reviver);
    expect(result.date).toBeInstanceOf(Date);
    expect(result.date.toISOString()).toBe(iso);
  });

  it('converts long numeric (>10 digits) to BigInt', () => {
    const big = 1234567890123;
    const result = JSON.parse(`{"num":${big}}`, BeamJsonUtils.reviver);
    expect(typeof result.num).toBe('bigint');
    expect(result.num).toBe(BigInt(big));
  });

  it('leaves short numeric strings as string', () => {
    const small = '1234567890';
    const result = JSON.parse(`{"num":"${small}"}`, BeamJsonUtils.reviver);
    expect(typeof result.num).toBe('string');
    expect(result.num).toBe(small);
  });

  it('leaves non-numeric strings untouched', () => {
    const str = 'not a date';
    const result = JSON.parse(`{"s":"${str}"}`, BeamJsonUtils.reviver);
    expect(typeof result.s).toBe('string');
    expect(result.s).toBe(str);
  });

  it('round-trips via replacer and reviver', () => {
    const original = {
      id: BigInt('9876543210987'),
      date: new Date('2025-02-02T02:02:02.002Z'),
      nested: { innerDate: new Date('2025-01-31T23:59:59.999Z') },
    };
    const json = JSON.stringify(original, BeamJsonUtils.replacer);
    const parsed = JSON.parse(json, BeamJsonUtils.reviver);
    expect(parsed.id).toBe(BigInt('9876543210987'));
    expect(parsed.date).toBeInstanceOf(Date);
    expect(parsed.date.toISOString()).toBe(original.date.toISOString());
    expect(parsed.nested.innerDate).toBeInstanceOf(Date);
    expect(parsed.nested.innerDate.toISOString()).toBe(
      original.nested.innerDate.toISOString(),
    );
  });
});
