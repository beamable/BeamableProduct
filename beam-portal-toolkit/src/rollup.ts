/**
 * Rollup config helpers for Beamable portal extensions.
 *
 * Provides pre-configured external/globals options so that `@beamable/sdk`
 * and `@beamable/sdk/api` are excluded from the extension bundle and resolved
 * to the versioned window globals that the Portal injects before running the
 * extension script.
 *
 * Usage in rollup.config.js:
 *
 *   import { portalExtensionRollupOptions } from '@beamable/portal-toolkit/rollup'
 *
 *   export default {
 *     input: 'src/index.js',
 *     output: {
 *       file: 'dist/index.js',
 *       format: 'iife',
 *       name: 'MyExtensionName',   // must match manifest.Name exactly
 *       ...portalExtensionRollupOptions.output,
 *     },
 *     external: portalExtensionRollupOptions.external,
 *   }
 */

import pkg from '../package.json'

// Versioned global names the Portal registers on window before the extension
// script runs. Must match the `globalName` in the Portal's beam-sdk-registry.
const SDK_VERSION = pkg.peerDependencies['@beamable/sdk']
const SDK_GLOBAL = `@beamable/sdk-${SDK_VERSION}`
const SDK_API_GLOBAL = `@beamable/sdk/api-${SDK_VERSION}`

/**
 * Spread `external` into the top-level rollup config and `output` into the
 * output object. Both are read-only — do not mutate them.
 */
export const portalExtensionRollupOptions = {
  external: ['@beamable/sdk', '@beamable/sdk/api'] as const,
  output: {
    globals: {
      '@beamable/sdk': `window['${SDK_GLOBAL}']`,
      '@beamable/sdk/api': `window['${SDK_API_GLOBAL}']`,
    },
  },
} as const
