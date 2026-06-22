import * as Linking from 'expo-linking';

/** Must match `scheme` in app.json. */
export const SCHEME = 'beamrnsample';

/**
 * In-app router path used for navigation and for notification deep-link data.
 * expo-router maps the URL `beamrnsample://details/<id>` to this path.
 */
export function detailsPath(id: string | number): string {
  return `/details/${id}`;
}

/** Full deep-link URL, e.g. `beamrnsample://details/42`. */
export function detailsUrl(id: string | number): string {
  return Linking.createURL(`details/${id}`);
}

/**
 * Turn a notification's raw deep-link value into a routable URL.
 *
 * Local notifications (and rich server pushes) carry a full URL like
 * `beamrnsample://details/42`, which we open as-is. But a remote push may carry a *bare*
 * value — e.g. Beamable's back-office sends `deeplink: "123"` — which has no scheme and
 * can't be opened directly. We treat such a value as a details id and build the URL for
 * it, so remote deep links route to the same screen as local ones.
 *
 * Returns null for empty input.
 */
export function normalizeDeepLink(raw: string | null | undefined): string | null {
  if (typeof raw !== 'string') return null;
  const value = raw.trim();
  if (value.length === 0) return null;
  // Already a URL (has a scheme like `beamrnsample://` or `https://`) → open as-is.
  if (/^[a-z][a-z0-9+.-]*:\/\//i.test(value)) return value;
  // Bare value (e.g. "123") → route it to the details screen.
  return detailsUrl(value);
}

/**
 * Opens a URL through the OS, exactly like an external app / browser / push
 * service would. Used by the "Simulate deep link" button so you can verify the
 * full scheme -> route mapping without leaving the app.
 */
export async function openUrl(url: string): Promise<void> {
  await Linking.openURL(url);
}
