import { registerReactZoneExtension } from '@beamable/portal-toolkit/react'
import { name as beamId } from '../package.json'
import './app.css'
import App from './App'

// The extension id comes from package.json `name` — the single source of
// truth the Beamable CLI also reads to register and bundle this extension.
// Vite inlines the string at build time, so there's nothing here to drift.
//
// This is a ZONE-scoped extension: it registers via `registerReactZoneExtension`
// so the portal hands `App` a `ZoneExtensionContext` (a `BeamZoneSdk`, scoped to
// `cid.zid`) instead of a realm-scoped `Beam`.
registerReactZoneExtension({ beamId, App })
