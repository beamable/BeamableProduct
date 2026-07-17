// Shared external/globals computation used by the vite and rollup helpers.
//
// React and ReactDOM are provided by the Portal host on `window` so that
// extensions do not bundle their own copy. The host (agentic-portal) is
// responsible for assigning the matching globals before any extension IIFE
// runs — see `extensionSdkRegistry` / `extensionMountHandler` in the Portal.

import pkg from '../package.json'

// Versioned global names the Portal registers on window before the extension
// script runs. Must match the `globalName` in the Portal's beam-sdk-registry.
const SDK_VERSION = pkg.peerDependencies['@beamable/sdk']
const SDK_GLOBAL = `@beamable/sdk-${SDK_VERSION}`
const SDK_API_GLOBAL = `@beamable/sdk/api-${SDK_VERSION}`

// React is pinned at the major-version level; same major shares one window
// global. Patch/minor differences between extensions are runtime-compatible.
const REACT_PEER = (pkg.peerDependencies as Record<string, string>).react ?? ''
const REACT_MAJOR = REACT_PEER.match(/\d+/)?.[0] ?? ''
const REACT_GLOBAL = `@beamable/react-${REACT_MAJOR}`
const REACT_DOM_GLOBAL = `@beamable/react-dom-${REACT_MAJOR}`
const REACT_DOM_CLIENT_GLOBAL = `@beamable/react-dom-client-${REACT_MAJOR}`
const REACT_JSX_RUNTIME_GLOBAL = `@beamable/react-jsx-runtime-${REACT_MAJOR}`

export interface PortalExtensionPluginOptions {
  /**
   * When true, externalize `react`, `react-dom`, `react-dom/client`, and
   * `react/jsx-runtime` so the extension bundle does not include its own
   * React copy. The Portal host is expected to assign the matching window
   * globals before the extension IIFE runs.
   */
  react?: boolean
}

export interface PortalExternalsResult {
  external: string[]
  globals: Record<string, string>
}

export function buildPortalExternals(
  options: PortalExtensionPluginOptions = {},
): PortalExternalsResult {
  const external: string[] = ['@beamable/sdk', '@beamable/sdk/api']
  const globals: Record<string, string> = {
    '@beamable/sdk': `window['${SDK_GLOBAL}']`,
    '@beamable/sdk/api': `window['${SDK_API_GLOBAL}']`,
  }

  if (options.react) {
    if (!REACT_MAJOR) {
      throw new Error(
        '@beamable/portal-toolkit: react peer dependency is missing or unparseable; cannot externalize React.',
      )
    }
    external.push('react', 'react-dom', 'react-dom/client', 'react/jsx-runtime', 'react/jsx-dev-runtime')
    Object.assign(globals, {
      'react': `window['${REACT_GLOBAL}']`,
      'react-dom': `window['${REACT_DOM_GLOBAL}']`,
      'react-dom/client': `window['${REACT_DOM_CLIENT_GLOBAL}']`,
      'react/jsx-runtime': `window['${REACT_JSX_RUNTIME_GLOBAL}']`,
      'react/jsx-dev-runtime': `window['${REACT_JSX_RUNTIME_GLOBAL}']`,
    })
  }

  return { external, globals }
}

export const portalRuntimeGlobals = {
  sdk: SDK_GLOBAL,
  sdkApi: SDK_API_GLOBAL,
  react: REACT_GLOBAL,
  reactDom: REACT_DOM_GLOBAL,
  reactDomClient: REACT_DOM_CLIENT_GLOBAL,
  reactJsxRuntime: REACT_JSX_RUNTIME_GLOBAL,
} as const
