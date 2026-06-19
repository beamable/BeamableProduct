/**
 * Helpers for the `PushNotificationService` microservice — registering this
 * device's push token and sending remote pushes through it. Thin wrappers over
 * the auto-generated `PushNotificationServiceClient` (see beamClient.ts).
 *
 * Flow:
 *   1. The native SDK gives the app a device token (the `tokenReceived` event
 *      after `registerForRemote()`): an APNs token on iOS, an FCM token on Android.
 *   2. `registerDevice(token)` forwards it to the microservice tagged with the
 *      platform ("apns" / "fcm"), which stores it against the authenticated player.
 *   3. `sendToSelf(...)` asks the microservice to deliver a real remote push to
 *      every device the player has registered — the server routes each device to
 *      Apple (APNs) or Firebase (FCM) by its stored platform.
 *
 * Remote delivery needs a physical device (neither APNs nor FCM deliver reliably
 * to a stock simulator/emulator for the push token) and the matching provider
 * credentials in the realm config: APNs (`ApnsSettings`, `apns_push`) for iOS,
 * FCM (`FcmSettings`, `fcm_push`) for Android.
 */
import { Platform } from 'react-native';
import { getPushService } from './beamClient';
import type {
  DeviceList,
  RegisterResult,
  SendResult,
} from './beamable/clients/types';

/** Push provider for a device token — matches the microservice's PushPlatform. */
export type PushPlatform = 'apns' | 'fcm';

/** The provider for tokens produced by this platform's native SDK. */
export const DEVICE_PLATFORM: PushPlatform =
  Platform.OS === 'android' ? 'fcm' : 'apns';

/**
 * Which APNs environment this build's tokens belong to (iOS only; ignored for FCM).
 * Dev builds (`expo run:ios`) and TestFlight use Apple's **sandbox**; only App Store
 * builds use **production**. Override per build if you ship to the App Store.
 */
export const APNS_ENVIRONMENT: 'sandbox' | 'production' = 'sandbox';

function service() {
  const svc = getPushService();
  if (!svc) throw new Error('Not connected — call initBeam() (Connect to Beamable) first.');
  return svc;
}

/**
 * Registers (or refreshes) this device's push token with the microservice.
 * `platform` defaults to this device's provider (FCM on Android, APNs on iOS).
 */
export function registerDevice(
  token: string,
  platform: PushPlatform = DEVICE_PLATFORM,
  environment: 'sandbox' | 'production' = APNS_ENVIRONMENT,
): Promise<RegisterResult> {
  return service().registerDeviceToken({ token, environment, platform });
}

/** Removes a device token from the player's registrations (e.g. on logout). */
export function unregisterDevice(token: string) {
  return service().unregisterDeviceToken({ token });
}

/** Lists the player's registered devices (tokens come back masked). */
export function listDevices(): Promise<DeviceList> {
  return service().listMyDevices();
}

/** Sends a remote push to every device the current player has registered. */
export function sendToSelf(
  title: string,
  body: string,
  deepLink?: string,
): Promise<SendResult> {
  return service().sendPushToSelf({ title, body, deepLink: deepLink ?? '' });
}
