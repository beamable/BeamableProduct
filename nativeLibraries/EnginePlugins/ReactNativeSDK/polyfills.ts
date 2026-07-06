/**
 * React Native polyfills for the Beamable Web SDK. Import this ONCE, before any
 * `@beamable/sdk` import (e.g. at the very top of your app entry / `_layout.tsx`):
 *
 *   import '@beamable/sdk-react-native/polyfills';
 *
 * The `export … from` runs `installShims` (URL / localStorage / structuredClone /
 * DOMException / BroadcastChannel) FIRST; only then does `fake-indexeddb/auto` run —
 * it depends on DOMException + structuredClone already existing. Metro resolves the
 * `.web.ts` sibling for web builds, where all of this is a no-op.
 */
export { hydrateLocalStorage } from './src/installShims';
import 'fake-indexeddb/auto';
