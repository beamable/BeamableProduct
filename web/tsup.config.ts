import { defineConfig } from 'tsup';
import path from 'path';

export default defineConfig({
  entry: ['src/index.ts'], // Entry file(s) for the library build
  splitting: false, // Disable code splitting (required for CJS and IIFE builds)
  sourcemap: true, // Generate source maps for debugging
  minify: true, // Minify the output for smaller bundle size
  clean: true, // Clean the output directory before each build
  dts: true, // Generate TypeScript declaration (.d.ts) files
  format: ['esm', 'cjs', 'iife'], // Output formats: ES modules, CommonJS, and browser-friendly IIFE
  globalName: 'Beam', // Global variable name for IIFE builds (window.Beam)
  esbuildOptions(options) {
    // Map the '@' prefix to the 'src' directory so that
    // imports like `import { foo } from '@/utils/foo'`
    // resolve correctly at build time (matches tsconfig paths).
    options.alias = {
      '@': path.resolve(__dirname, 'src'),
    };
  },
});
