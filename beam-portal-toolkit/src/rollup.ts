/**
 * Rollup config helpers for Beamable portal extensions.
 *
 * Provides pre-configured external/globals options so that Portal-provided
 * runtime modules are excluded from the extension bundle and resolved to the
 * versioned window globals that the Portal injects before running the
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
 *
 * For a React-based extension, opt in with the function form:
 *
 *   const opts = portalExtensionRollup({ react: true })
 *   export default {
 *     ...,
 *     external: opts.external,
 *     output: { ..., globals: opts.output.globals },
 *   }
 */

import {
  buildPortalExternals,
  type PortalExtensionPluginOptions,
} from './build-shared'

export type { PortalExtensionPluginOptions } from './build-shared'

/**
 * Programmatic form. Pass `{ react: true }` to externalize React along with
 * the Beamable SDK.
 */
export function portalExtensionRollup(options: PortalExtensionPluginOptions = {}) {
  const { external, globals } = buildPortalExternals(options)
  return {
    external,
    output: { globals },
  } as const
}

/**
 * Convenience constant for the default (SDK-only) case. Spread `external` into
 * the top-level rollup config and `output` into the output object. Both are
 * read-only — do not mutate them.
 */
export const portalExtensionRollupOptions = portalExtensionRollup()
