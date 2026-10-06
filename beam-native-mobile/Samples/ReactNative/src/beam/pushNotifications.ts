/**
 * App-specific binding for push-device registration.
 *
 * The native SDK hands the app a device token via the `tokenReceived` event
 * (`registerForRemote()`): an APNs token on iOS, an FCM token on Android. These helpers
 * register that token with the backend's **`push` message rail** through the Web SDK's
 * `beam.messageRail` service (`POST /api/message-rail/register`). The backend forwards it to
 * the push federation microservice, which stores it against the authenticated player. The
 * client never talks to the rail microservice directly.
 *
 * Listing the player's devices is still a direct read from `CampaignService`
 * (`listMyDevices`) — the message-rail endpoint only registers/unregisters.
 *
 * Remote delivery needs a physical device (neither APNs nor FCM deliver reliably to a
 * simulator/emulator for the push token) and the matching provider credentials in the realm
 * config: APNs (`apns_push`) for iOS, FCM (`fcm_push`) for Android.
 */
import type { MessageRailRegistrationResponse } from '@beamable/sdk/schema';
import {
  DEVICE_PLATFORM,
  DEFAULT_APNS_ENVIRONMENT,
  type PushPlatform,
  type ApnsEnvironment,
} from '@beamable/notifications-react-native';
import { getBeam, getPushService } from './beamClient';
import type { DeviceList } from './beamable/clients/types';

export type {
  PushPlatform,
  ApnsEnvironment,
} from '@beamable/notifications-react-native';
export {
  DEVICE_PLATFORM,
  DEFAULT_APNS_ENVIRONMENT as APNS_ENVIRONMENT,
} from '@beamable/notifications-react-native';

const NOT_CONNECTED =
  'Not connected — Beamable connects automatically on launch; wait for it, or use Retry connection.';

/** Resolve the message-rail service, or throw if not connected. */
function messageRail() {
  const beam = getBeam();
  if (!beam) throw new Error(NOT_CONNECTED);
  return beam.messageRail;
}

/** Resolve the CampaignService client (used only for listing), or throw if not connected. */
function service() {
  const svc = getPushService();
  if (!svc) throw new Error(NOT_CONNECTED);
  return svc;
}

/** Attempts for a device registration, including the first. */
const REGISTER_ATTEMPTS = 4;
/** Base backoff between registration attempts; doubles each time (0.5s, 1s, 2s). */
const REGISTER_BACKOFF_MS = 500;

const delay = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms));

/**
 * Whether a failed registration is worth retrying.
 *
 * The rail resolves its federation microservice through a TTL'd heartbeat, so while that service
 * is restarting the gateway answers 503 — routinely, on every local stack restart. That is the
 * transient case, and it is the one that matters: a dropped registration is not retried by
 * anything else, so the token is lost until the app happens to ask again, and the player silently
 * stops receiving every campaign push. A 4xx is the opposite — a rejected token or the wrong
 * player — and repeating it just delays the error the caller needs to see.
 */
function isTransientRegistrationFailure(error: unknown): boolean {
  const status = (error as { context?: { response?: { status?: unknown } } })?.context?.response
    ?.status;
  if (typeof status === 'number') {
    return status >= 500 || status === 429;
  }
  // No status at all means the request never completed — a transport fault, always worth a retry.
  return true;
}

/**
 * Registers (or refreshes) this device's push token with the backend `push` rail.
 * `registrationData` keys (`token`/`platform`/`environment`) are what the push federation reads.
 *
 * Retries transient backend failures — see {@link isTransientRegistrationFailure}. The call is
 * idempotent (the federation upserts on the token), so a retry after an ambiguous failure is safe.
 */
export async function registerDevice(
  token: string,
  platform: PushPlatform = DEVICE_PLATFORM,
  environment: ApnsEnvironment = DEFAULT_APNS_ENVIRONMENT,
): Promise<MessageRailRegistrationResponse> {
  let lastError: unknown;

  for (let attempt = 0; attempt < REGISTER_ATTEMPTS; attempt += 1) {
    try {
      return await messageRail().optIn('push', { token, platform, environment });
    } catch (error) {
      lastError = error;
      const isLastAttempt = attempt === REGISTER_ATTEMPTS - 1;
      if (isLastAttempt || !isTransientRegistrationFailure(error)) {
        throw error;
      }
      await delay(REGISTER_BACKOFF_MS * 2 ** attempt);
    }
  }

  // Unreachable: the loop either returns or throws. Kept so the function is total for the checker.
  throw lastError;
}

/** Removes the player's registration from the `push` rail (e.g. on logout). */
export function unregisterDevice(): Promise<MessageRailRegistrationResponse> {
  return messageRail().optOut('push');
}

/** Lists the player's registered devices (tokens come back masked) via CampaignService. */
export function listDevices(): Promise<DeviceList> {
  return service().listMyDevices();
}
