import { BeamBaseConfig } from '@/configs/BeamBaseConfig';
import type { Beam } from '@/core/Beam';

/** Configuration options for initializing the Beam Client SDK. */
export interface BeamConfig extends BeamBaseConfig {
  /** Name of the game engine (e.g., "Three.js", "Phaser", "Babylon", "PlayCanvas"). */
  gameEngine?: string;

  /** Version of the game engine. */
  gameEngineVersion?: string;

  /**
   * Optional callback invoked during Beam Client SDK initialization to register or configure client services.
   *
   * @example
   * ```ts
   * import { clientServices } from "beamable-sdk";
   *
   * const config: BeamConfig = {
   *   services: clientServices,
   * };
   * ```
   */
  services?: (beam: Beam) => void;

  /**
   * Controls the realtime (websocket) connection to Beamable server-events.
   *
   * When `enabled` is `false`, `Beam.init()` skips establishing the realtime
   * connection during initialization. This is useful when the SDK is used
   * purely as an API client (e.g. an admin/portal context with no player), or
   * when you want to defer realtime until a player exists. Call
   * `beam.connectRealtime()` to establish it later — for example after creating
   * a player via `beam.auth.loginAsGuest()`.
   *
   * @default { enabled: true }
   */
  realtime?: {
    /** Whether to auto-connect the realtime websocket on `init`. @default true */
    enabled?: boolean;
  };
}
