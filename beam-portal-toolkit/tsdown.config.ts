import { defineConfig } from 'tsdown';

export default defineConfig([
  // CJS + ESM build
  {
    entry: ['src/index.ts'],
    format: ['cjs', 'esm'],
    outDir: 'dist',
    clean: true,
    dts: false,
    sourcemap: false,
    minify: true,
    external: ['beamable-sdk'],
  },
  // Type declarations
  {
    entry: ['src/index.ts'],
    outDir: 'dist/types',
    clean: false,
    dts: {
      emitDtsOnly: true,
    },
    outExtensions: () => ({
      dts: '.d.ts',
    }),
    external: ['beamable-sdk', 'svelte'],
  },
  // Svelte element type augmentations
  {
    entry: ['src/generated/svelte-elements.ts'],
    outDir: 'dist/types',
    clean: false,
    dts: {
      emitDtsOnly: true,
    },
    outExtensions: () => ({
      dts: '.d.ts',
    }),
    external: ['svelte'],
  },
]);
