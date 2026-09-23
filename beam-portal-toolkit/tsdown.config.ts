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
    // `@vitejs/plugin-react` MUST stay external. src/vite.ts imports it dynamically
    // precisely so the toolkit ships without hard-depending on it; bundling it inlines
    // its napi loader, which then looks for `@rolldown/binding-*` — a package the
    // published tarball declares no dependency on, so every extension build fails at
    // config load with "Cannot find native binding". Extensions carry their own
    // @vitejs/plugin-react devDep, which is where this import should resolve from.
    external: ['@beamable/sdk', 'react', 'react-dom', '@vitejs/plugin-react'],
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
