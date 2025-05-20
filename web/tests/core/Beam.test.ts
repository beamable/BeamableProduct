import { describe, expect, it } from 'vitest';
import { Beam } from '@/core/Beam';
import { BeamEnvironmentType } from '@/configs/BeamEnvironmentConfig';

describe('Beam', () => {
  it('returns a formatted summary of the instance configuration', () => {
    const cid = '1713028771755577';
    const pid = 'DE_1740294079885317';
    const alias = 'beam-able';
    const beam = new Beam({
      cid,
      pid,
      alias,
      environment: BeamEnvironmentType.Dev,
    });

    expect(beam.toString()).toBe(
      `Beam(config: cid=${cid}, pid=${pid}, alias=${alias})`,
    );
  });
});
