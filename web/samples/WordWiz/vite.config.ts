import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';
import { defineConfig } from 'vite';
import tsconfigPaths from 'vite-tsconfig-paths';
import react from '@vitejs/plugin-react-swc';
import mkcert from 'vite-plugin-mkcert';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const projectRoot = resolve(__dirname, '..', '..');
const sdkSource = resolve(projectRoot, 'src');
const sampleSource = resolve(__dirname, 'src');

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => ({
  base: mode === 'production' ? './' : '/',
  css: {
    preprocessorOptions: {
      scss: {
        api: 'modern',
      },
    },
  },
  plugins: [
    // Allows using React dev server along with building a React application with Vite.
    // https://npmjs.com/package/@vitejs/plugin-react-swc
    react(),
    // Allows using the compilerOptions.paths property in tsconfig.json.
    // https://www.npmjs.com/package/vite-tsconfig-paths
    tsconfigPaths(),
    // Creates a custom SSL certificate valid for the local machine.
    // Using this plugin requires admin rights on the first dev-mode launch.
    // https://www.npmjs.com/package/vite-plugin-mkcert
    process.env.HTTPS && mkcert(),
  ],
  build: {
    target: 'esnext',
  },
  publicDir: './public',
  resolve: {
    alias: {
      'beamable-sdk': resolve(sdkSource, 'index.ts'),
      '@/defaults': resolve(sdkSource, 'defaults.browser.ts'),
      '@/utils/createHash': resolve(sdkSource, 'utils/createHashStub.ts'),
      '@': sdkSource,
      '@app': sampleSource,
    },
  },
  server: {
    // Exposes your dev server and makes it accessible for the devices in the same network.
    host: true,
    fs: {
      allow: [projectRoot],
    },
  },
}));
