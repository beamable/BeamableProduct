import { useBeam } from '@beamable/portal-toolkit/react'
import { type ExtensionContext } from '@beamable/portal-toolkit'

interface AppProps {
  context: ExtensionContext
}

export default function App({ context }: AppProps) {
  const beam = useBeam(context)

  return (
    <beam-card style={{ marginBottom: 20 }}>
      <h3 slot="header">PortalExtensionReactApp</h3>
      <div style={{ padding: 18 }}>
        <div>Player ID {beam?.player.id ?? '...'}</div>
        <beam-button variant="brand">Click</beam-button>
      </div>
    </beam-card>
  )
}
