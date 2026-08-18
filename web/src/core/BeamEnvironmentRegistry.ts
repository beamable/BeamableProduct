import {
  BeamEnvironmentConfig,
  BeamEnvironmentName,
  BuiltInEnv,
} from '@/configs/BeamEnvironmentConfig';
import { deepFreeze } from '@/utils/deepFreeze';

/** A mapping of Beamable built-in environment names to their configurations. */
const defaultEnvironments: Record<BuiltInEnv, BeamEnvironmentConfig> = {
  dev: {
    apiUrl: 'https://dev.api.beamable.com',
    portalUrl: 'https://dev-portal.beamable.com',
    beamMongoExpressUrl: 'https://dev.storage.beamable.com',
    dockerRegistryUrl: 'https://dev-microservices.beamable.com/v2/',
  },
  stg: {
    apiUrl: 'https://staging.api.beamable.com',
    portalUrl: 'https://staging-portal.beamable.com',
    beamMongoExpressUrl: 'https://staging.storage.beamable.com',
    dockerRegistryUrl: 'https://staging-microservices.beamable.com/v2/',
  },
  prod: {
    apiUrl: 'https://api.beamable.com',
    portalUrl: 'https://portal.beamable.com',
    beamMongoExpressUrl: 'https://storage.beamable.com',
    dockerRegistryUrl: 'https://microservices.beamable.com/v2/',
  },
};

/**
 * A registry for Beamable environment configurations.
 * Allows for registering and retrieving environment configurations by name.
 */
export class BeamEnvironmentRegistry {
  private readonly envs: Record<string, BeamEnvironmentConfig>;

  constructor(initial = defaultEnvironments) {
    this.envs = { ...initial };
  }

  /** Add or overwrite an environment configuration at runtime. */
  register(name: BeamEnvironmentName, cfg: BeamEnvironmentConfig): void {
    this.envs[name] = cfg;
  }

  /** Read-only snapshot of all registered environments (useful for debugging/UIs). */
  list(): Readonly<Record<string, BeamEnvironmentConfig>> {
    return deepFreeze({ ...this.envs });
  }

  /** Get a registered environment configuration. */
  get(name: BeamEnvironmentName): BeamEnvironmentConfig {
    const config = this.envs[name];
    if (!config) {
      throw new Error(
        `Beam environment “${name}” is not registered. ` +
          `Call BeamEnvironment.register(...) first.`,
      );
    }
    return config;
  }

  /**
   * Find a registered environment whose `apiUrl` matches the given host.
   * The comparison ignores any trailing slash on either side.
   */
  findByApiUrl(host: string): BeamEnvironmentConfig | undefined {
    const normalized = host.replace(/\/+$/, '');
    return Object.values(this.envs).find(
      (env) => env.apiUrl.replace(/\/+$/, '') === normalized,
    );
  }

  /**
   * Resolve a full environment configuration from a host URL.
   * A host matching a registered environment (built-in or custom) returns that config;
   * any other URL is treated as a custom host with the URL as its `apiUrl`.
   */
  fromHost(host: string): BeamEnvironmentConfig {
    return (
      this.findByApiUrl(host) ?? {
        apiUrl: host.replace(/\/+$/, ''),
        portalUrl: '',
        beamMongoExpressUrl: '',
        dockerRegistryUrl: '',
      }
    );
  }
}

/** An instance of BeamEnvironmentRegistry */
export const BeamEnvironment = new BeamEnvironmentRegistry();
