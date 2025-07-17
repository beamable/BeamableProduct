import { BeamBaseConfig } from '@/configs/BeamBaseConfig';

/** Configuration options for initializing the Beam Server SDK. */
export interface BeamServerConfig extends BeamBaseConfig {
  /** Name of the engine (e.g., "Node", "Deno", "Express", "Hono"). */
  engine?: string;

  /** Version of the engine. */
  engineVersion?: string;
}
