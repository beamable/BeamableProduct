/** Configuration settings for a Beamable environment. */
export interface BeamEnvironmentConfig {
  /** The base URL for the Beamable API. */
  apiUrl: string;

  /** The URL for the Beamable web portal. */
  portalUrl: string;

  /** The URL for the Beamable Mongo Express interface. */
  beamMongoExpressUrl: string;

  /** The URL for the Beamable Docker registry endpoint. */
  dockerRegistryUrl: string;
}

/** Built-in environment names */
export type BuiltInEnv = 'dev' | 'stg' | 'prod';

/**
 * Any legal Beamable environment name.
 * The branded intersection keeps intellisense for the three built-ins
 * while still accepting arbitrary strings.
 */
export type BeamEnvironmentName = BuiltInEnv | (string & {});
