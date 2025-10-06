import type { Beam } from 'beamable-sdk';
import { setupBeam } from '@app/beam.ts';

/**
 * Initializes the application and configures its dependencies.
 */
export async function init(): Promise<Beam> {
  return await setupBeam();
}
