/**
 * Web build of `@beamable/sdk-react-native/polyfills` (Metro resolves `.web.ts`
 * when bundling for web). A browser already provides URL / localStorage /
 * structuredClone / IndexedDB / BroadcastChannel, so there is nothing to install.
 */
export async function hydrateLocalStorage(): Promise<void> {
  // No-op on web — the browser provides real localStorage.
}
