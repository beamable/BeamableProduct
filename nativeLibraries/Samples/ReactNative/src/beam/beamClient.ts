// Polyfills MUST load before the SDK is imported. The Beamable Web SDK's native
// react-native build installs the browser-global URL polyfill it needs; token,
// config, and content storage all use AsyncStorage. On web this is a no-op.
import '@beamable/sdk/react-native/polyfills';

import {
  AccountService,
  AnnouncementsService,
  AuthService,
  Beam,
  ContentService,
  LeaderboardsService,
  StatsService,
} from '@beamable/sdk';
import { BEAM_CONFIG } from './config';
// The package façade is platform-resolved: native → the native module, web → the built-in
// Unity-WebView bridge (its `index.web.ts`). No per-app web file needed.
import { BeamNotifications } from '@beamable/notifications-react-native';
import { CampaignServiceClient } from './beamable/clients/CampaignServiceClient';

export type BeamStatus =
  | { state: 'idle' }
  | { state: 'connecting' }
  | { state: 'ready'; playerId: string }
  | { state: 'error'; message: string };

let beamInstance: Beam | null = null;
let initPromise: Promise<Beam> | null = null;

/** The current Beam instance, or null if not yet initialized. */
export function getBeam(): Beam | null {
  return beamInstance;
}
 
/**
 * The typed client for the `CampaignService` microservice, or null until
 * `initBeam()` has resolved. Use it to register this device's APNs/FCM token and
 * to list the player's registered devices — e.g. `getPushService()?.listMyDevices()`.
 * (Push delivery itself is driven server-side / from the Portal Campaign Builder.)
 */
export function getPushService(): CampaignServiceClient | null {
  return beamInstance?.campaignServiceClient ?? null;
}

/**
 * Fetch a host-served `beam-config.json` (the realm the host wants this build to use), or `null`
 * when none is served — a plain browser, or a Unity project without a `.beamable`. When hosted in a
 * Unity WebView, `com.beamable.notifications.web` serves this from the Unity project's
 * `.beamable/config.beam.json` (live in the Editor, staged into the app on device). The URL is
 * relative, so it resolves against the served origin.
 */
async function loadRuntimeConfig(): Promise<
  { cid: string; pid: string; host?: string } | null
> {
  try {
    const res = await fetch('beam-config.json', { cache: 'no-store' });
    if (!res.ok) return null;
    const c = (await res.json()) as { cid?: string; pid?: string; host?: string };
    if (c?.cid && c?.pid) return { cid: c.cid, pid: c.pid, host: c.host };
  } catch {
    // Not served (plain web / no host file) → fall back to the built-in config.
  }
  return null;
}

/**
 * Initializes the Beamable SDK once (subsequent calls return the same promise).
 *
 * `Beam.init()` performs a network guest-login and content sync, so it requires valid credentials —
 * resolved at runtime from a host-served `beam-config.json`, else the built-in `src/beam/config.ts`.
 * We pass our AsyncStorage-backed token storage so the guest session persists across app launches.
 */
export async function initBeam(): Promise<Beam> {
  if (initPromise) return initPromise;

  initPromise = (async () => {
    // Resolve the realm at runtime: if the page is hosted somewhere that serves a `beam-config.json`
    // next to it (a Unity WebView via com.beamable.notifications.web serves the Unity project's
    // `.beamable/config.beam.json`), use it; otherwise fall back to the config built into this bundle.
    // This lets one distributed build target each install's own realm — no rebuild, no per-host code.
    const runtime = await loadRuntimeConfig();
    const effective = runtime ?? BEAM_CONFIG;
    const { cid, pid, host } = effective;

    if (!cid || !pid || cid.startsWith('YOUR_') || pid.startsWith('YOUR_')) {
      throw new Error(
        'Beamable cid/pid not set. Edit .beamable/config.beam.json (or the host realm) with your credentials.',
      );
    }

    // No explicit token storage: the SDK's react-native build defaults to an
    // AsyncStorage-backed store that persists the guest session across app
    // launches (config marker `beam_cid`/`beam_pid` + tokens all in AsyncStorage).
    const beam = await Beam.init({ cid, pid, host, gameEngine: 'react-native' });

    // Register every high-level service the app uses. Accessors like
    // `beam.announcements` / `beam.content` / `beam.stats` / `beam.leaderboards`
    // throw "Call beam.use(...)" until their service is registered here.
    // (beam.player is built in and always available.)
     beam.use([
      AuthService,
      AccountService,
      ContentService,
      StatsService,
      AnnouncementsService,
      LeaderboardsService
    ]);
    // CampaignService is the only microservice the client talks to directly, and only for
    // reading the player's registered devices (`listMyDevices`). Device/email/in-game
    // registration goes through the backend `/message-rail` endpoint (see src/beam/messageRail.ts)
    // — the rail microservices (push/email/ingame/messagerail) are backend-only and are never
    // referenced from the client.
    beam.use(CampaignServiceClient);
    beamInstance = beam;

    // Best-effort: hand the player's tokens to the native side so the CLOSED-APP analytics
    // funnel can authenticate when the JS runtime is not running. Wrapped so a failure here
    // never breaks init. The host is the platform URL the SDK is pointed at. The SDK stores
    // `expiresIn` as an absolute epoch-MILLISECONDS timestamp, so it maps straight
    // onto `accessTokenExpiresAt`.
    try {
      const { accessToken, refreshToken, expiresIn } =
        await beam.tokenStorage.getTokenData();
      if (accessToken && refreshToken && host) {
        BeamNotifications.configureAuth({
          accessToken,
          refreshToken,
          accessTokenExpiresAt: expiresIn ?? 0,
          cid,
          pid,
          host,
        });
      }
    } catch {
      // Native funnel auth is best-effort; never block init on it.
    }

    return beam;
  })();

  // If init fails, allow a later retry by clearing the cached promise.
  initPromise.catch(() => {
    initPromise = null;
  });

  return initPromise;
}

