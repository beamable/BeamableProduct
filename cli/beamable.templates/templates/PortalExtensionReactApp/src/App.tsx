import { useEffect, useState } from 'react'
import { type Beam, type ExtensionContext } from '@beamable/portal-toolkit'

interface AppProps {
  context: ExtensionContext
}

export default function App({ context }: AppProps) {
  const [beam, setBeam] = useState<Beam | null>(null)

  useEffect(() => {
    let cancelled = false
    context.beam.then((b) => {
      if (!cancelled) setBeam(b)
    })
    return () => {
      cancelled = true
    }
  }, [context])

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
