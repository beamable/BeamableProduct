/**
 * React Native polyfills for the Beamable Web SDK. Import this ONCE, before any
 * `@beamable/sdk` import (e.g. at the very top of your app entry / `_layout.tsx`):
 *
 *   import '@beamable/sdk/react-native/polyfills';
 *
 * The React Native build of the SDK stores tokens, config, and content in
 * AsyncStorage, so no localStorage / IndexedDB / BroadcastChannel / structuredClone
 * shims are needed. The only browser global the SDK's runtime relies on is
 * `URL`/`URLSearchParams`, which Hermes does not fully implement — this installs it.
 */
/// <reference path="./modules.d.ts" />
import 'react-native-url-polyfill/auto';
