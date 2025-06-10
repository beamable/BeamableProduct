import { BeamEnvironmentName } from './BeamEnvironmentConfig';
import { HttpRequester } from '@/http/types/HttpRequester';
import { TokenStorage } from '@/platform/types/TokenStorage';

/**
 * Configuration options for initializing the Beamable SDK.
 * @interface BeamConfig
 */
export interface BeamConfig {
  /** The client identifier (CID) assigned to your Beamable account. */
  cid: string;

  /** The project identifier (PID) associated with your Beamable project. */
  pid: string;

  /** The target Beamable environment in which requests will be sent. Defaults to `'Prod'` */
  environment?: BeamEnvironmentName;

  /** Optional custom HTTP requester to use instead of the default implementation. */
  requester?: HttpRequester;

  /** Optional custom token storage strategy. */
  tokenStorage?: TokenStorage;

  /** Optional name of the game engine being used (e.g., "Three.js", "Phaser", "Babylon", "PlayCanvas"). */
  gameEngine?: string;

  /** Optional version of the game engine being used. */
  gameEngineVersion?: string;

  /** Optional version of the game to be published */
  gameVersion?: string;
}
