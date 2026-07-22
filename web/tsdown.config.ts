import { defineConfig } from 'tsdown';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

// Map the '@' prefix to the 'src' directory so that
// imports like `import { foo } from '@/utils/foo'`
// resolve correctly at build time (matches tsconfig paths).
const alias = { '@': path.resolve(__dirname, 'src') };
const nodeDefaultsEntry = path.resolve(__dirname, 'src/defaults.ts');
const browserDefaultsEntry = path.resolve(__dirname, 'src/defaults.browser.ts');
const reactNativeDefaultsEntry = path.resolve(
  __dirname,
  'src/defaults.reactnative.ts',
);
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
// React Native uses the AsyncStorage-backed defaults and the browser createHash
// stub (RN lacks node `crypto`; the client never signs requests).
const reactNativeAlias = {
  '@/defaults': reactNativeDefaultsEntry,
  '@/utils/createHash': browserCreateHashEntry,
  [nodeDefaultsEntry]: reactNativeDefaultsEntry,
  [nodeCreateHashEntry]: browserCreateHashEntry,
  ...alias,
};
const nodeBuiltins = ['fs', 'os', 'path', 'crypto'];
// Peer-provided native modules that must stay as bare imports in the RN build
// (never bundled — AsyncStorage is autolinked, the URL polyfill is app-supplied).
const reactNativeExternals = [
  '@react-native-async-storage/async-storage',
  'react-native-url-polyfill',
  'react-native-url-polyfill/auto',
];

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
  // Object-form entry so output basenames stay flat (dist/node/{index,resolveBeamConfig}.{cjs,mjs})
  // regardless of each source file's subdirectory. `resolveBeamConfig` is a Node-only,
  // build-time helper (uses node:fs) shipped solely on the node target — never browser/RN.
  withSharedConfig({
    entry: {
      index: 'src/index.ts',
      resolveBeamConfig: 'src/platform/resolveBeamConfig.ts',
    }, // Entry files for node build
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
  // React Native Build
  // Native target: AsyncStorage-backed storage (via reactNativeAlias), ESM only,
  // ES2021 to lower ES2022 static blocks for Hermes, and AsyncStorage / the URL
  // polyfill kept external so Metro/autolinking resolve them from the app.
  withSharedConfig({
    // Object form so output basenames are flat (dist/react-native/{index,api,polyfills}.mjs)
    // regardless of each source file's subdirectory.
    entry: {
      index: 'src/index.ts',
      api: 'src/api.ts',
      polyfills: 'src/react-native/polyfills.ts',
    }, // Entry files for the react-native build
    format: ['esm'], // Output format: ES modules (Metro consumes ESM)
    outDir: 'dist/react-native', // Output directory for the build
    platform: 'neutral', // Neither node nor browser globals injected
    target: 'es2021', // Lower ES2022 static blocks/fields for Hermes
    dts: false, // Types are platform-agnostic; the RN condition reuses dist/types
    clean: true, // Clean the react-native output directory before building
    external: reactNativeExternals, // Keep native/peer modules as bare imports
    alias: reactNativeAlias,
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
  // Types for the Node-only resolver (object-form entry keeps the basename flat:
  // dist/types/resolveBeamConfig.d.ts, matching the `./node` export condition).
  {
    entry: { resolveBeamConfig: 'src/platform/resolveBeamConfig.ts' },
    outDir: 'dist/types',
    clean: false,
    dts: {
      emitDtsOnly: true,
    },
    outExtensions: () => ({
      dts: '.d.ts',
    }),
  },
]);
