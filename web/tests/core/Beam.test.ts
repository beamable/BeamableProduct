import { describe, expect, it } from 'vitest';
import { Beam } from '@/core/Beam';

describe('Beam', () => {
  it('returns a formatted summary of the instance configuration', () => {
    const cid = '1713028771755577';
    const pid = 'DE_1740294079885317';
    const alias = 'beam-able';
    const beam = new Beam({
      environmentName: 'Dev',
      cid,
      pid,
      alias,
    });

    expect(beam.toString()).toBe(
      `Beam(config: cid=${cid}, pid=${pid}, alias=${alias})`,
    );
  });
});
