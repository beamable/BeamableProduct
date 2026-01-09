# Beam Web SDK

Beam Web SDK is a library built with TypeScript that can be used in both Node.js and browser environments.
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
