// Polyfills MUST load before fake-indexeddb and the SDK are imported.
import '../polyfills';
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
import { RNTokenStorage } from './RNTokenStorage';
import { SampleServiceClient } from './beamable/clients/SampleServiceClient';
import { PushNotificationServiceClient } from './beamable/clients/PushNotificationServiceClient';

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
 * The typed client for the `SampleService` microservice, or null until
 * `initBeam()` has resolved. Registered below via `beam.use(...)`, which adds
 * the typed `beam.sampleServiceClient` accessor. Call e.g.
 * `getSampleService()?.add({ a: 2, b: 3 })`.
 */
export function getSampleService(): SampleServiceClient | null {
  return beamInstance?.sampleServiceClient ?? null;
}

/**
 * The typed client for the `PushNotificationService` microservice, or null until
 * `initBeam()` has resolved. Use it to register this device's APNs token and to
 * send remote pushes — e.g. `getPushService()?.sendPushToSelf({ title, body })`.
 */
export function getPushService(): PushNotificationServiceClient | null {
  return beamInstance?.pushNotificationServiceClient ?? null;
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
    // Auto-generated client for the SampleService microservice. Registering
    // it adds the typed `beam.sampleServiceClient` accessor.
    beam.use(SampleServiceClient);
    // Auto-generated client for the PushNotificationService microservice
    // (adds the typed `beam.pushNotificationServiceClient` accessor).
    beam.use(PushNotificationServiceClient);
    beamInstance = beam;
  
    return beam;
  })();

  // If init fails, allow a later retry by clearing the cached promise.
  initPromise.catch(() => {
    initPromise = null;
  });

  return initPromise;
}

