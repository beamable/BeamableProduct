# Beam Web SDK

Beam Web SDK is a library built with TypeScript that can be used in Node.js, browser, and React Native environments.
It is distributed in multiple module formats (ESM, CommonJS, IIFE) and provides full TypeScript declarations.

## Table of Contents

- [Installation](#installation)
- [Using the SDK](#using-the-sdk)
- [Documentation](#documentation)
- [Contributing](CONTRIBUTION.md)
- [License](#license)

## Installation

Install the SDK into your project via npm (or pnpm):

```bash
# npm
npm install beamable-sdk
# pnpm
pnpm add beamable-sdk
```

## Using the SDK

You can use the Beam SDK across different JavaScript environments:

#### CommonJS (Node.js)

```js
const { Beam } = require('beamable-sdk');
const beam = await Beam.init({
  cid: 'YOUR_CUSTOMER_ID',
  pid: 'YOUR_PROJECT_ID',
});
console.log(beam.player.id);
```

#### ES Modules (Node.js)

```js
import { Beam } from 'beamable-sdk';
const beam = await Beam.init({
  cid: 'YOUR_CUSTOMER_ID',
  pid: 'YOUR_PROJECT_ID',
});
console.log(beam.player.id);
```

#### Browser (IIFE)

```html
<script src="https://unpkg.com/beamable-sdk"></script>
<script>
  // global variable exposed as Beamable
  const { Beam } = Beamable;
  const beam = await Beam.init({
    cid: 'YOUR_CUSTOMER_ID',
    pid: 'YOUR_PROJECT_ID',
  });
  console.log(beam.player.id);
</script>
```

#### React Native

The SDK ships a native `react-native` build target that persists tokens, config, and content
in [`@react-native-async-storage/async-storage`](https://react-native-async-storage.github.io/async-storage/).
Metro selects it automatically via the package `exports` `"react-native"` condition, so the
import is identical to every other platform:

```ts
// app entry (e.g. app/_layout.tsx) — once, before importing the SDK.
// Installs the URL polyfill Hermes lacks; no other browser globals are needed.
import '@beamable/sdk/react-native/polyfills';

import { Beam } from '@beamable/sdk';

// No explicit tokenStorage — Beam.init defaults to the AsyncStorage-backed store,
// which persists the session across app launches.
const beam = await Beam.init({
  cid: 'YOUR_CUSTOMER_ID',
  pid: 'YOUR_PROJECT_ID',
  gameEngine: 'react-native',
});
```

Install the peer dependencies alongside the SDK:

```bash
npm install @beamable/sdk @react-native-async-storage/async-storage react-native-url-polyfill
```

The RN build is compiled to ES2021, so Hermes parses it directly (no Babel static-block
transform required). For a full Expo/Metro setup — including the `withBeamableSdk` Metro
helper — see the samples under `nativeLibraries/Samples/`.

## Configuring the connection

`Beam.init` accepts `cid` and `pid` plus an optional target for the Beamable platform. You can
name a built-in environment or pass a `host` URL directly:

```ts
// Named environment ('prod' | 'stg' | 'dev'; defaults to 'prod')
await Beam.init({ cid: 'YOUR_CUSTOMER_ID', pid: 'YOUR_PROJECT_ID', environment: 'dev' });

// …or a host URL. A built-in URL (https://api.beamable.com, https://staging.api.beamable.com,
// https://dev.api.beamable.com) resolves to the matching environment; any other URL is treated
// as a custom host. `host` takes precedence over `environment`.
await Beam.init({ cid: 'YOUR_CUSTOMER_ID', pid: 'YOUR_PROJECT_ID', host: 'http://localhost:9000' });
```

`host` and `environment` are both optional; resolution precedence is **`host` → `environment` →
`prod`**. So for a manual setup you can just name an environment and skip the host entirely.

### Reuse the CLI's `.beamable/config.beam.json`

If you have run the Beamable CLI's `beam init`, it writes `.beamable/config.beam.json`
(`{ cid, pid, host }`). Because that shape matches what `Beam.init` accepts, the recommended
pattern is a tiny wrapper module that imports the JSON and re-exports it, then passes it to
`Beam.init` — no hand-written config, and one place to swap the source:

```ts
// src/beam/config.ts
import BEAM_CONFIG from '.beamable/config.beam.json'; // requires "resolveJsonModule": true
export { BEAM_CONFIG };

// where you initialize
import { BEAM_CONFIG } from './beam/config';
const beam = await Beam.init(BEAM_CONFIG);
// React Native adds the engine:
// const beam = await Beam.init({ ...BEAM_CONFIG, gameEngine: 'react-native' });
```

For manual setup, the JSON can omit `host` and use `{ "cid": "...", "pid": "...", "environment": "dev" }`
instead. And the plain `Beam.init({ cid, pid })` usage above keeps working unchanged — the JSON is
never required.

## Documentation

Find detailed API references, usage examples, and integration guides for the Beam Web SDK:

[Beam Web SDK Documentation](https://help.beamable.com/WebSDK-Latest/)

## Token storage and automatic refresh

The SDK stores authentication tokens using a platform-appropriate strategy and automatically handles access token renewal when needed.

### Automatic refresh

All HTTP requests that receive a 401 response will trigger an automatic access token refresh using the stored refresh token.
On successful refresh, the original request is retried with the new access token. If the refresh fails, stored tokens are cleared and the error is propagated.

#### Refresh failures

If the SDK fails to refresh the access token (for example, the refresh token has expired), it throws a `RefreshAccessTokenError` and stored tokens are cleared. Consumers should catch this error to start a new login flow:

```ts
import { RefreshAccessTokenError } from 'beamable-sdk';

try {
  await beam.someService.someMethod();
} catch (error) {
  if (error instanceof RefreshAccessTokenError) {
    // Refresh failed: redirect user to login
    redirectToLoginPage();
  }
}
```

### Custom token storage

If you need a different storage mechanism, implement the `TokenStorage` interface and pass it in the configuration:

```ts
import { TokenStorage } from 'beamable-sdk';

class MyTokenStorage implements TokenStorage {
  /* implement getTokenData, setTokenData, ... */
}

const beam = await Beam.init({
  cid: 'YOUR_CUSTOMER_ID',
  pid: 'YOUR_PROJECT_ID',
  tokenStorage: new MyTokenStorage(),
});
```

## License

This project is licensed under the MIT License.
