import { BeamEnvironmentName } from './BeamEnvironmentConfig';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { TokenStorage } from '@/platform/types/TokenStorage';

/** Configuration options for initializing the Beamable SDK. */
export interface BeamBaseConfig {
  /** Beamable Customer ID (CID). */
  cid: string;

  /** Beamable Project ID (PID). */
  pid: string;

  /**
   * The Beamable environment to connect to.
   * Can be one of 'prod', 'stg', 'dev', or a custom environment name.
   * @default 'prod'
   */
  environment?: BeamEnvironmentName;

  /**
   * Explicit Beamable platform host URL (e.g. imported from `.beamable/config.beam.json`).
   * Optional. When set, the environment is derived from it: a built-in URL resolves to
   * 'dev'/'stg'/'prod'; any other URL is treated as a custom host. Takes precedence over
   * `environment`.
   */
  host?: string;

  /** Custom HTTP requester implementation. */
  requester?: HttpRequester;

  /** Custom token storage implementation. */
  tokenStorage?: TokenStorage;

  /** Unique tag for instance-specific token storage synchronization. */
  instanceTag?: string;

  /** Published version of the game. */
  gameVersion?: string;

  /** List of content namespaces to load. By default, only 'global' is loaded. */
  contentNamespaces?: string[];
}
