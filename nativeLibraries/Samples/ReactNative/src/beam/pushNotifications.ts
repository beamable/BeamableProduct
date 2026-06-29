/**
 * App-specific binding for the `CampaignService` microservice's device endpoints.
 *
 * The native SDK hands the app a device token via the `tokenReceived` event
 * (`registerForRemote()`): an APNs token on iOS, an FCM token on Android. These helpers
 * forward that token to `CampaignService`, which stores it against the authenticated player.
 *
 * Device registration/listing is player-facing (`[ClientCallable]`). Actual push *delivery*
 * is driven server-side / from the Portal Campaign Builder (CampaignService exposes only the
 * admin `SendCampaignPushToPlayer`), so this app no longer sends a push to itself.
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
import type {
  DeviceList,
  RegisterResult,
  UnregisterResult,
} from './beamable/clients/types';

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

/** Resolve the CampaignService client, or throw a friendly error if not connected yet. */
function service() {
  const svc = getPushService();
  if (!svc) throw new Error(NOT_CONNECTED);
  return svc;
}

/** Registers (or refreshes) this device's push token with the microservice. */
export function registerDevice(
  token: string,
  platform: PushPlatform = DEVICE_PLATFORM,
  environment: ApnsEnvironment = DEFAULT_APNS_ENVIRONMENT,
): Promise<RegisterResult> {
  return service().registerDeviceToken({ token, environment, platform });
}

/** Removes a device token from the player's registrations (e.g. on logout). */
export function unregisterDevice(token: string): Promise<UnregisterResult> {
  return service().unregisterDeviceToken({ token });
}

/** Lists the player's registered devices (tokens come back masked). */
export function listDevices(): Promise<DeviceList> {
  return service().listMyDevices();
}
