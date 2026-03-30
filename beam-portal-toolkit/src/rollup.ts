/**
 * Rollup config helpers for Beamable portal extensions.
 *
 * Provides pre-configured external/globals options so that `beamable-sdk`
 * is excluded from the extension bundle and resolved to the versioned window
 * global that the Portal injects before running the extension script.
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
 *
 * When the toolkit's beamable-sdk peer dependency version changes, update
 * SDK_GLOBAL below to match.
 *
 * Note: only the main `beamable-sdk` entry is externalized. The subpath
 * `beamable-sdk/api` has no browser IIFE build and is stateless (no shared
 * connection or auth state), so it is safe — and correct — to bundle it
 * directly into the extension. Do not add it to externals/globals.
 */

// Versioned global name the Portal registers on window before the extension
// script runs. Must match the `globalName` in the Portal's beam-sdk-registry.
const SDK_GLOBAL = 'beamable-sdk-0.6.0'

/**
 * Spread `external` into the top-level rollup config and `output` into the
 * output object. Both are read-only — do not mutate them.
 */
export const portalExtensionRollupOptions = {
  external: ['beamable-sdk'] as const,
  output: {
    globals: {
      'beamable-sdk': `window['${SDK_GLOBAL}']`,
    },
  },
} as const
