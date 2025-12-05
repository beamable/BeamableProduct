# Contributing

Contributions are welcome! This project is configured with:

- `pnpm` as the package manager.

- TypeScript for compile-time type safety.

- `tsdown` for bundling the code into multiple formats:
  - esm
  - cjs
  - iife (for CDN usage)

- Vitest as the test runner.

- ESLint/Prettier for maintaining code quality and style.

- TypeDoc for SDK documentation.

## Getting Started

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

## Testing SDK Builds Locally

To test the local SDK builds across different module formats by creating temporary files in a `temp` folder and importing from the `dist` directory.
This ensures the bundle works correctly in Node.js (CommonJS, ESM) and in the browser (IIFE).

### CommonJS (Node.js)

Create a file `temp/test-cjs.js`:

```js
const { Beam } = require('./dist/index.js');
const beam = await Beam.init({
  cid: 'YOUR_CUSTOMER_ID',
  pid: 'YOUR_PROJECT_ID',
});
console.log(beam.player.id);
```

Run:

```bash
node test-cjs.js
```

### ES Modules (Node.js)

Create a file `temp/test-esm.mjs`:

```js
import { Beam } from './dist/index.mjs';
const beam = await Beam.init({
  cid: 'YOUR_CUSTOMER_ID',
  pid: 'YOUR_PROJECT_ID',
});
console.log(beam.player.id);
```

Run:

```bash
node test-esm.mjs
```

### Browser (IIFE)

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
      const beam = await Beam.init({
        cid: 'YOUR_CUSTOMER_ID',
        pid: 'YOUR_PROJECT_ID',
        // Environment must be one of: 'Dev', 'Stg', or 'Prod'
        environment: 'Prod',
      });

      // List all built-in environments:
      console.log(BeamEnvironment.list());

      // Display current player id:
      console.log(beam.player.id);
    </script>
  </body>
</html>
```

Open the file in your browser and watch the console for output.

## Releasing & Versioning

Please ensure that all notable changes are clearly documented in the `CHANGELOG.md` file before releasing.

When we are ready to release, update the version and build:

```bash
pnpm release # This updates the version and build the project
pnpm publish --access public
```
