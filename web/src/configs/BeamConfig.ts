import { TokenStorage } from '@/platform/types/TokenStorage';
import { BeamBaseConfig } from '@/configs/BeamBaseConfig';

/** Configuration options for initializing the Beam Client SDK. */
export interface BeamConfig extends BeamBaseConfig {
  /** Custom token storage implementation. */
  tokenStorage?: TokenStorage;

  /** Unique tag for instance-specific token storage synchronization. */
  instanceTag?: string;

  /** Name of the game engine (e.g., "Three.js", "Phaser", "Babylon", "PlayCanvas"). */
  gameEngine?: string;

  /** Version of the game engine. */
  gameEngineVersion?: string;
}
