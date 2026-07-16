/**
 * Beamable connection configuration.
 *
 * The values come from `.beamable/config.beam.json`, written by the Beamable CLI's `beam init`
 * (`{ cid, pid, host }`). A committed seed file lives at the project root so this sample builds
 * out of the box; run `beam init` to regenerate it with your own realm, and edit the cid/pid
 * there rather than here.
 *
 * `host` is the platform URL — a built-in URL (https://api.beamable.com,
 * https://staging.api.beamable.com, https://dev.api.beamable.com) resolves to the matching
 * environment; any other URL is treated as a custom host. For manual setup you can instead omit
 * `host` and set `"environment": "dev"` in the JSON (the SDK resolves host → environment → prod).
 */
import BEAM_CONFIG from '../../.beamable/config.beam.json';

export { BEAM_CONFIG };

/** True once real credentials have been filled in. */
export function isConfigured(): boolean {
  return (
    !!BEAM_CONFIG.cid &&
    !!BEAM_CONFIG.pid &&
    !BEAM_CONFIG.cid.startsWith('YOUR_') &&
    !BEAM_CONFIG.pid.startsWith('YOUR_')
  );
}
