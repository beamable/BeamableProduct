/**
 * App-specific binding for push-device registration.
 *
 * The native SDK hands the app a device token via the `tokenReceived` event
 * (`registerForRemote()`): an APNs token on iOS, an FCM token on Android. These helpers
 * register that token with the backend's **`push` message rail** (via `/message-rail/register`
 * — see messageRail.ts). The backend forwards it to the push federation microservice, which
 * stores it against the authenticated player. The client never talks to the rail microservice
 * directly.
 *
 * Listing the player's devices is still a direct read from `CampaignService`
 * (`listMyDevices`) — the message-rail endpoint only registers/unregisters.
 *
 * Remote delivery needs a physical device (neither APNs nor FCM deliver reliably to a
 * simulator/emulator for the push token) and the matching provider credentials in the realm
 * config: APNs (`apns_push`) for iOS, FCM (`fcm_push`) for Android.
 */
import {
  DEVICE_PLATFORM,
  DEFAULT_APNS_ENVIRONMENT,
  type PushPlatform,
  type ApnsEnvironment,
} from '@beamable/notifications-react-native';
import { getPushService } from './beamClient';
import {
  registerRail,
  unregisterRail,
  type MessageRailRegistrationResponse,
} from './messageRail';
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
  'Not connected — call initBeam() (Connect to Beamable) first.';

/** Resolve the CampaignService client (used only for listing), or throw if not connected. */
function service() {
  const svc = getPushService();
  if (!svc) throw new Error(NOT_CONNECTED);
  return svc;
}

/**
 * Registers (or refreshes) this device's push token with the backend `push` rail.
 * `registrationData` keys (`token`/`platform`/`environment`) are what the push federation reads.
 */
export function registerDevice(
  token: string,
  platform: PushPlatform = DEVICE_PLATFORM,
  environment: ApnsEnvironment = DEFAULT_APNS_ENVIRONMENT,
): Promise<MessageRailRegistrationResponse> {
  return registerRail('push', { token, platform, environment });
}

/** Removes the player's registration from the `push` rail (e.g. on logout). */
export function unregisterDevice(): Promise<MessageRailRegistrationResponse> {
  return unregisterRail('push');
}

/** Lists the player's registered devices (tokens come back masked) via CampaignService. */
export function listDevices(): Promise<DeviceList> {
  return service().listMyDevices();
}
