import { BeamEnvironmentName } from './BeamEnvironmentConfig';
import { HttpRequester } from '@/http/types/HttpRequester';

/**
 * Configuration options for initializing the Beamable SDK.
 * @interface BeamConfig
 */
export interface BeamConfig {
  /** The client identifier (CID) assigned to your Beamable account. */
  cid: string;

  /** The project identifier (PID) associated with your Beamable project. */
  pid: string;

  /** A human-readable alias for the CID (e.g., 'demo-app'). */
  alias: string;

  /** The game realm to target */
  realm: string;

  /** The target Beamable environment name in which requests will be sent. */
  environment: BeamEnvironmentName;

  /** Optional custom HTTP requester to use instead of the default implementation. */
  requester?: HttpRequester;
}
