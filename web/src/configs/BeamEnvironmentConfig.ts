/**
 * Configuration settings for a Beamable environment.
 *
 * @interface BeamEnvironmentConfig
 */
export interface BeamEnvironmentConfig {
  /**
   * The base URL for the Beamable API.
   */
  apiUrl: string;

  /**
   * The URL for the Beamable web portal.
   */
  portalUrl: string;

  /**
   * The URL for the Beamable Mongo Express interface.
   */
  beamMongoExpressUrl: string;

  /**
   * The URL for the Beamable Docker registry endpoint.
   */
  dockerRegistryUrl: string;
}

/**
 * Enumeration of Beam environment types.
 *
 * @enum {string}
 */
export enum BeamEnvironmentType {
  /**
   * Development environment.
   */
  Dev = 'Dev',

  /**
   * Staging environment.
   */
  Stg = 'Stg',

  /**
   * Production environment.
   */
  Prod = 'Prod',
}

/**
 * A mapping of Beam environment types to their configurations.
 */
export const BeamEnvironment: Record<
  BeamEnvironmentType,
  BeamEnvironmentConfig
> = {
  /**
   * Configuration for the Development environment.
   */
  [BeamEnvironmentType.Dev]: {
    apiUrl: 'https://dev.api.beamable.com',
    portalUrl: 'https://dev-portal.beamable.com',
    beamMongoExpressUrl: 'https://dev.storage.beamable.com',
    dockerRegistryUrl: 'https://dev-microservices.beamable.com/v2/',
  },

  /**
   * Configuration for the Staging environment.
   */
  [BeamEnvironmentType.Stg]: {
    apiUrl: 'https://staging.api.beamable.com',
    portalUrl: 'https://staging-portal.beamable.com',
    beamMongoExpressUrl: 'https://staging.storage.beamable.com',
    dockerRegistryUrl: 'https://staging-microservices.beamable.com/v2/',
  },

  /**
   * Configuration for the Production environment.
   */
  [BeamEnvironmentType.Prod]: {
    apiUrl: 'https://api.beamable.com',
    portalUrl: 'https://portal.beamable.com',
    beamMongoExpressUrl: 'https://storage.beamable.com',
    dockerRegistryUrl: 'https://microservices.beamable.com/v2/',
  },
};
