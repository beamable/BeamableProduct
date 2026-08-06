import { defineConfig } from 'tsdown';

export default defineConfig([
  // CJS + ESM build — runtime + build-tool entry points
  {
    entry: ['src/index.ts', 'src/vite.ts', 'src/rollup.ts', 'src/react.ts'],
    format: ['cjs', 'esm'],
    outDir: 'dist',
    clean: true,
    dts: false,
    sourcemap: false,
    minify: true,
    external: ['@beamable/sdk', 'react', 'react-dom'],
  },
  // Type declarations
  {
    entry: ['src/index.ts', 'src/vite.ts', 'src/rollup.ts', 'src/react.ts'],
    outDir: 'dist/types',
    clean: false,
    dts: {
      emitDtsOnly: true,
    },
    outExtensions: () => ({
      dts: '.d.ts',
    }),
    external: ['@beamable/sdk', 'svelte', 'react', 'react-dom'],
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
  // React JSX element type augmentations
  {
    entry: ['src/generated/react-elements.ts'],
    outDir: 'dist/types',
    clean: false,
    dts: {
      emitDtsOnly: true,
    },
    outExtensions: () => ({
      dts: '.d.ts',
    }),
    external: ['react'],
  },
]);
