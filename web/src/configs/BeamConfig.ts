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
   * import { clientServices } from "@beamable/sdk";
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
   * `Beam.init()` resolves only after the socket has opened and the
   * `session-start` frame has been sent, and rejects if that doesn't happen
   * within `connectTimeoutMs`.
   *
   * @default { enabled: true, connectTimeoutMs: 15000 }
   */
  realtime?: {
    /** Whether to auto-connect the realtime websocket on `init`. @default true */
    enabled?: boolean;
    /**
     * How long `Beam.init()` / `connectRealtime()` wait for the realtime socket to
     * open, in milliseconds, before rejecting with a `BeamWebSocketError`. A socket
     * that never opens usually means WebSockets are blocked by a proxy or firewall.
     * `0` disables the timeout.
     * @default 15000
     */
    connectTimeoutMs?: number;
  };
}
