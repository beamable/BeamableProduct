// Polyfills MUST load before the SDK is imported. The Beamable Web SDK's native
// react-native build installs the browser-global URL polyfill it needs; token,
// config, and content storage all use AsyncStorage. On web this is a no-op.
import '@beamable/sdk/react-native/polyfills';

import {
  AccountService,
  AnnouncementsService,
  AuthService,
  Beam,
  BeamEnvironment,
  ContentService,
  LeaderboardsService,
  StatsService,
} from '@beamable/sdk';
import { BEAM_CONFIG, isConfigured } from './config';
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
 * Initializes the Beamable SDK once (subsequent calls return the same promise).
 *
 * `Beam.init()` performs a network guest-login and content sync, so it requires
 * valid credentials in `src/beam/config.ts`. We pass our AsyncStorage-backed
 * token storage so the guest session persists across app launches.
 */
export async function initBeam(): Promise<Beam> {
  if (initPromise) return initPromise;

  initPromise = (async () => {
    if (!isConfigured()) {
      throw new Error(
        'Beamable cid/pid not set. Edit src/beam/config.ts with your realm credentials.',
      );
    }

    // If env.local provided an API base URL, register it as the `local` environment so the
    // SDK points its requests at that host (Beam.init has no direct apiUrl option — it
    // resolves the base URL from the named environment).
    if (BEAM_CONFIG.apiBase) {
      BeamEnvironment.register(BEAM_CONFIG.environment, {
        apiUrl: BEAM_CONFIG.apiBase,
        portalUrl: '',
        beamMongoExpressUrl: '',
        dockerRegistryUrl: '',
      });
    }

    // No explicit token storage: the SDK's react-native build defaults to an
    // AsyncStorage-backed store that persists the guest session across app
    // launches (config marker `beam_cid`/`beam_pid` + tokens all in AsyncStorage).
    const beam = await Beam.init({
      cid: BEAM_CONFIG.cid,
      pid: BEAM_CONFIG.pid,
      environment: BEAM_CONFIG.environment,
      gameEngine: 'react-native',
    });

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
    // never breaks init. The host is the API base URL the SDK is pointed at (the explicit
    // env.local override, otherwise the named environment's apiUrl). The SDK stores
    // `expiresIn` as an absolute epoch-MILLISECONDS timestamp, so it maps straight
    // onto `accessTokenExpiresAt`.
    try {
      const { accessToken, refreshToken, expiresIn } =
        await beam.tokenStorage.getTokenData();
      const host =
        BEAM_CONFIG.apiBase ??
        BeamEnvironment.get(BEAM_CONFIG.environment).apiUrl;
      if (accessToken && refreshToken && host) {
        BeamNotifications.configureAuth({
          accessToken,
          refreshToken,
          accessTokenExpiresAt: expiresIn ?? 0,
          cid: BEAM_CONFIG.cid,
          pid: BEAM_CONFIG.pid,
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

