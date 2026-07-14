/**
 * Convenience helpers over the unified `BeamableNotifications` façade.
 *
 * These are the generic, app-agnostic wrappers that used to live in the React Native
 * sample's `src/notifications/beamableNotifications.ts`. They add:
 *   - `isBeamableNotificationsSupported` — a platform gate for UI.
 *   - named function wrappers (`requestBeamablePermission`, `registerForRemote`, …) that
 *     no-op on unsupported platforms instead of throwing.
 *   - `BEAMABLE_EVENTS` — the list of every event the SDK can emit.
 *   - `addBeamableListener` / `addBeamableDeepLinkListener` — listeners that are inert
 *     stubs on unsupported platforms.
 *
 * Autolinking selects the per-platform native code, so there is no longer any runtime
 * `Platform.OS` branch to pick a package — the single `BeamableNotifications` object below
 * already routes per platform internally.
 *
 * NOTE: the demo Slack/Discord webhook that previously lived in the sample façade has been
 * removed. Funnel analytics (Received/Opened/Sent/Clicked/Converted) are emitted natively
 * via the Beamable analytics endpoint; the sample no longer POSTs a webhook.
 */
// Values from `./index` (Metro platform-swaps to `./index.web.ts` on web); types from
// `./types` (shared, platform-agnostic).
import {
  BeamableNotifications,
  addListener,
  addDeepLinkListener,
  isBeamableNotificationsSupported,
} from './index';
import type {
  ConfigureAuthOptions,
  DeepLinkEvent,
  DeliveryReceipt,
  EventMap,
  LocalRequest,
  NotificationData,
  PermissionResult,
} from './types';

// `isBeamableNotificationsSupported`, `BEAMABLE_EVENTS`, and `BeamableEvent` now live in
// ./index (single source, shared with the BeamNotifications façade). These wrappers just add
// the same platform gate the façade already applies, kept for back-compat.

/**
 * Subscribe to a Beamable notification event. Same per-event typing as the façade's own
 * `addListener` (handler payload inferred from the event name). Returns a subscription with
 * `.remove()`. Inert stub on unsupported platforms.
 */
export const addBeamableListener: typeof addListener = ((
  event: keyof EventMap,
  handler: never,
) => {
  if (!isBeamableNotificationsSupported) return { remove: () => {} };
  return addListener(event, handler);
}) as typeof addListener;

/**
 * Android-only: subscribe to native URL-scheme deep links (VIEW intents). No-op on iOS
 * (which routes deep links via notification payloads) and on web.
 */
export function addBeamableDeepLinkListener(
  handler: (event: DeepLinkEvent) => void,
): { remove: () => void } {
  if (!isBeamableNotificationsSupported) return { remove: () => {} };
  return addDeepLinkListener(handler);
}

/** Initialize push + deeplink. Call once at app start. No-op on unsupported platforms. */
export function initBeamableNotifications(): void {
  if (isBeamableNotificationsSupported) BeamableNotifications.initialize();
}

/**
 * Ask for notification permission. Resolves with the outcome (the `permissionResult` event
 * still fires too). Resolves `{ granted: false }` on unsupported platforms.
 */
export function requestBeamablePermission(): Promise<PermissionResult> {
  return BeamableNotifications.requestPermission({
    alert: true,
    badge: true,
    sound: true,
  });
}

/** Re-read the current permission status. Resolves with it (iOS; `notDetermined` on Android). */
export function getPermissionStatus(): Promise<PermissionResult> {
  return BeamableNotifications.getPermissionStatus();
}

/** Schedule a local notification with a full request (passthrough to the SDK). */
export function scheduleLocal(request: LocalRequest): void {
  if (isBeamableNotificationsSupported)
    BeamableNotifications.scheduleLocal(request);
}

/** Cancel a pending local notification by id. */
export function cancelLocal(id: string): void {
  if (isBeamableNotificationsSupported) BeamableNotifications.cancelLocal(id);
}

/** Cancel all pending local notifications. */
export function cancelAllLocal(): void {
  if (isBeamableNotificationsSupported) BeamableNotifications.cancelAllLocal();
}

/**
 * Resolve the pending (scheduled, not-yet-delivered) local notifications. iOS only;
 * resolves `[]` on Android / unsupported platforms. The `pendingNotifications` event still
 * fires too (iOS).
 */
export function getPending(): Promise<NotificationData[]> {
  return BeamableNotifications.getPending();
}

/**
 * Register for remote push and resolve with the device token (the `tokenReceived` event
 * still fires too). Rejects on `tokenError`/timeout and on unsupported platforms.
 */
export function registerForRemote(options?: {
  timeoutMs?: number;
}): Promise<{ token: string }> {
  return BeamableNotifications.registerForRemote(options);
}

/** Stop receiving remote push (iOS). */
export function unregisterForRemote(): void {
  if (isBeamableNotificationsSupported)
    BeamableNotifications.unregisterForRemote();
}

/**
 * Resolve stored delivery receipts. iOS only; resolves `[]` on Android / unsupported
 * platforms. The `deliveryReceipts` event still fires too (iOS).
 */
export function getDeliveryReceipts(): Promise<DeliveryReceipt[]> {
  return BeamableNotifications.getDeliveryReceipts();
}

/**
 * Write the player's Beamable tokens into native shared storage so the closed-app analytics
 * funnel can authenticate while the JS runtime is not running. No-op on unsupported platforms.
 * `accessTokenExpiresAt` is an absolute epoch-MILLISECONDS timestamp.
 */
export function configureAuth(auth: ConfigureAuthOptions): void {
  if (isBeamableNotificationsSupported)
    BeamableNotifications.configureAuth(auth);
}

/** Clear the player auth previously written via {@link configureAuth}. */
export function clearAuth(): void {
  if (isBeamableNotificationsSupported) BeamableNotifications.clearAuth();
}

/** Set the app icon badge count (iOS). */
export function setBadge(count: number): void {
  if (isBeamableNotificationsSupported) BeamableNotifications.setBadge(count);
}

/** Clear delivered notifications from Notification Center (iOS). */
export function clearDelivered(): void {
  if (isBeamableNotificationsSupported) BeamableNotifications.clearDelivered();
}

/**
 * Cold-start "get launch notification": resolves the payload (with `deeplink`) if the app
 * was launched by tapping a notification, else null. null on unsupported platforms.
 */
export function getLaunchNotification() {
  if (!isBeamableNotificationsSupported) return Promise.resolve(null);
  return BeamableNotifications.getLaunchNotification();
}
