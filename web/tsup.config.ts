import { defineConfig } from 'tsup';
import path from 'path';

// Map the '@' prefix to the 'src' directory so that
// imports like `import { foo } from '@/utils/foo'`
// resolve correctly at build time (matches tsconfig paths).
const alias = { '@': path.resolve(__dirname, 'src') };

const config = {
  splitting: false, // Disable code splitting (required for CJS and IIFE builds)
  sourcemap: false, // Generate source maps for debugging
  minify: true, // Minify the output for smaller bundle size
  metafile: false, // Generate a metafile for bundle analysis
};

export default defineConfig([
  // CJS Node Build
  {
    ...config,
    entry: ['src/index.ts'], // Entry files for the library build
    format: 'cjs', // Output formats: CommonJS
    outDir: 'dist/node', // Output directory for the build
    platform: 'node', // Target node environment for bundling
    clean: true, // Clean the output directory before building
    dts: true, // Generate TypeScript declaration (.d.ts) files
    esbuildOptions(options) {
      options.alias = alias;
    },
  },
  // ESM Node Build
  {
    ...config,
    entry: ['src/index.ts'], // Entry files for the library build
    format: 'esm', // Output formats: ES modules
    outDir: 'dist/node', // Output directory for the build
    platform: 'node', // Target node environment for bundling
    esbuildOptions(options) {
      options.alias = alias;
    },
  },
  // ESM Browser Build
  {
    ...config,
    entry: ['src/index.browser.ts'], // Entry files for the library build
    format: 'esm', // Output formats: ES modules
    outDir: 'dist/browser', // Output directory for the build
    platform: 'browser', // Target browser environment for bundling
    esbuildOptions(options) {
      options.alias = {
        ...alias,
        '@/index': path.resolve(__dirname, 'src/index.browser.ts'),
      };
    },
  },
  // IIFE Browser Build
  {
    ...config,
    entry: ['src/index.browser.ts'], // Entry files for the library build
    format: 'iife', // Output formats: browser-friendly IIFE
    outDir: 'dist/browser', // Output directory for the build
    platform: 'browser', // Target browser environment for bundling
    globalName: 'Beamable', // Global variable name for IIFE builds (window.Beamable)
    dts: true, // Generate TypeScript declaration (.d.ts) files
    esbuildOptions(options) {
      options.alias = {
        ...alias,
        '@/index': path.resolve(__dirname, 'src/index.browser.ts'),
      };
    },
  },
  // Common Build (CJS and ESM)
  {
    ...config,
    entry: ['src/schema.ts'], // Entry files for the library build
    format: ['cjs', 'esm'], // Output formats: CommonJS
    outDir: 'dist', // Output directory for the build
    dts: true, // Generate TypeScript declaration (.d.ts) files
    esbuildOptions(options) {
      options.alias = alias;
    },
  },
]);
