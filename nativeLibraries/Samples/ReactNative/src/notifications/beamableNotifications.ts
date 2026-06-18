/**
 * Beamable Notifications — native iOS SDK wrapper (the `beamable-notifications`
 * package). This is the *native* notification path, shown alongside the
 * expo-notifications path in `notifications.ts` so you can compare them.
 *
 * The native module exists on iOS only. Every call here is a no-op on Android,
 * where the package's NativeModule is absent (it would otherwise throw a
 * "not linked" error). Use `isBeamableNotificationsSupported` to gate UI.
 *
 * The SDK is event-driven: methods like `requestPermission()` /
 * `registerForRemote()` return void and the result arrives later on an event
 * (`permissionResult`, `tokenReceived`, `notificationTapped`, …). Subscribe via
 * `addBeamableListener`.
 */
import { Platform } from 'react-native';
import { detailsUrl } from '../linking/links';

export const isBeamableNotificationsSupported = Platform.OS === 'ios';

// Lazy require so Android (and Metro on web) never touches the iOS-only module.
type BeamableModule = typeof import('beamable-notifications');
let mod: BeamableModule['default'] | null = null;
function sdk(): BeamableModule['default'] | null {
  if (!isBeamableNotificationsSupported) return null;
  if (!mod) {
    // eslint-disable-next-line @typescript-eslint/no-var-requires
    mod = require('beamable-notifications').default;
  }
  return mod;
}

export type {
  NotificationData,
  PermissionResult,
  LocalRequest,
  DeliveryReceipt,
  AnalyticsConfig,
} from 'beamable-notifications';

/**
 * Endpoint the Notification Service Extension POSTs to on closed-app delivery.
 *
 * NOTE: this is a Discord webhook for testing. The SDK posts a fixed JSON body
 * ({ event, notificationId, source, ...commonParams }); Discord requires a
 * `content` field and ignores unknown ones, so `configureClosedAppAnalytics`
 * injects `content` via commonParams to make the message render. Treat this URL
 * as a secret — it's shipped into the client here only for a demo.
 */
export const ANALYTICS_ENDPOINT =
  'https://canary.discord.com/api/webhooks/1517215291623608532/nuiDOyAaW3l4Ysn1UzUUAigEdH6SqrtECNNg3RkotAuoEELWkvwDiWmec_3zPH-eke7G';

/**
 * Every event the SDK can emit (feature 3 — "all callbacks"). The Callbacks
 * screen subscribes to all of these. Each comment notes what triggers it.
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
 * Subscribe to a Beamable notification event. Same per-event typing as the
 * SDK's own `addListener` (handler payload is inferred from the event name).
 * Returns a subscription with `.remove()`; call it on unmount. No-op (returns a
 * stub) on Android.
 */
export const addBeamableListener: BeamableModule['addListener'] = ((
  event: never,
  handler: never,
) => {
  const s = sdk();
  if (!s) return { remove: () => {} };
  return s.addListener(event, handler);
}) as BeamableModule['addListener'];

/** Initialize the SDK (wires native delegates). Call once at app start. */
export function initBeamableNotifications(): void {
  sdk()?.initialize();
}

/** Ask for notification permission. Result arrives on the `permissionResult` event. */
export function requestBeamablePermission(): void {
  sdk()?.requestPermission({ alert: true, badge: true, sound: true });
}

/**
 * Schedule a LOCAL notification whose tap deep-links into the app. We stash a
 * full `beamrnsample://details/<id>` URL under `userInfo.deepLink`; the tap
 * handler in `app/_layout.tsx` opens it through the OS, exactly like a real
 * server push carrying a deep link would.
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

/** Re-read the current permission status. Result arrives on `permissionResult`. */
export function getPermissionStatus(): void {
  sdk()?.getPermissionStatus();
}

/** Schedule a local notification with a full request (passthrough to the SDK). */
export function scheduleLocal(
  request: import('beamable-notifications').LocalRequest,
): void {
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

/** Request the pending notifications. Result arrives on `pendingNotifications`. */
export function getPending(): void {
  sdk()?.getPending();
}

/** Register for remote (APNs) push. Token arrives on the `tokenReceived` event. */
export function registerForRemote(): void {
  sdk()?.registerForRemote();
}

/** Stop receiving remote (APNs) push. */
export function unregisterForRemote(): void {
  sdk()?.unregisterForRemote();
}

/** Request stored delivery receipts. Result arrives on `deliveryReceipts`. */
export function getDeliveryReceipts(): void {
  sdk()?.getDeliveryReceipts();
}

/**
 * Configure closed-app analytics. Persists the config into the App Group; the
 * Notification Service Extension reads it and fires the HTTP POST on each push
 * delivery (even when the app is killed). Does not itself make any request.
 */
export function configureAnalytics(
  config: import('beamable-notifications').AnalyticsConfig,
): void {
  sdk()?.configureAnalytics(config);
}

/**
 * Point closed-app analytics at ANALYTICS_ENDPOINT (the Discord webhook). The
 * `content` in commonParams is what Discord renders; the SDK's other fields
 * (event/notificationId/source) ride along and are ignored by Discord.
 *
 * Only has effect once the NSE is built (config plugin `enableServiceExtension`)
 * and a `mutable-content:1` push is delivered on a physical device.
 */
export function configureClosedAppAnalytics(): void {
  configureAnalytics({
    enabled: true,
    endpoint: ANALYTICS_ENDPOINT,
    commonParams: {
      content: '📬 Beamable notification delivered (app closed)',
    },
  });
}

/**
 * Outcome of an app-side webhook POST, surfaced to any `addWebhookListener`
 * subscriber (the Callbacks screen) so the attempt + HTTP result are visible
 * locally, not just in Discord.
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
 * This complements the NSE: the NSE covers REMOTE pushes delivered while the app
 * is closed, but iOS runs NO code for LOCAL notifications delivered while closed.
 * So for local notifications we report from the app in the only moments code
 * runs — foreground presentation and tap/launch. Best-effort; failures are
 * swallowed (analytics must never crash the app) but reported to listeners.
 */
export async function reportDelivery(label: string, n: {
  id?: string;
  title?: string;
}): Promise<void> {
  // `content` is what Discord renders; the rest is structured data.
  const body: Record<string, unknown> = {
    content: `📬 ${label}: ${n.title ?? n.id ?? '(untitled)'}`,
    event: label,
    notificationId: n.id,
    source: 'app',
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

/** Set the app icon badge count. */
export function setBadge(count: number): void {
  sdk()?.setBadge(count);
}

/** Clear delivered notifications from Notification Center. */
export function clearDelivered(): void {
  sdk()?.clearDelivered();
}

/**
 * Cold-start "get intent": if the app was launched by tapping a notification,
 * returns its data (with `deepLink`); otherwise null. null on Android.
 */
export async function getLaunchNotification() {
  const s = sdk();
  if (!s) return null;
  return s.getLaunchNotification();
}

/**
 * Pull a routable deep-link URL out of a notification's payload. The SDK exposes
 * it as `deepLink`, but a raw push may instead carry it under `userInfo`.
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
