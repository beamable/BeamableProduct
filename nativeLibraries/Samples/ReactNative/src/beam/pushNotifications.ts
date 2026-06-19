/**
 * Helpers for the `PushNotificationService` microservice — registering this
 * device's APNs token and sending remote pushes through it. Thin wrappers over
 * the auto-generated `PushNotificationServiceClient` (see beamClient.ts).
 *
 * Flow:
 *   1. The native iOS SDK gives the app an APNs device token (the
 *      `tokenReceived` event after `registerForRemote()`).
 *   2. `registerDevice(token)` forwards it to the microservice, which stores it
 *      against the authenticated player.
 *   3. `sendToSelf(...)` asks the microservice to deliver a real remote push to
 *      every device the player has registered (Apple → device).
 *
 * Remote delivery needs: a physical iOS device (APNs never delivers to the
 * Simulator) and APNs credentials set in the realm's config (see the service's
 * `ApnsSettings`).
 */
import { getPushService } from './beamClient';
import type {
  DeviceList,
  RegisterResult,
  SendResult,
} from './beamable/clients/types';

/**
 * Which APNs environment this build's tokens belong to. Dev builds
 * (`expo run:ios`) and TestFlight use Apple's **sandbox**; only App Store builds
 * use **production**. Override per build if you ship to the App Store.
 */
export const APNS_ENVIRONMENT: 'sandbox' | 'production' = 'sandbox';

function service() {
  const svc = getPushService();
  if (!svc) throw new Error('Not connected — call initBeam() (Connect to Beamable) first.');
  return svc;
}

/** Registers (or refreshes) this device's APNs token with the microservice. */
export function registerDevice(
  token: string,
  environment: 'sandbox' | 'production' = APNS_ENVIRONMENT,
): Promise<RegisterResult> {
  return service().registerDeviceToken({ token, environment });
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
