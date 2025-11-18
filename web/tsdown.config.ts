import { defineConfig } from 'tsdown';
import path from 'path';

// Map the '@' prefix to the 'src' directory so that
// imports like `import { foo } from '@/utils/foo'`
// resolve correctly at build time (matches tsconfig paths).
const alias = { '@': path.resolve(__dirname, 'src') };
const nodeDefaultsEntry = path.resolve(__dirname, 'src/defaults.ts');
const browserDefaultsEntry = path.resolve(__dirname, 'src/defaults.browser.ts');
const nodeCreateHashEntry = path.resolve(__dirname, 'src/utils/createHash.ts');
const browserCreateHashEntry = path.resolve(
  __dirname,
  'src/utils/createHashStub.ts',
);
const browserAlias = {
  // Browser-only replacements
  '@/defaults': browserDefaultsEntry,
  '@/utils/createHash': browserCreateHashEntry,
  [nodeDefaultsEntry]: browserDefaultsEntry,
  [nodeCreateHashEntry]: browserCreateHashEntry,
  ...alias,
};
const nodeBuiltins = ['fs', 'os', 'path', 'crypto'];

const baseConfig = {
  sourcemap: false, // Generate source maps for debugging
  minify: true, // Minify the output for smaller bundle size
  metafile: false, // Generate a metafile for bundle analysis
  nodeProtocol: 'strip' as const, // Strip node protocol
};

const withSharedConfig = (overrides: Record<string, unknown>) => ({
  ...baseConfig,
  ...overrides,
});

const typeEntries = ['src/index.ts', 'src/api.ts', 'src/schema.ts'];

export default defineConfig([
  // Node Build
  withSharedConfig({
    entry: ['src/index.ts'], // Entry files for node build
    format: ['cjs', 'esm'], // Output formats: CommonJS and ES modules
    outDir: 'dist/node', // Output directory for the build
    platform: 'node', // Target node environment for bundling
    dts: false, // Do not generate TypeScript declaration files for node build
    clean: true, // Clean the output directory before building
    external: nodeBuiltins, // External dependencies for node build
    alias,
  }),
  // Browser Build
  withSharedConfig({
    entry: ['src/index.ts'], // Entry files for browser build
    format: ['esm', 'iife'], // Output formats: ES modules and IIFE
    outDir: 'dist/browser', // Output directory for the build
    platform: 'browser', // Target browser environment for bundling
    globalName: 'Beamable', // Global variable name for IIFE build (window.Beamable)
    dts: false, // Do not generate TypeScript declaration files for browser build
    clean: true, // Clean the output directory before building
    alias: browserAlias,
  }),
  // Api and Schema Build (CJS and ESM)
  withSharedConfig({
    entry: ['src/api.ts', 'src/schema.ts'], // Entry files for the api and schema build
    format: ['cjs', 'esm'], // Output formats: CommonJS and ES modules
    outDir: 'dist', // Output directory for the build
    dts: false, // Do not generate TypeScript declaration files for api and schema build
    clean: true, // Clean the output directory before building
    external: nodeBuiltins, // External dependencies for api and schema build
    alias,
  }),
  // Api Build (IIFE)
  withSharedConfig({
    entry: ['src/api.ts'], // Entry files for api iife build
    format: 'iife', // Output formats: IIFE
    outDir: 'dist', // Output directory for the build
    platform: 'browser', // Target browser environment for bundling
    globalName: 'BeamableApi', // Global variable name for IIFE build (window.BeamableApi)
    dts: false, // Do not generate TypeScript declaration files for IIFE build
    clean: false, // Do not clean, as this is part of a multi-format build
    alias: browserAlias,
  }),
  // Type declarations (first cleans, rest re-use existing artifacts)
  ...typeEntries.map((entry, index) => ({
    entry: [entry], // Entry files for the library build
    outDir: 'dist/types', // Output directory for the types
    clean: index === 0, // Clean only before the first type build
    dts: {
      emitDtsOnly: true, // Emit only TypeScript declaration files
    },
    outExtensions: () => ({
      dts: '.d.ts',
    }),
  })),
]);
