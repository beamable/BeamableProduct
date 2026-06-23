import { useEffect, useState, useCallback, useMemo } from 'react'
import { type Beam, type ExtensionContext } from '@beamable/portal-toolkit'
import {
  BeamPage,
  BeamPageHeader,
  BeamCard,
  BeamButton,
  BeamBadge,
  BeamSpinner,
  BeamInput,
  BeamTextarea,
  BeamTable,
  BeamColumn,
  BeamTag,
  BeamCheckbox,
} from '@beamable/portal-toolkit/react'
import { PushNotificationServiceClient } from '../beamable/clients/PushNotificationServiceClient'
import type { RegisteredPlayer } from '../beamable/clients/types'

interface AppProps {
  context: ExtensionContext
}

/** Aggregate of a bulk send across multiple players. */
interface BulkSendResult {
  playersAttempted: number
  playersOk: number
  devicesDelivered: number
  devicesFailed: number
  messages: string[]
}

function formatUnixSeconds(value: bigint | string): string {
  const seconds = Number(value)
  if (!seconds || Number.isNaN(seconds)) return '—'
  return new Date(seconds * 1000).toLocaleString()
}

export default function App({ context }: AppProps) {
  const [beam, setBeam] = useState<Beam | null>(null)

  // Roster state
  const [players, setPlayers] = useState<RegisteredPlayer[]>([])
  const [rosterLoading, setRosterLoading] = useState(false)
  const [rosterError, setRosterError] = useState<string | null>(null)
  const [rosterNote, setRosterNote] = useState<string | null>(null)

  // Selection state (keyed by stringified playerId — playerId is bigint|string)
  const [selected, setSelected] = useState<Set<string>>(new Set())

  // Send form state
  const [playerId, setPlayerId] = useState('') // optional ad-hoc single target
  const [title, setTitle] = useState('')
  const [body, setBody] = useState('')
  const [deepLink, setDeepLink] = useState('')
  const [sending, setSending] = useState(false)
  const [sendResult, setSendResult] = useState<BulkSendResult | null>(null)
  const [sendError, setSendError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    context.beam.then((b) => {
      if (!cancelled) setBeam(b)
    })
    return () => {
      cancelled = true
    }
  }, [context])

  const loadRoster = useCallback(async () => {
    if (!beam) return
    setRosterLoading(true)
    setRosterError(null)
    setRosterNote(null)
    try {
      const client = new PushNotificationServiceClient(beam)
      const result = await client.listRegisteredPlayers()
      setPlayers(result.players ?? [])
      setRosterNote(result.message ?? null)
    } catch (err) {
      setRosterError(err instanceof Error ? err.message : String(err))
    } finally {
      setRosterLoading(false)
    }
  }, [beam])

  // Load the roster as soon as the Beam context is ready.
  useEffect(() => {
    if (beam) void loadRoster()
  }, [beam, loadRoster])

  // --- Selection helpers ---------------------------------------------
  const toggle = useCallback((id: string) => {
    setSelected((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }, [])

  const selectAll = useCallback(() => {
    setSelected(new Set(players.map((p) => String(p.playerId))))
  }, [players])

  const clearSelection = useCallback(() => setSelected(new Set()), [])

  // The set of player IDs a send will target: every checked row, plus the
  // optional manually-typed ID.
  const targets = useMemo(() => {
    const t = new Set(selected)
    if (playerId.trim()) t.add(playerId.trim())
    return t
  }, [selected, playerId])

  async function sendPush() {
    if (!beam) return
    if (targets.size === 0) {
      setSendError('Select at least one player (or type a player ID).')
      return
    }
    if (!title.trim() && !body.trim()) {
      setSendError('A title or body is required.')
      return
    }
    setSending(true)
    setSendError(null)
    setSendResult(null)

    const client = new PushNotificationServiceClient(beam)
    const payload = { title: title.trim(), body: body.trim(), deepLink: deepLink.trim() }

    const outcomes = await Promise.all(
      [...targets].map((id) =>
        client
          .sendPushToPlayer({ playerId: id, ...payload })
          .then((r) => ({ id, r }))
          .catch((e) => ({ id, err: e instanceof Error ? e.message : String(e) })),
      ),
    )

    const agg: BulkSendResult = {
      playersAttempted: targets.size,
      playersOk: 0,
      devicesDelivered: 0,
      devicesFailed: 0,
      messages: [],
    }
    for (const o of outcomes) {
      if ('err' in o) {
        agg.messages.push(`${o.id}: ${o.err}`)
        continue
      }
      const r = o.r
      if (r.success) agg.playersOk++
      agg.devicesDelivered += r.succeeded
      agg.devicesFailed += r.failed
      for (const m of r.messages ?? []) agg.messages.push(`${o.id}: ${m}`)
    }

    setSendResult(agg)
    setSending(false)
    // Refresh the roster — a send may have pruned dead tokens.
    void loadRoster()
  }

  const targetCount = targets.size

  return (
    <BeamPage>
      <BeamPageHeader>Push Notifications</BeamPageHeader>

      {/* --- Send a push ------------------------------------------------ */}
      <BeamCard style={{ marginBottom: 20 }}>
        <h3 slot="header">Send a notification</h3>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 12, padding: 18 }}>
          <BeamInput
            label="Player ID (optional)"
            placeholder="Tick players below, and/or paste a specific ID"
            value={playerId}
            onValueChange={setPlayerId}
          />
          <BeamInput label="Title" placeholder="Notification title" value={title} onValueChange={setTitle} />
          <BeamTextarea label="Body" placeholder="Notification body" rows={3} value={body} onValueChange={setBody} />
          <BeamInput
            label="Deep link (optional)"
            placeholder="e.g. myapp://inbox/42"
            value={deepLink}
            onValueChange={setDeepLink}
          />
          <div>
            <BeamButton
              variant="brand"
              onClick={sendPush}
              disabled={!beam || sending || targetCount === 0}
              loading={sending}
            >
              {sending ? 'Sending…' : `Send to ${targetCount} player${targetCount === 1 ? '' : 's'}`}
            </BeamButton>
            {sendError && (
              <span style={{ marginLeft: 12, color: 'var(--beam-color-danger-600, #c0392b)' }}>{sendError}</span>
            )}
          </div>

          {sendResult && (
            <div style={{ marginTop: 4 }}>
              <BeamBadge variant={sendResult.playersOk === sendResult.playersAttempted ? 'success' : 'danger'}>
                {sendResult.playersOk === sendResult.playersAttempted ? 'Sent' : 'Partial'}
              </BeamBadge>
              <span style={{ marginLeft: 10 }}>
                Sent to {sendResult.playersOk}/{sendResult.playersAttempted} player(s) —{' '}
                {sendResult.devicesDelivered} device(s) delivered
                {sendResult.devicesFailed > 0 ? `, ${sendResult.devicesFailed} failed` : ''}
              </span>
              {sendResult.messages.length > 0 && (
                <pre
                  style={{
                    marginTop: 10,
                    padding: 12,
                    background: 'var(--beam-color-neutral-100, #f4f4f5)',
                    borderRadius: 4,
                    overflow: 'auto',
                    whiteSpace: 'pre-wrap',
                  }}
                >
                  {sendResult.messages.join('\n')}
                </pre>
              )}
            </div>
          )}
        </div>
      </BeamCard>

      {/* --- Registered players ---------------------------------------- */}
      <BeamCard>
        <h3 slot="header">
          Registered players{' '}
          {rosterLoading && <BeamSpinner style={{ marginLeft: 8 }} />}
        </h3>
        <div style={{ padding: 18 }}>
          <div style={{ marginBottom: 12, display: 'flex', alignItems: 'center', gap: 12, flexWrap: 'wrap' }}>
            <BeamButton onClick={loadRoster} disabled={!beam || rosterLoading}>
              Refresh
            </BeamButton>
            <BeamButton appearance="outlined" onClick={selectAll} disabled={players.length === 0}>
              Select all
            </BeamButton>
            <BeamButton appearance="outlined" onClick={clearSelection} disabled={selected.size === 0}>
              Clear
            </BeamButton>
            <span style={{ fontWeight: 600 }}>Selected: {selected.size}</span>
            {rosterError && (
              <span style={{ color: 'var(--beam-color-danger-600, #c0392b)' }}>{rosterError}</span>
            )}
            {rosterNote && <span style={{ fontStyle: 'italic' }}>{rosterNote}</span>}
          </div>

          <BeamTable<RegisteredPlayer>
            data={players}
            rowKey={(row) => String(row.playerId)}
            emptyMessage="No players have registered a device yet."
            loading={rosterLoading}
            loadingMessage="Loading roster…"
          >
            <BeamColumn<RegisteredPlayer>
              header=""
              width="44px"
              children={(row) => (
                <BeamCheckbox
                  checked={selected.has(String(row.playerId))}
                  onCheckedChange={() => toggle(String(row.playerId))}
                />
              )}
            />
            <BeamColumn<RegisteredPlayer>
              field="playerId"
              header="Player ID"
              sortable
              format={(value) => String(value)}
            />
            <BeamColumn<RegisteredPlayer> field="deviceCount" header="Devices" sortable align="center" />
            <BeamColumn<RegisteredPlayer>
              header="Push platforms"
              children={(row) => (
                <span style={{ display: 'inline-flex', gap: 6 }}>
                  {row.platforms.map((p) => (
                    <BeamTag key={p}>{p}</BeamTag>
                  ))}
                </span>
              )}
            />
            <BeamColumn<RegisteredPlayer>
              field="gamePlatform"
              header="Game platform"
              sortable
              format={(value) => (value ? String(value) : '—')}
            />
            <BeamColumn<RegisteredPlayer>
              field="gameDevice"
              header="Device"
              sortable
              format={(value) => (value ? String(value) : '—')}
            />
            <BeamColumn<RegisteredPlayer>
              field="lastUpdated"
              header="Last updated"
              sortable
              format={(value) => formatUnixSeconds(value as bigint | string)}
            />
          </BeamTable>
        </div>
      </BeamCard>
    </BeamPage>
  )
}
