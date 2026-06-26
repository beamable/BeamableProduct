import { registerReactExtension } from '@beamable/portal-toolkit/react'
import { name as beamId } from '../package.json'
import './app.css'
import App from './App'

// beamId must equal package.json#name.
registerReactExtension({ beamId, App })
