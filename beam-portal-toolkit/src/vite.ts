/**
 * Vite plugin for Beamable portal extensions.
 *
 * Configures the build so that `@beamable/sdk` and `@beamable/sdk/api` are
 * treated as externals and resolved to the versioned window globals that the
 * Portal injects before running the extension script.
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
 */

import pkg from '../package.json'

// Versioned global names the Portal registers on window before the extension
// script runs. Must match the `globalName` in the Portal's beam-sdk-registry.
const SDK_VERSION = pkg.peerDependencies['@beamable/sdk']
const SDK_GLOBAL = `@beamable/sdk-${SDK_VERSION}`
const SDK_API_GLOBAL = `@beamable/sdk/api-${SDK_VERSION}`

/**
 * Returns a Vite plugin that marks `@beamable/sdk` and `@beamable/sdk/api`
 * as external and maps them to the Portal-provided window globals at runtime.
 */
export function portalExtensionPlugin() {
  return {
    name: 'beamable-portal-extension',
    config() {
      return {
        build: {
          rollupOptions: {
            external: ['@beamable/sdk', '@beamable/sdk/api'],
            output: {
              globals: {
                '@beamable/sdk': `window['${SDK_GLOBAL}']`,
                '@beamable/sdk/api': `window['${SDK_API_GLOBAL}']`,
              },
            },
          },
        },
      }
    },
  }
}
