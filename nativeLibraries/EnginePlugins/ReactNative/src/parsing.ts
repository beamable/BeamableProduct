/**
 * Pure, app-agnostic parsers for notification payloads. No native imports and no
 * app-specific linking — safe to call on any platform. Exposed on the
 * `BeamNotifications` façade and as flat named exports. Params are plain structural
 * shapes (not `Pick<NotificationData>`) so a full `NotificationData` — which carries an
 * index signature — assigns cleanly.
 */

/**
 * Pull a routable deep-link URL out of a notification payload. Returns the raw value
 * as-is (a full `<scheme>://…` URL): the native SDK already completes schemeless
 * remote deep links, and local notifications should carry a full URL. Reads the
 * canonical `deeplink`, tolerating `deepLink` and a `userInfo` fallback. Returns null
 * when no deep link is present.
 */
export function deepLinkFromNotification(n: {
  deeplink?: string;
  deepLink?: string;
  userInfo?: Record<string, unknown>;
}): string | null {
  const primary = n.deeplink ?? n.deepLink;
  if (typeof primary === 'string' && primary.length > 0) return primary;
  const info = n.userInfo ?? {};
  for (const key of ['deeplink', 'deepLink', 'deep_link']) {
    const value = info[key];
    if (typeof value === 'string' && value.length > 0) return value;
  }
  return null;
}

/**
 * Pull campaign coordinates (campaignId / nodeId) out of a notification payload. The
 * native SDK lifts these onto top-level fields for tracked campaign pushes; falls back
 * to `userInfo`. Returns only the keys actually present.
 */
export function campaignCoordsFromNotification(n: {
  campaignId?: string;
  nodeId?: string;
  userInfo?: Record<string, unknown>;
}): { campaignId?: string; nodeId?: string } {
  const out: { campaignId?: string; nodeId?: string } = {};
  const info = n.userInfo ?? {};
  const campaignId = n.campaignId ?? (info.campaignId as string | undefined);
  const nodeId = n.nodeId ?? (info.nodeId as string | undefined);
  if (typeof campaignId === 'string' && campaignId.length > 0) out.campaignId = campaignId;
  if (typeof nodeId === 'string' && nodeId.length > 0) out.nodeId = nodeId;
  return out;
}
