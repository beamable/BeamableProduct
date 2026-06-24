/**
 * Beamable Notifications — app glue over the shared SDK.
 *
 * The cross-platform façade (event vocabulary, native routing, permission/scheduling/remote
 * APIs) now lives in the single unified package `@beamable/notifications-react-native`.
 * Autolinking selects the correct native code per platform (iOS xcframework / Android AAR),
 * so there is no runtime `Platform.OS` branch to pick a package anymore.
 *
 * This file re-exports the package's generic helpers and adds only the few APP-SPECIFIC
 * pieces that depend on the sample's deep-link scheme (`beamrnsample`):
 *   - `scheduleBeamableDeepLink` — schedule a local notification whose tap deep-links into
 *     a details screen via a full `beamrnsample://details/<id>` URL.
 *   - `deepLinkFromNotification` — pull a routable deep-link URL out of a notification.
 *
 * NOTE: the demo Slack/Discord webhook is gone. Funnel analytics (Received/Opened/Sent/
 * Clicked/Converted) are emitted natively via the Beamable analytics endpoint; the sample
 * no longer POSTs a webhook.
 */
import { detailsUrl, normalizeDeepLink } from '../linking/links';
import {
  BeamableNotifications,
  isBeamableNotificationsSupported,
} from '@beamable/notifications-react-native';

// Re-export the generic helpers + types from the unified package so existing import sites
// in the app (`from '../src/notifications/beamableNotifications'`) keep working.
export {
  isBeamableNotificationsSupported,
  BEAMABLE_EVENTS,
  type BeamableEvent,
  addBeamableListener,
  addBeamableDeepLinkListener,
  initBeamableNotifications,
  requestBeamablePermission,
  getPermissionStatus,
  scheduleLocal,
  cancelLocal,
  cancelAllLocal,
  getPending,
  registerForRemote,
  unregisterForRemote,
  getDeliveryReceipts,
  configureAnalytics,
  setBadge,
  clearDelivered,
  getLaunchNotification,
} from '@beamable/notifications-react-native';

export type {
  NotificationData,
  PermissionResult,
  LocalRequest,
  DeliveryReceipt,
  AnalyticsConfig,
} from '@beamable/notifications-react-native';

/**
 * Schedule a LOCAL notification whose tap deep-links into the app. We stash a full
 * `beamrnsample://details/<id>` URL under `userInfo.deepLink`; the tap handler in
 * `app/_layout.tsx` opens it through the OS, exactly like a real server push carrying a
 * deep link would.
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
  if (!isBeamableNotificationsSupported) return;
  BeamableNotifications.scheduleLocal({
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

/**
 * Pull a routable deep-link URL out of a notification's payload.
 *
 * The native SDK lifts the deep link onto `deeplink` (tolerant of `deeplink` / `deepLink`
 * / `deep_link` on the wire), so that's the primary source. We still fall back to scanning
 * `userInfo` for those same key spellings in case a raw payload carries it there. The
 * extracted value is normalized into a routable URL (a bare value like "123" becomes a
 * details URL) — see {@link normalizeDeepLink}.
 */
export function deepLinkFromNotification(n: {
  deeplink?: string;
  deepLink?: string;
  userInfo?: Record<string, unknown>;
}): string | null {
  const primary = n.deeplink ?? n.deepLink;
  if (typeof primary === 'string' && primary.length > 0) {
    return normalizeDeepLink(primary);
  }
  const info = n.userInfo ?? {};
  for (const key of ['deeplink', 'deepLink', 'deep_link']) {
    const value = info[key];
    if (typeof value === 'string' && value.length > 0) {
      return normalizeDeepLink(value);
    }
  }
  return null;
}
