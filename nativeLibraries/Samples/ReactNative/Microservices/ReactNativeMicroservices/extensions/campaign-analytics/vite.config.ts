import { definePortalExtensionConfig } from '@beamable/portal-toolkit/vite'
import { name } from './package.json'

// React is externalized to the Portal host's version-pinned window globals;
// emits the index.js + style.css the CLI expects.
export default definePortalExtensionConfig({ entry: 'src/main.tsx', name })
