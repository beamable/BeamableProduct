/**
 * Beamable connection configuration.
 *
 * 👉 Replace the placeholder cid / pid with values from your Beamable realm
 *    (Beamable Portal -> realm settings). Until you do, the app still runs and
 *    the notification + deep-link demos work; only the "Connect to Beamable"
 *    action will report that it is not configured.
 *
 * The API target comes from `env.local` (`VITE_API_BASE`), surfaced via
 * `app.config.js` → `expo-constants`. When set, the SDK connects to that base URL
 * (see beamClient.ts, which registers it as the `local` environment); otherwise it
 * falls back to the built-in `environment` below.
 */
import Constants from 'expo-constants';

const apiBase = (
  Constants.expoConfig?.extra as { apiBase?: string } | undefined
)?.apiBase;

export const BEAM_CONFIG = {
  /** Beamable Customer ID (CID). */
  cid: '88020011326637056',
  /** Beamable Project ID (PID). */
  pid: 'DE_88020011330831362',
  /** 'prod' | 'stg' | 'dev' (or a custom environment name). */
  environment: (apiBase ? 'local' : 'dev') as
    | 'prod'
    | 'stg'
    | 'dev'
    | (string & {}),
  /** API base URL from env.local (undefined → use the named `environment` above). */
  apiBase,
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
