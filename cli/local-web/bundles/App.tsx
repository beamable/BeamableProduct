import { Fragment, useEffect, useMemo, useRef, useState, type CSSProperties } from 'react'
import {
  BeamBadge,
  BeamButton,
  BeamCallout,
  BeamColumn,
  BeamCopyButton,
  BeamConfirmDialog,
  BeamDialog,
  BeamDropdown,
  BeamDropdownItem,
  BeamIcon,
  BeamInput,
  BeamOption,
  BeamPageHeader,
  BeamProgressBar,
  BeamRelativeTime,
  BeamSpinner,
  BeamTab,
  BeamTabGroup,
  BeamTabPanel,
  BeamTable,
  BeamTextarea,
} from '@beamable/portal-toolkit/react'
import { BoundSelect, ThemeToggle, errorMessage, mono, text, textMuted, useSessionState, useToasts } from '../shared/ui'
import { sessionToken } from '../shared/connection'
import {
  CliClient,
  jobError,
  jobResult,
  type DeployPlanResult,
  type Job,
  type JobProgress,
  type JobParams,
  type ListResult,
  type PlanResult,
  type PublishResult,
  type Release,
  type ReleasesResult,
  type Workspace,
} from './cliApi'

const TABS = ['bundles', 'pins', 'releases', 'activity']

const CLI_COMMAND = 'beam bundles management'

const QUIET: CSSProperties = { ...textMuted, fontSize: '12px' }
const QUIET_MONO: CSSProperties = { ...textMuted, ...mono, fontSize: '11px' }
const ROW: CSSProperties = { display: 'flex', alignItems: 'center', gap: '0.5rem', flexWrap: 'wrap' }
const STACK: CSSProperties = { display: 'flex', flexDirection: 'column', gap: '0.75rem' }
const MENU_ITEM: CSSProperties = { display: 'flex', alignItems: 'center', gap: '0.5rem' }

type Scope = 'realm' | 'zone'
type Acl = '' | 'private' | 'org' | 'public'

/** One row of the Bundles tab: a bundle authored here, published, or both. */
/** A bundle another bundle depends on, and whether the catalog can satisfy it yet. */
interface Dependency {
  name: string
  range: string
  /** Lowest release the range accepts. */
  min: number
  /** Newest published release, 0 when the bundle was never published. */
  latestVersion: number
  /** Releases are numbered 1, 2, 3…, so the range is satisfiable once the latest reaches its minimum. */
  satisfied: boolean
}

interface BundleRow {
  name: string
  scope: Scope
  local: boolean
  published: boolean
  components: string[]
  dependencies: Dependency[]
  latestVersion: number
  latestChecksum: string
  tags: string[]
  acl: string
  pinned: string
}

/** `@ns/name` → `name`. */
function shortName(name: string): string {
  const slash = name.indexOf('/')
  return name.startsWith('@') && slash > 0 ? name.slice(slash + 1) : name
}

function shortChecksum(checksum: string): string {
  const hash = checksum.startsWith('sha256:') ? checksum.slice('sha256:'.length) : checksum
  return hash.length > 12 ? `sha256:…${hash.slice(-8)}` : checksum || '-'
}

function aclLabel(acl: string): string {
  if (!acl) return '-'
  if (acl === '*') return 'Public'
  return acl.includes('.') ? 'Private' : 'Org'
}

const STATUS_VARIANT: Record<string, 'neutral' | 'brand' | 'success' | 'danger' | 'warning'> = {
  queued: 'neutral',
  running: 'brand',
  succeeded: 'success',
  failed: 'danger',
  cancelled: 'warning',
}

function StatusBadge({ status }: { status: string }) {
  return (
    <BeamBadge variant={STATUS_VARIANT[status] ?? 'neutral'} appearance="filled" pill>
      {status}
    </BeamBadge>
  )
}

function formatElapsed(ms: number): string {
  const s = Math.max(0, Math.floor(ms / 1000))
  return s < 60 ? `${s}s` : `${Math.floor(s / 60)}m ${String(s % 60).padStart(2, '0')}s`
}

/** How far a step is, 0..1. Steps without a known length only report started (0) and done (1); failed ones are over. */
function stepDone(step: JobProgress): number {
  if (step.ratio < 0 || step.ratio >= 1) return 1
  return step.isKnownLength ? step.ratio : 0
}

/** Overall progress of plan, publish and deploy: every step they reported so far, weighted equally. */
function JobProgressView({ job }: { job: Job }) {
  const running = job.status === 'running' || job.status === 'queued'
  const [now, setNow] = useState(Date.now())
  useEffect(() => {
    if (!running) return
    const timer = setInterval(() => setNow(Date.now()), 1000)
    return () => clearInterval(timer)
  }, [running])

  const steps = job.progress ?? []
  const started = job.startedAt || job.createdAt
  const elapsed = (running ? now : job.finishedAt || now) - started
  const percent = steps.length ? Math.round((steps.reduce((sum, s) => sum + stepDone(s), 0) / steps.length) * 100) : 0
  const finished = steps.filter((s) => stepDone(s) >= 1).length
  const current = steps.filter((s) => stepDone(s) < 1).map((s) => s.name)

  if (!running && steps.length === 0) return null
  return (
    <div style={{ ...STACK, gap: '0.375rem' }}>
      <BeamProgressBar value={running ? percent : 100} indeterminate={running && steps.length === 0} label="Progress" />
      <div style={{ ...ROW, justifyContent: 'space-between' }}>
        <span style={QUIET}>
          {steps.length === 0
            ? 'Starting…'
            : `${percent}% · ${finished} of ${steps.length} steps done${current.length ? ` · ${current.slice(0, 3).join(', ')}${current.length > 3 ? '…' : ''}` : ''}`}
        </span>
        <span style={QUIET}>{formatElapsed(elapsed)}</span>
      </div>
      {steps.length > 0 && (
        <div style={{ display: 'grid', gridTemplateColumns: 'minmax(0, 1fr) 8rem 2.75rem', gap: '0.25rem 0.75rem', alignItems: 'center', maxHeight: '10rem', overflow: 'auto' }}>
          {steps.map((s) => (
            <Fragment key={s.name}>
              <span style={{ ...QUIET_MONO, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{s.name}</span>
              <BeamProgressBar
                value={Math.round(stepDone(s) * 100)}
                indeterminate={running && !s.isKnownLength && stepDone(s) < 1}
                label={s.name}
                style={{ '--track-height': '0.375rem' } as CSSProperties}
              />
              <span style={{ ...QUIET_MONO, textAlign: 'right', color: s.ratio < 0 ? 'var(--color-beam-error)' : undefined }}>
                {s.ratio < 0 ? 'failed' : `${Math.round(stepDone(s) * 100)}%`}
              </span>
            </Fragment>
          ))}
        </div>
      )}
    </div>
  )
}

/** A job's command, status, progress and log, following the tail while it runs. */
function JobLogView({ job }: { job: Job | null }) {
  const end = useRef<HTMLDivElement>(null)
  useEffect(() => {
    end.current?.scrollIntoView({ block: 'nearest' })
  }, [job?.logs.length])
  if (!job) return null
  return (
    <div style={STACK}>
      <div style={ROW}>
        <StatusBadge status={job.status} />
        <span style={QUIET_MONO}>{job.commandLine}</span>
      </div>
      <JobProgressView job={job} />
      <div
        style={{
          ...mono,
          fontSize: '11px',
          maxHeight: '16rem',
          overflow: 'auto',
          padding: '0.5rem 0.75rem',
          borderRadius: '0.375rem',
          background: 'var(--color-beam-neutral-soft)',
          border: '1px solid var(--color-beam-border)',
          whiteSpace: 'pre-wrap',
          overflowWrap: 'anywhere',
        }}
      >
        {job.logs.length === 0 && <span style={QUIET}>Waiting for output…</span>}
        {job.logs.map((l, i) => (
          <div
            key={i}
            style={{
              color: /error|critical/i.test(l.level)
                ? 'var(--color-beam-error)'
                : /warn/i.test(l.level)
                  ? 'var(--color-beam-warning)'
                  : undefined,
            }}
          >
            {l.message}
          </div>
        ))}
        <div ref={end} />
      </div>
    </div>
  )
}

export default function App() {
  const { toast, Toaster } = useToasts()

  const [storedTab, setStoredTab] = useSessionState('tab', 'bundles')
  const activeTab = TABS.includes(storedTab) ? storedTab : 'bundles'

  // ── Connection to the CLI ──
  const [client, setClient] = useState<CliClient | null>(null)
  const [connecting, setConnecting] = useState(true)
  const [connectError, setConnectError] = useState('')
  const [workspace, setWorkspace] = useState<Workspace | null>(null)

  async function connect() {
    setConnecting(true)
    setConnectError('')
    setClient(null)
    try {
      // The CLI opens this page with its session token in the fragment.
      if (!sessionToken()) return
      const cli = new CliClient()
      setWorkspace(await cli.getWorkspace())
      setClient(cli)
    } catch (err) {
      setConnectError(errorMessage(err, 'Could not reach the CLI. Is `bundles management` still running?'))
    } finally {
      setConnecting(false)
    }
  }

  useEffect(() => { connect() }, [])

  // ── Jobs ──
  const [jobs, setJobs] = useState<Job[]>([])
  const [busy, setBusy] = useState(false)

  function track(job: Job) {
    setJobs((all) => [job, ...all.filter((j) => j.id !== job.id)].slice(0, 30))
  }

  /** Runs a job, records it under Activity, and returns its parsed result or null on failure. */
  async function run<T>(op: string, params: JobParams = {}, onUpdate?: (job: Job) => void): Promise<T | null> {
    if (!client) return null
    try {
      const job = await client.runJob(op, params, (j) => { track(j); onUpdate?.(j) })
      if (job.status !== 'succeeded') {
        toast.error(jobError(job))
        return null
      }
      const result = jobResult<T>(job)
      // Every operation reports a result; without one the command stopped early (see its log in Activity).
      if (result === null) toast.error(`${job.commandLine} finished without a result. See its log under Activity.`)
      return result
    } catch (err) {
      toast.error(errorMessage(err, `${op} failed`))
      return null
    }
  }

  // ── Bundles + pins ──
  const [list, setList] = useState<ListResult | null>(null)
  const [loading, setLoading] = useState(false)

  async function refresh() {
    if (!client) return
    setLoading(true)
    try {
      const [ws, listed] = await Promise.all([client.getWorkspace(), run<ListResult>('list')])
      setWorkspace(ws)
      if (listed) setList(listed)
    } catch (err) {
      toast.error(errorMessage(err, 'Failed to refresh'))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { refresh() }, [client])

  const pins = useMemo(() => {
    const map = new Map<string, { scope: Scope; checksum: string }>()
    workspace?.realmPins.forEach((p) => map.set(p.name, { scope: 'realm', checksum: p.checksum }))
    workspace?.zonePins.forEach((p) => map.set(p.name, { scope: 'zone', checksum: p.checksum }))
    return map
  }, [workspace])

  const rows = useMemo<BundleRow[]>(() => {
    const byName = new Map<string, BundleRow>()
    for (const p of list?.published ?? []) {
      byName.set(p.name, {
        name: p.name,
        scope: 'realm',
        local: false,
        published: true,
        components: [],
        dependencies: [],
        latestVersion: p.latestVersion,
        latestChecksum: p.latestChecksum,
        tags: p.tags.filter((t) => t.checksum === p.latestChecksum).map((t) => t.tag),
        acl: p.acl,
        pinned: pins.get(p.name)?.checksum ?? '',
      })
    }
    for (const l of list?.local ?? []) {
      // Local names are namespaced only when the CLI could resolve the customer alias; match on the short name.
      const existing = [...byName.values()].find((r) => shortName(r.name) === shortName(l.name))
      const components = [...l.serviceNames, ...l.storageIds, ...l.portalExtensionNames]
      const scope: Scope = l.scope?.toLowerCase() === 'zone' ? 'zone' : 'realm'
      const dependencies = Object.entries(l.bundleDependencies ?? {}).map(([name, range]): Dependency => {
        const min = Number.parseInt(range, 10) || 1
        const latestVersion = Number(
          list?.published.find((p) => shortName(p.name) === shortName(name))?.latestVersion ?? 0,
        )
        return { name, range, min, latestVersion, satisfied: latestVersion >= min }
      })
      if (existing) {
        Object.assign(existing, { local: true, components, scope, dependencies })
      } else {
        byName.set(l.name, {
          name: l.name, scope, local: true, published: false, components, dependencies,
          latestVersion: 0, latestChecksum: '', tags: [], acl: '', pinned: '',
        })
      }
    }
    for (const row of byName.values()) {
      const pin = pins.get(row.name)
      if (pin) row.scope = pin.scope
    }
    return [...byName.values()].sort((a, b) => shortName(a.name).localeCompare(shortName(b.name)))
  }, [list, pins])

  const [search, setSearch] = useState('')
  const filteredRows = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return rows
    return rows.filter((r) => r.name.toLowerCase().includes(q) || r.components.some((c) => c.toLowerCase().includes(q)))
  }, [rows, search])

  // ── Pin / unpin ──
  const [pinTarget, setPinTarget] = useState<{ name: string; tag: string; checksum: string } | null>(null)

  async function confirmPin() {
    if (!pinTarget) return
    setBusy(true)
    const done = await run('install', {
      bundle: pinTarget.name,
      ...(pinTarget.checksum ? { checksum: pinTarget.checksum } : { tag: pinTarget.tag || 'latest' }),
    })
    setBusy(false)
    if (done) {
      toast.success(`Pinned ${pinTarget.name}`)
      setPinTarget(null)
      await refresh()
    }
  }

  async function unpin(name: string) {
    if (await run('uninstall', { bundle: name })) {
      toast.success(`Unpinned ${name}`)
      await refresh()
    }
  }

  async function pruneYanked() {
    const result = await run<{ yanked: unknown[] }>('prune-yanked', { flag: true })
    if (result) {
      toast.success(result.yanked.length ? `Removed ${result.yanked.length} yanked pin(s)` : 'No yanked pins')
      await refresh()
    }
  }

  // ── Plan & publish ──
  const [publishTarget, setPublishTarget] = useState<string | null>(null)
  const [planJob, setPlanJob] = useState<Job | null>(null)
  const [plan, setPlan] = useState<PlanResult | null>(null)
  const [publishJob, setPublishJob] = useState<Job | null>(null)
  const [published, setPublished] = useState<PublishResult | null>(null)
  const [comment, setComment] = useState('')
  const [tag, setTag] = useState('')
  const [acl, setAcl] = useState<Acl>('')

  // A bundle can't publish while a dependency has no release in its range: the catalog rejects it
  // (BundleDepRangeUnresolvable). Those have to be published first.
  const [blockedBy, setBlockedBy] = useState<Dependency[]>([])

  async function openPublish(name: string) {
    setPublishTarget(name)
    setPlan(null)
    setPlanJob(null)
    setPublishJob(null)
    setPublished(null)
    setComment('')
    setTag('')
    setAcl('')
    const missing = rows.find((r) => r.name === name)?.dependencies.filter((d) => !d.satisfied) ?? []
    setBlockedBy(missing)
    if (missing.length > 0) return
    setBusy(true)
    setPlan(await run<PlanResult>('plan', { bundle: shortName(name) }, setPlanJob))
    setBusy(false)
  }

  async function publish() {
    if (!publishTarget) return
    setBusy(true)
    const result = await run<PublishResult>(
      'publish',
      { bundle: shortName(publishTarget), comment, tag: tag.trim(), acl, flag: true },
      setPublishJob,
    )
    setBusy(false)
    if (result) {
      setPublished(result)
      toast.success(`Published ${result.name} v${result.version}`)
      await refresh()
    }
  }

  // ── Deploy pins ──
  const [deployScope, setDeployScope] = useState<Scope | null>(null)
  const [deployComment, setDeployComment] = useState('')
  const [deployPlanJob, setDeployPlanJob] = useState<Job | null>(null)
  const [deployPlan, setDeployPlan] = useState<DeployPlanResult | null>(null)
  const [deployReleaseJob, setDeployReleaseJob] = useState<Job | null>(null)

  function openDeploy(scope: Scope) {
    setDeployScope(scope)
    setDeployComment('')
    setDeployPlan(null)
    setDeployPlanJob(null)
    setDeployReleaseJob(null)
  }

  async function planDeploy() {
    if (!deployScope) return
    setBusy(true)
    setDeployPlan(await run<DeployPlanResult>('deploy-plan', { scope: deployScope, comment: deployComment }, setDeployPlanJob))
    setBusy(false)
  }

  async function releaseDeploy() {
    if (!deployScope) return
    setBusy(true)
    const result = await run('deploy-release', { scope: deployScope, comment: deployComment }, setDeployReleaseJob)
    setBusy(false)
    if (result) toast.success(`Deployed the ${deployScope} manifest`)
  }

  // ── Releases ──
  const [releasesBundle, setReleasesBundle] = useState('')
  const [releases, setReleases] = useState<ReleasesResult | null>(null)
  const [releasesLoading, setReleasesLoading] = useState(false)

  async function loadReleases(name: string) {
    setReleasesBundle(name)
    setStoredTab('releases')
    setReleasesLoading(true)
    setReleases(await run<ReleasesResult>('releases', { bundle: name }))
    setReleasesLoading(false)
  }

  const [tagTarget, setTagTarget] = useState<{ name: string; checksum: string } | null>(null)
  const [newTag, setNewTag] = useState('')

  async function confirmTag() {
    if (!tagTarget) return
    setBusy(true)
    const done = await run('tag', { bundle: `${tagTarget.name}@${tagTarget.checksum}`, tag: newTag.trim() })
    setBusy(false)
    if (done) {
      toast.success(`Tagged ${shortChecksum(tagTarget.checksum)} as ${newTag.trim()}`)
      setTagTarget(null)
      await Promise.all([loadReleases(tagTarget.name), refresh()])
    }
  }

  const [yankTarget, setYankTarget] = useState<{ name: string; checksum: string } | null>(null)

  async function confirmYank() {
    if (!yankTarget) return
    const target = yankTarget
    setYankTarget(null)
    if (await run('yank', { bundle: `${target.name}@${target.checksum}` })) {
      toast.success(`Yanked ${shortChecksum(target.checksum)}`)
      await loadReleases(target.name)
    }
  }

  const [aclTarget, setAclTarget] = useState<string | null>(null)
  const [aclScope, setAclScope] = useState<Acl>('org')

  async function confirmAcl() {
    if (!aclTarget || !aclScope) return
    setBusy(true)
    const done = await run('acl', { bundle: aclTarget, acl: aclScope })
    setBusy(false)
    if (done) {
      toast.success(`Updated ACL for ${aclTarget}`)
      setAclTarget(null)
      await refresh()
    }
  }

  const [jsonData, setJsonData] = useState<unknown>(null)
  // A job's result arrives as a JSON string; show it parsed so it reads like the rest.
  const jsonText = jsonData == null ? '' : JSON.stringify(jsonData, (key, value) => {
    if (key !== 'resultJson' || typeof value !== 'string') return value
    try { return jobResult({ resultJson: value } as Job) } catch { return value }
  }, 2)

  // ── Render ──
  const header = (
    <BeamPageHeader
      label="Bundles"
      description="List, pin, plan, publish and deploy bundles through the Beamable CLI running in your workspace"
    >
      <div slot="actions"><ThemeToggle /></div>
    </BeamPageHeader>
  )

  if (connecting) {
    return (
      <div style={STACK}>
        {header}
        <div style={{ display: 'flex', justifyContent: 'center', padding: '4rem' }}>
          <BeamSpinner style={{ fontSize: '2rem' }} />
        </div>
      </div>
    )
  }

  if (!client) {
    return (
      <div style={STACK}>
        {header}
        <BeamCallout variant={connectError ? 'danger' : 'neutral'}>
          <BeamIcon slot="icon" name="terminal" />
          <div style={STACK}>
            <span style={text}>
              {connectError || 'This page is not connected to a CLI.'}
            </span>
            <span style={QUIET}>
              Run this in your Beamable workspace. It opens this page already connected, and every action here
              runs in that CLI.
            </span>
            <div
              style={{
                ...ROW,
                flexWrap: 'nowrap',
                justifyContent: 'space-between',
                padding: '0.375rem 0.5rem 0.375rem 0.75rem',
                borderRadius: '0.375rem',
                background: 'var(--color-beam-neutral-soft)',
                border: '1px solid var(--color-beam-border)',
                maxWidth: '32rem',
              }}
            >
              <code style={{ ...text, ...mono, fontSize: '13px' }}>{CLI_COMMAND}</code>
              <BeamCopyButton value={CLI_COMMAND} copy-label="Copy command" success-label="Copied!" />
            </div>
            <div>
              <BeamButton size="small" appearance="outlined" onClick={connect}>
                <BeamIcon slot="start" name="rotate" /> Retry
              </BeamButton>
            </div>
          </div>
        </BeamCallout>
        <Toaster />
      </div>
    )
  }

  const realmPins = workspace?.realmPins ?? []
  const zonePins = workspace?.zonePins ?? []
  const releaseTagsByChecksum = new Map<string, string[]>()
  for (const t of releases?.tags ?? []) {
    releaseTagsByChecksum.set(t.checksum, [...(releaseTagsByChecksum.get(t.checksum) ?? []), t.tag])
  }

  const pinsTable = (scope: Scope, data: { name: string; checksum: string }[]) => (
    <div style={STACK}>
      <div style={{ ...ROW, justifyContent: 'space-between' }}>
        <span style={{ ...text, fontWeight: 600 }}>{scope === 'realm' ? `Realm ${workspace?.pid ?? ''}` : `Zone ${workspace?.zid ?? ''}`}</span>
        <BeamButton size="small" variant="brand" onClick={() => openDeploy(scope)}>
          <BeamIcon slot="start" name="rocket" /> Deploy {scope} pins
        </BeamButton>
      </div>
      <BeamTable<{ name: string; checksum: string }>
        data={data}
        pageSize={10}
        pageSizeOptions={[10, 25, 50]}
        rowKey={(r) => r.name}
        emptyMessage={`No ${scope} bundles pinned in this workspace`}
      >
        <BeamColumn<{ name: string; checksum: string }>
          field="name"
          header="Bundle"
          sortable
          children={(row) => <span style={{ ...text, fontWeight: 600, fontSize: '13px' }}>{row.name}</span>}
        />
        <BeamColumn<{ name: string; checksum: string }>
          field="checksum"
          header="Pinned checksum"
          width="180px"
          children={(row) => <span title={row.checksum} style={QUIET_MONO}>{shortChecksum(row.checksum)}</span>}
        />
        <BeamColumn<{ name: string; checksum: string }>
          field="name"
          header="Tags"
          width="140px"
          children={(row) => {
            const tags = list?.published.find((p) => p.name === row.name)?.tags.filter((t) => t.checksum === row.checksum) ?? []
            return tags.length ? <span style={QUIET}>{tags.map((t) => t.tag).join(', ')}</span> : <span style={QUIET}>-</span>
          }}
        />
        <BeamColumn<{ name: string; checksum: string }>
          field="name"
          header=""
          width="96px"
          align="right"
          children={(row) => (
            <BeamButton size="small" appearance="plain" onClick={() => unpin(row.name)}>
              Unpin
            </BeamButton>
          )}
        />
      </BeamTable>
    </div>
  )

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
      {header}

      <BeamCallout variant="success">
        <BeamIcon slot="icon" name="plug" />
        <div style={{ ...ROW, justifyContent: 'space-between' }}>
          <span style={QUIET}>
            Connected to the CLI in <code style={mono}>{workspace?.workingDirectory}</code> as {workspace?.ownerEmail} ·
            realm <code style={mono}>{workspace?.pid}</code>
          </span>
          <BeamButton size="small" appearance="outlined" disabled={loading} onClick={refresh}>
            <BeamIcon slot="start" name="rotate" /> Refresh
          </BeamButton>
        </div>
      </BeamCallout>

      <BeamTabGroup
        active={activeTab}
        onWaTabShow={(e: CustomEvent) => {
          const name = (e.detail as { name?: string })?.name
          if (name && TABS.includes(name)) setStoredTab(name)
        }}
      >
        <BeamTab slot="nav" panel="bundles">Bundles</BeamTab>
        <BeamTab slot="nav" panel="pins">
          Pins
          {realmPins.length + zonePins.length > 0 && (
            <BeamBadge variant="neutral" appearance="filled" pill style={{ marginInlineStart: '0.375rem' }}>
              {realmPins.length + zonePins.length}
            </BeamBadge>
          )}
        </BeamTab>
        <BeamTab slot="nav" panel="releases">Releases</BeamTab>
        <BeamTab slot="nav" panel="activity">Activity</BeamTab>

        {/* ── BUNDLES ── */}
        <BeamTabPanel name="bundles">
          <div style={{ ...ROW, minHeight: '2.5rem', margin: '0.5rem 0' }}>
            <BeamInput
              size="small"
              appearance="outlined"
              value={search}
              onValueInput={setSearch}
              onValueChange={setSearch}
              placeholder="Search by name or component..."
              style={{ maxWidth: '22rem', flex: 1 }}
            />
            {loading && <BeamSpinner />}
          </div>
          <BeamTable<BundleRow>
            data={filteredRows}
            pageSize={10}
            pageSizeOptions={[10, 25, 50]}
            rowKey={(r) => r.name}
            emptyMessage={loading ? 'Loading…' : 'No bundles in this workspace or the catalog'}
          >
            <BeamColumn<BundleRow>
              field="name"
              header="Bundle"
              sortable
              width="24%"
              children={(row) => (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.125rem' }}>
                  <span style={{ ...text, fontWeight: 600, fontSize: '13px' }}>{shortName(row.name)}</span>
                  {row.name !== shortName(row.name) && <span style={QUIET_MONO}>{row.name}</span>}
                </div>
              )}
            />
            <BeamColumn<BundleRow>
              field="scope"
              header="Scope"
              width="80px"
              children={(row) => <BeamBadge variant="neutral" appearance="outlined" pill>{row.scope}</BeamBadge>}
            />
            <BeamColumn<BundleRow>
              field="local"
              header="Source"
              width="150px"
              children={(row) => (
                <div style={ROW}>
                  {row.local && <BeamBadge variant="brand" appearance="filled" pill>local</BeamBadge>}
                  {row.published ? (
                    <BeamBadge variant="success" appearance="filled" pill>v{row.latestVersion}</BeamBadge>
                  ) : (
                    <BeamBadge variant="warning" appearance="filled" pill>unpublished</BeamBadge>
                  )}
                </div>
              )}
            />
            <BeamColumn<BundleRow>
              field="dependencies"
              header="Dependencies"
              width="190px"
              children={(row) =>
                row.dependencies.length === 0 ? (
                  <span style={QUIET}>-</span>
                ) : (
                  <div style={{ display: 'flex', flexDirection: 'column', gap: '0.25rem', alignItems: 'flex-start' }}>
                    {row.dependencies.map((d) => (
                      <BeamBadge
                        key={d.name}
                        variant={d.satisfied ? 'neutral' : 'danger'}
                        appearance={d.satisfied ? 'outlined' : 'filled'}
                        pill
                        title={d.satisfied
                          ? `${d.name} ${d.range}: v${d.latestVersion} is published`
                          : `${d.name} ${d.range}: no release in range yet, publish it first`}
                      >
                        {shortName(d.name)} {d.range}{d.satisfied ? '' : ' · unpublished'}
                      </BeamBadge>
                    ))}
                  </div>
                )
              }
            />
            <BeamColumn<BundleRow>
              field="latestChecksum"
              header="Latest"
              width="170px"
              children={(row) => (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.125rem' }}>
                  <span title={row.latestChecksum} style={QUIET_MONO}>{shortChecksum(row.latestChecksum)}</span>
                  {row.tags.length > 0 && <span style={QUIET}>{row.tags.join(', ')}</span>}
                </div>
              )}
            />
            <BeamColumn<BundleRow>
              field="acl"
              header="ACL"
              width="80px"
              format={(_v, row) => aclLabel(row.acl)}
            />
            <BeamColumn<BundleRow>
              field="pinned"
              header="Pinned"
              width="150px"
              children={(row) =>
                row.pinned ? (
                  <BeamBadge
                    variant={row.pinned === row.latestChecksum ? 'success' : 'warning'}
                    appearance="filled"
                    pill
                    title={row.pinned}
                  >
                    {row.pinned === row.latestChecksum ? 'latest' : shortChecksum(row.pinned)}
                  </BeamBadge>
                ) : (
                  <span style={QUIET}>-</span>
                )
              }
            />
            <BeamColumn<BundleRow>
              field="name"
              header=""
              width="36px"
              align="right"
              children={(row) => (
                <BeamDropdown placement="bottom-end">
                  <BeamButton slot="trigger" appearance="plain" size="small">
                    <BeamIcon name="ellipsis-vertical" style={{ fontSize: '0.875rem' }} />
                  </BeamButton>
                  {row.local && (
                    <BeamDropdownItem onClick={() => openPublish(row.name)}>
                      <span style={MENU_ITEM}><BeamIcon name="upload" style={{ fontSize: '0.8rem' }} /> Plan &amp; publish</span>
                    </BeamDropdownItem>
                  )}
                  {row.published && (
                    <BeamDropdownItem onClick={() => setPinTarget({ name: row.name, tag: 'latest', checksum: '' })}>
                      <span style={MENU_ITEM}><BeamIcon name="thumbtack" style={{ fontSize: '0.8rem' }} /> {row.pinned ? 'Re-pin' : 'Pin'}</span>
                    </BeamDropdownItem>
                  )}
                  {row.pinned && (
                    <BeamDropdownItem onClick={() => unpin(row.name)}>
                      <span style={MENU_ITEM}><BeamIcon name="link-slash" style={{ fontSize: '0.8rem' }} /> Unpin</span>
                    </BeamDropdownItem>
                  )}
                  {row.published && (
                    <BeamDropdownItem onClick={() => loadReleases(row.name)}>
                      <span style={MENU_ITEM}><BeamIcon name="clock-rotate-left" style={{ fontSize: '0.8rem' }} /> Releases</span>
                    </BeamDropdownItem>
                  )}
                  {row.published && (
                    <BeamDropdownItem onClick={() => { setAclTarget(row.name); setAclScope('org') }}>
                      <span style={MENU_ITEM}><BeamIcon name="shield" style={{ fontSize: '0.8rem' }} /> Change ACL</span>
                    </BeamDropdownItem>
                  )}
                </BeamDropdown>
              )}
            />
          </BeamTable>
        </BeamTabPanel>

        {/* ── PINS ── */}
        <BeamTabPanel name="pins">
          <div style={{ ...STACK, gap: '1.5rem', marginTop: '0.5rem' }}>
            <span style={QUIET}>
              Pins live in <code style={mono}>.beamable/bundles.manifest.beam.json</code>. Deploying a scope plans
              and releases its manifest from this workspace, which also applies every local service of that scope.
            </span>
            {pinsTable('realm', realmPins)}
            {pinsTable('zone', zonePins)}
            <div>
              <BeamButton size="small" appearance="outlined" onClick={pruneYanked}>
                <BeamIcon slot="start" name="broom" /> Remove yanked pins
              </BeamButton>
            </div>
          </div>
        </BeamTabPanel>

        {/* ── RELEASES ── */}
        <BeamTabPanel name="releases">
          <div style={{ ...ROW, minHeight: '2.5rem', margin: '0.5rem 0' }}>
            <span style={QUIET}>Bundle:</span>
            <BeamDropdown placement="bottom-start">
              <BeamButton slot="trigger" appearance="outlined" size="small">
                {releasesBundle || 'Select a bundle'}
                <BeamIcon name="chevron-down" style={{ fontSize: '0.625rem', marginInlineStart: '0.25rem' }} />
              </BeamButton>
              {rows.filter((r) => r.published).map((r) => (
                <BeamDropdownItem key={r.name} onClick={() => loadReleases(r.name)}>{r.name}</BeamDropdownItem>
              ))}
            </BeamDropdown>
          </div>
          {releasesLoading ? (
            <div style={{ display: 'flex', justifyContent: 'center', padding: '2rem' }}>
              <BeamSpinner style={{ fontSize: '1.5rem' }} />
            </div>
          ) : (
            <BeamTable<Release>
              data={releases?.releases ?? []}
              pageSize={10}
              pageSizeOptions={[10, 25, 50]}
              rowKey={(r) => String(r.version)}
              emptyMessage={releasesBundle ? 'No releases' : 'Select a bundle to view its releases'}
            >
              <BeamColumn<Release> field="version" header="Version" width="80px" sortable format={(v) => `v${v}`} />
              <BeamColumn<Release>
                field="checksum"
                header="Checksum"
                width="170px"
                children={(row) => (
                  <div style={{ display: 'flex', flexDirection: 'column', gap: '0.125rem' }}>
                    <span title={row.checksum} style={QUIET_MONO}>{shortChecksum(row.checksum)}</span>
                    {releaseTagsByChecksum.has(row.checksum) && (
                      <span style={QUIET}>{releaseTagsByChecksum.get(row.checksum)!.join(', ')}</span>
                    )}
                  </div>
                )}
              />
              <BeamColumn<Release>
                field="comment"
                header="Comment"
                children={(row) =>
                  row.comment ? <span style={{ ...text, fontSize: '13px', whiteSpace: 'pre-wrap' }}>{row.comment}</span> : <span style={QUIET}>-</span>
                }
              />
              <BeamColumn<Release>
                field="publishedByAccountId"
                header="By"
                width="110px"
                format={(v) => (v && v !== '0' ? (String(v) === String(workspace?.ownerAccountId) ? workspace?.ownerEmail ?? String(v) : String(v)) : '-')}
              />
              <BeamColumn<Release>
                field="publishedAt"
                header="Published"
                width="120px"
                align="right"
                children={(row) =>
                  row.publishedAt ? (
                    <BeamRelativeTime date={new Date(row.publishedAt * 1000).toISOString()} numeric="auto" with-utc-popover style={QUIET} />
                  ) : (
                    <span style={QUIET}>--</span>
                  )
                }
              />
              <BeamColumn<Release>
                field="version"
                header=""
                width="36px"
                align="right"
                children={(row) => (
                  <BeamDropdown placement="bottom-end">
                    <BeamButton slot="trigger" appearance="plain" size="small">
                      <BeamIcon name="ellipsis-vertical" style={{ fontSize: '0.875rem' }} />
                    </BeamButton>
                    <BeamDropdownItem onClick={() => setPinTarget({ name: releasesBundle, tag: '', checksum: row.checksum })}>
                      <span style={MENU_ITEM}><BeamIcon name="thumbtack" style={{ fontSize: '0.8rem' }} /> Pin this release</span>
                    </BeamDropdownItem>
                    <BeamDropdownItem onClick={() => { setTagTarget({ name: releasesBundle, checksum: row.checksum }); setNewTag('') }}>
                      <span style={MENU_ITEM}><BeamIcon name="tag" style={{ fontSize: '0.8rem' }} /> Tag…</span>
                    </BeamDropdownItem>
                    <BeamDropdownItem onClick={() => setYankTarget({ name: releasesBundle, checksum: row.checksum })}>
                      <span style={{ ...MENU_ITEM, color: 'var(--color-beam-error)' }}><BeamIcon name="ban" style={{ fontSize: '0.8rem' }} /> Yank</span>
                    </BeamDropdownItem>
                  </BeamDropdown>
                )}
              />
            </BeamTable>
          )}
        </BeamTabPanel>

        {/* ── ACTIVITY ── */}
        <BeamTabPanel name="activity">
          <BeamTable<Job>
            data={jobs}
            pageSize={10}
            pageSizeOptions={[10, 25]}
            rowKey={(r) => r.id}
            emptyMessage="Nothing has run yet"
          >
            <BeamColumn<Job> field="status" header="Status" width="110px" children={(row) => <StatusBadge status={row.status} />} />
            <BeamColumn<Job> field="commandLine" header="Command" children={(row) => <span style={QUIET_MONO}>{row.commandLine}</span>} />
            <BeamColumn<Job>
              field="createdAt"
              header="Started"
              width="120px"
              align="right"
              children={(row) => <BeamRelativeTime date={new Date(row.createdAt).toISOString()} numeric="auto" style={QUIET} />}
            />
            <BeamColumn<Job>
              field="id"
              header=""
              width="36px"
              align="right"
              children={(row) => (
                <BeamButton appearance="plain" size="small" onClick={() => setJsonData(row)}>
                  <BeamIcon name="code" style={{ fontSize: '0.8rem' }} />
                </BeamButton>
              )}
            />
          </BeamTable>
        </BeamTabPanel>
      </BeamTabGroup>

      {/* ── Plan & publish ── */}
      <BeamDialog
        open={publishTarget != null}
        label={`Publish ${publishTarget ? shortName(publishTarget) : ''}`}
        with-footer
        onWaHide={(e: Event) => { if (e.target === e.currentTarget && !busy) setPublishTarget(null) }}
        style={{ '--width': '52rem' } as CSSProperties}
      >
        <div style={STACK}>
          {blockedBy.length > 0 && (
            <BeamCallout variant="warning">
              <BeamIcon slot="icon" name="triangle-exclamation" />
              <div style={STACK}>
                <span style={text}>
                  {shortName(publishTarget ?? '')} depends on bundles that have no published release in range yet.
                  Publish them first, then come back to this one.
                </span>
                {blockedBy.map((d) => {
                  const localDep = rows.find((r) => r.local && shortName(r.name) === shortName(d.name))
                  return (
                    <div key={d.name} style={{ ...ROW, justifyContent: 'space-between' }}>
                      <span style={{ ...text, ...mono, fontSize: '12px' }}>
                        {d.name} {d.range}
                        <span style={QUIET}>{d.latestVersion ? ` (latest v${d.latestVersion})` : ' (never published)'}</span>
                      </span>
                      {localDep && (
                        <BeamButton size="small" appearance="outlined" onClick={() => openPublish(localDep.name)}>
                          <BeamIcon slot="start" name="upload" /> Plan &amp; publish {shortName(d.name)}
                        </BeamButton>
                      )}
                    </div>
                  )
                })}
              </div>
            </BeamCallout>
          )}
          {blockedBy.length === 0 && <span style={{ ...text, fontWeight: 600 }}>1. Plan</span>}
          <JobLogView job={planJob} />
          {plan?.isNoOp && <BeamCallout variant="neutral">Nothing to publish: the bundle matches its latest release.</BeamCallout>}
          {plan && !plan.isNoOp && (
            <BeamTable<PlanResult['changes'][number]>
              data={plan.changes}
              pageSize={10}
              rowKey={(r) => `${r.kind}:${r.name}`}
              emptyMessage="No component changes"
            >
              <BeamColumn field="change" header="Change" width="100px" />
              <BeamColumn field="kind" header="Kind" width="100px" />
              <BeamColumn field="name" header="Component" />
              <BeamColumn
                field="localChecksum"
                header="Checksum"
                width="170px"
                children={(row: PlanResult['changes'][number]) => (
                  <span style={QUIET_MONO}>{shortChecksum(row.localChecksum || row.publishedChecksum)}</span>
                )}
              />
            </BeamTable>
          )}
          {plan && !plan.isNoOp && (
            <>
              <span style={{ ...text, fontWeight: 600 }}>2. Publish</span>
              <BeamTextarea
                label="Comment"
                placeholder="What changed in this release"
                rows={3}
                value={comment}
                onValueInput={setComment}
                onValueChange={setComment}
              />
              <div style={ROW}>
                <BeamInput
                  size="small"
                  label="Extra tag"
                  placeholder="e.g. stable (latest always moves)"
                  value={tag}
                  onValueChange={setTag}
                  style={{ flex: 1, minWidth: '12rem' }}
                />
                <BoundSelect size="small" label="Widen ACL" defaultValue="" onChange={(v: string) => setAcl(v as Acl)} style={{ width: '12rem' }}>
                  <BeamOption value="">Keep</BeamOption>
                  <BeamOption value="private">Private</BeamOption>
                  <BeamOption value="org">Whole org</BeamOption>
                  <BeamOption value="public">Public</BeamOption>
                </BoundSelect>
              </div>
              <JobLogView job={publishJob} />
              {published && (
                <BeamCallout variant="success">
                  Published {published.name} v{published.version} ({shortChecksum(published.checksum)})
                </BeamCallout>
              )}
            </>
          )}
        </div>
        <div slot="footer" style={{ ...ROW, justifyContent: 'flex-end' }}>
          <BeamButton appearance="outlined" size="small" disabled={busy} onClick={() => setPublishTarget(null)}>
            {published ? 'Close' : 'Cancel'}
          </BeamButton>
          {plan && !plan.isNoOp && !published && (
            <BeamButton size="small" variant="brand" disabled={busy} onClick={publish}>
              {busy ? 'Publishing…' : 'Publish'}
            </BeamButton>
          )}
        </div>
      </BeamDialog>

      {/* ── Deploy pins ── */}
      <BeamDialog
        open={deployScope != null}
        label={`Deploy ${deployScope ?? ''} pins`}
        with-footer
        onWaHide={(e: Event) => { if (e.target === e.currentTarget && !busy) setDeployScope(null) }}
        style={{ '--width': '52rem' } as CSSProperties}
      >
        <div style={STACK}>
          <BeamCallout variant="warning">
            This plans the {deployScope} manifest from this workspace: its pins and every local {deployScope}-scoped
            service, storage and extension. Anything deployed that is not in the workspace is removed.
          </BeamCallout>
          <BeamTextarea
            label="Comment"
            placeholder="Shown with the deployed manifest"
            rows={2}
            value={deployComment}
            onValueInput={setDeployComment}
            onValueChange={setDeployComment}
          />
          <JobLogView job={deployPlanJob} />
          {deployPlan && (
            <div style={ROW}>
              <span style={text}>{deployPlan.plan?.changeCount ?? 0} change(s) planned.</span>
              <BeamButton size="small" appearance="plain" onClick={() => setJsonData(deployPlan.plan)}>
                <BeamIcon slot="start" name="code" /> View plan
              </BeamButton>
            </div>
          )}
          <JobLogView job={deployReleaseJob} />
        </div>
        <div slot="footer" style={{ ...ROW, justifyContent: 'flex-end' }}>
          <BeamButton appearance="outlined" size="small" disabled={busy} onClick={() => setDeployScope(null)}>
            Close
          </BeamButton>
          <BeamButton size="small" appearance={deployPlan ? 'outlined' : 'filled'} variant="brand" disabled={busy} onClick={planDeploy}>
            {deployPlan ? 'Re-plan' : 'Plan'}
          </BeamButton>
          {deployPlan && deployReleaseJob?.status !== 'succeeded' && (
            <BeamButton size="small" variant="danger" disabled={busy} onClick={releaseDeploy}>
              {busy ? 'Deploying…' : 'Deploy'}
            </BeamButton>
          )}
        </div>
      </BeamDialog>

      {/* ── Pin ── */}
      <BeamDialog
        open={pinTarget != null}
        label={`Pin ${pinTarget?.name ?? ''}`}
        with-footer
        onWaHide={(e: Event) => { if (e.target === e.currentTarget) setPinTarget(null) }}
        style={{ '--width': '28rem' } as CSSProperties}
      >
        {pinTarget && (
          pinTarget.checksum ? (
            <p style={{ ...QUIET, margin: 0 }}>Pin release <code style={mono}>{shortChecksum(pinTarget.checksum)}</code>.</p>
          ) : (
            <BeamInput
              size="small"
              label="Tag to resolve"
              value={pinTarget.tag}
              onValueChange={(v: string) => setPinTarget({ ...pinTarget, tag: v })}
            />
          )
        )}
        <div slot="footer" style={{ ...ROW, justifyContent: 'flex-end' }}>
          <BeamButton appearance="outlined" size="small" onClick={() => setPinTarget(null)}>Cancel</BeamButton>
          <BeamButton size="small" variant="brand" disabled={busy} onClick={confirmPin}>{busy ? 'Pinning…' : 'Pin'}</BeamButton>
        </div>
      </BeamDialog>

      {/* ── Tag ── */}
      <BeamDialog
        open={tagTarget != null}
        label="Tag release"
        with-footer
        onWaHide={(e: Event) => { if (e.target === e.currentTarget) setTagTarget(null) }}
        style={{ '--width': '28rem' } as CSSProperties}
      >
        <BeamInput size="small" label="Tag" placeholder="e.g. stable" value={newTag} onValueChange={setNewTag} />
        <div slot="footer" style={{ ...ROW, justifyContent: 'flex-end' }}>
          <BeamButton appearance="outlined" size="small" onClick={() => setTagTarget(null)}>Cancel</BeamButton>
          <BeamButton size="small" variant="brand" disabled={busy || !newTag.trim()} onClick={confirmTag}>Tag</BeamButton>
        </div>
      </BeamDialog>

      {/* ── ACL ── */}
      <BeamDialog
        open={aclTarget != null}
        label="Change bundle ACL"
        with-footer
        onWaHide={(e: Event) => { if (e.target === e.currentTarget) setAclTarget(null) }}
        style={{ '--width': '28rem' } as CSSProperties}
      >
        <p style={{ ...QUIET, marginTop: 0 }}>
          Visibility of <strong style={text}>{aclTarget}</strong> applies to every version published under the name.
        </p>
        <div style={ROW}>
          {(['private', 'org', 'public'] as const).map((opt) => (
            <BeamButton
              key={opt}
              size="small"
              appearance={aclScope === opt ? 'filled' : 'outlined'}
              variant={aclScope === opt ? 'brand' : 'neutral'}
              onClick={() => setAclScope(opt)}
            >
              {opt === 'org' ? 'Whole org' : opt[0].toUpperCase() + opt.slice(1)}
            </BeamButton>
          ))}
        </div>
        <div slot="footer" style={{ ...ROW, justifyContent: 'flex-end' }}>
          <BeamButton appearance="outlined" size="small" onClick={() => setAclTarget(null)}>Cancel</BeamButton>
          <BeamButton size="small" variant="brand" disabled={busy} onClick={confirmAcl}>Save</BeamButton>
        </div>
      </BeamDialog>

      <BeamConfirmDialog
        open={yankTarget != null}
        label="Yank release"
        message={`Yank ${yankTarget?.name ?? ''} at ${shortChecksum(yankTarget?.checksum ?? '')}? New references to this checksum are blocked; deploys already pinned to it keep working.`}
        confirm-label="Yank"
        confirm-variant="danger"
        onWaConfirm={confirmYank}
        onWaCancel={() => setYankTarget(null)}
      />

      <BeamDialog
        open={jsonData != null}
        label="Details"
        light-dismiss
        with-footer
        onWaHide={(e: Event) => { if (e.target === e.currentTarget) setJsonData(null) }}
        style={{ '--width': '48rem' } as CSSProperties}
      >
        {/* `BeamJson` isn't exposed by the host portal's React component registry (rendering it crashes the
            page), so a plain `<pre>` stands in. */}
        <pre style={{
          ...text,
          ...mono,
          fontSize: '12px',
          maxHeight: 'calc(80vh - 8rem)',
          overflow: 'auto',
          margin: 0,
          padding: '12px',
          background: 'var(--color-beam-surface)',
          borderRadius: '6px',
          whiteSpace: 'pre-wrap',
          wordBreak: 'break-word',
        }}>
          {jsonText}
        </pre>
        <div slot="footer" style={{ ...ROW, justifyContent: 'flex-end' }}>
          <BeamCopyButton value={jsonText} copy-label="Copy" success-label="Copied!" />
        </div>
      </BeamDialog>

      <Toaster />
    </div>
  )
}
