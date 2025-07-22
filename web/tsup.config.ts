import { defineConfig } from 'tsup';
import path from 'path';

// Map the '@' prefix to the 'src' directory so that
// imports like `import { foo } from '@/utils/foo'`
// resolve correctly at build time (matches tsconfig paths).
const alias = { '@': path.resolve(__dirname, 'src') };
const browserAlias = {
  ...alias,
  // Browser-only replacements
  '@/defaults': path.resolve(__dirname, 'src/defaults.browser.ts'),
  '@/utils/createHash': path.resolve(__dirname, 'src/utils/createHashStub.ts'),
};

const config = {
  splitting: false, // Disable code splitting (required for CJS and IIFE builds)
  sourcemap: false, // Generate source maps for debugging
  minify: true, // Minify the output for smaller bundle size
  metafile: false, // Generate a metafile for bundle analysis
};

export default defineConfig([
  // Node Build
  {
    ...config,
    entry: ['src/index.ts'], // Entry files for the library build
    format: ['cjs', 'esm'], // Output formats: CommonJS and ES modules
    outDir: 'dist/node', // Output directory for the build
    platform: 'node', // Target node environment for bundling
    clean: true, // Clean the output directory before building
    esbuildOptions(options) {
      options.alias = alias;
    },
  },
  // Browser Build
  {
    ...config,
    entry: ['src/index.ts'], // Entry files for the library build
    format: ['esm', 'iife'], // Output formats: ES modules and IIFE
    outDir: 'dist/browser', // Output directory for the build
    platform: 'browser', // Target browser environment for bundling
    globalName: 'Beamable', // Global variable name for IIFE build (window.Beamable)
    clean: true, // Clean the output directory before building
    esbuildOptions(options) {
      options.alias = browserAlias;
    },
  },
  // Api and Schema Build (CJS and ESM)
  {
    ...config,
    entry: ['src/api.ts', 'src/schema.ts'], // Entry files for the library build
    format: ['cjs', 'esm'], // Output formats: CommonJS and ES modules
    outDir: 'dist', // Output directory for the build
    clean: true, // Clean the output directory before building
    esbuildOptions(options) {
      options.alias = alias;
    },
  },
  // Api Build (IIFE)
  {
    ...config,
    entry: ['src/api.ts'], // Entry files for the library build
    format: 'iife', // Output formats: IIFE
    outDir: 'dist', // Output directory for the build
    globalName: 'BeamableApi', // Global variable name for IIFE build (window.BeamableApi)
    clean: false, // Do not clean, as this is part of a multi-format build
    esbuildOptions(options) {
      options.alias = alias;
    },
  },
  // Type declarations
  {
    entry: ['src/index.ts'], // Entry files for the library build
    outDir: 'dist/types', // Output directory for the types
    dts: { only: true }, // Generate TypeScript declaration files
    clean: true, // Clean the output directory before building
  },
  {
    entry: ['src/api.ts'], // Entry files for the library build
    outDir: 'dist/types', // Output directory for the types
    dts: { only: true }, // Generate TypeScript declaration files
  },
  {
    entry: ['src/schema.ts'], // Entry files for the library build
    outDir: 'dist/types', // Output directory for the types
    dts: { only: true }, // Generate TypeScript declaration files
  },
]);
