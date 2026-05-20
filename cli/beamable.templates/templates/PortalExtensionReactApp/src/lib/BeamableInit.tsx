import { useState } from 'react'
import type { Beam } from '@beamable/portal-toolkit'

interface PlayerData {
  cid: string
  pid: string
  playerId: string
}

interface BeamableInitProps {
  beam: Beam | null
}

export default function BeamableInit({ beam }: BeamableInitProps) {
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [playerData, setPlayerData] = useState<PlayerData | null>(null)

  async function init() {
    if (!beam) return
    setLoading(true)
    setError(null)
    try {
      setPlayerData({
        cid: beam.cid,
        pid: beam.pid,
        playerId: String(beam.player.id),
      })
    } catch (err) {
      console.error('Failed to init player:', err)
      setError(err instanceof Error ? err.message : String(err))
    } finally {
      setLoading(false)
    }
  }

  return (
    <beam-card>
      <div slot="actions">
        <beam-button
          onClick={init}
          disabled={loading || !!playerData ? true : undefined}
          loading={loading ? true : undefined}
        >
          {loading
            ? 'Initializing...'
            : playerData
              ? 'Player Loaded'
              : 'Initialize Player'}
        </beam-button>

        {error && <p style={{ color: 'red', marginTop: '0.5rem', fontSize: '0.9rem' }}>{error}</p>}
      </div>

      {playerData ? (
        <dl>
          <dt>CID</dt>
          <dd>{playerData.cid}</dd>
          <dt>PID</dt>
          <dd>{playerData.pid}</dd>
          <dt>Player ID</dt>
          <dd>{playerData.playerId}</dd>
        </dl>
      ) : (
        <p style={{ color: '#666', fontStyle: 'italic' }}>Click the button to load player data.</p>
      )}
    </beam-card>
  )
}
