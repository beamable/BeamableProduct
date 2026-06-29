// Polyfills MUST load before fake-indexeddb and the SDK are imported.
import '../polyfills';
import { hydrateLocalStorage } from '../polyfills';
// IndexedDB polyfill for the SDK's content-manifest cache. Imported here (not
// in polyfills.ts) so it loads AFTER the DOMException/structuredClone shims it
// depends on.
import 'fake-indexeddb/auto';

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
import type { TokenStorage } from '@beamable/sdk';
import { BEAM_CONFIG, isConfigured } from './config';
import { RNTokenStorage, configureAuth } from '@beamable/notifications-react-native';
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

    // Load the persisted localStorage entries (the SDK's `beam_cid`/`beam_pid`
    // realm marker) before Beam.init() reads them synchronously. Without this,
    // the SDK treats every cold start as a realm change and clears our tokens,
    // creating a new guest player each launch.
    await hydrateLocalStorage();

    const storage = await RNTokenStorage.create(BEAM_CONFIG.pid);

    const beam = await Beam.init({
      cid: BEAM_CONFIG.cid,
      pid: BEAM_CONFIG.pid,
      environment: BEAM_CONFIG.environment,
      // RNTokenStorage replicates TokenStorage's public shape; see that file.
      tokenStorage: storage as unknown as TokenStorage,
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
    // Auto-generated client for the CampaignService microservice
    // (adds the typed `beam.campaignServiceClient` accessor).
    beam.use(CampaignServiceClient);
    beamInstance = beam;

    // Best-effort: hand the player's tokens to the native side so the CLOSED-APP analytics
    // funnel can authenticate when the JS runtime is not running. Wrapped so a failure here
    // never breaks init. The host is the API base URL the SDK is pointed at (the explicit
    // env.local override, otherwise the named environment's apiUrl). RNTokenStorage already
    // stores `expiresIn` as an absolute epoch-MILLISECONDS timestamp (see its `isExpired`),
    // so it maps straight onto `accessTokenExpiresAt`.
    try {
      const { accessToken, refreshToken, expiresIn } =
        await storage.getTokenData();
      const host =
        BEAM_CONFIG.apiBase ??
        BeamEnvironment.get(BEAM_CONFIG.environment).apiUrl;
      if (accessToken && refreshToken && host) {
        configureAuth({
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

