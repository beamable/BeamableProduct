/**
 * Generic device push-token helpers for a Beamable `PushNotificationService`-style
 * microservice client.
 *
 * The native SDK hands the app a device token via the `tokenReceived` event
 * (`registerForRemote()`): an APNs token on iOS, an FCM token on Android. These helpers
 * forward that token to a microservice that stores it against the authenticated player and
 * can deliver a real remote push back (the server routes each device to Apple/APNs or
 * Firebase/FCM by its stored platform).
 *
 * The microservice client itself is app-specific (its typed proxy is auto-generated per
 * game), so this module does NOT import it. Instead `createPushDevice(getClient)` adapts to
 * any client exposing the standard `registerDeviceToken` / `unregisterDeviceToken` /
 * `listMyDevices` / `sendPushToSelf` methods. The app supplies the binding (see the sample's
 * `src/beam/pushNotifications.ts`).
 *
 * Remote delivery needs a physical device (neither APNs nor FCM deliver reliably to a
 * simulator/emulator for the push token) and the matching provider credentials in the realm
 * config: APNs (`apns_push`) for iOS, FCM (`fcm_push`) for Android.
 */
import { Platform } from 'react-native';

/** Push provider for a device token — matches the microservice's PushPlatform. */
export type PushPlatform = 'apns' | 'fcm';

/** APNs environment a build's tokens belong to (iOS only; ignored for FCM). */
export type ApnsEnvironment = 'sandbox' | 'production';

/** The provider for tokens produced by this platform's native SDK. */
export const DEVICE_PLATFORM: PushPlatform =
  Platform.OS === 'android' ? 'fcm' : 'apns';

/**
 * Which APNs environment this build's tokens belong to (iOS only; ignored for FCM).
 * Dev builds (`expo run:ios`) and TestFlight use Apple's **sandbox**; only App Store builds
 * use **production**. Override per build via `createPushDevice(..., { environment })`.
 */
export const DEFAULT_APNS_ENVIRONMENT: ApnsEnvironment = 'sandbox';

/** Minimal shape of the microservice client these helpers drive. */
export interface PushDeviceServiceClient {
  registerDeviceToken(params: {
    token: string;
    environment: string;
    platform: string;
  }): Promise<unknown>;
  unregisterDeviceToken(params: { token: string }): Promise<unknown>;
  listMyDevices(): Promise<unknown>;
  sendPushToSelf(params: {
    title: string;
    body: string;
    deepLink: string;
  }): Promise<unknown>;
}

export interface PushDeviceOptions {
  /** Default APNs environment for `registerDevice` (iOS). Defaults to `sandbox`. */
  environment?: ApnsEnvironment;
  /**
   * Message shown when the client is not yet available (player not connected).
   * Defaults to a generic "connect first" message.
   */
  notConnectedMessage?: string;
}

export interface PushDevice<C extends PushDeviceServiceClient> {
  /** Registers (or refreshes) this device's push token with the microservice. */
  registerDevice(
    token: string,
    platform?: PushPlatform,
    environment?: ApnsEnvironment,
  ): ReturnType<C['registerDeviceToken']>;
  /** Removes a device token from the player's registrations (e.g. on logout). */
  unregisterDevice(token: string): ReturnType<C['unregisterDeviceToken']>;
  /** Lists the player's registered devices (tokens come back masked). */
  listDevices(): ReturnType<C['listMyDevices']>;
  /** Sends a remote push to every device the current player has registered. */
  sendToSelf(
    title: string,
    body: string,
    deepLink?: string,
  ): ReturnType<C['sendPushToSelf']>;
}

/**
 * Build the device push-token helpers bound to a microservice client.
 *
 * @param getClient resolves the (typed, auto-generated) client, or null/undefined if the
 *   player isn't connected yet. The app owns this resolution (e.g. `() => beam.pushService`).
 */
export function createPushDevice<C extends PushDeviceServiceClient>(
  getClient: () => C | null | undefined,
  options: PushDeviceOptions = {},
): PushDevice<C> {
  const defaultEnv = options.environment ?? DEFAULT_APNS_ENVIRONMENT;
  const notConnected =
    options.notConnectedMessage ??
    'Not connected — initialize Beamable before registering a device.';

  function client(): C {
    const svc = getClient();
    if (!svc) throw new Error(notConnected);
    return svc;
  }

  return {
    registerDevice(token, platform = DEVICE_PLATFORM, environment = defaultEnv) {
      return client().registerDeviceToken({
        token,
        environment,
        platform,
      }) as ReturnType<C['registerDeviceToken']>;
    },
    unregisterDevice(token) {
      return client().unregisterDeviceToken({ token }) as ReturnType<
        C['unregisterDeviceToken']
      >;
    },
    listDevices() {
      return client().listMyDevices() as ReturnType<C['listMyDevices']>;
    },
    sendToSelf(title, body, deepLink) {
      return client().sendPushToSelf({
        title,
        body,
        deepLink: deepLink ?? '',
      }) as ReturnType<C['sendPushToSelf']>;
    },
  };
}
