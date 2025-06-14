# Beam Web SDK

Beam Web SDK is a library built with TypeScript that can be used in both Node.js and browser environments.
It is distributed in multiple module formats (ESM, CommonJS, IIFE) and provides full TypeScript declarations.

## Table of Contents

- [Installation](#installation)
- [Using the SDK](#using-the-sdk)
- [Documentation](#documentation)
- [Releasing & Versioning](#releasing--versioning)
- [Contributing](#contributing)
- [License](#license)

## Installation

Install the SDK into your project via npm (or pnpm):

```bash
# npm
npm install @beamable/sdk
# pnpm
pnpm add @beamable/sdk
```

## Using the SDK

You can use the Beam SDK across different JavaScript environments:

#### CommonJS (Node.js)

```js
const { Beam } = require('@beamable/sdk');
const beam = new Beam({
  cid: 'YOUR_CUSTOMER_ID',
  pid: 'YOUR_PROJECT_ID',
});
await beam.ready();
console.log(beam.toString());
console.log(beam.player.id);
```

#### ES Modules (Node.js)

```js
import { Beam } from '@beamable/sdk';
const beam = new Beam({
  cid: 'YOUR_CUSTOMER_ID',
  pid: 'YOUR_PROJECT_ID',
});
await beam.ready();
console.log(beam.toString());
console.log(beam.player.id);
```

#### Browser (IIFE)

```html
<script src="https://unpkg.com/@beamable/sdk"></script>
<script>
  // global variable exposed as Beamable
  const { Beam, BeamEnvironment } = Beamable;
  const beam = new Beam({
    cid: 'YOUR_CUSTOMER_ID',
    pid: 'YOUR_PROJECT_ID',
  });
  await beam.ready();
  console.log(beam.toString());
  console.log(beam.player.id);
</script>
```

## Documentation

Find detailed API references, usage examples, and integration guides for the Beam Web SDK:

[Beam Web SDK Documentation](https://docs.beamable.com/docs/beamable-overview)

## Releasing & Versioning

We use [changesets](https://github.com/changesets/changesets) for versioning. To create a changeset, run:

```bash
pnpm changeset
```

When we are ready to release, update the version and build:

```bash
pnpm release # This updates the version and build the project
pnpm publish --access public
```

## Contributing

Contributions are welcome! This project is configured with:

- `pnpm` as the package manager.

- TypeScript for compile-time type safety.

- `tsup` for bundling the code into multiple formats:

  - esm
  - cjs
  - iife (for CDN usage)

- Vitest as the test runner.

- ESLint/Prettier for maintaining code quality and style.

- Changesets for versioning and changelog management.

- TypeDoc for SDK documentation.

### Getting Started

1. Fork the repository.

2. Run the setup script (recommended):

   ```bash
   cd web
   npm run setup-project  # Checks node/pnpm versions and installs dependencies
   ```

   If you prefer manual setup, ensure you are using node version >= `22.14.0` and pnpm version `10.8.0`

   ```bash
   cd web
   npm i -g pnpm@10.8.0 # If you don't have pnpm installed
   pnpm install
   ```

3. Create a new feature branch.

4. Make your changes and commit with clear messages.

5. Open a pull request with a description of your changes.

6. Before submitting a PR, make sure to format, lint, and test:
   ```bash
   pnpm format   # Format code
   pnpm lint     # Run linter
   pnpm test     # Run test suite
   pnpm dev      # Run development build
   ```

### Testing SDK Builds Locally

To test the local SDK builds across different module formats by creating temporary files in a `temp` folder and importing from the `dist` directory.
This ensures the bundle works correctly in Node.js (CommonJS, ESM) and in the browser (IIFE).

#### CommonJS (Node.js)

Create a file `temp/test-cjs.js`:

```js
const { Beam } = require('./dist/index.js');
const beam = new Beam({
  cid: 'YOUR_CUSTOMER_ID',
  pid: 'YOUR_PROJECT_ID',
});
await beam.ready();
console.log(beam.toString());
console.log(beam.player.id);
```

Run:

```bash
node test-cjs.js
```

#### ES Modules (Node.js)

Create a file `temp/test-esm.mjs`:

```js
import { Beam } from './dist/index.mjs';
const beam = new Beam({
  cid: 'YOUR_CUSTOMER_ID',
  pid: 'YOUR_PROJECT_ID',
});
await beam.ready();
console.log(beam.toString());
console.log(beam.player.id);
```

Run:

```bash
node test-esm.mjs
```

#### Browser (IIFE)

Create an HTML file `temp/test-iife.html`:

```html
<!DOCTYPE html>
<html lang="en">
  <head>
    <meta charset="utf-8" />
    <title>Beam SDK Test</title>
  </head>
  <body>
    <!-- Load the IIFE bundle; exposes a global `Beam` symbol -->
    <script src="../dist/index.global.js"></script>

    <script>
      // Instantiate the SDK via the `Beam` constructor
      const { Beam, BeamEnvironment } = Beamable;
      const beam = new Beam({
        cid: 'YOUR_CUSTOMER_ID',
        pid: 'YOUR_PROJECT_ID',
        // Environment must be one of: 'Dev', 'Stg', or 'Prod'
        environment: 'Prod',
      });
      await beam.ready();

      // Display a human-readable configuration string:
      console.log(beam.toString());

      // List all built-in environments:
      console.log(BeamEnvironment.list());

      // Display current player id:
      console.log(beam.player.id);
    </script>
  </body>
</html>
```

Open the file in your browser and watch the console for output.

## Token storage and automatic refresh

The SDK stores authentication tokens using a platform-appropriate strategy and automatically handles access token renewal when needed.

### Automatic refresh

All HTTP requests that receive a 401 response will trigger an automatic access token refresh using the stored refresh token.
On successful refresh, the original request is retried with the new access token. If the refresh fails, stored tokens are cleared and the error is propagated.

#### Refresh failures

If the SDK fails to refresh the access token (for example, the refresh token has expired), it throws a `RefreshAccessTokenError` and stored tokens are cleared. Consumers should catch this error to start a new login flow:

```ts
import { RefreshAccessTokenError } from '@beamable/sdk';

try {
  await beam.api.someService.someEndpoint();
} catch (error) {
  if (error instanceof RefreshAccessTokenError) {
    // Refresh failed: redirect user to login
    redirectToLoginPage();
  }
  throw error;
}
```

### Custom token storage

If you need a different storage mechanism, implement the `TokenStorage` interface and pass it in the configuration:

```ts
import { TokenStorage } from '@beamable/sdk';

class MyTokenStorage implements TokenStorage {
  /* implement getAccessToken, setAccessToken, ... */
}

const beam = new Beam({
  cid: 'YOUR_CUSTOMER_ID',
  pid: 'YOUR_PROJECT_ID',
  tokenStorage: new MyTokenStorage(),
});
```

## License

This project is licensed under the MIT License.
