import { BeamEnvironmentName } from './BeamEnvironmentConfig';
import { HttpRequester } from '@/network/http/types/HttpRequester';

/** Configuration options for initializing the Beamable SDK. */
export interface BeamBaseConfig {
  /** Beamable Customer ID (CID). */
  cid: string;

  /** Beamable Project ID (PID). */
  pid: string;

  /**
   * The Beamable environment to connect to.
   * Can be one of 'Prod', 'Stg', 'Dev', or a custom environment name.
   * @default 'Prod'
   */
  environment?: BeamEnvironmentName;

  /** Custom HTTP requester implementation. */
  requester?: HttpRequester;

  /** Published version of the game. */
  gameVersion?: string;
}
