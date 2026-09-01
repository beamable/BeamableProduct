// Polyfills MUST load before the SDK is imported. The Beamable Web SDK's native
// react-native build installs the browser-global URL polyfill it needs; token,
// config, and content storage all use AsyncStorage. On web this is a no-op.
import '@beamable/sdk/react-native/polyfills';

import {
  AccountService,
  AnalyticsService,
  AnnouncementsService,
  AuthService,
  Beam,
  ContentService,
  LeaderboardsService,
  MailService,
  MessageRailService,
  StatsService,
  CampaignOfferService,
} from '@beamable/sdk';
import { BEAM_CONFIG, LOCAL_ROUTING_KEY } from './config';
import { CampaignServiceClient } from './beamable/clients/CampaignServiceClient';
import { PlayerStatsServiceClient } from './beamable/clients/PlayerStatsServiceClient';

/**
 * The microservices this app calls. Only used to build the routing-key header below, where the
 * platform names them `micro_<ServiceName>`.
 */
const MICROSERVICES = ['CampaignService', 'PlayerStatsService'] as const;

/**
 * A routing key identifies a developer's machine: letters, digits, `_`, `.`, `-`. Nothing else.
 * Anything failing this is a typo, and a malformed header value makes the platform reject EVERY
 * request with `BadRoutingKeyHeaderException` — so a bad key must never reach the wire.
 */
const ROUTING_KEY_PATTERN = /^[A-Za-z0-9_.-]+$/;

/** The header the platform reads (the SDK's internal `HEADERS.ROUTING_KEY`, which is not exported). */
const ROUTING_KEY_HEADER = 'X-BEAM-SERVICE-ROUTING-KEY';

/**
 * Point microservice calls at services running locally (`beam project run`), when
 * `BEAM_ROUTING_KEY` is set in `env.local`. A no-op otherwise, which is the right default for a
 * realm whose services are deployed.
 *
 * Two deliberate choices, both of which keep authentication out of the blast radius:
 *
 *  - It is applied **after `Beam.init()`**, by mutating the requester's `defaultHeaders` in place
 *    (that object is shared by reference with the requester, so later requests pick it up). The
 *    alternative — `BeamBase.env.BEAM_ROUTING_KEY`, which the SDK also honours — is read on EVERY
 *    request including the guest login inside `init()`. Doing it here means a wrong key can break
 *    a stats call but can never break logging in. This mirrors what the Portal does for extension
 *    SDK instances (`src/lib/utils/extensionSdkRegistry.ts`).
 *  - The key is validated and expanded per service. A raw multi-service string is NOT passed
 *    through: the only accepted input is one machine key.
 */
function applyLocalRoutingKey(beam: Beam): void {
  const key = LOCAL_ROUTING_KEY?.trim();
  if (!key) return;

  if (!ROUTING_KEY_PATTERN.test(key)) {
    console.warn(
      `[beam] Ignoring BEAM_ROUTING_KEY from env.local: '${key}' is not a valid routing key ` +
        '(letters, digits, "_", "." and "-" only). Microservice calls will target the realm\'s ' +
        'deployed services instead.',
    );
    return;
  }

  // `defaultHeaders` is protected on BeamBase; the Portal reaches it the same way.
  const headers = (beam as unknown as { defaultHeaders?: Record<string, string> }).defaultHeaders;
  if (!headers) {
    console.warn('[beam] Could not reach defaultHeaders — local microservice routing not applied.');
    return;
  }

  headers[ROUTING_KEY_HEADER] = MICROSERVICES.map((name) => `micro_${name}:${key}`).join(',');
}

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
 * The typed client for the `PlayerStatsService` microservice, or null until `initBeam()` has
 * resolved. It owns the player's GAME-PRIVATE stats (`game.private.player.{playerId}`) — the
 * domain Beamable segments are evaluated from, and one a client token cannot write. See
 * `src/beam/segments.ts`.
 */
export function getPlayerStatsService(): PlayerStatsServiceClient | null {
  return beamInstance?.playerStatsServiceClient ?? null;
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

    // Init (and its guest login) is done — only now is it safe to add the local-microservice
    // routing header, so a bad key can never break authentication.
    applyLocalRoutingKey(beam);

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
      LeaderboardsService,
      MessageRailService,
      // The virtual offer federation's two client-callable gateway routes (entitlements /
      // redeem). Like the rail, this goes through `/api/campaign-offer/*`, NOT through
      // `micro_BeamableCampaignOfferService` — the federation routes on the microservice
      // itself trust whatever playerId they are handed, so the gateway is the
      // authorized front door and the only thing a client should call.
      CampaignOfferService,
      // The in-game rail delivers campaigns as Beamable mail, so the Inbox tab reads
      // `beam.mail`. AnalyticsService is registered alongside it deliberately: MailService
      // reports the campaign funnel's `Opened` through it on the Unread -> Read transition, so
      // without analytics the Inbox still works but every in-game campaign reports zero
      // engagement -- a silent failure, which is why they are listed together.
      MailService,
      AnalyticsService,
    ]);
    // CampaignService is the only microservice the client talks to directly, and only for
    // reading the player's registered devices (`listMyDevices`). Device/email/in-game opt-in
    // goes through the SDK's `beam.messageRail` service, which calls the backend
    // `/api/message-rail/{register,unregister}` endpoints — the rail microservices
    // (push/email/ingame/messagerail) are backend-only and are never referenced from the client.
    beam.use(CampaignServiceClient);
    // PlayerStatsService is the Segments tab's write path: segments are evaluated from
    // game-private player stats, which a client token cannot touch, so the service does it.
    beam.use(PlayerStatsServiceClient);
    beamInstance = beam;

    // Best-effort: hand the player's tokens to the native side so the CLOSED-APP analytics
    // funnel can authenticate when the JS runtime is not running. Wrapped so a failure here
    // never breaks init. The payload is assembled by `nativeAuth.ts`, which the Analytics tab's
    // auth viewer also uses — one source of truth for what native receives.
    try {
      const { configureNativeAuth } = await import('./nativeAuth');
      await configureNativeAuth();
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

