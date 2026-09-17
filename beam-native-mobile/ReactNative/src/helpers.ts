/**
 * @deprecated Back-compat flat wrappers. **Prefer the single `BeamNotifications` façade** (and
 * the React hooks) — `BeamNotifications.<method>()` is already platform-gated, so these add
 * nothing but a second name for each call. They remain exported so existing code keeps working;
 * new code should not use them. Each is annotated with its `BeamNotifications` equivalent.
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
 * @deprecated Use `BeamNotifications.addListener`.
 * Subscribe to a Beamable notification event. Inert stub on unsupported platforms.
 */
export const addBeamableListener: typeof addListener = ((
  event: keyof EventMap,
  handler: never,
) => {
  if (!isBeamableNotificationsSupported) return { remove: () => {} };
  return addListener(event, handler);
}) as typeof addListener;

/**
 * @deprecated Use `BeamNotifications.addDeepLinkListener`.
 * Android-only: subscribe to native URL-scheme deep links (VIEW intents). No-op on iOS / web.
 */
export function addBeamableDeepLinkListener(
  handler: (event: DeepLinkEvent) => void,
): { remove: () => void } {
  if (!isBeamableNotificationsSupported) return { remove: () => {} };
  return addDeepLinkListener(handler);
}

/** @deprecated Use `BeamNotifications.initialize()`. Initialize push + deeplink once at app start. */
export function initBeamableNotifications(): void {
  if (isBeamableNotificationsSupported) BeamableNotifications.initialize();
}

/**
 * @deprecated Use `BeamNotifications.requestPermission()`.
 * Ask for notification permission. Resolves with the outcome.
 */
export function requestBeamablePermission(): Promise<PermissionResult> {
  return BeamableNotifications.requestPermission({
    alert: true,
    badge: true,
    sound: true,
  });
}

/** @deprecated Use `BeamNotifications.getPermissionStatus()`. Resolves the permission status. */
export function getPermissionStatus(): Promise<PermissionResult> {
  return BeamableNotifications.getPermissionStatus();
}

/** @deprecated Use `BeamNotifications.scheduleLocal()`. */
export function scheduleLocal(request: LocalRequest): void {
  if (isBeamableNotificationsSupported)
    BeamableNotifications.scheduleLocal(request);
}

/** @deprecated Use `BeamNotifications.cancelLocal()`. */
export function cancelLocal(id: string): void {
  if (isBeamableNotificationsSupported) BeamableNotifications.cancelLocal(id);
}

/** @deprecated Use `BeamNotifications.cancelAllLocal()`. */
export function cancelAllLocal(): void {
  if (isBeamableNotificationsSupported) BeamableNotifications.cancelAllLocal();
}

/** @deprecated Use `BeamNotifications.getPending()`. Resolves pending locals (iOS; `[]` else). */
export function getPending(): Promise<NotificationData[]> {
  return BeamableNotifications.getPending();
}

/** @deprecated Use `BeamNotifications.registerForRemote()`. Resolves the device token. */
export function registerForRemote(options?: {
  timeoutMs?: number;
}): Promise<{ token: string }> {
  return BeamableNotifications.registerForRemote(options);
}

/** @deprecated Use `BeamNotifications.unregisterForRemote()`. */
export function unregisterForRemote(): void {
  if (isBeamableNotificationsSupported)
    BeamableNotifications.unregisterForRemote();
}

/** @deprecated Use `BeamNotifications.getDeliveryReceipts()`. Resolves receipts (iOS; `[]` else). */
export function getDeliveryReceipts(): Promise<DeliveryReceipt[]> {
  return BeamableNotifications.getDeliveryReceipts();
}

/**
 * @deprecated Use `BeamNotifications.configureAuth()`.
 * `accessTokenExpiresAt` is an absolute epoch-MILLISECONDS timestamp.
 */
export function configureAuth(auth: ConfigureAuthOptions): void {
  if (isBeamableNotificationsSupported)
    BeamableNotifications.configureAuth(auth);
}

/** @deprecated Use `BeamNotifications.clearAuth()`. */
export function clearAuth(): void {
  if (isBeamableNotificationsSupported) BeamableNotifications.clearAuth();
}

/** @deprecated Use `BeamNotifications.setBadge()`. */
export function setBadge(count: number): void {
  if (isBeamableNotificationsSupported) BeamableNotifications.setBadge(count);
}

/** @deprecated Use `BeamNotifications.clearDelivered()`. */
export function clearDelivered(): void {
  if (isBeamableNotificationsSupported) BeamableNotifications.clearDelivered();
}

/** @deprecated Use `BeamNotifications.getLaunchNotification()`. Resolves the cold-start payload. */
export function getLaunchNotification() {
  if (!isBeamableNotificationsSupported) return Promise.resolve(null);
  return BeamableNotifications.getLaunchNotification();
}
