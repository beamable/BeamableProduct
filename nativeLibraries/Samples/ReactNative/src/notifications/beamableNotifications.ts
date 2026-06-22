/**
 * Beamable Notifications — cross-platform native SDK façade.
 *
 * This is the app's single notification path. It routes to the right native package per
 * platform:
 *   - iOS     → `beamable-notifications-ios`     (Swift core, compiled into the pod)
 *   - Android → `beamable-notifications-android`  (the prebuilt `.aar`'s RN bridges)
 *
 * Both packages expose the SAME API surface, event names, and DTOs, so everything below is
 * platform-agnostic. Calls are no-ops on web (where neither native module exists). Use
 * `isBeamableNotificationsSupported` to gate UI.
 *
 * The SDK is event-driven: methods like `requestPermission()` / `registerForRemote()`
 * return void and the result arrives later on an event (`permissionResult`,
 * `tokenReceived`, `notificationTapped`, …). Subscribe via `addBeamableListener`.
 *
 * Platform differences worth knowing:
 *   - iOS-only features (templates, categories, NSE closed-app analytics, badge, pending
 *     list) are no-ops on Android.
 *   - Android-only: a receive-time `PushNotificationReceivedHandler` (native Java, runs even
 *     when the app is killed — see `plugins/android/BeamablePushReceivedHandler.java`) and a
 *     native URL-scheme deep-link capture surfaced via `addBeamableDeepLinkListener`.
 */
import { Platform } from 'react-native';
import { detailsUrl } from '../linking/links';

// Types are identical across both packages; import them from the iOS package as canonical.
import type {
  NotificationData,
  PermissionResult,
  LocalRequest,
  DeliveryReceipt,
  AnalyticsConfig,
} from 'beamable-notifications-ios';

export type {
  NotificationData,
  PermissionResult,
  LocalRequest,
  DeliveryReceipt,
  AnalyticsConfig,
};

type BeamableSdk = typeof import('beamable-notifications-ios').default & {
  // Android-only extension (feature-detected at runtime).
  addDeepLinkListener?: (
    handler: (e: { url: string; isColdStart: boolean }) => void,
  ) => { remove: () => void };
};

export const isBeamableNotificationsSupported =
  Platform.OS === 'ios' || Platform.OS === 'android';

// Lazy require so web (and Metro) never touches a native-only module, and so each platform
// only loads its own package.
let mod: BeamableSdk | null = null;
function sdk(): BeamableSdk | null {
  if (!isBeamableNotificationsSupported) return null;
  if (!mod) {
    // eslint-disable-next-line @typescript-eslint/no-var-requires
    if (Platform.OS === 'ios') {
      mod = require('beamable-notifications-ios').default as BeamableSdk;
    } else {
      // eslint-disable-next-line @typescript-eslint/no-var-requires
      mod = require('beamable-notifications-android').default as BeamableSdk;
    }
  }
  return mod;
}

/**
 * Endpoint the Notification Service Extension POSTs to on closed-app delivery (iOS), and
 * the same webhook the Android receive-time handler posts to.
 *
 * NOTE: this is a Slack workflow webhook trigger for testing. It expects a JSON body with a
 * `message` field (`{ "message": "…" }`); `reportDelivery` and the Android handler post
 * exactly that, and `configureClosedAppAnalytics` injects `message` via commonParams. Treat
 * this URL as a secret — it's shipped into the client here only for a demo.
 */
export const ANALYTICS_ENDPOINT =
  'https://hooks.slack.com/triggers/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';

/**
 * Every event the SDK can emit (feature 3 — "all callbacks"). The Callbacks screen
 * subscribes to all of these. Each comment notes what triggers it.
 */
export const BEAMABLE_EVENTS = [
  'permissionResult', //   requestPermission() / getPermissionStatus()
  'tokenReceived', //      registerForRemote() succeeds (real device)
  'tokenError', //         registerForRemote() fails
  'notificationPresented', // a notification arrives while app is FOREGROUND
  'notificationReceived', //  a notification is delivered (received)
  'notificationTapped', //    user taps a notification
  'pendingNotifications', //  getPending()
  'deliveryReceipts', //      getDeliveryReceipts()
] as const;

export type BeamableEvent = (typeof BEAMABLE_EVENTS)[number];

/**
 * Subscribe to a Beamable notification event. Same per-event typing as the SDK's own
 * `addListener` (handler payload is inferred from the event name). Returns a subscription
 * with `.remove()`; call it on unmount. No-op (returns a stub) on unsupported platforms.
 */
export const addBeamableListener: BeamableSdk['addListener'] = ((
  event: never,
  handler: never,
) => {
  const s = sdk();
  if (!s) return { remove: () => {} };
  return s.addListener(event, handler);
}) as BeamableSdk['addListener'];

/**
 * Android-only: subscribe to native URL-scheme deep links (VIEW intents) captured by the
 * Beamable deeplink module. No-op on iOS (which routes deep links via notification
 * payloads) and on web. Returns a subscription with `.remove()`.
 */
export function addBeamableDeepLinkListener(
  handler: (event: { url: string; isColdStart: boolean }) => void,
): { remove: () => void } {
  const s = sdk();
  if (!s || typeof s.addDeepLinkListener !== 'function') {
    return { remove: () => {} };
  }
  return s.addDeepLinkListener(handler);
}

/** Initialize the SDK (wires native delegates). Call once at app start. */
export function initBeamableNotifications(): void {
  sdk()?.initialize();
}

/** Ask for notification permission. Result arrives on the `permissionResult` event. */
export function requestBeamablePermission(): void {
  sdk()?.requestPermission({ alert: true, badge: true, sound: true });
}

/**
 * Schedule a LOCAL notification whose tap deep-links into the app. We stash a full
 * `beamrnsample://details/<id>` URL under `userInfo.deepLink`; the tap handler in
 * `app/_layout.tsx` opens it through the OS, exactly like a real server push carrying a
 * deep link would. On Android this also exercises the receive-time handler.
 *
 * @param seconds delay before firing. Omit/0 to fire immediately.
 */
export function scheduleBeamableDeepLink(opts: {
  id: string;
  title: string;
  body: string;
  detailsId: string | number;
  seconds?: number;
}): void {
  sdk()?.scheduleLocal({
    id: opts.id,
    title: opts.title,
    body: opts.body,
    trigger:
      opts.seconds && opts.seconds > 0
        ? { type: 'timeInterval', seconds: opts.seconds }
        : { type: 'immediate' },
    userInfo: { deepLink: detailsUrl(opts.detailsId) },
  });
}

/** Re-read the current permission status. Result arrives on `permissionResult` (iOS). */
export function getPermissionStatus(): void {
  sdk()?.getPermissionStatus();
}

/** Schedule a local notification with a full request (passthrough to the SDK). */
export function scheduleLocal(request: LocalRequest): void {
  sdk()?.scheduleLocal(request);
}

/** Cancel a pending local notification by id. */
export function cancelLocal(id: string): void {
  sdk()?.cancelLocal(id);
}

/** Cancel all pending local notifications. */
export function cancelAllLocal(): void {
  sdk()?.cancelAllLocal();
}

/** Request the pending notifications. Result arrives on `pendingNotifications` (iOS). */
export function getPending(): void {
  sdk()?.getPending();
}

/** Register for remote push. Token arrives on the `tokenReceived` event (APNs / FCM). */
export function registerForRemote(): void {
  sdk()?.registerForRemote();
}

/** Stop receiving remote push (iOS). */
export function unregisterForRemote(): void {
  sdk()?.unregisterForRemote();
}

/** Request stored delivery receipts. Result arrives on `deliveryReceipts` (iOS). */
export function getDeliveryReceipts(): void {
  sdk()?.getDeliveryReceipts();
}

/**
 * Configure closed-app analytics (iOS). Persists the config into the App Group; the
 * Notification Service Extension reads it and fires the HTTP POST on each push delivery
 * (even when the app is killed). No-op on Android, where the equivalent is the native
 * receive-time handler (`BeamablePushReceivedHandler`).
 */
export function configureAnalytics(config: AnalyticsConfig): void {
  sdk()?.configureAnalytics(config);
}

/**
 * Point closed-app analytics at ANALYTICS_ENDPOINT (the Discord webhook) on iOS. The
 * `content` in commonParams is what Discord renders. No-op on Android.
 *
 * Only has effect on iOS once the NSE is built (config plugin `enableServiceExtension`)
 * and a `mutable-content:1` push is delivered on a physical device.
 */
export function configureClosedAppAnalytics(): void {
  configureAnalytics({
    enabled: true,
    endpoint: ANALYTICS_ENDPOINT,
    commonParams: {
      message: '📬 Beamable notification delivered (app closed)',
    },
  });
}

/**
 * Outcome of an app-side webhook POST, surfaced to any `addWebhookListener` subscriber (the
 * Callbacks screen) so the attempt + HTTP result are visible locally, not just in Discord.
 */
export type WebhookReport = {
  label: string;
  body: Record<string, unknown>;
  ok: boolean;
  status?: number;
  error?: string;
};

const webhookListeners = new Set<(r: WebhookReport) => void>();

/** Subscribe to app-side webhook POST outcomes. Returns a remover. */
export function addWebhookListener(fn: (r: WebhookReport) => void): {
  remove: () => void;
} {
  webhookListeners.add(fn);
  return { remove: () => webhookListeners.delete(fn) };
}

function emitWebhook(r: WebhookReport): void {
  webhookListeners.forEach((fn) => fn(r));
}

/**
 * App-side delivery report — POSTs to ANALYTICS_ENDPOINT directly from JS.
 *
 * This complements the native receive-time paths: it reports from the app in the moments
 * code runs (foreground presentation and tap/launch), covering local notifications which
 * iOS runs no code for while closed. Best-effort; failures are swallowed (analytics must
 * never crash the app) but reported to listeners.
 */
export async function reportDelivery(label: string, n: {
  id?: string;
  title?: string;
}): Promise<void> {
  // The Slack workflow trigger reads a single `message` field.
  const body: Record<string, unknown> = {
    message: `📬 ${label}: ${n.title ?? n.id ?? '(untitled)'}`,
  };
  try {
    const res = await fetch(ANALYTICS_ENDPOINT, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
    emitWebhook({ label, body, ok: res.ok, status: res.status });
  } catch (e) {
    emitWebhook({
      label,
      body,
      ok: false,
      error: e instanceof Error ? e.message : String(e),
    });
  }
}

/** Set the app icon badge count (iOS). */
export function setBadge(count: number): void {
  sdk()?.setBadge(count);
}

/** Clear delivered notifications from Notification Center (iOS). */
export function clearDelivered(): void {
  sdk()?.clearDelivered();
}

/**
 * Cold-start "get intent": if the app was launched by tapping a notification, returns its
 * data (with `deepLink`); otherwise null. null on web.
 */
export async function getLaunchNotification() {
  const s = sdk();
  if (!s) return null;
  return s.getLaunchNotification();
}

/**
 * Pull a routable deep-link URL out of a notification's payload. The SDK exposes it as
 * `deepLink`, but a raw push may instead carry it under `userInfo`.
 */
export function deepLinkFromNotification(n: {
  deepLink?: string;
  userInfo?: Record<string, unknown>;
}): string | null {
  if (typeof n.deepLink === 'string' && n.deepLink.length > 0) return n.deepLink;
  const fromUserInfo = n.userInfo?.deepLink;
  return typeof fromUserInfo === 'string' && fromUserInfo.length > 0
    ? fromUserInfo
    : null;
}
