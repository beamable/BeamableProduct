import { BeamBaseConfig } from '@/configs/BeamBaseConfig';
import type { Beam } from '@/core/Beam';

/** Configuration options for initializing the Beam Client SDK. */
export interface BeamConfig extends BeamBaseConfig {
  /** Name of the game engine (e.g., "Three.js", "Phaser", "Babylon", "PlayCanvas"). */
  gameEngine?: string;

  /** Version of the game engine. */
  gameEngineVersion?: string;

  /**
   * Optional callback invoked during Beam Client SDK initialization to register or configure client services.
   *
   * @example
   * ```ts
   * import { clientServices } from "beamable-sdk";
   *
   * const config: BeamConfig = {
   *   services: clientServices,
   * };
   * ```
   */
  services?: (beam: Beam) => void;
}
