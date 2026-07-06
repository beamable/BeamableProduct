// Polyfills MUST load before the SDK is imported. The RN SDK adapter installs the
// browser-global shims + fake-indexeddb (in the required order) and exposes
// hydrateLocalStorage. On web the adapter's `.web` entry is a no-op.
import '@beamable/sdk-react-native/polyfills';
import { hydrateLocalStorage } from '@beamable/sdk-react-native/polyfills';

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
import { RNTokenStorage } from '@beamable/sdk-react-native';

let beamInstance: Beam | null = null;
let initPromise: Promise<Beam> | null = null;

/** The current Beam instance, or null if not yet initialized. */
export function getBeam(): Beam | null {
  return beamInstance;
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

    // Register every high-level service the Explorer uses. Accessors like
    // `beam.announcements` / `beam.content` / `beam.stats` / `beam.leaderboards`
    // throw "Call beam.use(...)" until their service is registered here.
    // (beam.player is built in and always available.)
    beam.use([
      AuthService,
      AccountService,
      ContentService,
      StatsService,
      AnnouncementsService,
      LeaderboardsService,
    ]);
    beamInstance = beam;

    return beam;
  })();

  // If init fails, allow a later retry by clearing the cached promise.
  initPromise.catch(() => {
    initPromise = null;
  });

  return initPromise;
}
