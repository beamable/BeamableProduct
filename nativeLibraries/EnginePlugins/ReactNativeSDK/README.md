# @beamable/sdk-react-native

React Native adapter for the **Beamable Web SDK** (`@beamable/sdk`). The Web SDK ships
only `browser` and `node` builds (no `react-native` export condition), so running it in
RN otherwise means hand-rolling polyfills, a custom token store, and a Metro config. This
package bundles those behind one import each.

It ships **raw TypeScript** (like `@beamable/notifications-react-native`) — your app's
Metro/babel compile it; there's no build step.

## Install

```bash
npm install @beamable/sdk @beamable/sdk-react-native @react-native-async-storage/async-storage
```

## Use — three one-liners

**1. metro.config.js** — resolve the SDK's browser build + watch its source:

```js
const { getDefaultConfig } = require('expo/metro-config');
const { withBeamableSdk } = require('@beamable/sdk-react-native/metro');

module.exports = withBeamableSdk(getDefaultConfig(__dirname));
```

**2. App entry** (before any `@beamable/sdk` import — e.g. top of `app/_layout.tsx`):

```ts
import '@beamable/sdk-react-native/polyfills';
```

This installs `URL` / `localStorage` / `structuredClone` / `DOMException` /
`BroadcastChannel` and `fake-indexeddb`, in the order the SDK needs. On web it's a no-op.

**3. Token storage** — pass an AsyncStorage-backed `TokenStorage` to `Beam.init()`:

```ts
import { Beam, type TokenStorage } from '@beamable/sdk';
import { RNTokenStorage } from '@beamable/sdk-react-native';
import { hydrateLocalStorage } from '@beamable/sdk-react-native/polyfills';

await hydrateLocalStorage();                 // load the realm marker before Beam.init reads it
const tokenStorage = await RNTokenStorage.create(pid);
const beam = await Beam.init({ cid, pid, environment, tokenStorage: tokenStorage as unknown as TokenStorage, gameEngine: 'react-native' });
```

> `hydrateLocalStorage` is exported from `@beamable/sdk-react-native/polyfills`. Await it
> before `Beam.init()` so the SDK's `beam_cid`/`beam_pid` marker is readable synchronously
> and the SDK doesn't treat a cold start as a realm change (which would wipe tokens).

## The one thing this can't do for you

The SDK's browser build ships ES2022 **static class blocks** that Hermes/Metro don't
parse. Add this to your **babel.config.js** (a package can't edit your babel config):

```js
module.exports = function (api) {
  api.cache(true);
  return {
    presets: ['babel-preset-expo'],
    plugins: ['@babel/plugin-transform-class-static-block'],
  };
};
```

## Why an adapter (and not the SDK or the notifications plugin)?

- **The proper long-term fix** is a `react-native` export condition + an RN build inside
  `@beamable/sdk` itself — then Metro would "just work" and most of this would vanish. This
  adapter is the no-SDK-build-change version of that.
- The **notifications plugin** (`@beamable/notifications-react-native`) is about push
  notifications, not the SDK; token storage / polyfills don't belong there (this package is
  now the canonical home for `RNTokenStorage`).

## Exports

| Import | What |
|---|---|
| `@beamable/sdk-react-native` | `RNTokenStorage` |
| `@beamable/sdk-react-native/polyfills` | side-effecting shims + `hydrateLocalStorage()` |
| `@beamable/sdk-react-native/metro` | `withBeamableSdk(config, opts?)` |
