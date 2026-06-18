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
 * Opens a URL through the OS, exactly like an external app / browser / push
 * service would. Used by the "Simulate deep link" button so you can verify the
 * full scheme -> route mapping without leaving the app.
 */
export async function openUrl(url: string): Promise<void> {
  await Linking.openURL(url);
}
