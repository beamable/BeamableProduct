/**
 * The auth handoff to the native SDK.
 *
 * The native side runs the analytics funnel when the JS runtime is NOT running (a push tapped
 * from a killed app), so it needs its own copy of the player's tokens. `configureAuth` writes
 * them (Android SharedPreferences / iOS shared container); `clearAuth` removes them.
 *
 * IMPORTANT — there is no read-back. The native modules expose only `configureAuth` and
 * `clearAuth` on both platforms (`PushManager.kt`, `NotificationManager.swift`); nothing returns
 * the stored values to JS. So `buildAuthPayload()` reports what the app SENDS to native, not
 * what native currently holds. They match unless `clearAuth` has since run or the write failed —
 * which is exactly why the viewer in the Analytics tab labels it that way rather than claiming
 * to inspect native state.
 */
import { BeamNotifications } from '@beamable/notifications-react-native';
import type { ConfigureAuthOptions } from '@beamable/notifications-react-native';

import { getBeam } from './beamClient';
import { BEAM_CONFIG } from './config';

/**
 * Assemble the exact `ConfigureAuthOptions` the app hands to native, from the SDK's current
 * token storage plus the resolved realm. Throws when not connected or when no tokens exist yet.
 */
export async function buildAuthPayload(): Promise<ConfigureAuthOptions> {
  const beam = getBeam();
  if (!beam) throw new Error('Not connected to Beamable yet');

  const { accessToken, refreshToken, expiresIn } = await beam.tokenStorage.getTokenData();
  if (!accessToken || !refreshToken) throw new Error('No tokens in storage yet');

  const host = String(BEAM_CONFIG.host ?? '');
  if (!host) throw new Error('No host configured — native auth needs an absolute API base URL');

  return {
    accessToken,
    refreshToken,
    // The SDK stores `expiresIn` as an absolute epoch-MILLISECONDS timestamp, so it maps
    // straight onto `accessTokenExpiresAt`.
    accessTokenExpiresAt: expiresIn ?? 0,
    cid: BEAM_CONFIG.cid,
    pid: BEAM_CONFIG.pid,
    host,
  };
}

/** Push the current tokens to native. Best-effort — callers decide whether a failure matters. */
export async function configureNativeAuth(): Promise<ConfigureAuthOptions> {
  const payload = await buildAuthPayload();
  BeamNotifications.configureAuth(payload);
  return payload;
}

/** A redacted, human-readable rendering of the payload — safe to show on screen. */
export function describeAuthPayload(auth: ConfigureAuthOptions): string {
  const expiry = auth.accessTokenExpiresAt
    ? `${new Date(auth.accessTokenExpiresAt).toLocaleString()}${
        auth.accessTokenExpiresAt < Date.now() ? '  ← EXPIRED' : ''
      }`
    : '(none)';
  return [
    `cid            ${auth.cid}`,
    `pid            ${auth.pid}`,
    `host           ${auth.host}`,
    `accessToken    ${mask(auth.accessToken)}`,
    `refreshToken   ${mask(auth.refreshToken)}`,
    `expiresAt      ${expiry}`,
  ].join('\n');
}

/** Tokens are credentials — show only enough to correlate them with a server-side log. */
function mask(token: string): string {
  if (token.length <= 14) return `${token.slice(0, 4)}… (${token.length} chars)`;
  return `${token.slice(0, 8)}…${token.slice(-4)} (${token.length} chars)`;
}
