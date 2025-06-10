import { defineConfig } from 'tsup';
import path from 'path';

// Map the '@' prefix to the 'src' directory so that
// imports like `import { foo } from '@/utils/foo'`
// resolve correctly at build time (matches tsconfig paths).
const alias = { '@': path.resolve(__dirname, 'src') };

export default defineConfig({
  entry: ['src/index.ts'], // Entry files for the library build
  format: ['esm', 'cjs', 'iife'], // Output formats: ES modules, CommonJS and browser-friendly IIFE
  globalName: 'Beamable', // Global variable name for IIFE builds (window.Beamable)
  outDir: 'dist', // Output directory for the build
  splitting: false, // Disable code splitting (required for CJS and IIFE builds)
  sourcemap: false, // Generate source maps for debugging
  minify: true, // Minify the output for smaller bundle size
  clean: true, // Clean the output directory before each build
  dts: true, // Generate TypeScript declaration (.d.ts) files
  metafile: false, // Generate a metafile for bundle analysis
  esbuildOptions(options) {
    options.alias = alias;
  },
});
