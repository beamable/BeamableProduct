/**
 * Vite plugin for Beamable portal extensions.
 *
 * Configures the build so that `@beamable/sdk` is treated as an external and
 * resolved to the versioned window global that the Portal injects before
 * running the extension script.
 *
 * Usage in vite.config.js / vite.config.ts:
 *
 *   import { portalExtensionPlugin } from '@beamable/portal-toolkit/vite'
 *
 *   export default defineConfig({
 *     plugins: [portalExtensionPlugin()],
 *     build: {
 *       lib: {
 *         entry: 'src/index.js',
 *         name: 'MyExtensionName',   // must match manifest.Name exactly
 *         formats: ['iife'],
 *         fileName: () => 'index.js',
 *       },
 *     },
 *   })
 *
 * Note: only the main `@beamable/sdk` entry is externalized. The subpath
 * `@beamable/sdk/api` has no browser IIFE build and is stateless (no shared
 * connection or auth state), so it is safe — and correct — to bundle it
 * directly into the extension. Do not add it to externals/globals.
 */

import pkg from '../package.json'

// Versioned global name the Portal registers on window before the extension
// script runs. Must match the `globalName` in the Portal's beam-sdk-registry.
const SDK_GLOBAL = `@beamable/sdk-${pkg.peerDependencies['@beamable/sdk']}`

/**
 * Returns a Vite plugin that marks `@beamable/sdk` as external and maps it
 * to the Portal-provided window global at runtime.
 */
export function portalExtensionPlugin() {
  return {
    name: 'beamable-portal-extension',
    config() {
      return {
        build: {
          rollupOptions: {
            external: ['@beamable/sdk'],
            output: {
              globals: {
                '@beamable/sdk': `window['${SDK_GLOBAL}']`,
              },
            },
          },
        },
      }
    },
  }
}
