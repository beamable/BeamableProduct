/**
 * OPTIONAL: Beamable native-push registration.
 *
 * Not used by the default demo (we chose local notifications), but included to
 * show how the device's native push token (FCM on Android / APNS on iOS) is
 * registered with Beamable via the SDK's PushApi.
 *
 * To actually receive server-sent pushes you must also configure an FCM/APNS
 * provider in your Beamable realm. Get the device token from the native SDK's
 * `tokenReceived` event after `registerForRemote()` (see
 * src/notifications/beamableNotifications.ts; requires a real device + a dev
 * build, not Expo Go), then call `registerPushToken`.
 */
import { pushPostRegisterBasic } from '@beamable/sdk/api';
import type { Beam } from '@beamable/sdk';

export type PushProvider = 'fcm' | 'apns' | (string & {});

/** Register a native device push token with Beamable for the current player. */
export async function registerPushToken(
  beam: Beam,
  provider: PushProvider,
  token: string,
) {
  // `beam.requester` is the authenticated HTTP requester (carries the player's
  // bearer token), which this endpoint requires.
  return pushPostRegisterBasic(beam.requester, { provider, token });
}
