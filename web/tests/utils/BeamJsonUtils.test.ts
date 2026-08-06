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
    expect(json).toContain(`"${date.toISOString()}`);
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
    const result = BeamJsonUtils.parse(`{"date":"${iso}"}`);
    expect(result.date).toBeInstanceOf(Date);
    expect(result.date.toISOString()).toBe(iso);
  });

  it('converts long numeric (>10 digits) to BigInt without precision loss', () => {
    const raw = '{"num":70820408384930816}';
    const result = BeamJsonUtils.parse(raw);
    expect(typeof result.num).toBe('bigint');
    expect(result.num).toBe(BigInt('70820408384930816'));
  });

  it('leaves short numbers as number', () => {
    const result = BeamJsonUtils.parse('{"num":42}');
    expect(typeof result.num).toBe('number');
    expect(result.num).toBe(42);
  });

  it('leaves short numeric strings as string', () => {
    const small = '1234567890';
    const result = BeamJsonUtils.parse(`{"num":"${small}"}`);
    expect(typeof result.num).toBe('string');
    expect(result.num).toBe(small);
  });

  it('leaves non-numeric strings untouched', () => {
    const str = 'not a date';
    const result = BeamJsonUtils.parse(`{"s":"${str}"}`);
    expect(typeof result.s).toBe('string');
    expect(result.s).toBe(str);
  });

  it('round-trips via replacer and parse', () => {
    const original = {
      id: BigInt('70820408384930816'),
      date: new Date('2025-02-02T02:02:02.002Z'),
      nested: { innerDate: new Date('2025-01-31T23:59:59.999Z') },
    };
    const json = JSON.stringify(original, BeamJsonUtils.replacer);
    const parsed = BeamJsonUtils.parse(json);
    expect(parsed.id).toBe(BigInt('70820408384930816'));
    expect(parsed.date).toBeInstanceOf(Date);
    expect(parsed.date.toISOString()).toBe(original.date.toISOString());
    expect(parsed.nested.innerDate).toBeInstanceOf(Date);
    expect(parsed.nested.innerDate.toISOString()).toBe(
      original.nested.innerDate.toISOString(),
    );
  });

  it('handles large ints in arrays', () => {
    const raw = '{"ids":[70820408384930816,70820408384930817]}';
    const result = BeamJsonUtils.parse(raw);
    expect(result.ids[0]).toBe(BigInt('70820408384930816'));
    expect(result.ids[1]).toBe(BigInt('70820408384930817'));
  });

  it('handles negative large ints', () => {
    const raw = '{"val":-70820408384930816}';
    const result = BeamJsonUtils.parse(raw);
    expect(result.val).toBe(BigInt('-70820408384930816'));
  });

  it('does not quote numbers inside strings', () => {
    const raw = '{"msg":"id is 70820408384930816 here"}';
    const result = BeamJsonUtils.parse(raw);
    expect(typeof result.msg).toBe('string');
    expect(result.msg).toBe('id is 70820408384930816 here');
  });
});

describe('quoteLargeInts', () => {
  it('quotes integers with more than 10 digits', () => {
    const input = '{"id":70820408384930816}';
    const output = BeamJsonUtils.quoteLargeInts(input);
    expect(output).toBe('{"id":"70820408384930816"}');
  });

  it('does not quote short integers', () => {
    const input = '{"id":1234567890}';
    const output = BeamJsonUtils.quoteLargeInts(input);
    expect(output).toBe('{"id":1234567890}');
  });

  it('does not quote already-quoted strings', () => {
    const input = '{"id":"70820408384930816"}';
    const output = BeamJsonUtils.quoteLargeInts(input);
    expect(output).toBe('{"id":"70820408384930816"}');
  });
});
