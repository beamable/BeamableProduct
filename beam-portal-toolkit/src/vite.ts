/**
 * Vite plugin for Beamable portal extensions.
 *
 * Configures the build so that runtime dependencies provided by the Portal
 * (`@beamable/sdk`, `@beamable/sdk/api`, and optionally React) are treated as
 * externals and resolved to the versioned window globals that the Portal
 * injects before running the extension script.
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
 * For a React-based extension, opt in to React externals:
 *
 *   portalExtensionPlugin({ react: true })
 */

import {
  buildPortalExternals,
  type PortalExtensionPluginOptions,
} from './build-shared'

export type { PortalExtensionPluginOptions } from './build-shared'

/**
 * Returns a Vite plugin that marks Portal-provided runtime modules as external
 * and maps them to the Portal-provided window globals at runtime.
 */
export function portalExtensionPlugin(options: PortalExtensionPluginOptions = {}) {
  const { external, globals } = buildPortalExternals(options)
  return {
    name: 'beamable-portal-extension',
    config() {
      return {
        build: {
          rollupOptions: {
            external,
            output: {
              globals,
            },
          },
        },
      }
    },
  }
}
