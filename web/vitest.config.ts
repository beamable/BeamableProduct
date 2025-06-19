import { defineConfig } from 'vitest/config';
import tsconfigPaths from 'vite-tsconfig-paths';

export default defineConfig({
  plugins: [
    tsconfigPaths(), // reads the tsconfig.json "paths" and applies them
  ],
  test: {
    globals: true,
    environment: 'node',
  },
});
