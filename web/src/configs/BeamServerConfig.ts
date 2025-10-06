import { BeamBaseConfig } from '@/configs/BeamBaseConfig';
import { ServerEventType } from '@/core/types/ServerEventType';

/** Configuration options for initializing the Beam Server SDK. */
export interface BeamServerConfig extends BeamBaseConfig {
  /** Name of the engine (e.g., "Node", "Deno", "Express", "Hono"). */
  engine?: string;

  /** Version of the engine. */
  engineVersion?: string;

  /**
   * Enables signing outgoing requests with a signature header.
   * @remarks
   * This option is only supported in Node.js environments.
   * When running in a browser, this setting will be ignored.
   * @defaultValue false
   */
  useSignedRequest?: boolean;

  /** Configuration for server-events. */
  serverEvents?: ServerEventsConfig;
}

/** Configuration options for server-events. */
export interface ServerEventsConfig {
  /**
   * Enables the server-events feature (gateway notifications over WebSocket).
   * @default false
   */
  enabled?: boolean;
  /** A list of server events to subscribe to. If not provided, all events will be subscribed to. */
  eventWhitelist?: ServerEventType[];
}
