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
const Beam = require('@beamable/sdk');
console.log(Beam.print());
```

#### ES Modules (Node.js)

```js
import { print } from '@beamable/sdk';
console.log(print());
```

#### Browser (IIFE)

```html
<script src="https://unpkg.com/@beamable/sdk"></script>
<script>
  // global variable exposed as Beam
  console.log(Beam.print());
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
const Beam = require('./dist/index.js');
console.log(Beam.print());
```

Run:

```bash
node test-cjs.js
```

#### ES Modules (Node.js)

Create a file `temp/test-esm.mjs`:

```js
import { print } from './dist/index.mjs';
console.log(print());
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
    <script src="./dist/index.global.js"></script>
    <script>
      // global variable exposed as Beam
      console.log(Beam.print());
    </script>
  </body>
</html>
```

Open the file in your browser and check the console output.

## License

This project is licensed under the MIT License.
