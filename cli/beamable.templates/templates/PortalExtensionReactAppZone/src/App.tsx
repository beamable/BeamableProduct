import { useEffect, useState } from 'react'
import { useZoneBeam, BeamPageHeader, BeamCard, BeamButton } from '@beamable/portal-toolkit/react'
import { type ZoneExtensionContext } from '@beamable/portal-toolkit'

interface AppProps {
  context: ZoneExtensionContext
}

export default function App({ context }: AppProps) {
  // Zone-scoped SDK: customer realm/zone directory only — no player/realm surface.
  const beam = useZoneBeam(context)
  const [realmCount, setRealmCount] = useState<number | null>(null)

  useEffect(() => {
    if (!beam) return
    let cancelled = false
    beam.customer.getRealms().then((realms) => {
      if (!cancelled) setRealmCount(realms.length)
    })
    return () => {
      cancelled = true
    }
  }, [beam])

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 24 }}>
      <BeamPageHeader
        label="PortalExtensionReactAppZone"
        description="A zone-scoped Beamable Portal Extension page."
      />

      <BeamCard>
        <div style={{ padding: 18, display: 'flex', flexDirection: 'column', gap: 12 }}>
          <div>Zone ID {context.zid ?? '...'}</div>
          <div>Realms in customer {realmCount ?? '...'}</div>
          <div>
            <BeamButton variant="brand">Click</BeamButton>
          </div>
        </div>
      </BeamCard>
    </div>
  )
}
