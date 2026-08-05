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

/**
 * Registers (or refreshes) this device's push token with the backend `push` rail.
 * `registrationData` keys (`token`/`platform`/`environment`) are what the push federation reads.
 */
export function registerDevice(
  token: string,
  platform: PushPlatform = DEVICE_PLATFORM,
  environment: ApnsEnvironment = DEFAULT_APNS_ENVIRONMENT,
): Promise<MessageRailRegistrationResponse> {
  return messageRail().optIn('push', { token, platform, environment });
}

/** Removes the player's registration from the `push` rail (e.g. on logout). */
export function unregisterDevice(): Promise<MessageRailRegistrationResponse> {
  return messageRail().optOut('push');
}

/** Lists the player's registered devices (tokens come back masked) via CampaignService. */
export function listDevices(): Promise<DeviceList> {
  return service().listMyDevices();
}
