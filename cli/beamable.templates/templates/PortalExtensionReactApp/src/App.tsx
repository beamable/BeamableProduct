import { useBeam, BeamPageHeader, BeamCard, BeamButton } from '@beamable/portal-toolkit/react'
import { type ExtensionContext } from '@beamable/portal-toolkit'

interface AppProps {
  context: ExtensionContext
}

export default function App({ context }: AppProps) {
  const beam = useBeam(context)

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 24 }}>
      <BeamPageHeader
        label="PortalExtensionReactApp"
        description="A Beamable Portal Extension page."
      />

      <BeamCard>
        <div style={{ padding: 18, display: 'flex', flexDirection: 'column', gap: 12 }}>
          <div>Player ID {beam?.player.id ?? '...'}</div>
          <div>
            <BeamButton variant="brand">Click</BeamButton>
          </div>
        </div>
      </BeamCard>
    </div>
  )
}
