import { useCallback, useEffect, useState } from 'react'
import { type ExtensionContext } from '@beamable/portal-toolkit'
import {
  useBeam,
  BeamPageHeader,
  BeamCard,
  BeamButton,
  BeamBadge,
  BeamSpinner,
  BeamCallout,
  BeamTable,
  BeamColumn,
  BeamSelect,
  BeamOption,
  BeamDetails,
} from '@beamable/portal-toolkit/react'

interface AppProps {
  context: ExtensionContext
}

type Beam = NonNullable<ReturnType<typeof useBeam>>
type Row = Record<string, string>

const STAGES = ['Sent', 'Received', 'Opened', 'Clicked', 'Converted'] as const
type Stage = (typeof STAGES)[number]

// ── Analytics pipeline access ────────────────────────────────────────────────
// POST /basic/history/query/url → presigned URL → GET it directly (CORS: *) → CSV.
// Funnel events live in `client_notification_funnel_<stage>` tables; event params are
// columns prefixed `e.` (e.g. "e.campaignId", "e.gamerTag", "e.funnelType", "e.deeplink",
// "e.offerData.itemId"). Column/value names with dots must be double-quoted in Athena SQL.

function parseCsvLine(line: string): string[] {
  const out: string[] = []
  let cur = ''
  let q = false
  for (let i = 0; i < line.length; i++) {
    const ch = line[i]
    if (ch === '"') {
      if (q && line[i + 1] === '"') {
        cur += '"'
        i++
      } else q = !q
    } else if (ch === ',' && !q) {
      out.push(cur.trim())
      cur = ''
    } else cur += ch
  }
  out.push(cur.trim())
  return out
}

function parseCsv(text: string): Row[] {
  const lines = text.trim().split('\n')
  if (lines.length < 2) return []
  const headers = parseCsvLine(lines[0])
  const rows: Row[] = []
  for (let i = 1; i < lines.length; i++) {
    const vals = parseCsvLine(lines[i])
    if (vals.length !== headers.length) continue
    const row: Row = {}
    headers.forEach((h, idx) => (row[h] = vals[idx]))
    rows.push(row)
  }
  return rows
}

async function runQuery(beam: Beam, sql: string): Promise<Row[]> {
  const resp = await beam.requester.request<{ url?: string }>({
    url: '/basic/history/query/url',
    method: 'POST',
    body: { query: sql },
    headers: { 'X-DE-TIMEOUT': '60000' },
    withAuth: true,
  })
  const url = resp.body?.url
  if (!url) return []
  const r = await fetch(url)
  if (!r.ok) throw new Error(`Result fetch failed (${r.status})`)
  return parseCsv(await r.text())
}

async function listFunnelTables(beam: Beam): Promise<string[]> {
  try {
    const resp = await beam.requester.request<{ namespaces?: string[] }>({
      url: '/basic/history/events',
      method: 'GET',
      withAuth: true,
    })
    return (resp.body?.namespaces ?? []).filter((n) => /notification_funnel/i.test(n))
  } catch {
    return []
  }
}

const sqlStr = (v: string) => `'${v.replace(/'/g, "''")}'`
const dash = (v: string | undefined) => (v && v.length ? v : '—')

// Athena column naming for event params can vary (`e.campaignData`, `e.campaign_data`,
// camel vs snake case). Match loosely on a normalized key name so the value is found regardless
// of the exact column spelling, with or without the `e.` param prefix.
function pickCol(r: Row, name: string): string {
  const target = name.toLowerCase().replace(/[^a-z0-9]/g, '')
  for (const k of Object.keys(r)) {
    const norm = k.toLowerCase().replace(/[^a-z0-9]/g, '')
    if (norm === target || norm === 'e' + target) {
      const v = r[k]
      if (v != null && v !== '') return v
    }
  }
  return ''
}

// A push can carry several offers; render them in an expandable dropdown so the table cell stays
// compact. Collapsed it shows the offer count; opened it lists each offer's item/value/customData.
function OffersCell({ offers }: { offers: OfferView[] }) {
  if (offers.length === 0) return <span style={{ color: 'var(--color-beam-text-muted)' }}>—</span>
  const summary = offers.length === 1 ? '1 offer' : `${offers.length} offers`
  return (
    <BeamDetails summary={summary} appearance="plain">
      <div style={{ display: 'flex', flexDirection: 'column', gap: 8, padding: '4px 2px', minWidth: 220 }}>
        {offers.map((o, i) => (
          <div
            key={i}
            style={{
              display: 'flex',
              flexDirection: 'column',
              gap: 2,
              fontSize: 12,
              paddingTop: i === 0 ? 0 : 8,
              borderTop: i === 0 ? 'none' : '1px solid var(--color-beam-border, rgba(127,127,127,0.25))',
            }}
          >
            {offers.length > 1 && (
              <span style={{ color: 'var(--color-beam-text-muted)', fontWeight: 600 }}>Offer {i + 1}</span>
            )}
            <span><strong>item:</strong> {dash(o.itemId)}</span>
            <span><strong>value:</strong> {dash(o.value)}</span>
            <span><strong>customData:</strong> {dash(o.customData)}</span>
          </div>
        ))}
      </div>
    </BeamDetails>
  )
}

interface OfferView {
  itemId: string
  value: string
  customData: string
}

// Normalize one raw offer object into the display shape. `value` may arrive as a number
// (native funnel stages) or string (server-side Sent); `customData` may be an embedded object
// (native) or a stringified JSON string (server) — both collapse to a display string.
function toOfferView(o: { itemId?: unknown; value?: unknown; customData?: unknown }): OfferView {
  const cd = o?.customData
  return {
    itemId: o?.itemId != null ? String(o.itemId) : '',
    value: o?.value != null ? String(o.value) : '',
    customData: cd == null ? '' : typeof cd === 'string' ? cd : JSON.stringify(cd),
  }
}

// `e.offerData` is a single column holding a stringified JSON array of offer objects
// (`[{itemId, value, customData}, ...]`). A push can carry several offers, so parse the whole
// array. Falls back to the old single dotted-key columns for rows ingested before the schema
// changed, so historical events still display.
function parseOffers(r: Row): OfferView[] {
  // Loose column match (like campaignData) so naming variants — `e.offerData`, `e.offer_data`,
  // `offerData` — all resolve to the stringified JSON array.
  const raw = pickCol(r, 'offerData')
  if (raw) {
    try {
      const parsed = JSON.parse(raw)
      const arr = Array.isArray(parsed) ? parsed : [parsed]
      const views = arr.filter((o) => o != null).map(toOfferView)
      if (views.length) return views
    } catch {
      /* malformed JSON — fall through to the legacy columns below */
    }
  }
  // Legacy dotted-key columns: at most a single offer.
  const legacy = toOfferView({
    itemId: r['e.offerData.itemId'],
    value: r['e.offerData.value'],
    customData: r['e.offerData.customData'],
  })
  return legacy.itemId || legacy.value || legacy.customData ? [legacy] : []
}

interface PlayerRow {
  player: string
  reached: Record<Stage, boolean>
}
interface DetailRow {
  funnelType: string
  player: string
  deeplink: string
  offers: OfferView[]
  campaignData: string
  cidPid: string
  time: string
}
interface Result {
  players: PlayerRow[]
  details: DetailRow[]
  counts: Record<Stage, number>
}

export default function App({ context }: AppProps) {
  const beam = useBeam(context)

  const [tables, setTables] = useState<string[]>([])
  const [pairs, setPairs] = useState<{ campaignId: string; nodeId: string }[]>([])
  const [campaign, setCampaign] = useState('')
  const [node, setNode] = useState('')

  const [optStatus, setOptStatus] = useState<'loading' | 'ready' | 'error'>('loading')
  const [optError, setOptError] = useState<string | null>(null)

  const [dataStatus, setDataStatus] = useState<'idle' | 'loading' | 'ready' | 'error'>('idle')
  const [dataError, setDataError] = useState<string | null>(null)
  const [result, setResult] = useState<Result | null>(null)

  // Query every existing funnel-stage table for the selected campaign+node, then fold the
  // rows into a per-player step matrix and a flat event-detail list.
  const loadData = useCallback(
    async (funnelTables: string[], c: string, n: string) => {
      if (!beam || !c || !n) return
      setDataStatus('loading')
      setDataError(null)
      try {
        const where = `WHERE "e.campaignId" = ${sqlStr(c)} AND "e.nodeId" = ${sqlStr(n)}`
        // Per-table SELECT * (columns differ per stage, so no UNION); merge client-side.
        const perTable = await Promise.all(
          funnelTables.map((t) =>
            runQuery(beam, `SELECT * FROM ${t} ${where}`).catch(() => [] as Row[]),
          ),
        )
        const rows = perTable.flat()

        const byPlayer = new Map<string, Record<Stage, boolean>>()
        const details: DetailRow[] = []
        for (const r of rows) {
          const player = r['e.gamerTag'] || r['gamer_tag'] || '(unknown)'
          const ft = (r['e.funnelType'] || '') as Stage
          if (!byPlayer.has(player)) {
            byPlayer.set(player, { Sent: false, Received: false, Opened: false, Clicked: false, Converted: false })
          }
          if (STAGES.includes(ft)) byPlayer.get(player)![ft] = true
          details.push({
            funnelType: r['e.funnelType'] || '',
            player,
            deeplink: r['e.deeplink'] || '',
            offers: parseOffers(r),
            campaignData: pickCol(r, 'campaignData'),
            cidPid: pickCol(r, 'cidPid'),
            time: r['act_time'] || r['act_date'] || '',
          })
        }

        const players: PlayerRow[] = [...byPlayer.entries()]
          .map(([player, reached]) => ({ player, reached }))
          .sort((a, b) => a.player.localeCompare(b.player))

        const counts = Object.fromEntries(
          STAGES.map((s) => [s, players.filter((p) => p.reached[s]).length]),
        ) as Record<Stage, number>

        // Order details by the funnel sequence, then player.
        details.sort(
          (a, b) =>
            STAGES.indexOf(a.funnelType as Stage) - STAGES.indexOf(b.funnelType as Stage) ||
            a.player.localeCompare(b.player),
        )

        setResult({ players, details, counts })
        setDataStatus('ready')
      } catch (e) {
        setDataError(e instanceof Error ? e.message : String(e))
        setDataStatus('error')
      }
    },
    [beam],
  )

  // Load the funnel tables + the distinct campaign/node pairs that populate the filters.
  const loadOptions = useCallback(async () => {
    if (!beam) return
    setOptStatus('loading')
    setOptError(null)
    setResult(null)
    try {
      const funnelTables = await listFunnelTables(beam)
      setTables(funnelTables)
      if (funnelTables.length === 0) {
        setPairs([])
        setOptStatus('ready')
        return
      }
      const union = funnelTables
        .map((t) => `SELECT DISTINCT "e.campaignId" AS campaignId, "e.nodeId" AS nodeId FROM ${t}`)
        .join(' UNION ')
      const rows = await runQuery(beam, union)
      const seen = new Set<string>()
      const ps: { campaignId: string; nodeId: string }[] = []
      for (const r of rows) {
        const campaignId = r['campaignId'] || ''
        const nodeId = r['nodeId'] || ''
        if (!campaignId) continue
        const key = `${campaignId} ${nodeId}`
        if (seen.has(key)) continue
        seen.add(key)
        ps.push({ campaignId, nodeId })
      }
      ps.sort((a, b) => a.campaignId.localeCompare(b.campaignId) || a.nodeId.localeCompare(b.nodeId))
      setPairs(ps)
      setOptStatus('ready')
      if (ps.length > 0) {
        setCampaign(ps[0].campaignId)
        setNode(ps[0].nodeId)
        void loadData(funnelTables, ps[0].campaignId, ps[0].nodeId)
      }
    } catch (e) {
      setOptError(e instanceof Error ? e.message : String(e))
      setOptStatus('error')
    }
  }, [beam, loadData])

  useEffect(() => {
    void loadOptions()
  }, [loadOptions])

  const campaigns = [...new Set(pairs.map((p) => p.campaignId))]
  const nodesForCampaign = pairs.filter((p) => p.campaignId === campaign).map((p) => p.nodeId)

  function onCampaignChange(c: string) {
    const firstNode = pairs.find((p) => p.campaignId === c)?.nodeId ?? ''
    setCampaign(c)
    setNode(firstNode)
    void loadData(tables, c, firstNode)
  }
  function onNodeChange(n: string) {
    setNode(n)
    void loadData(tables, campaign, n)
  }

  if (!beam) return <BeamSpinner />

  return (
    <>
      <BeamPageHeader
        label="Campaign Funnel"
        description="Per-player push funnel (Sent → Received → Opened → Clicked → Converted), filtered by campaign and node."
      />

      {optStatus === 'loading' && <BeamSpinner />}

      {optStatus === 'error' && (
        <BeamCallout variant="danger" appearance="filled-outlined">
          Failed to load campaigns: {optError}
        </BeamCallout>
      )}

      {optStatus === 'ready' && tables.length === 0 && (
        <BeamCallout variant="neutral" appearance="outlined">
          No push funnel events ingested yet. Fire a campaign push (or the app’s Track offer
          buttons); analytics ingestion can lag a few minutes, then Refresh.
        </BeamCallout>
      )}

      {optStatus === 'ready' && tables.length > 0 && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 18 }}>
          {/* Filters */}
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12, alignItems: 'flex-end' }}>
            <label style={{ display: 'flex', flexDirection: 'column', gap: 4, minWidth: 200 }}>
              <span style={{ fontSize: 12, color: 'var(--color-beam-text-muted)' }}>Campaign ID</span>
              <BeamSelect value={campaign} onValueChange={onCampaignChange}>
                {campaigns.map((c) => (
                  <BeamOption key={c} value={c}>
                    {c}
                  </BeamOption>
                ))}
              </BeamSelect>
            </label>
            <label style={{ display: 'flex', flexDirection: 'column', gap: 4, minWidth: 200 }}>
              <span style={{ fontSize: 12, color: 'var(--color-beam-text-muted)' }}>Node ID</span>
              <BeamSelect value={node} onValueChange={onNodeChange}>
                {nodesForCampaign.map((n) => (
                  <BeamOption key={n} value={n}>
                    {n}
                  </BeamOption>
                ))}
              </BeamSelect>
            </label>
            <BeamButton variant="brand" appearance="filled" loading={dataStatus === 'loading'} onClick={() => loadOptions()}>
              Refresh
            </BeamButton>
          </div>

          {dataStatus === 'error' && (
            <BeamCallout variant="danger" appearance="filled-outlined">
              Failed to load funnel: {dataError}
            </BeamCallout>
          )}

          {dataStatus === 'loading' && <BeamSpinner />}

          {dataStatus === 'ready' && result && (
            <>
              {/* Funnel summary — players reaching each stage */}
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: 10 }}>
                {STAGES.map((s) => (
                  <BeamCard key={s} appearance="filled-outlined" style={{ flex: '1 1 120px', minWidth: 120 }}>
                    <div style={{ padding: 14, display: 'flex', flexDirection: 'column', gap: 2 }}>
                      <span style={{ fontSize: 24, fontWeight: 700, color: 'var(--color-beam-text)' }}>
                        {result.counts[s]}
                      </span>
                      <span style={{ fontSize: 12, color: 'var(--color-beam-text-muted)' }}>{s}</span>
                    </div>
                  </BeamCard>
                ))}
              </div>

              {/* Per-player step matrix */}
              <BeamTable<PlayerRow>
                data={result.players}
                tableTitle={`Players in ${campaign} / ${node} — steps reached`}
                card
                rowKey={(r) => r.player}
                emptyMessage="No players found for this campaign/node."
              >
                <BeamColumn<PlayerRow> field="player" header="Player (gamerTag)" />
                {STAGES.map((s) => (
                  <BeamColumn<PlayerRow> key={s} header={s} align="center">
                    {(row) =>
                      row.reached[s] ? (
                        <BeamBadge variant="success" appearance="filled">
                          ✓
                        </BeamBadge>
                      ) : (
                        <span style={{ color: 'var(--color-beam-text-muted)' }}>✗</span>
                      )
                    }
                  </BeamColumn>
                ))}
              </BeamTable>

              {/* All event data */}
              <BeamTable<DetailRow>
                data={result.details}
                tableTitle="Event details"
                card
                rowKey={(r, i) => `${r.funnelType}:${r.player}:${i}`}
                emptyMessage="No events."
              >
                <BeamColumn<DetailRow> field="funnelType" header="Step" />
                <BeamColumn<DetailRow> field="player" header="Player" />
                <BeamColumn<DetailRow> header="Deeplink">{(r) => dash(r.deeplink)}</BeamColumn>
                <BeamColumn<DetailRow> header="Offers">{(r) => <OffersCell offers={r.offers} />}</BeamColumn>
                <BeamColumn<DetailRow> header="Campaign data">{(r) => dash(r.campaignData)}</BeamColumn>
                <BeamColumn<DetailRow> header="cid.pid" width="300px">
                  {(r) => <span style={{ whiteSpace: 'nowrap' }}>{dash(r.cidPid)}</span>}
                </BeamColumn>
                <BeamColumn<DetailRow> header="Time">{(r) => dash(r.time)}</BeamColumn>
              </BeamTable>
            </>
          )}
        </div>
      )}
    </>
  )
}
