/**
 * Beamable connection configuration — the built-in / fallback realm.
 *
 * The values come from `.beamable/config.beam.json`, written by the Beamable CLI's `beam init`
 * (`{ cid, pid, host }`). A committed seed file lives at the project root so this sample builds
 * out of the box; run `beam init` to regenerate it with your own realm, and edit the cid/pid
 * there rather than here.
 *
 * When the web build is hosted somewhere that serves a `beam-config.json` next to it (e.g. a Unity
 * WebView via `com.beamable.notifications.web`, which serves the host project's `.beamable`), the
 * app prefers that at runtime and only falls back to this — see `beamClient.ts`.
 *
 * `host` is the platform URL — a built-in URL (https://api.beamable.com,
 * https://staging.api.beamable.com, https://dev.api.beamable.com) resolves to the matching
 * environment; any other URL is treated as a custom host. For manual setup you can instead omit
 * `host` and set `"environment": "dev"` in the JSON (the SDK resolves host → environment → prod).
 */
import Constants from 'expo-constants';

import BEAM_CONFIG from '../../.beamable/config.beam.json';

export { BEAM_CONFIG };

/**
 * The routing key for microservices started with `beam project run`, or undefined when the
 * services are deployed to the realm.
 *
 * `beam project run` — which is how a local stack starts them — registers each service's binding
 * behind a per-machine key, and the platform routes to it ONLY for callers that present that key.
 * Without it every microservice call answers `BindingNotFoundException`, which reads like the
 * service doesn't exist. A DEPLOYED service binds unkeyed, so leave this unset for those.
 *
 * Get the value from `beam fed local-key` and put it in `env.local` as `BEAM_ROUTING_KEY=…`;
 * `app.config.js` reads that file and passes it through `extra`. It lives there rather than in
 * committed config because it identifies one developer's machine.
 *
 * `beamClient.ts` turns it into the `X-BEAM-SERVICE-ROUTING-KEY` header — after `Beam.init()`, so
 * authentication never carries it. See `applyLocalRoutingKey` there.
 */
export const LOCAL_ROUTING_KEY: string | undefined =
  (Constants.expoConfig?.extra?.routingKey as string | undefined) || undefined;

/** True once real credentials have been filled in. */
export function isConfigured(): boolean {
  return (
    !!BEAM_CONFIG.cid &&
    !!BEAM_CONFIG.pid &&
    !BEAM_CONFIG.cid.startsWith('YOUR_') &&
    !BEAM_CONFIG.pid.startsWith('YOUR_')
  );
}
