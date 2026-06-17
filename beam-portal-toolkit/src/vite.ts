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

// ---------------------------------------------------------------------------
// definePortalExtensionConfig
// ---------------------------------------------------------------------------

/**
 * Options for {@link definePortalExtensionConfig}.
 */
export interface PortalExtensionConfigOptions {
  /**
   * Path to the entry file. Defaults to `'src/main.tsx'` — the convention
   * for React extensions. Pass `'src/main.ts'` for non-React or your own
   * path for anything custom.
   */
  entry?: string;
  /**
   * Library name passed to rollup's `lib.name`. Mostly cosmetic — the
   * extension is an IIFE that registers itself on `window`. Defaults to
   * `'PortalExtension'`.
   */
  name?: string;
  /**
   * Whether the extension uses React. Toggles the React externals so the
   * Portal-provided `window['@beamable/react-19']` etc. are used at
   * runtime. Defaults to `true` — almost every extension is React.
   */
  react?: boolean;
  /**
   * Additional Vite plugins to merge in. Order: `[react(), portalExtensionPlugin(),
   * ...extraPlugins]`. Use for things like `vite-plugin-svgr`.
   */
  extraPlugins?: unknown[];
  /**
   * Output directory. Defaults to `'assets'` — the convention the Portal's
   * CLI expects.
   */
  outDir?: string;
  /**
   * Disable minification. Defaults to `false` (i.e. minify) for production
   * builds; flip to `true` for easier debugging.
   */
  minify?: boolean;
}

/**
 * Returns a Vite config object for a Beamable portal extension.
 *
 * Captures the ~30-line boilerplate every extension's `vite.config.ts`
 * was copying:
 *   - the `@vitejs/plugin-react` + `portalExtensionPlugin({ react: true })`
 *     plugin pair
 *   - IIFE library mode with `inlineDynamicImports`
 *   - `index.js` + `style.css` output naming the CLI expects
 *   - the port-pinned dev server (4951 by default to avoid collisions
 *     with the host portal on 4950)
 *
 * Usage:
 *
 *   // vite.config.ts
 *   import { definePortalExtensionConfig } from '@beamable/portal-toolkit/vite';
 *   export default definePortalExtensionConfig({ entry: 'src/main.tsx' });
 *
 * @example  default config + a custom plugin
 *   import svgr from 'vite-plugin-svgr';
 *   export default definePortalExtensionConfig({
 *     entry: 'src/main.tsx',
 *     extraPlugins: [svgr()],
 *   });
 */
export async function definePortalExtensionConfig(
  options: PortalExtensionConfigOptions = {}
) {
  const {
    entry = 'src/main.tsx',
    name: rawName = 'PortalExtension',
    react: useReact = true,
    extraPlugins = [],
    outDir = 'assets',
    minify = false,
  } = options;

  // Rollup's IIFE wrapper uses `name` as a JS identifier (`var <name> = …`),
  // so dashes, dots, and other manifest-style separators are rejected.
  // Sanitize: keep alnum, replace everything else with underscore, prefix
  // a leading digit. Mostly cosmetic — the extension doesn't read this
  // value at runtime.
  const name = rawName.replace(/[^a-zA-Z0-9_$]/g, '_').replace(/^([0-9])/, '_$1');

  // React plugin is loaded dynamically so the toolkit can ship without
  // hard-depending on it for non-React extensions.
  const plugins: unknown[] = [];
  if (useReact) {
    const { default: reactPlugin } = await import('@vitejs/plugin-react');
    plugins.push(reactPlugin());
  }
  plugins.push(portalExtensionPlugin({ react: useReact }));
  plugins.push(...extraPlugins);

  return {
    plugins,
    resolve: {
      // Force shared packages to resolve to THIS extension's single copy, even when imported
      // from a file:-linked common library whose own folder has no node_modules. Without this,
      // Rollup can't resolve a bare import (e.g. @beamable/portal-toolkit) from the library's
      // source, and you'd risk bundling a second copy of the toolkit (double web-component
      // registration). react/react-dom are externalized by the plugin, but are listed here too
      // so any direct import from a library still resolves to one instance.
      dedupe: ['@beamable/portal-toolkit', 'react', 'react-dom'] as string[],
    },
    build: {
      minify,
      outDir,
      lib: {
        entry,
        name,
        formats: ['iife'] as const,
      },
      rollupOptions: {
        input: entry,
        output: {
          format: 'iife' as const,
          inlineDynamicImports: true,
          entryFileNames: 'index.js',
          assetFileNames: (assetInfo: { name?: string }) => {
            if (assetInfo.name && assetInfo.name.endsWith('.css')) {
              return 'style.css';
            }
            return '[name][extname]';
          },
        },
      },
    },
  };
}
