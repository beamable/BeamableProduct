/**
 * App-specific binding for the `PushNotificationService` microservice.
 *
 * The generic device push-token logic (platform/env resolution + the
 * register/unregister/list/sendToSelf wrappers) now lives in the shared package
 * `@beamable/notifications-react-native` (`createPushDevice`). This file only binds that
 * helper to THIS app's auto-generated `PushNotificationServiceClient` (resolved via
 * `getPushService()` in beamClient.ts) and re-exports the typed surface the screens use.
 *
 * Remote delivery needs a physical device (neither APNs nor FCM deliver reliably to a
 * simulator/emulator for the push token) and the matching provider credentials in the realm
 * config: APNs (`apns_push`) for iOS, FCM (`fcm_push`) for Android.
 */
import { createPushDevice } from '@beamable/notifications-react-native';
import { getPushService } from './beamClient';
import type {
  DeviceList,
  RegisterResult,
  SendResult,
} from './beamable/clients/types';

export type {
  PushPlatform,
  ApnsEnvironment,
} from '@beamable/notifications-react-native';
export {
  DEVICE_PLATFORM,
  DEFAULT_APNS_ENVIRONMENT as APNS_ENVIRONMENT,
} from '@beamable/notifications-react-native';

// Bind the generic helper to this app's microservice client. The typed return values
// (RegisterResult / DeviceList / SendResult) flow through because the client's methods are
// typed; we re-declare the narrow surface below for the screens.
const device = createPushDevice(getPushService, {
  notConnectedMessage:
    'Not connected — call initBeam() (Connect to Beamable) first.',
});

/** Registers (or refreshes) this device's push token with the microservice. */
export const registerDevice = device.registerDevice as (
  token: string,
  platform?: import('@beamable/notifications-react-native').PushPlatform,
  environment?: import('@beamable/notifications-react-native').ApnsEnvironment,
) => Promise<RegisterResult>;

/** Removes a device token from the player's registrations (e.g. on logout). */
export const unregisterDevice = device.unregisterDevice;

/** Lists the player's registered devices (tokens come back masked). */
export const listDevices = device.listDevices as () => Promise<DeviceList>;

/** Sends a remote push to every device the current player has registered. */
export const sendToSelf = device.sendToSelf as (
  title: string,
  body: string,
  deepLink?: string,
) => Promise<SendResult>;
