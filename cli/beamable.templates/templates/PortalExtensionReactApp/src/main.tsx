import { registerReactExtension } from '@beamable/portal-toolkit/react'
import { name as beamId } from '../package.json'
import './app.css'
import App from './App'

// The extension id comes from package.json `name` — the single source of
// truth the Beamable CLI also reads to register and bundle this extension.
// Vite inlines the string at build time, so there's nothing here to drift.
registerReactExtension({ beamId, App })
