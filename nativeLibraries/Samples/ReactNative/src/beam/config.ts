/**
 * Beamable connection configuration.
 *
 * 👉 Replace the placeholder cid / pid with values from your Beamable realm
 *    (Beamable Portal -> realm settings). Until you do, the app still runs and
 *    the notification + deep-link demos work; only the "Connect to Beamable"
 *    action will report that it is not configured.
 */
export const BEAM_CONFIG = {
  /** Beamable Customer ID (CID). */
  cid: '1752011665993752',
  /** Beamable Project ID (PID). */
  pid: 'DE_83112773772143616',
  /** 'prod' | 'stg' | 'dev' (or a custom environment name). */
  environment: 'prod' as 'prod' | 'stg' | 'dev' | (string & {}),
};

/** True once real credentials have been filled in. */
export function isConfigured(): boolean {
  return (
    !!BEAM_CONFIG.cid &&
    !!BEAM_CONFIG.pid &&
    !BEAM_CONFIG.cid.startsWith('YOUR_') &&
    !BEAM_CONFIG.pid.startsWith('YOUR_')
  );
}
