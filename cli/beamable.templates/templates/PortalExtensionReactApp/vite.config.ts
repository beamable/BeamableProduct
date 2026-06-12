import { definePortalExtensionConfig } from '@beamable/portal-toolkit/vite'
import { name } from './package.json'

// Builds the extension as a self-registering IIFE with React externalized to
// the Portal host's window globals, emitting `assets/index.js` + `style.css`.
// `name` is derived from package.json so the extension id lives in exactly one
// place. definePortalExtensionConfig captures the rest of the build/rollup
// boilerplate; pass `extraPlugins`, `minify`, `outDir`, etc. here to override.
export default definePortalExtensionConfig({ entry: 'src/main.tsx', name })
