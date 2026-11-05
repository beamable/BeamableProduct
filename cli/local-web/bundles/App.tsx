import { Fragment, useEffect, useMemo, useRef, useState, type CSSProperties } from 'react'
import {
  BeamBadge,
  BeamButton,
  BeamCallout,
  BeamCheckbox,
  BeamColumn,
  BeamCopyButton,
  BeamConfirmDialog,
  BeamDialog,
  BeamDropdown,
  BeamDropdownItem,
  BeamIcon,
  BeamInput,
  BeamOption,
  BeamProgressBar,
  BeamRelativeTime,
  BeamSpinner,
  BeamTab,
  BeamTabGroup,
  BeamTabPanel,
  BeamTable,
} from '../shared/beam'
import { BoundSelect, errorMessage, mono, text, textMuted, toast, useSessionState } from '../shared/ui'
import { ApiError, sessionToken } from '../shared/connection'
import {
  CliClient,
  jobError,
  jobResult,
  type AcquireResult,
  type BundleNotice,
  type HistoryEntry,
  type HistoryResult,
  type InstallResult,
  type Installation,
  type InstallationPlan,
  type InstallationTarget,
  type InstallationsResult,
  type Job,
  type JobProgress,
  type JobParams,
  type LibraryEntry,
  type LibraryResult,
  type ListResult,
  type LocalBundle,
  type PlanResult,
  type PublishResult,
  type PublishedBundle,
  type Release,
  type ReleaseContent,
  type ReleasesResult,
  type SiteResult,
  type Workspace,
} from './cliApi'

const TABS = ['bundles', 'installations', 'releases', 'activity']

const CLI_COMMAND = 'beam local management'

const QUIET: CSSProperties = { ...textMuted, fontSize: '12px' }
const QUIET_MONO: CSSProperties = { ...textMuted, ...mono, fontSize: '11px' }
const ROW: CSSProperties = { display: 'flex', alignItems: 'center', gap: '0.5rem', flexWrap: 'wrap' }
const STACK: CSSProperties = { display: 'flex', flexDirection: 'column', gap: '0.75rem' }
const COLUMN: CSSProperties = { display: 'flex', flexDirection: 'column', gap: '0.125rem' }
const MENU_ITEM: CSSProperties = { display: 'flex', alignItems: 'center', gap: '0.5rem' }
const STRONG: CSSProperties = { ...text, fontWeight: 600, fontSize: '13px' }

/** One row of the Bundles tab: a bundle authored here, in the org's catalog, or both. */
interface BundleRow {
  name: string
  local: LocalBundle | null
  published: PublishedBundle | null
}

/** `@ns/name` → `name`. */
function shortName(name: string): string {
  const slash = name.indexOf('/')
  return name.startsWith('@') && slash > 0 ? name.slice(slash + 1) : name
}

function describeTarget(t: InstallationTarget | null): string {
  if (!t) return '-'
  const groups = t.componentGroups.length ? t.componentGroups.join(', ') : 'no component groups'
  return `v${t.version} · ${groups} · realms ${t.realms} · auto-update ${t.autoUpdate ? 'on' : 'off'}`
}

function componentSummary(row: BundleRow): string {
  const components = row.local?.components ?? []
  if (components.length === 0) return '-'
  const bySite = (site: string) => components.filter((c) => c.site === site).length
  const parts = [`${bySite('realm')} realm`, `${bySite('zone')} zone`].filter((p) => !p.startsWith('0 '))
  return `${components.length} (${parts.join(' · ') || 'unknown site'})`
}

type Variant = 'neutral' | 'brand' | 'success' | 'danger' | 'warning'

const STATUS_VARIANT: Record<string, Variant> = {
  queued: 'neutral',
  running: 'brand',
  succeeded: 'success',
  failed: 'danger',
  cancelled: 'warning',
}

/** Where a version stands in its notices: supported, deprecated, unsupported (no new installs) or end of life. */
const SUPPORT_VARIANT: Record<string, Variant> = {
  supported: 'success',
  deprecated: 'warning',
  unsupported: 'danger',
  endoflife: 'danger',
}

const SITE_VARIANT: Record<string, Variant> = {
  applied: 'success',
  pending: 'warning',
  failed: 'danger',
}

function StatusBadge({ status }: { status: string }) {
  return (
    <BeamBadge variant={STATUS_VARIANT[status] ?? 'neutral'} appearance="filled" pill>
      {status}
    </BeamBadge>
  )
}

function NoticeLine({ notice }: { notice: BundleNotice }) {
  return (
    <div style={ROW}>
      <BeamBadge variant={SUPPORT_VARIANT[notice.status] ?? 'neutral'} appearance="filled" pill>{notice.status}</BeamBadge>
      <span style={QUIET}>
        v{notice.minVersion}–v{notice.maxVersion}: end of support {notice.endOfSupport.slice(0, 10)}, end of life{' '}
        {notice.endOfLife.slice(0, 10)}
        {notice.replacement ? `, use ${notice.replacement}` : ''}
        {notice.message ? `. ${notice.message}` : ''}
      </span>
    </div>
  )
}

function SiteResults({ results }: { results: SiteResult[] }) {
  if (results.length === 0) return <span style={QUIET}>-</span>
  return (
    <div style={COLUMN}>
      {results.map((r) => (
        <div key={r.site} style={ROW} title={r.error ?? r.manifestId ?? ''}>
          <BeamBadge variant={SITE_VARIANT[r.state] ?? 'neutral'} appearance="outlined" pill>{r.state}</BeamBadge>
          <span style={QUIET_MONO}>{r.site}</span>
        </div>
      ))}
    </div>
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

/** Overall progress of plan and publish: every step they reported so far, weighted equally. */
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

/** A list of messages in a callout, or nothing when there are none. */
function Messages({ variant, title, items }: { variant: Variant; title: string; items: string[] }) {
  if (items.length === 0) return null
  return (
    <BeamCallout variant={variant}>
      <div style={{ ...STACK, gap: '0.25rem' }}>
        <span style={STRONG}>{title}</span>
        {items.map((m, i) => <span key={i} style={{ ...text, fontSize: '13px' }}>{m}</span>)}
      </div>
    </BeamCallout>
  )
}

/** An installation plan as the server made it: what changes, on which manifest, and what stands in the way. */
function InstallationPlanView({ plan }: { plan: InstallationPlan }) {
  const sites = [...plan.sites].sort((a, b) => Number(b.isZone) - Number(a.isZone))
  return (
    <div style={STACK}>
      <div style={COLUMN}>
        <span style={STRONG}>{plan.change || 'Change'}</span>
        {plan.from && <span style={QUIET}>Now: {describeTarget(plan.from)}</span>}
        {plan.to && (
          <span style={QUIET}>
            After: {describeTarget(plan.to)}
            {plan.autoUpdateLocked ? ' (auto-update required by the publisher)' : ''}
          </span>
        )}
        {plan.compatible === false && (
          <span style={{ ...QUIET, color: 'var(--color-beam-warning)' }}>
            This upgrade is breaking for at least one component, or changes data irreversibly.
          </span>
        )}
      </div>
      <Messages variant="danger" title="Refused" items={plan.refusals} />
      <Messages variant="danger" title="Blocked" items={plan.blockers} />
      <Messages variant="warning" title="Warnings" items={plan.warnings} />
      {sites.map((s) => (
        <div key={s.site} style={COLUMN}>
          <span style={{ ...text, fontSize: '13px' }}>
            {s.isZone ? 'Zone' : 'Realm'} <code style={mono}>{s.site}</code>
            {s.changes.length === 0 && <span style={QUIET}> · no changes</span>}
          </span>
          {s.changes.map((c) => <span key={c} style={{ ...QUIET_MONO, paddingInlineStart: '1rem' }}>{c}</span>)}
        </div>
      ))}
    </div>
  )
}

/** What the install dialog changes. A release of '' keeps the default (latest, or the installed one on a change). */
interface InstallForm {
  bundle: string
  current: Installation | null
  release: string
  /** null keeps the defaults on a new install, and the installed ones on a change. */
  componentGroups: string[] | null
  realms: '' | 'all' | 'none'
  autoUpdate: '' | 'on' | 'off'
}

function installParams(form: InstallForm): JobParams {
  const params: JobParams = { bundle: form.bundle }
  if (form.release.startsWith('v:')) params.version = form.release.slice(2)
  if (form.release.startsWith('t:')) params.tag = form.release.slice(2)
  if (form.componentGroups) params.componentGroups = form.componentGroups
  if (form.realms) params.realms = form.realms
  if (form.autoUpdate) params.autoUpdate = form.autoUpdate === 'on'
  return params
}

export default function App() {

  const [storedTab, setStoredTab] = useSessionState('bundles:tab', 'bundles')
  const activeTab = TABS.includes(storedTab) ? storedTab : 'bundles'

  // ── Connection to the CLI ──
  const [client, setClient] = useState<CliClient | null>(null)
  const [connecting, setConnecting] = useState(true)
  const [connectError, setConnectError] = useState('')
  const [unavailable, setUnavailable] = useState(false)
  const [workspace, setWorkspace] = useState<Workspace | null>(null)

  async function connect() {
    setConnecting(true)
    setConnectError('')
    setUnavailable(false)
    setClient(null)
    try {
      // The CLI opens this page with its session token in the fragment.
      if (!sessionToken()) return
      const cli = new CliClient()
      setWorkspace(await cli.getWorkspace())
      setClient(cli)
    } catch (err) {
      setUnavailable(err instanceof ApiError && err.status === 409)
      setConnectError(errorMessage(err, 'Could not reach the CLI. Is `beam local management` still running?'))
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

  // ── Bundles, library, installations ──
  const [list, setList] = useState<ListResult | null>(null)
  const [library, setLibrary] = useState<LibraryEntry[]>([])
  const [installations, setInstallations] = useState<Installation[]>([])
  const [loading, setLoading] = useState(false)

  async function refresh() {
    if (!client) return
    setLoading(true)
    try {
      const [ws, listed, lib, installed] = await Promise.all([
        client.getWorkspace(),
        run<ListResult>('list'),
        run<LibraryResult>('library'),
        run<InstallationsResult>('installations'),
      ])
      setWorkspace(ws)
      if (listed) setList(listed)
      if (lib) setLibrary(lib.bundles)
      if (installed) setInstallations(installed.installations)
    } catch (err) {
      toast.error(errorMessage(err, 'Failed to refresh'))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { refresh() }, [client])

  const rows = useMemo<BundleRow[]>(() => {
    const byName = new Map<string, BundleRow>()
    for (const p of list?.published ?? []) byName.set(p.name, { name: p.name, local: null, published: p })
    for (const l of list?.local ?? []) {
      // Local names are namespaced only when the CLI could resolve the customer alias; match on the short name.
      const existing = [...byName.values()].find((r) => shortName(r.name) === shortName(l.name))
      if (existing) existing.local = l
      else byName.set(l.name, { name: l.name, local: l, published: null })
    }
    return [...byName.values()].sort((a, b) => shortName(a.name).localeCompare(shortName(b.name)))
  }, [list])

  const [search, setSearch] = useState('')
  const filteredRows = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return rows
    return rows.filter((r) => r.name.toLowerCase().includes(q) || (r.local?.components ?? []).some((c) => c.name.toLowerCase().includes(q)))
  }, [rows, search])

  // ── Create ──
  const [createTarget, setCreateTarget] = useState<string | null>(null)
  const [createVisibility, setCreateVisibility] = useState('private')

  async function confirmCreate() {
    if (!createTarget) return
    setBusy(true)
    const done = await run('create', { bundle: shortName(createTarget), visibility: createVisibility })
    setBusy(false)
    if (done) {
      toast.success(`Created ${shortName(createTarget)}`)
      setCreateTarget(null)
      await refresh()
    }
  }

  // ── Visibility ──
  const [visibilityTarget, setVisibilityTarget] = useState<PublishedBundle | null>(null)

  async function confirmVisibility() {
    if (!visibilityTarget) return
    const target = visibilityTarget
    const visibility = target.visibility === 'public' ? 'private' : 'public'
    setVisibilityTarget(null)
    if (await run('visibility', { bundle: target.name, visibility })) {
      toast.success(`${shortName(target.name)} is ${visibility}`)
      await refresh()
    }
  }

  // ── Plan & publish ──
  const [publishTarget, setPublishTarget] = useState<string | null>(null)
  const [planJob, setPlanJob] = useState<Job | null>(null)
  const [plan, setPlan] = useState<PlanResult | null>(null)
  const [publishJob, setPublishJob] = useState<Job | null>(null)
  const [published, setPublished] = useState<PublishResult | null>(null)
  const [tag, setTag] = useState('')

  async function openPublish(name: string) {
    setPublishTarget(name)
    setPlan(null)
    setPlanJob(null)
    setPublishJob(null)
    setPublished(null)
    setTag('')
    setBusy(true)
    setPlan(await run<PlanResult>('plan', { bundle: shortName(name) }, setPlanJob))
    setBusy(false)
  }

  async function publish() {
    if (!publishTarget) return
    setBusy(true)
    const result = await run<PublishResult>(
      'publish',
      { bundle: shortName(publishTarget), tag: tag.trim(), flag: true },
      setPublishJob,
    )
    setBusy(false)
    if (result) {
      setPublished(result)
      toast.success(result.isNewVersion ? `Published ${result.name} v${result.version}` : `${result.name} v${result.version} already has this content`)
      await refresh()
    }
  }

  // ── Install, change and uninstall ──
  const [installForm, setInstallForm] = useState<InstallForm | null>(null)
  const [uninstall, setUninstall] = useState(false)
  const [installReleases, setInstallReleases] = useState<ReleasesResult | null>(null)
  const [installContent, setInstallContent] = useState<ReleaseContent | null>(null)
  const [installPlan, setInstallPlan] = useState<InstallationPlan | null>(null)
  const [plannedParams, setPlannedParams] = useState<JobParams | null>(null)
  const [installJob, setInstallJob] = useState<Job | null>(null)
  const [installResult, setInstallResult] = useState<InstallResult | null>(null)

  function resetInstallPlan() {
    setInstallPlan(null)
    setPlannedParams(null)
    setInstallJob(null)
    setInstallResult(null)
  }

  async function openInstall(bundle: string, isUninstall = false) {
    const current = installations.find((i) => i.bundle === bundle) ?? null
    setUninstall(isUninstall)
    setInstallForm({ bundle, current, release: '', componentGroups: null, realms: '', autoUpdate: '' })
    setInstallReleases(null)
    setInstallContent(null)
    resetInstallPlan()
    if (isUninstall) {
      await planInstall({ bundle }, true)
      return
    }
    const releases = await run<ReleasesResult>('releases', { bundle })
    setInstallReleases(releases)
    const version = current?.target?.version ?? releases?.tags.find((t) => t.tag === 'latest')?.version ?? 0
    if (version) await loadContent(bundle, version)
  }

  async function loadContent(bundle: string, version: number) {
    const result = await run<ReleasesResult>('releases', { bundle, version: String(version) })
    setInstallContent(result?.content ?? null)
  }

  function updateForm(change: Partial<InstallForm>) {
    if (!installForm) return
    const next = { ...installForm, ...change }
    setInstallForm(next)
    resetInstallPlan()
    if (change.release !== undefined) {
      const version = next.release.startsWith('v:')
        ? Number(next.release.slice(2))
        : next.release.startsWith('t:')
          ? installReleases?.tags.find((t) => t.tag === next.release.slice(2))?.version ?? 0
          : next.current?.target?.version ?? installReleases?.tags.find((t) => t.tag === 'latest')?.version ?? 0
      if (version && version !== installContent?.version) loadContent(next.bundle, version)
    }
  }

  async function planInstall(params: JobParams, isUninstall = uninstall) {
    setBusy(true)
    resetInstallPlan()
    const result = await run<InstallResult>(isUninstall ? 'uninstall' : 'install', { ...params, planOnly: true }, setInstallJob)
    setBusy(false)
    if (result?.plan) {
      setInstallPlan(result.plan)
      setPlannedParams(params)
    }
  }

  async function applyInstall() {
    if (!installPlan || !plannedParams) return
    setBusy(true)
    const result = await run<InstallResult>(
      uninstall ? 'uninstall' : 'install',
      { ...plannedParams, planHash: installPlan.hash },
      setInstallJob,
    )
    setBusy(false)
    if (result) {
      setInstallResult(result)
      if (result.applied) {
        const pending = result.results.some((r) => r.state !== 'applied')
        if (pending) toast.error('Not every manifest was applied. Plan and apply again to retry the ones left.')
        else toast.success(`${uninstall ? 'Uninstalled' : 'Applied'} ${shortName(installPlan.bundle)}`)
      }
      await refresh()
    }
  }

  const planChanges = installPlan != null && (installPlan.sites.some((s) => s.changes.length > 0) || installPlan.change === 'AutoUpdate')
  const canApply = installPlan != null && planChanges && installPlan.refusals.length === 0 && installPlan.blockers.length === 0 && !installResult?.applied

  // ── Installation history ──
  const [historyTarget, setHistoryTarget] = useState<string | null>(null)
  const [history, setHistory] = useState<HistoryEntry[] | null>(null)

  async function openHistory(bundle: string) {
    setHistoryTarget(bundle)
    setHistory(null)
    const result = await run<HistoryResult>('history', { bundle })
    setHistory(result?.entries ?? [])
  }

  // ── Library ──
  const [acquireName, setAcquireName] = useState('')
  const [acquireConfirm, setAcquireConfirm] = useState<string | null>(null)
  const [acquireResult, setAcquireResult] = useState<AcquireResult | null>(null)

  async function confirmAcquire() {
    if (!acquireConfirm) return
    const name = acquireConfirm
    setAcquireConfirm(null)
    setBusy(true)
    const result = await run<AcquireResult>('acquire', { bundle: name })
    setBusy(false)
    if (!result) return
    if (result.acquired) {
      toast.success(`${result.name} is in your org's library`)
      setAcquireName('')
      setAcquireResult(null)
      await refresh()
    } else {
      setAcquireResult(result)
    }
  }

  const [removeTarget, setRemoveTarget] = useState<string | null>(null)

  async function confirmRemove() {
    if (!removeTarget) return
    const name = removeTarget
    setRemoveTarget(null)
    if (await run('remove', { bundle: name })) {
      toast.success(`Removed ${name} from your org's library`)
      await refresh()
    }
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

  const [tagTarget, setTagTarget] = useState<{ name: string; version: number } | null>(null)
  const [newTag, setNewTag] = useState('')

  async function confirmTag() {
    if (!tagTarget) return
    setBusy(true)
    const done = await run('tag', { bundle: tagTarget.name, tag: newTag.trim(), version: String(tagTarget.version) })
    setBusy(false)
    if (done) {
      toast.success(`Tagged v${tagTarget.version} as ${newTag.trim()}`)
      setTagTarget(null)
      await Promise.all([loadReleases(tagTarget.name), refresh()])
    }
  }

  const [yankTarget, setYankTarget] = useState<{ name: string; version: number } | null>(null)

  async function confirmYank() {
    if (!yankTarget) return
    const target = yankTarget
    setYankTarget(null)
    if (await run('yank', { bundle: target.name, version: String(target.version) })) {
      toast.success(`Yanked v${target.version}`)
      await loadReleases(target.name)
    }
  }

  async function showContent(name: string, version: number) {
    const result = await run<ReleasesResult>('releases', { bundle: name, version: String(version) })
    if (result?.content) setJsonData(result.content)
  }

  const [jsonData, setJsonData] = useState<unknown>(null)
  // A job's result arrives as a JSON string; show it parsed so it reads like the rest.
  const jsonText = jsonData == null ? '' : JSON.stringify(jsonData, (key, value) => {
    if (key !== 'resultJson' || typeof value !== 'string') return value
    try { return jobResult({ resultJson: value } as Job) } catch { return value }
  }, 2)

  // ── Render ──

  if (connecting) {
    return (
      <div style={STACK}>
        <div style={{ display: 'flex', justifyContent: 'center', padding: '4rem' }}>
          <BeamSpinner style={{ fontSize: '2rem' }} />
        </div>
      </div>
    )
  }

  if (!client && unavailable) {
    return (
      <div style={STACK}>
        <BeamCallout variant="warning">
          <BeamIcon slot="icon" name="triangle-exclamation" />
          <div style={STACK}>
            <span style={text}>{connectError}</span>
            <div>
              <BeamButton size="small" appearance="outlined" onClick={connect}>
                <BeamIcon slot="start" name="rotate" /> Retry
              </BeamButton>
            </div>
          </div>
        </BeamCallout>
      </div>
    )
  }

  if (!client) {
    return (
      <div style={STACK}>
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
      </div>
    )
  }

  const zoneId = workspace?.zid ?? ''
  const installedHere = new Set(installations.map((i) => i.bundle))
  const tagsByVersion = new Map<number, string[]>()
  for (const t of releases?.tags ?? []) tagsByVersion.set(t.version, [...(tagsByVersion.get(t.version) ?? []), t.tag])
  const ownPublished = rows.filter((r) => r.published)
  const contentGroups = Object.keys(installContent?.componentGroups ?? {})
  const pickedGroups = installForm?.componentGroups ?? installForm?.current?.target?.componentGroups ?? []

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>

      <BeamCallout variant="success">
        <BeamIcon slot="icon" name="plug" />
        <div style={{ ...ROW, justifyContent: 'space-between' }}>
          <span style={QUIET}>
            Connected to the CLI in <code style={mono}>{workspace?.workingDirectory}</code> as {workspace?.ownerEmail} ·
            zone <code style={mono}>{zoneId}</code> · realm <code style={mono}>{workspace?.pid}</code>
          </span>
          <BeamButton size="small" appearance="outlined" disabled={loading} onClick={refresh}>
            <BeamIcon slot="start" name="rotate" /> Refresh
          </BeamButton>
        </div>
      </BeamCallout>

      <BeamTabGroup
        active={activeTab}
        onChange={(name) => { if (TABS.includes(name)) setStoredTab(name) }}
      >
        <BeamTab slot="nav" panel="bundles">Bundles</BeamTab>
        <BeamTab slot="nav" panel="installations">
          Installations
          {installations.length > 0 && (
            <BeamBadge variant="neutral" appearance="filled" pill style={{ marginInlineStart: '0.375rem' }}>
              {installations.length}
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
            emptyMessage={loading ? 'Loading…' : 'No bundles in this workspace or your org\'s catalog'}
          >
            <BeamColumn<BundleRow>
              field="name"
              header="Bundle"
              sortable
              width="24%"
              children={(row) => (
                <div style={COLUMN}>
                  <span style={STRONG}>{shortName(row.name)}</span>
                  {row.name !== shortName(row.name) && <span style={QUIET_MONO}>{row.name}</span>}
                </div>
              )}
            />
            <BeamColumn<BundleRow>
              field="local"
              header="Components"
              width="150px"
              children={(row) => (
                <span
                  style={QUIET}
                  title={(row.local?.components ?? []).map((c) => `${c.name} (${c.kind}, ${c.site}${c.source ? ', source' : ''})`).join('\n')}
                >
                  {componentSummary(row)}
                </span>
              )}
            />
            <BeamColumn<BundleRow>
              field="published"
              header="Release"
              width="170px"
              children={(row) => (
                <div style={COLUMN}>
                  <div style={ROW}>
                    {row.local && <BeamBadge variant="brand" appearance="filled" pill>local</BeamBadge>}
                    {!row.published && <BeamBadge variant="warning" appearance="filled" pill>not created</BeamBadge>}
                    {row.published && row.published.latestVersion === 0 && <BeamBadge variant="warning" appearance="filled" pill>no release</BeamBadge>}
                    {row.published && row.published.latestVersion > 0 && (
                      <BeamBadge variant="success" appearance="filled" pill>v{row.published.latestVersion}</BeamBadge>
                    )}
                  </div>
                  {row.published && row.published.tags.length > 0 && (
                    <span style={QUIET}>{row.published.tags.map((t) => `${t.tag} v${t.version}`).join(', ')}</span>
                  )}
                </div>
              )}
            />
            <BeamColumn<BundleRow>
              field="published"
              header="Visibility"
              width="90px"
              format={(_v, row) => row.published?.visibility || '-'}
            />
            <BeamColumn<BundleRow>
              field="local"
              header="Dependencies"
              width="190px"
              children={(row) => {
                const deps = Object.entries(row.local?.dependencies ?? {})
                return deps.length === 0 ? (
                  <span style={QUIET}>-</span>
                ) : (
                  <div style={{ display: 'flex', flexDirection: 'column', gap: '0.25rem', alignItems: 'flex-start' }}>
                    {deps.map(([name, range]) => (
                      <BeamBadge key={name} variant="neutral" appearance="outlined" pill title={`${name} ${range}`}>
                        {shortName(name)} {range}
                      </BeamBadge>
                    ))}
                  </div>
                )
              }}
            />
            <BeamColumn<BundleRow>
              field="name"
              header="On this zone"
              width="110px"
              children={(row) => {
                const installed = installations.find((i) => shortName(i.bundle) === shortName(row.name))
                return installed
                  ? <BeamBadge variant="success" appearance="outlined" pill>v{installed.target?.version ?? '?'}</BeamBadge>
                  : <span style={QUIET}>-</span>
              }}
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
                  {row.local && !row.published && (
                    <BeamDropdownItem onClick={() => { setCreateTarget(row.name); setCreateVisibility('private') }}>
                      <span style={MENU_ITEM}><BeamIcon name="plus" style={{ fontSize: '0.8rem' }} /> Create in the catalog</span>
                    </BeamDropdownItem>
                  )}
                  {row.local && row.published && (
                    <BeamDropdownItem onClick={() => openPublish(row.name)}>
                      <span style={MENU_ITEM}><BeamIcon name="upload" style={{ fontSize: '0.8rem' }} /> Plan &amp; publish</span>
                    </BeamDropdownItem>
                  )}
                  {row.published && row.published.latestVersion > 0 && (
                    <BeamDropdownItem onClick={() => openInstall(row.published!.name)}>
                      <span style={MENU_ITEM}><BeamIcon name="rocket" style={{ fontSize: '0.8rem' }} /> {installedHere.has(row.published.name) ? 'Change installation' : 'Install on this zone'}</span>
                    </BeamDropdownItem>
                  )}
                  {row.published && (
                    <BeamDropdownItem onClick={() => loadReleases(row.published!.name)}>
                      <span style={MENU_ITEM}><BeamIcon name="clock-rotate-left" style={{ fontSize: '0.8rem' }} /> Releases</span>
                    </BeamDropdownItem>
                  )}
                  {row.published && (
                    <BeamDropdownItem onClick={() => setVisibilityTarget(row.published)}>
                      <span style={MENU_ITEM}><BeamIcon name="shield" style={{ fontSize: '0.8rem' }} /> Make {row.published.visibility === 'public' ? 'private' : 'public'}</span>
                    </BeamDropdownItem>
                  )}
                </BeamDropdown>
              )}
            />
          </BeamTable>
        </BeamTabPanel>

        {/* ── INSTALLATIONS ── */}
        <BeamTabPanel name="installations">
          <div style={{ ...STACK, gap: '1.5rem', marginTop: '0.5rem' }}>
            <div style={STACK}>
              <span style={{ ...text, fontWeight: 600 }}>Installed on zone {zoneId}</span>
              <BeamTable<Installation>
                data={installations}
                pageSize={10}
                pageSizeOptions={[10, 25, 50]}
                rowKey={(r) => r.bundle}
                emptyMessage={loading ? 'Loading…' : 'No bundles installed on this zone'}
              >
                <BeamColumn<Installation>
                  field="bundle"
                  header="Bundle"
                  sortable
                  width="22%"
                  children={(row) => (
                    <div style={COLUMN}>
                      <span style={STRONG}>{shortName(row.bundle)}</span>
                      <span style={QUIET_MONO}>{row.bundle}</span>
                    </div>
                  )}
                />
                <BeamColumn<Installation>
                  field="target"
                  header="Running"
                  children={(row) => (
                    <div style={COLUMN}>
                      <span style={{ ...text, fontSize: '13px' }}>v{row.target?.version ?? '?'}</span>
                      <span style={QUIET}>
                        {row.target?.componentGroups.length ? row.target.componentGroups.join(', ') : 'no component groups'} · realms {row.target?.realms}
                      </span>
                      <span style={QUIET}>
                        auto-update {row.target?.autoUpdate ? 'on' : 'off'}{row.autoUpdateLocked ? ' (required)' : ''}
                        {row.update ? ` · ${row.update}` : ''}
                      </span>
                    </div>
                  )}
                />
                <BeamColumn<Installation>
                  field="status"
                  header="Support"
                  width="130px"
                  children={(row) => (
                    <div style={COLUMN}>
                      <BeamBadge variant={SUPPORT_VARIANT[row.status] ?? 'neutral'} appearance="filled" pill title={row.notice?.message ?? ''}>
                        {row.status || 'supported'}
                      </BeamBadge>
                      {row.uninstalling && <BeamBadge variant="warning" appearance="outlined" pill>uninstalling</BeamBadge>}
                    </div>
                  )}
                />
                <BeamColumn<Installation> field="sites" header="Manifests" width="200px" children={(row) => <SiteResults results={row.sites} />} />
                <BeamColumn<Installation>
                  field="overrides"
                  header="Overrides"
                  width="170px"
                  children={(row) => row.overrides.length === 0 ? <span style={QUIET}>-</span> : (
                    <div style={COLUMN}>
                      {row.overrides.map((o) => (
                        <span key={`${o.site}:${o.component}`} style={QUIET} title={`connection ${o.connectionId}`}>
                          {o.component} on {o.site} (from v{o.baseVersion}{o.changedIn ? `; v${o.changedIn} changed it` : ''})
                        </span>
                      ))}
                    </div>
                  )}
                />
                <BeamColumn<Installation>
                  field="bundle"
                  header=""
                  width="36px"
                  align="right"
                  children={(row) => (
                    <BeamDropdown placement="bottom-end">
                      <BeamButton slot="trigger" appearance="plain" size="small">
                        <BeamIcon name="ellipsis-vertical" style={{ fontSize: '0.875rem' }} />
                      </BeamButton>
                      <BeamDropdownItem onClick={() => openInstall(row.bundle)}>
                        <span style={MENU_ITEM}><BeamIcon name="pen" style={{ fontSize: '0.8rem' }} /> Change…</span>
                      </BeamDropdownItem>
                      <BeamDropdownItem onClick={() => openHistory(row.bundle)}>
                        <span style={MENU_ITEM}><BeamIcon name="clock-rotate-left" style={{ fontSize: '0.8rem' }} /> History</span>
                      </BeamDropdownItem>
                      <BeamDropdownItem onClick={() => openInstall(row.bundle, true)}>
                        <span style={{ ...MENU_ITEM, color: 'var(--color-beam-error)' }}><BeamIcon name="link-slash" style={{ fontSize: '0.8rem' }} /> Uninstall…</span>
                      </BeamDropdownItem>
                    </BeamDropdown>
                  )}
                />
              </BeamTable>
            </div>

            <div style={STACK}>
              <span style={{ ...text, fontWeight: 600 }}>Your org's library</span>
              <span style={QUIET}>
                Bundles your org acquired or owns. Only these can be installed on a zone; a bundle's hard dependencies must be
                acquired before it.
              </span>
              <div style={ROW}>
                <BeamInput
                  size="small"
                  placeholder="@namespace/bundle-name"
                  value={acquireName}
                  onValueInput={setAcquireName}
                  onValueChange={setAcquireName}
                  style={{ maxWidth: '22rem', flex: 1 }}
                />
                <BeamButton size="small" variant="brand" disabled={busy || !acquireName.trim()} onClick={() => { setAcquireResult(null); setAcquireConfirm(acquireName.trim()) }}>
                  <BeamIcon slot="start" name="plus" /> Acquire
                </BeamButton>
              </div>
              {acquireResult && !acquireResult.acquired && (
                <Messages
                  variant="danger"
                  title={`${acquireResult.name} can't be acquired`}
                  items={[
                    ...acquireResult.refusals,
                    ...acquireResult.missingHardDependencies.map((d) => `Acquire its hard dependency ${d} first.`),
                  ]}
                />
              )}
              <BeamTable<LibraryEntry>
                data={library}
                pageSize={10}
                pageSizeOptions={[10, 25, 50]}
                rowKey={(r) => r.bundle}
                emptyMessage={loading ? 'Loading…' : 'Your org has no bundles in its library'}
              >
                <BeamColumn<LibraryEntry> field="bundle" header="Bundle" sortable children={(row) => <span style={STRONG}>{row.bundle}</span>} />
                <BeamColumn<LibraryEntry>
                  field="owned"
                  header="How"
                  width="110px"
                  format={(_v, row) => row.owned ? 'owned' : 'acquired'}
                />
                <BeamColumn<LibraryEntry>
                  field="installations"
                  header="Installed on"
                  children={(row) => row.installations.length === 0 ? <span style={QUIET}>no zone</span> : (
                    <div style={COLUMN}>
                      {row.installations.map((i) => (
                        <span key={i.zoneId} style={QUIET}>
                          {i.zoneId === zoneId ? 'this zone' : i.zoneId} · v{i.version}
                          {i.status && i.status !== 'supported' ? ` · ${i.status}` : ''}
                          {i.uninstalling ? ' · uninstalling' : ''}
                        </span>
                      ))}
                    </div>
                  )}
                />
                <BeamColumn<LibraryEntry>
                  field="bundle"
                  header=""
                  width="36px"
                  align="right"
                  children={(row) => (
                    <BeamDropdown placement="bottom-end">
                      <BeamButton slot="trigger" appearance="plain" size="small">
                        <BeamIcon name="ellipsis-vertical" style={{ fontSize: '0.875rem' }} />
                      </BeamButton>
                      <BeamDropdownItem onClick={() => openInstall(row.bundle)}>
                        <span style={MENU_ITEM}><BeamIcon name="rocket" style={{ fontSize: '0.8rem' }} /> {installedHere.has(row.bundle) ? 'Change installation' : 'Install on this zone'}</span>
                      </BeamDropdownItem>
                      {row.acquired && !row.owned && (
                        <BeamDropdownItem disabled={row.installations.length > 0} onClick={() => setRemoveTarget(row.bundle)}>
                          <span style={{ ...MENU_ITEM, color: 'var(--color-beam-error)' }}><BeamIcon name="trash" style={{ fontSize: '0.8rem' }} /> Remove from library</span>
                        </BeamDropdownItem>
                      )}
                    </BeamDropdown>
                  )}
                />
              </BeamTable>
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
              {ownPublished.map((r) => (
                <BeamDropdownItem key={r.name} onClick={() => loadReleases(r.name)}>{r.name}</BeamDropdownItem>
              ))}
            </BeamDropdown>
          </div>
          {releasesLoading ? (
            <div style={{ display: 'flex', justifyContent: 'center', padding: '2rem' }}>
              <BeamSpinner style={{ fontSize: '1.5rem' }} />
            </div>
          ) : (
            <div style={STACK}>
              <BeamTable<Release>
                data={releases?.releases ?? []}
                pageSize={10}
                pageSizeOptions={[10, 25, 50]}
                rowKey={(r) => String(r.version)}
                emptyMessage={releasesBundle ? 'No releases' : 'Select a bundle to view its releases'}
              >
                <BeamColumn<Release>
                  field="version"
                  header="Version"
                  width="160px"
                  sortable
                  children={(row) => (
                    <div style={COLUMN}>
                      <div style={ROW}>
                        <span style={STRONG}>v{row.version}</span>
                        {row.yanked && <BeamBadge variant="danger" appearance="filled" pill>yanked</BeamBadge>}
                      </div>
                      {tagsByVersion.has(row.version) && <span style={QUIET}>{tagsByVersion.get(row.version)!.join(', ')}</span>}
                    </div>
                  )}
                />
                <BeamColumn<Release>
                  field="irreversibleDataChange"
                  header="Data"
                  width="150px"
                  children={(row) => row.irreversibleDataChange
                    ? <BeamBadge variant="warning" appearance="outlined" pill>irreversible change</BeamBadge>
                    : <span style={QUIET}>-</span>}
                />
                <BeamColumn<Release>
                  field="checksum"
                  header="Checksum"
                  width="170px"
                  children={(row) => <span title={row.checksum} style={QUIET_MONO}>{row.checksum.slice(0, 16)}…</span>}
                />
                <BeamColumn<Release>
                  field="publishedBy"
                  header="By"
                  width="140px"
                  format={(v) => (v ? (String(v) === String(workspace?.ownerAccountId) ? workspace?.ownerEmail ?? String(v) : String(v)) : '-')}
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
                      <BeamDropdownItem onClick={() => showContent(releasesBundle, row.version)}>
                        <span style={MENU_ITEM}><BeamIcon name="file-lines" style={{ fontSize: '0.8rem' }} /> Content</span>
                      </BeamDropdownItem>
                      <BeamDropdownItem onClick={() => { setTagTarget({ name: releasesBundle, version: row.version }); setNewTag('') }}>
                        <span style={MENU_ITEM}><BeamIcon name="tag" style={{ fontSize: '0.8rem' }} /> Tag…</span>
                      </BeamDropdownItem>
                      {!row.yanked && (
                        <BeamDropdownItem onClick={() => setYankTarget({ name: releasesBundle, version: row.version })}>
                          <span style={{ ...MENU_ITEM, color: 'var(--color-beam-error)' }}><BeamIcon name="ban" style={{ fontSize: '0.8rem' }} /> Yank</span>
                        </BeamDropdownItem>
                      )}
                    </BeamDropdown>
                  )}
                />
              </BeamTable>
              {(releases?.notices.length ?? 0) > 0 && (
                <div style={STACK}>
                  <span style={{ ...text, fontWeight: 600 }}>Deprecation notices</span>
                  {releases!.notices.map((n) => <NoticeLine key={n.id} notice={n} />)}
                </div>
              )}
            </div>
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

      {/* ── Create ── */}
      <BeamDialog
        open={createTarget != null}
        label={`Create ${createTarget ? shortName(createTarget) : ''}`}
        with-footer
        onClose={() => { if (!busy) setCreateTarget(null) }}
        style={{ '--width': '30rem' } as CSSProperties}
      >
        <div style={STACK}>
          <span style={QUIET}>
            Claims the name in your org's namespace so it can be published. A public bundle is listed in the marketplace
            once it has a release; a private one is only your org's.
          </span>
          <BoundSelect key={createTarget ?? ''} size="small" label="Visibility" defaultValue={createVisibility} onChange={(v: string) => setCreateVisibility(v)}>
            <BeamOption value="private">Private</BeamOption>
            <BeamOption value="public">Public</BeamOption>
          </BoundSelect>
        </div>
        <div slot="footer" style={{ ...ROW, justifyContent: 'flex-end' }}>
          <BeamButton appearance="outlined" size="small" disabled={busy} onClick={() => setCreateTarget(null)}>Cancel</BeamButton>
          <BeamButton size="small" variant="brand" disabled={busy} onClick={confirmCreate}>{busy ? 'Creating…' : 'Create'}</BeamButton>
        </div>
      </BeamDialog>

      {/* ── Plan & publish ── */}
      <BeamDialog
        open={publishTarget != null}
        label={`Publish ${publishTarget ? shortName(publishTarget) : ''}`}
        with-footer
        onClose={() => { if (!busy) setPublishTarget(null) }}
        style={{ '--width': '52rem' } as CSSProperties}
      >
        <div style={STACK}>
          <span style={{ ...text, fontWeight: 600 }}>1. Plan</span>
          <JobLogView job={planJob} />
          {plan?.isNoOp && <BeamCallout variant="neutral">Nothing to publish: the bundle matches its latest release, v{plan.publishedVersion}.</BeamCallout>}
          {plan && !plan.isNoOp && (
            <>
              <span style={QUIET}>
                {plan.publishedVersion ? `Compared with v${plan.publishedVersion}.` : 'The first release of this bundle.'}
              </span>
              <BeamTable<PlanResult['changes'][number]>
                data={plan.changes}
                pageSize={10}
                rowKey={(r) => `${r.kind}:${r.name}`}
                emptyMessage="No component changes"
              >
                <BeamColumn field="change" header="Change" width="100px" />
                <BeamColumn field="kind" header="Kind" width="130px" />
                <BeamColumn field="name" header="Name" />
                <BeamColumn
                  field="local"
                  header="Now"
                  width="200px"
                  children={(row: PlanResult['changes'][number]) => (
                    <span title={`was: ${row.published || '-'}`} style={QUIET_MONO}>{row.local || '-'}</span>
                  )}
                />
              </BeamTable>
            </>
          )}
          {plan && !plan.isNoOp && (
            <>
              <span style={{ ...text, fontWeight: 600 }}>2. Publish</span>
              <BeamInput
                size="small"
                label="Extra tag"
                placeholder="e.g. stable (latest always moves)"
                value={tag}
                onValueChange={setTag}
                style={{ maxWidth: '20rem' }}
              />
              <JobLogView job={publishJob} />
              {published && (
                <BeamCallout variant="success">
                  {published.isNewVersion
                    ? `Published ${published.name} v${published.version}.`
                    : `Nothing new: v${published.version} already has this content.`}
                </BeamCallout>
              )}
              {published && <Messages variant="warning" title="Warnings" items={published.warnings} />}
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

      {/* ── Install / change / uninstall ── */}
      <BeamDialog
        open={installForm != null}
        label={installForm
          ? `${uninstall ? 'Uninstall' : installForm.current ? 'Change' : 'Install'} ${shortName(installForm.bundle)} on zone ${zoneId}`
          : ''}
        with-footer
        onClose={() => { if (!busy) setInstallForm(null) }}
        style={{ '--width': '52rem' } as CSSProperties}
      >
        {installForm && (
          <div style={STACK}>
            {installForm.current && <span style={QUIET}>Now: {describeTarget(installForm.current.target)}</span>}
            {uninstall && (
              <span style={QUIET}>
                The bundle's components are archived on the zone and its realms. Storage data is kept.
              </span>
            )}
            {!uninstall && (
              <>
                <div style={ROW}>
                  <BoundSelect
                    key={`release:${installForm.bundle}:${installReleases ? 'loaded' : 'loading'}`}
                    size="small"
                    label="Release"
                    defaultValue={installForm.release}
                    onChange={(v: string) => updateForm({ release: v })}
                    style={{ width: '16rem' }}
                  >
                    <BeamOption value="">{installForm.current ? `Keep v${installForm.current.target?.version}` : 'Latest'}</BeamOption>
                    <small>Tags</small>
                    {(installReleases?.tags ?? []).map((t) => <BeamOption key={t.tag} value={`t:${t.tag}`}>{`${t.tag} (v${t.version})`}</BeamOption>)}
                    <small>Versions</small>
                    {(installReleases?.releases ?? []).filter((r) => !r.yanked).map((r) => (
                      <BeamOption key={r.version} value={`v:${r.version}`}>{`v${r.version}`}</BeamOption>
                    ))}
                  </BoundSelect>
                  <BoundSelect key={`realms:${installForm.bundle}`} size="small" label="Realms" defaultValue={installForm.realms} onChange={(v: string) => updateForm({ realms: v as InstallForm['realms'] })} style={{ width: '14rem' }}>
                    <BeamOption value="">{installForm.current ? `Keep (${installForm.current.target?.realms})` : 'Default (all)'}</BeamOption>
                    <BeamOption value="all">All realms of the zone</BeamOption>
                    <BeamOption value="none">None (zone components only)</BeamOption>
                  </BoundSelect>
                  <BoundSelect key={`auto:${installForm.bundle}`} size="small" label="Auto-update" defaultValue={installForm.autoUpdate} onChange={(v: string) => updateForm({ autoUpdate: v as InstallForm['autoUpdate'] })} style={{ width: '12rem' }}>
                    <BeamOption value="">{installForm.current ? `Keep (${installForm.current.target?.autoUpdate ? 'on' : 'off'})` : "Your org's setting"}</BeamOption>
                    <BeamOption value="on">On</BeamOption>
                    <BeamOption value="off">Off</BeamOption>
                  </BoundSelect>
                </div>
                <div style={STACK}>
                  <BeamCheckbox
                    checked={installForm.componentGroups == null}
                    onCheckedChange={(on) => updateForm({ componentGroups: on ? null : [...pickedGroups] })}
                  >
                    {installForm.current ? 'Keep the installed component groups' : 'The component groups on by default'}
                  </BeamCheckbox>
                  {installForm.componentGroups != null && (
                    contentGroups.length === 0 ? (
                      <span style={QUIET}>{installContent ? 'This release has no component groups.' : 'Loading the release\'s component groups…'}</span>
                    ) : (
                      <div style={{ ...ROW, paddingInlineStart: '1.5rem' }}>
                        {contentGroups.map((g) => (
                          <BeamCheckbox
                            key={g}
                            checked={installForm.componentGroups!.includes(g)}
                            onCheckedChange={(on) => updateForm({
                              componentGroups: on
                                ? [...installForm.componentGroups!, g]
                                : installForm.componentGroups!.filter((x) => x !== g),
                            })}
                          >
                            <span title={installContent?.componentGroups[g].join(', ')}>{g}</span>
                          </BeamCheckbox>
                        ))}
                      </div>
                    )
                  )}
                </div>
              </>
            )}
            <JobLogView job={installJob} />
            {installPlan && <InstallationPlanView plan={installPlan} />}
            {installPlan && !planChanges && <BeamCallout variant="neutral">Nothing to change.</BeamCallout>}
            {installResult?.applied && (
              <div style={STACK}>
                <Messages variant="warning" title="Warnings" items={installResult.warnings} />
                <span style={{ ...text, fontWeight: 600 }}>Result</span>
                <SiteResults results={installResult.results} />
              </div>
            )}
          </div>
        )}
        <div slot="footer" style={{ ...ROW, justifyContent: 'flex-end' }}>
          <BeamButton appearance="outlined" size="small" disabled={busy} onClick={() => setInstallForm(null)}>
            {installResult?.applied ? 'Close' : 'Cancel'}
          </BeamButton>
          {installForm && !installResult?.applied && (
            <BeamButton size="small" appearance={installPlan ? 'outlined' : 'filled'} variant="brand" disabled={busy}
              onClick={() => planInstall(uninstall ? { bundle: installForm.bundle } : installParams(installForm))}>
              {installPlan ? 'Plan again' : 'Plan'}
            </BeamButton>
          )}
          {canApply && (
            <BeamButton size="small" variant={uninstall ? 'danger' : 'brand'} disabled={busy} onClick={applyInstall}>
              {busy ? 'Applying…' : uninstall ? 'Uninstall' : 'Apply'}
            </BeamButton>
          )}
        </div>
      </BeamDialog>

      {/* ── History ── */}
      <BeamDialog
        open={historyTarget != null}
        label={`History of ${historyTarget ? shortName(historyTarget) : ''} on zone ${zoneId}`}
        light-dismiss
        with-footer
        onClose={() => setHistoryTarget(null)}
        style={{ '--width': '52rem' } as CSSProperties}
      >
        {history == null ? (
          <div style={{ display: 'flex', justifyContent: 'center', padding: '2rem' }}><BeamSpinner style={{ fontSize: '1.5rem' }} /></div>
        ) : (
          <BeamTable<HistoryEntry>
            data={history}
            pageSize={10}
            rowKey={(r) => `${r.at}:${r.change}`}
            emptyMessage="No changes yet"
          >
            <BeamColumn<HistoryEntry>
              field="at"
              header="When"
              width="120px"
              children={(row) => <BeamRelativeTime date={row.at} numeric="auto" with-utc-popover style={QUIET} />}
            />
            <BeamColumn<HistoryEntry>
              field="change"
              header="Change"
              width="130px"
              children={(row) => (
                <div style={COLUMN}>
                  <span style={{ ...text, fontSize: '13px' }}>{row.change}</span>
                  {row.actor && <span style={QUIET}>{row.actor}</span>}
                </div>
              )}
            />
            <BeamColumn<HistoryEntry>
              field="to"
              header="From → to"
              children={(row) => (
                <div style={COLUMN}>
                  <span style={QUIET}>{describeTarget(row.from)}</span>
                  <span style={{ ...text, fontSize: '12px' }}>{describeTarget(row.to)}</span>
                </div>
              )}
            />
            <BeamColumn<HistoryEntry> field="results" header="Manifests" width="190px" children={(row) => <SiteResults results={row.results} />} />
          </BeamTable>
        )}
        <div slot="footer" style={{ ...ROW, justifyContent: 'flex-end' }}>
          <BeamButton appearance="outlined" size="small" onClick={() => setHistoryTarget(null)}>Close</BeamButton>
        </div>
      </BeamDialog>

      {/* ── Tag ── */}
      <BeamDialog
        open={tagTarget != null}
        label={`Tag v${tagTarget?.version ?? ''}`}
        with-footer
        onClose={() => setTagTarget(null)}
        style={{ '--width': '28rem' } as CSSProperties}
      >
        <BeamInput size="small" label="Tag" placeholder="e.g. stable" value={newTag} onValueChange={setNewTag} />
        <div slot="footer" style={{ ...ROW, justifyContent: 'flex-end' }}>
          <BeamButton appearance="outlined" size="small" onClick={() => setTagTarget(null)}>Cancel</BeamButton>
          <BeamButton size="small" variant="brand" disabled={busy || !newTag.trim()} onClick={confirmTag}>Tag</BeamButton>
        </div>
      </BeamDialog>

      <BeamConfirmDialog
        open={yankTarget != null}
        label="Yank release"
        message={`Yank ${yankTarget?.name ?? ''} v${yankTarget?.version ?? ''}? Nobody can install it any more; installations running it keep it.`}
        confirm-label="Yank"
        confirm-variant="danger"
        onConfirm={confirmYank}
        onCancel={() => setYankTarget(null)}
      />

      <BeamConfirmDialog
        open={visibilityTarget != null}
        label={visibilityTarget?.visibility === 'public' ? 'Make private' : 'Make public'}
        message={visibilityTarget?.visibility === 'public'
          ? `Make ${visibilityTarget?.name ?? ''} private? It leaves the marketplace; orgs that already acquired it keep it.`
          : `Make ${visibilityTarget?.name ?? ''} public? Once it has a release, it is listed in the marketplace and any org can acquire it.`}
        confirm-label={visibilityTarget?.visibility === 'public' ? 'Make private' : 'Make public'}
        onConfirm={confirmVisibility}
        onCancel={() => setVisibilityTarget(null)}
      />

      <BeamConfirmDialog
        open={acquireConfirm != null}
        label="Acquire bundle"
        message={`Add ${acquireConfirm ?? ''} to your org's library? Nothing is installed until you install it on a zone.`}
        confirm-label="Acquire"
        onConfirm={confirmAcquire}
        onCancel={() => setAcquireConfirm(null)}
      />

      <BeamConfirmDialog
        open={removeTarget != null}
        label="Remove from library"
        message={`Remove ${removeTarget ?? ''} from your org's library? It can be acquired again later.`}
        confirm-label="Remove"
        confirm-variant="danger"
        onConfirm={confirmRemove}
        onCancel={() => setRemoveTarget(null)}
      />

      <BeamDialog
        open={jsonData != null}
        label="Details"
        light-dismiss
        with-footer
        onClose={() => setJsonData(null)}
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

    </div>
  )
}
