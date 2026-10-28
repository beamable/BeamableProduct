import { Fragment, useEffect, useMemo, useRef, useState, type CSSProperties } from 'react'
import {
  BeamBadge,
  BeamButton,
  BeamCallout,
  BeamCheckbox,
  BeamDetails,
  BeamDivider,
  BeamColumn,
  BeamConfirmDialog,
  BeamDialog,
  BeamDropdown,
  BeamDropdownItem,
  BeamIcon,
  BeamInput,
  BeamOption,
  BeamPageHeader,
  BeamSpinner,
  BeamTab,
  BeamTabGroup,
  BeamTabPanel,
  BeamTable,
  BeamTextarea,
} from '@beamable/portal-toolkit/react'
import { StepPicker, type PickerGroup } from './StepPicker'
import { BoundSelect, ThemeToggle, errorMessage, mono, text, textMuted, useSessionState, useToasts } from '../shared/ui'
import { sessionToken } from '../shared/connection'
import {
  deleteStack,
  dryRun,
  getChoices,
  getDefinition,
  getLogs,
  getStackSteps,
  getStacks,
  getStatus,
  getWorkspace,
  saveStack,
  startStack,
  stepAction,
  stopStack,
  type Choices,
  type DryRun,
  type Layer,
  type ListedStack,
  type Listing,
  type OwnCommand,
  type Status,
  type StackForm,
  type StepStatus,
  type Workspace,
} from './api'

const QUIET: CSSProperties = { ...textMuted, fontSize: '12px' }
const QUIET_MONO: CSSProperties = { ...textMuted, ...mono, fontSize: '11px' }
const ROW: CSSProperties = { display: 'flex', alignItems: 'center', gap: '0.5rem', flexWrap: 'wrap' }
const STACK: CSSProperties = { display: 'flex', flexDirection: 'column', gap: '0.75rem' }
const MENU_ITEM: CSSProperties = { display: 'flex', alignItems: 'center', gap: '0.5rem' }
const LOG: CSSProperties = {
  ...mono, fontSize: '12px', margin: 0, padding: '0.75rem', maxHeight: '60vh', overflow: 'auto', whiteSpace: 'pre-wrap', overflowWrap: 'anywhere',
  background: 'var(--color-beam-neutral-soft)', border: '1px solid var(--color-beam-border)', borderRadius: '0.375rem',
}

/** Whether the window is phone-narrow, kept up to date as it resizes. */
function useNarrow(query = '(max-width: 640px)') {
  const [narrow, setNarrow] = useState(() => window.matchMedia(query).matches)
  useEffect(() => {
    const list = window.matchMedia(query)
    const update = () => setNarrow(list.matches)
    list.addEventListener('change', update)
    return () => list.removeEventListener('change', update)
  }, [query])
  return narrow
}

/** A command line that wraps only between its words; browsers otherwise break `--detach` after its first dash. */
function CommandLine({ line, style }: { line: string; style?: CSSProperties }) {
  return (
    <code style={style}>
      {line.split(' ').map((word, i) => (
        <Fragment key={i}>{i > 0 && ' '}<span style={{ whiteSpace: 'nowrap' }}>{word}</span></Fragment>
      ))}
    </code>
  )
}

/** Log lines that stay scrolled to the newest one, unless the reader scrolled up to read. */
function FollowLog({ lines, style }: { lines: string[]; style?: CSSProperties }) {
  const ref = useRef<HTMLPreElement>(null)
  const following = useRef(true)
  useEffect(() => {
    if (following.current && ref.current) ref.current.scrollTop = ref.current.scrollHeight
  }, [lines])
  return (
    <pre ref={ref} style={{ ...LOG, ...style }}
      onScroll={(e) => { const el = e.currentTarget; following.current = el.scrollHeight - el.scrollTop - el.clientHeight < 24 }}>
      {lines.join('\n')}
    </pre>
  )
}

const TABS = ['stacks', 'running']
const CLI_COMMAND = 'beam local management'
const LAYER_LABEL: Record<Layer, string> = { 'built-in': 'built-in', shared: 'team', local: 'mine' }
/** How the pickers title a kind of step: its first tag. */
const KIND_TITLES: Record<string, string> = {
  docker: 'Docker', csharp: 'C# hosts', scala: 'Scala services', portal: 'Portal', build: 'Builds',
  microservices: 'Microservices', extensions: 'Portal extensions', 'web-registry': 'Web packages',
  observability: 'Observability', custom: 'Own commands',
}

/** Steps the CLI names after the stack's services (see LocalStackTemplate): managed by the services picker, not by Adds. */
const MANAGED_PREFIXES = ['microservice: ', 'portal extension: ', 'group: ']
/** The "Starts from" option for a stack that starts from no other. */
const NONE = '__none__'

type CommandKind = 'program' | 'shell' | 'beam'

/** A row of the form's command list: the command, what kind it is, and its environment as typed. */
interface CommandRow {
  key: number
  kind: CommandKind
  envText: string
  command: OwnCommand
  /**
   * Whether it is expanded: a new one starts open, the ones a stack has start collapsed. Kept here and set on every
   * render, because the page re-renders every couple of seconds (the status poll) and the details element would
   * otherwise be put back to how it started.
   */
  open: boolean
}

let nextCommandKey = 1

function commandRow(command: OwnCommand = { name: '' }, open = true): CommandRow {
  const kind: CommandKind = command.beam != null ? 'beam' : command.shell != null ? 'shell' : 'program'
  return { key: nextCommandKey++, kind, envText: (command.env ?? []).join(', '), command, open }
}

/** What a collapsed command shows next to its name. */
function commandSummary(row: CommandRow): string {
  const c = row.command
  const runs = row.kind === 'beam' ? `beam ${c.beam ?? ''}` : row.kind === 'shell' ? (c.shell ?? '') : `${c.command ?? ''} ${c.args ?? ''}`
  return [runs.trim() || 'nothing to run yet', c.once ? 'runs once' : '', c.after ? `after ${c.after}` : ''].filter(Boolean).join(' · ')
}

/** What `--commands` takes for a row: only its kind's fields, so the CLI sees exactly one thing to run. */
function commandOf(row: CommandRow): OwnCommand {
  const { command: c } = row
  const runs = row.kind === 'beam' ? { beam: c.beam ?? '' }
    : row.kind === 'shell' ? { shell: c.shell ?? '', ...(c.shellKind ? { shellKind: c.shellKind } : {}) }
    : { command: c.command ?? '', args: c.args ?? '' }
  return {
    name: c.name.trim(), ...runs,
    dir: c.dir, env: row.envText.split(',').map((e) => e.trim()).filter(Boolean), once: c.once,
    readyUrl: c.readyUrl, readyLog: c.readyLog, port: c.port, timeout: c.timeout, label: c.label, tags: c.tags,
    after: c.after, first: c.first,
  }
}

/** A step's board state as a badge color. */
function stateVariant(state: string | null): 'success' | 'warning' | 'danger' | 'neutral' | 'brand' {
  switch (state) {
    case 'Ready': case 'Done': case 'UpToDate': return 'success'
    case 'Failed': return 'danger'
    case 'Starting': case 'Restarting': case 'Building': return 'warning'
    case 'Running': return 'brand'
    default: return 'neutral'
  }
}

function emptyForm(): StackForm {
  return { name: '', shared: false, extends: '', include: [], exclude: [], services: [], extensions: [], groups: [], target: '', description: '' }
}

export default function App() {
  const { toast, Toaster } = useToasts()
  const [storedTab, setStoredTab] = useSessionState('tab', 'stacks')
  const activeTab = TABS.includes(storedTab) ? storedTab : 'stacks'

  // ── Connection and data ──
  const [workspace, setWorkspace] = useState<Workspace | null>(null)
  const [connecting, setConnecting] = useState(true)
  const [connectError, setConnectError] = useState('')
  const [listing, setListing] = useState<Listing | null>(null)
  const [choices, setChoices] = useState<Choices | null>(null)
  const [status, setStatus] = useState<Status | null>(null)

  async function connect() {
    setConnecting(true)
    setConnectError('')
    setWorkspace(null)
    try {
      // The CLI opens this page with its session token in the fragment.
      if (!sessionToken()) return
      const ws = await getWorkspace()
      setWorkspace(ws)
      if (ws.hasSettings) {
        await refreshStacks()
        // What the stack form offers takes a workspace scan; start it now so the form opens at once.
        loadChoices()
      }
    } catch (err) {
      setConnectError(errorMessage(err, 'Could not reach the CLI. Is `beam local management` still running?'))
    } finally {
      setConnecting(false)
    }
  }

  async function refreshStacks() {
    try {
      setListing(await getStacks())
    } catch (err) {
      toast.error(errorMessage(err, 'Could not list the stacks'))
    }
  }

  useEffect(() => { connect() }, [])

  // The running stack, polled while the page is open.
  useEffect(() => {
    if (!workspace?.hasSettings) return
    let alive = true
    async function poll() {
      try {
        const next = await getStatus()
        if (alive) setStatus(next)
      } catch { /* the CLI stopped; the next poll says so again */ }
    }
    poll()
    const timer = setInterval(poll, 2000)
    return () => { alive = false; clearInterval(timer) }
  }, [workspace])

  const running = status?.stack?.stack ? status.stack : null
  const narrow = useNarrow()
  // The stacks list has more columns than the steps table, so it folds them sooner.
  const compact = useNarrow('(max-width: 960px)')
  const fromBadges = (row: ListedStack) => (
    <span style={{ ...ROW, gap: '0.25rem' }}>
      <BeamBadge variant={row.layer === 'built-in' ? 'neutral' : 'brand'} appearance="outlined" pill>{LAYER_LABEL[row.layer]}</BeamBadge>
      {row.changedBy.map((l) => (
        <BeamBadge key={l} variant="warning" appearance="outlined" pill title={`Changed by ${LAYER_LABEL[l]}`}>+{LAYER_LABEL[l]}</BeamBadge>
      ))}
    </span>
  )
  const grafana = running ? status?.grafana ?? null : null
  const operation = status?.operation ?? null
  const operationBusy = operation != null && !operation.finished

  // A finished start/stop changes what is listed as running.
  const lastOperation = useRef<string>('')
  useEffect(() => {
    if (!operation?.finished) return
    const key = `${operation.commandLine}@${operation.startedAt}`
    if (lastOperation.current === key) return
    lastOperation.current = key
    if (operation.succeeded) toast.success(`\`${operation.commandLine}\` finished.`)
    else toast.error(`\`${operation.commandLine}\` failed (exit ${operation.exitCode}). Its output is on the Running tab.`)
  }, [operation])

  async function act(label: string, action: () => Promise<unknown>) {
    try {
      await action()
      setStoredTab('running')
      setStatus(await getStatus())
    } catch (err) {
      toast.error(errorMessage(err, `${label} failed`))
    }
  }

  // ── Start ──
  const [startTarget, setStartTarget] = useState<ListedStack | null>(null)
  const [startVariants, setStartVariants] = useState<string[]>([])
  const startName = startTarget ? [startTarget.name, ...startVariants].join(':') : ''
  // Variants that pick the same thing: one backend, and either :build or :no-build.
  const exclusive = [listing?.targets ?? [], ['build', 'no-build']]
  const conflicts = (v: string) =>
    exclusive.some((group) => group.includes(v) && startVariants.some((p) => p !== v && group.includes(p)))

  function confirmStart() {
    const name = startName
    setStartTarget(null)
    act(`Starting ${name}`, () => startStack(name))
  }

  // ── Preview (dry run) ──
  const [preview, setPreview] = useState<{ name: string; result: DryRun | null; error: string } | null>(null)

  async function openPreview(name: string) {
    setPreview({ name, result: null, error: '' })
    try {
      const result = await dryRun(name)
      setPreview({ name, result, error: '' })
    } catch (err) {
      setPreview({ name, result: null, error: errorMessage(err, 'The preview failed') })
    }
  }

  // ── Create / edit ──
  const [formOpen, setFormOpen] = useState(false)
  const [formMode, setFormMode] = useState<'create' | 'edit'>('create')
  const [form, setForm] = useState<StackForm>(emptyForm())
  const [formKey, setFormKey] = useState(0)
  const [saving, setSaving] = useState(false)
  // The stack's own commands, edited as a list; saving sends them all (`--commands`).
  const [commands, setCommands] = useState<CommandRow[]>([])
  const updateCommand = (key: number, change: (row: CommandRow) => CommandRow) =>
    setCommands((rows) => rows.map((r) => (r.key === key ? change(r) : r)))
  const setField = (key: number, field: keyof OwnCommand, value: unknown) =>
    updateCommand(key, (r) => ({ ...r, command: { ...r.command, [field]: value } }))
  // The steps the "Starts from" stack runs: what can still be added, and what can be left out.
  const [baseSteps, setBaseSteps] = useState<string[]>([])

  useEffect(() => {
    if (!formOpen || !form.extends) {
      setBaseSteps([])
      return
    }
    let alive = true
    getStackSteps(form.extends)
      .then((r) => { if (alive) setBaseSteps(r.steps) })
      .catch((err) => { if (alive) { setBaseSteps([]); toast.error(errorMessage(err, `Could not read what ${form.extends} runs`)) } })
    return () => { alive = false }
  }, [formOpen, form.extends])

  async function loadChoices(refresh = false) {
    try {
      setChoices(await getChoices(refresh))
    } catch (err) {
      toast.error(errorMessage(err, 'Could not read what a stack can run'))
    }
  }

  async function openCreate() {
    if (!choices) await loadChoices()
    setFormMode('create')
    setForm(emptyForm())
    setCommands([])
    setFormKey((k) => k + 1)
    setFormOpen(true)
  }

  async function openEdit(stack: ListedStack) {
    try {
      const [definition] = await Promise.all([getDefinition(stack.name), choices ? Promise.resolve() : loadChoices()])
      // Edit the layer that is yours to change: your own file first, then the team's.
      const layer = definition.layers.find((l) => l.layer === 'local') ?? definition.layers.find((l) => l.layer === 'shared')
      if (!layer) {
        toast.warning(`${stack.name} is built in. Create a stack that extends it instead.`)
        return
      }
      const def = layer.definition
      const picked = def.extensions ?? []
      setFormMode('edit')
      setForm({
        name: stack.name,
        shared: layer.layer === 'shared',
        extends: def.extends ?? '',
        // The CLI lists the stack's own commands and services in include itself; the form edits those apart.
        include: (def.include ?? []).filter((i) => !layer.commands.some((c) => c.name === i) && !MANAGED_PREFIXES.some((p) => i.startsWith(p))),
        exclude: def.exclude ?? [],
        services: picked.filter((id) => !(choices?.extensions.includes(id) || choices?.groups.includes(id))),
        extensions: picked.filter((id) => choices?.extensions.includes(id) ?? false),
        groups: picked.filter((id) => choices?.groups.includes(id) ?? false),
        target: def.target ?? '',
        description: def.description ?? '',
      })
      setCommands(layer.commands.map((c) => commandRow(c, false)))
      setFormKey((k) => k + 1)
      setFormOpen(true)
    } catch (err) {
      toast.error(errorMessage(err, `Could not read ${stack.name}`))
    }
  }

  async function submitForm() {
    if (!form.name.trim()) {
      toast.error('Give the stack a name.')
      return
    }
    setSaving(true)
    try {
      await saveStack(formMode, { ...form, name: form.name.trim(), commands: commands.map(commandOf) })
      toast.success(`${formMode === 'create' ? 'Created' : 'Saved'} ${form.name.trim()}.`)
      setFormOpen(false)
      await refreshStacks()
    } catch (err) {
      toast.error(errorMessage(err, 'Saving the stack failed'))
    } finally {
      setSaving(false)
    }
  }

  // ── Delete ──
  const [deleteTarget, setDeleteTarget] = useState<ListedStack | null>(null)

  async function confirmDelete() {
    const target = deleteTarget
    setDeleteTarget(null)
    if (!target) return
    try {
      await deleteStack(target.name, target.layer === 'shared')
      toast.success(`Deleted ${target.name}.`)
      await refreshStacks()
    } catch (err) {
      toast.error(errorMessage(err, `Deleting ${target.name} failed`))
    }
  }

  // ── Stop, logs ──
  const [purgeAsk, setPurgeAsk] = useState(false)
  const [logs, setLogs] = useState<{ step: string; lines: string[] } | null>(null)

  async function openLogs(step: StepStatus) {
    try {
      setLogs(await getLogs(step.name))
    } catch (err) {
      toast.error(errorMessage(err, `Could not read the log of ${step.name}`))
    }
  }

  const stacks = listing?.stacks ?? []
  const ready = running?.steps.filter((s) => ['Ready', 'Done', 'UpToDate'].includes(s.state ?? '')).length ?? 0
  /** Tags first, then the steps by kind, in the order the settings list them; anything else under Other. */
  const groupsOf = (items: string[], extra: PickerGroup[] = []): PickerGroup[] => {
    if (!choices) return [{ title: 'Other', items }]
    const kindOf = (name: string) => choices.steps.find((s) => s.name === name)?.tags[0]
    const tags = items.filter((i) => choices.tags.includes(i) && !choices.steps.some((s) => s.name === i))
    const byKind = new Map<string, string[]>()
    const other: string[] = []
    for (const item of items) {
      if (tags.includes(item)) continue
      const kind = kindOf(item)
      if (kind) byKind.set(kind, [...(byKind.get(kind) ?? []), item])
      else other.push(item)
    }
    return [
      ...(tags.length ? [{ title: 'Tags (every step with the tag)', items: tags }] : []),
      ...[...byKind].map(([kind, list]) => ({ title: KIND_TITLES[kind] ?? kind, items: list })),
      ...extra,
      ...(other.length ? [{ title: 'Other', items: other }] : []),
    ]
  }

  // What the stack runs as the form stands: what it starts from, plus what it adds, minus what it leaves out.
  const stackSteps = useMemo(() => {
    if (!choices) return [] as string[]
    const expand = (x: string) => choices.steps.some((s) => s.name === x) ? [x] : choices.steps.filter((s) => s.tags.includes(x)).map((s) => s.name)
    const runs = new Set([...baseSteps, ...form.include.flatMap(expand)])
    for (const out of form.exclude.flatMap(expand)) runs.delete(out)
    return choices.steps.map((s) => s.name).filter((n) => runs.has(n))
  }, [choices, baseSteps, form.include, form.exclude])

  // Adds: what the base stack doesn't run yet (a tag counts when it brings at least one more step). Leaves out: what
  // it runs (a tag counts when it has at least one of its steps). What the stack already lists stays offered.
  const { addChoices, leaveOutChoices } = useMemo(() => {
    if (!choices) return { addChoices: [] as string[], leaveOutChoices: [] as string[] }
    const base = new Set(baseSteps)
    const steps = choices.steps.map((s) => s.name)
    const tagged = (tag: string) => choices.steps.filter((s) => s.tags.includes(tag)).map((s) => s.name)
    const tags = choices.tags.filter((t) => !steps.includes(t))
    const keep = (list: string[], picked: string[]) => [...picked.filter((p) => !list.includes(p)), ...list]
    return {
      addChoices: keep([...tags.filter((t) => tagged(t).some((s) => !base.has(s))), ...steps.filter((s) => !base.has(s))], form.include),
      leaveOutChoices: keep([...tags.filter((t) => tagged(t).some((s) => base.has(s))), ...steps.filter((s) => base.has(s))], form.exclude),
    }
  }, [choices, baseSteps, form.include, form.exclude])

  // ── Render ──
  const header = (
    <BeamPageHeader label="Local stacks" description="Create, start and follow the local stacks of this workspace through the Beamable CLI">
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

  if (!workspace) {
    return (
      <div style={STACK}>
        {header}
        <BeamCallout variant={connectError ? 'danger' : 'neutral'}>
          <BeamIcon slot="icon" name="terminal" />
          <div style={STACK}>
            <span style={text}>{connectError || 'This page is not connected to a CLI.'}</span>
            <span style={QUIET}>
              Run <code style={mono}>{CLI_COMMAND}</code> in your Beamable workspace. It opens this page already connected.
            </span>
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

  if (!workspace.hasSettings) {
    return (
      <div style={STACK}>
        {header}
        <BeamCallout variant="warning">
          <BeamIcon slot="icon" name="triangle-exclamation" />
          <div style={STACK}>
            <span style={text}>This workspace has no local stack settings yet.</span>
            <span style={QUIET}>
              Run <code style={mono}>beam local setup</code> in <code style={mono}>{workspace.workspace}</code>, then Retry.
            </span>
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

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
      {header}

      <BeamCallout variant="success">
        <BeamIcon slot="icon" name="plug" />
        <div style={{ ...ROW, justifyContent: 'space-between' }}>
          <span style={QUIET}>
            Connected to the CLI in <code style={mono}>{workspace.workspace}</code>
            {running && <> · running <code style={mono}>{running.stack}</code> ({ready} of {running.steps.length} ready)</>}
          </span>
          <BeamButton size="small" appearance="outlined" onClick={refreshStacks}>
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
        <BeamTab slot="nav" panel="stacks">Stacks</BeamTab>
        <BeamTab slot="nav" panel="running">
          Running
          {running && (
            <BeamBadge variant={running.up ? 'success' : 'warning'} appearance="filled" pill style={{ marginInlineStart: '0.375rem' }}>
              {running.up ? 'up' : `${ready}/${running.steps.length}`}
            </BeamBadge>
          )}
        </BeamTab>

        {/* ── STACKS ── */}
        <BeamTabPanel name="stacks">
          <div style={{ ...ROW, justifyContent: 'flex-end', margin: '0.5rem 0' }}>
            <BeamButton size="small" variant="brand" onClick={openCreate}>
              <BeamIcon slot="start" name="plus" /> New stack
            </BeamButton>
          </div>
          <BeamTable<ListedStack>
            data={stacks}
            pageSize={25}
            pageSizeOptions={[10, 25, 50]}
            rowKey={(r) => r.name}
            emptyMessage="No stacks"
          >
            <BeamColumn<ListedStack>
              field="name"
              header="Stack"
              sortable
              width={compact ? undefined : '34%'}
              children={(row) => (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.125rem' }}>
                  <span style={{ ...ROW, gap: '0.375rem' }}>
                    <span style={{ ...text, fontWeight: 600, fontSize: '13px' }}>{row.name}</span>
                    {running?.stack?.split(':')[0] === row.name && (
                      <BeamBadge variant="success" appearance="filled" pill>running</BeamBadge>
                    )}
                  </span>
                  {row.description && <span style={QUIET}>{row.description}</span>}
                  {compact && (
                    // On a phone what the other columns say goes under the description.
                    <span style={{ ...ROW, gap: '0.375rem' }}>
                      {fromBadges(row)}
                      <span style={QUIET_MONO}>{row.target ?? 'local'}</span>
                      {row.variants.length > 0 && <span style={QUIET_MONO}>{row.variants.map((v) => `:${v.name}`).join(' ')}</span>}
                    </span>
                  )}
                </div>
              )}
            />
            {!compact && (
              <BeamColumn<ListedStack>
                field="layer"
                header="From"
                width="110px"
                children={fromBadges}
              />
            )}
            {!compact && (
              <BeamColumn<ListedStack>
                field="target"
                header="Backend"
                width="100px"
                children={(row) => <span style={QUIET_MONO}>{row.target ?? 'local'}</span>}
              />
            )}
            {!compact && (
              <BeamColumn<ListedStack>
                field="name"
                header="Variants"
                children={(row) => (
                  <span style={QUIET_MONO}>{row.variants.length ? row.variants.map((v) => `:${v.name}`).join(' ') : '-'}</span>
                )}
              />
            )}
            <BeamColumn<ListedStack>
              field="name"
              header=""
              width={compact ? '84px' : '150px'}
              align="right"
              children={(row) => (
                <span style={{ ...ROW, justifyContent: 'flex-end', flexWrap: 'nowrap' }}>
                  <BeamButton size="small" variant="brand" disabled={operationBusy || running != null} onClick={() => { setStartVariants([]); setStartTarget(row) }}
                    title={compact ? 'Start' : undefined}>
                    <BeamIcon slot="start" name="play" />{compact ? null : ' Start'}
                  </BeamButton>
                  <BeamDropdown placement="bottom-end">
                    <BeamButton slot="trigger" appearance="plain" size="small">
                      <BeamIcon name="ellipsis-vertical" style={{ fontSize: '0.875rem' }} />
                    </BeamButton>
                    <BeamDropdownItem onClick={() => openPreview(row.name)}>
                      <span style={MENU_ITEM}><BeamIcon name="list-check" style={{ fontSize: '0.8rem' }} /> What it runs</span>
                    </BeamDropdownItem>
                    {row.layer !== 'built-in' && (
                      <BeamDropdownItem onClick={() => openEdit(row)}>
                        <span style={MENU_ITEM}><BeamIcon name="pen" style={{ fontSize: '0.8rem' }} /> Edit</span>
                      </BeamDropdownItem>
                    )}
                    {row.layer !== 'built-in' && (
                      <BeamDropdownItem onClick={() => setDeleteTarget(row)}>
                        <span style={{ ...MENU_ITEM, color: 'var(--color-beam-error)' }}><BeamIcon name="trash" style={{ fontSize: '0.8rem' }} /> Delete</span>
                      </BeamDropdownItem>
                    )}
                  </BeamDropdown>
                </span>
              )}
            />
          </BeamTable>
        </BeamTabPanel>

        {/* ── RUNNING ── */}
        <BeamTabPanel name="running">
          <div style={STACK}>
            {operation && (
              <BeamCallout variant={!operation.finished ? 'brand' : operation.succeeded ? 'success' : 'danger'}>
                <BeamIcon slot="icon" name={!operation.finished ? 'spinner' : operation.succeeded ? 'check' : 'xmark'} />
                <div style={STACK}>
                  <span style={{ ...ROW }}>
                    {!operation.finished && <BeamSpinner />}
                    <CommandLine line={operation.commandLine} style={{ ...text, ...mono }} />
                    <span style={QUIET}>
                      {operation.finished
                        ? `${operation.succeeded ? 'finished' : `failed, exit ${operation.exitCode}`} after ${Math.round((operation.finishedAt - operation.startedAt) / 1000)}s`
                        : `running for ${Math.round((Date.now() - operation.startedAt) / 1000)}s…`}
                    </span>
                  </span>
                  {operation.output.length > 0 && (
                    <FollowLog lines={operation.output} style={{ maxHeight: '14rem' }} />
                  )}
                </div>
              </BeamCallout>
            )}

            {!running ? (
              <BeamCallout variant="neutral">
                <BeamIcon slot="icon" name="circle-info" />
                No stack is running. Start one from the Stacks tab.
              </BeamCallout>
            ) : (
              <>
                <div style={{ ...ROW, justifyContent: 'space-between' }}>
                  <span style={{ ...ROW }}>
                    <span style={{ ...text, fontWeight: 600 }}>{running.stack}</span>
                    <BeamBadge variant={running.up ? 'success' : 'warning'} appearance="filled" pill>
                      {running.up ? 'up' : 'starting'}
                    </BeamBadge>
                    <span style={QUIET}>{ready} of {running.steps.length} ready{running.attached ? ' · attached to a terminal' : ''}</span>
                    {running.portalUrl && <a href={running.portalUrl} target="_blank" rel="noreferrer" style={QUIET}>Portal</a>}
                  </span>
                  <span style={{ ...ROW }}>
                    {grafana && (
                      <BeamButton size="small" appearance="outlined" href={grafana.all} target="_blank">
                        <BeamIcon slot="start" name="chart-line" /> Logs in Grafana
                      </BeamButton>
                    )}
                    <BeamButton size="small" appearance="outlined" disabled={operationBusy} onClick={() => act('Stopping', () => stopStack(false))}>
                      <BeamIcon slot="start" name="stop" /> Stop
                    </BeamButton>
                    <BeamButton size="small" variant="danger" appearance="outlined" disabled={operationBusy} onClick={() => setPurgeAsk(true)}>
                      Stop &amp; purge
                    </BeamButton>
                  </span>
                </div>
                {!grafana && (
                  <span style={QUIET}>
                    {running.stack} doesn't send its logs to Grafana (it has no <code style={mono}>logs: forward</code> step).
                    Stacks that do, such as <code style={mono}>full</code>, <code style={mono}>backend</code> and{' '}
                    <code style={mono}>csharp-portal</code>, link to their Grafana dashboards here.
                  </span>
                )}
                <BeamTable<StepStatus>
                  data={running.steps}
                  pageSize={50}
                  pageSizeOptions={[25, 50, 100]}
                  rowKey={(r) => r.name}
                  emptyMessage="No steps"
                >
                  <BeamColumn<StepStatus>
                    field="name"
                    header="Step"
                    sortable
                    children={(row) => (
                      // Port and detail sit under the name, so the table fits a phone.
                      <span style={{ display: 'flex', flexDirection: 'column', gap: '0.125rem', minWidth: 0 }}>
                        <span style={{ ...text, fontSize: '13px' }}>
                          {row.label || row.name}
                          {row.port ? <span style={{ ...QUIET_MONO, marginInlineStart: '0.5rem' }}>:{row.port}</span> : null}
                        </span>
                        {row.detail && <span style={{ ...QUIET, overflowWrap: 'anywhere' }}>{row.detail}</span>}
                        {narrow && (
                          <span style={{ ...ROW, gap: '0.375rem' }}>
                            <BeamBadge variant={stateVariant(row.state)} appearance="filled" pill>{row.state ?? '-'}</BeamBadge>
                            <span style={QUIET_MONO}>{row.elapsedSeconds}s</span>
                          </span>
                        )}
                      </span>
                    )}
                  />
                  {!narrow && (
                    <BeamColumn<StepStatus>
                      field="state"
                      header="State"
                      width="120px"
                      sortable
                      children={(row) => <BeamBadge variant={stateVariant(row.state)} appearance="filled" pill>{row.state ?? '-'}</BeamBadge>}
                    />
                  )}
                  {!narrow && (
                    <BeamColumn<StepStatus>
                      field="elapsedSeconds"
                      header="Time"
                      width="80px"
                      children={(row) => <span style={QUIET_MONO}>{row.elapsedSeconds}s</span>}
                    />
                  )}
                  <BeamColumn<StepStatus>
                    field="name"
                    header=""
                    width="36px"
                    align="right"
                    children={(row) => (
                      <BeamDropdown placement="bottom-end">
                        <BeamButton slot="trigger" appearance="plain" size="small">
                          <BeamIcon name="ellipsis-vertical" style={{ fontSize: '0.875rem' }} />
                        </BeamButton>
                        <BeamDropdownItem onClick={() => openLogs(row)}>
                          <span style={MENU_ITEM}><BeamIcon name="file-lines" style={{ fontSize: '0.8rem' }} /> Log</span>
                        </BeamDropdownItem>
                        {grafana?.steps[row.name] && (
                          <BeamDropdownItem onClick={() => window.open(grafana.steps[row.name], '_blank', 'noopener')}>
                            <span style={MENU_ITEM}><BeamIcon name="chart-line" style={{ fontSize: '0.8rem' }} /> Logs in Grafana</span>
                          </BeamDropdownItem>
                        )}
                        {!row.once && (
                          <BeamDropdownItem disabled={operationBusy} onClick={() => act(`Restarting ${row.name}`, () => stepAction(row.name, 'restart'))}>
                            <span style={MENU_ITEM}><BeamIcon name="rotate" style={{ fontSize: '0.8rem' }} /> Restart</span>
                          </BeamDropdownItem>
                        )}
                        {!row.once && (row.running ? (
                          <BeamDropdownItem disabled={operationBusy} onClick={() => act(`Stopping ${row.name}`, () => stepAction(row.name, 'stop'))}>
                            <span style={MENU_ITEM}><BeamIcon name="stop" style={{ fontSize: '0.8rem' }} /> Stop</span>
                          </BeamDropdownItem>
                        ) : (
                          <BeamDropdownItem disabled={operationBusy} onClick={() => act(`Starting ${row.name}`, () => stepAction(row.name, 'start'))}>
                            <span style={MENU_ITEM}><BeamIcon name="play" style={{ fontSize: '0.8rem' }} /> Start</span>
                          </BeamDropdownItem>
                        ))}
                      </BeamDropdown>
                    )}
                  />
                </BeamTable>
              </>
            )}
          </div>
        </BeamTabPanel>
      </BeamTabGroup>

      {/* ── Start ── */}
      <BeamDialog
        open={startTarget != null}
        label={`Start ${startTarget?.name ?? ''}`}
        with-footer
        onWaHide={(e: Event) => { if (e.target === e.currentTarget) setStartTarget(null) }}
        style={{ '--width': '32rem' } as CSSProperties}
      >
        {startTarget && (
          <div style={STACK}>
            <BoundSelect
              key={startTarget.name}
              label="Variants"
              multiple
              clearable
              placeholder="None"
              defaultValue={[]}
              onChange={(v: string[]) => setStartVariants(v)}
            >
              {startTarget.variants.map((v) => <BeamOption key={v.name} value={v.name}>:{v.name}{v.description ? ` (${v.description})` : ''}</BeamOption>)}
              {(listing?.builtInVariants ?? []).map((v) => <BeamOption key={v} value={v} disabled={conflicts(v)}>:{v}</BeamOption>)}
              {(listing?.targets ?? []).map((t) => <BeamOption key={`target-${t}`} value={t} disabled={conflicts(t)}>:{t} (backend)</BeamOption>)}
            </BoundSelect>
            {startVariants.length > 0 && (
              <div style={{ ...STACK, gap: '0.375rem' }}>
                {startVariants.map((v) => (
                  <span key={v} style={QUIET}>
                    <code style={{ ...mono, ...text }}>:{v}</code>{' '}
                    {startTarget.variants.find((sv) => sv.name === v)?.description ?? listing?.descriptions?.[v] ?? 'No description.'}
                  </span>
                ))}
              </div>
            )}
            <span style={QUIET}>
              Runs <CommandLine line={`beam local run ${startName} --detach`} style={mono} />. It keeps running when you close this page or stop the CLI.
            </span>
          </div>
        )}
        <div slot="footer" style={{ ...ROW, justifyContent: 'flex-end' }}>
          <BeamButton appearance="outlined" size="small" onClick={() => setStartTarget(null)}>Cancel</BeamButton>
          <BeamButton size="small" variant="brand" onClick={confirmStart}>Start</BeamButton>
        </div>
      </BeamDialog>

      {/* ── Preview ── */}
      <BeamDialog
        open={preview != null}
        label={`What ${preview?.name ?? ''} runs`}
        onWaHide={(e: Event) => { if (e.target === e.currentTarget) setPreview(null) }}
        style={{ '--width': '52rem' } as CSSProperties}
      >
        {preview && !preview.result && !preview.error && (
          <div style={{ display: 'flex', justifyContent: 'center', padding: '2rem' }}><BeamSpinner style={{ fontSize: '1.5rem' }} /></div>
        )}
        {preview?.error && <BeamCallout variant="danger">{preview.error}</BeamCallout>}
        {preview?.result && (
          <div style={STACK}>
            {preview.result.description && <span style={QUIET}>{preview.result.description}</span>}
            <BeamTable<DryRun['steps'][number]>
              data={preview.result.steps}
              pageSize={100}
              rowKey={(r) => r.name}
              emptyMessage="No steps"
            >
              <BeamColumn<DryRun['steps'][number]>
                field="name"
                header="Step"
                width="40%"
                children={(row) => <span style={{ ...text, fontSize: '13px', opacity: row.selected ? 1 : 0.5 }}>{row.label || row.name}</span>}
              />
              <BeamColumn<DryRun['steps'][number]>
                field="runs"
                header="Runs"
                children={(row) => <span style={QUIET}>{row.runs ?? ''}</span>}
              />
              <BeamColumn<DryRun['steps'][number]>
                field="port"
                header="Port"
                width="70px"
                children={(row) => <span style={QUIET_MONO}>{row.port || ''}</span>}
              />
            </BeamTable>
          </div>
        )}
      </BeamDialog>

      {/* ── Create / edit ── */}
      <BeamDialog
        open={formOpen}
        label={formMode === 'create' ? 'New stack' : `Edit ${form.name}`}
        with-footer
        onWaHide={(e: Event) => { if (e.target === e.currentTarget && !saving) setFormOpen(false) }}
        style={{ '--width': '44rem' } as CSSProperties}
      >
        <div key={formKey} style={STACK}>
          <div style={{ ...ROW, alignItems: 'flex-end', flexWrap: 'nowrap' }}>
            <BeamInput
              size="small"
              label="Name"
              value={form.name}
              disabled={formMode === 'edit'}
              onValueInput={(v: string) => setForm({ ...form, name: v })}
              onValueChange={(v: string) => setForm({ ...form, name: v })}
              placeholder="wallet-dev"
              style={{ flex: 1 }}
            />
            <BoundSelect
              label="For"
              defaultValue={form.shared ? 'shared' : 'local'}
              disabled={formMode === 'edit'}
              onChange={(v: string) => setForm((f) => ({ ...f, shared: v === 'shared' }))}
              style={{ width: '14rem' }}
            >
              <BeamOption value="local">Just me (not committed)</BeamOption>
              <BeamOption value="shared">The team (committed)</BeamOption>
            </BoundSelect>
          </div>
          <BoundSelect
            label="Starts from"
            defaultValue={form.extends || NONE}
            onChange={(v: string) => setForm((f) => ({ ...f, extends: v === NONE ? '' : v }))}
          >
            <BeamOption value={NONE}>None: start from nothing</BeamOption>
            {(choices?.stacks ?? []).filter((s) => s !== form.name).map((s) => <BeamOption key={s} value={s}>{s}</BeamOption>)}
          </BoundSelect>
          <StepPicker
            label={form.extends ? `Adds (tags or steps ${form.extends} doesn't run)` : 'Runs (tags or steps)'}
            groups={groupsOf(addChoices)}
            value={form.include}
            onChange={(v) => setForm((f) => ({ ...f, include: v }))}
          />
          {(form.extends || form.exclude.length > 0) && (
            <StepPicker
              label={`Leaves out (tags or steps ${form.extends || 'it starts from'} runs)`}
              groups={groupsOf(leaveOutChoices)}
              value={form.exclude}
              onChange={(v) => setForm((f) => ({ ...f, exclude: v }))}
            />
          )}
          <StepPicker
            label="This workspace's service groups, microservices and portal extensions"
            emptyText="None"
            searchPlaceholder="Search groups, microservices and extensions"
            groups={[
              { title: 'Service groups (every microservice and extension in the group)', items: choices?.groups ?? [] },
              { title: 'Microservices', items: choices?.services ?? [] },
              { title: 'Portal extensions', items: choices?.extensions ?? [] },
              // Picked before but no longer in this workspace: still listed so it can be taken out.
              {
                title: 'Other',
                items: [...form.groups, ...form.services, ...form.extensions].filter((id) =>
                  !choices?.groups.includes(id) && !choices?.services.includes(id) && !choices?.extensions.includes(id)),
              },
            ].filter((g) => g.items.length > 0)}
            value={[...form.groups, ...form.services, ...form.extensions]}
            onChange={(v) => setForm((f) => ({
              ...f,
              groups: v.filter((id) => choices?.groups.includes(id)),
              extensions: v.filter((id) => choices?.extensions.includes(id)),
              services: v.filter((id) => !choices?.groups.includes(id) && !choices?.extensions.includes(id)),
            }))}
          />
          <BoundSelect
            label="Backend"
            clearable
            placeholder="Same as what it starts from (local)"
            defaultValue={form.target}
            onChange={(v: string) => setForm((f) => ({ ...f, target: v }))}
          >
            {(choices?.targets ?? []).map((t) => <BeamOption key={t} value={t}>{t}</BeamOption>)}
          </BoundSelect>
          <BeamTextarea
            size="small"
            label={form.shared ? 'Description (required for the team)' : 'Description'}
            value={form.description}
            rows={2}
            onValueInput={(v: string) => setForm((f) => ({ ...f, description: v }))}
            onValueChange={(v: string) => setForm((f) => ({ ...f, description: v }))}
          />
          <div style={{ ...STACK, gap: '0.5rem' }}>
            <div style={{ ...ROW, justifyContent: 'space-between' }}>
              <span style={{ ...text, fontWeight: 600, fontSize: '13px' }}>Own commands</span>
              <BeamButton size="small" appearance="outlined" onClick={() => setCommands((rows) => [...rows, commandRow()])}>
                <BeamIcon slot="start" name="plus" /> Add command
              </BeamButton>
            </div>
            {commands.length === 0 && <span style={QUIET}>None. A command of its own is a data seed, a watcher, a script…</span>}
            {commands.map((row) => {
              const c = row.command
              const set = (field: keyof OwnCommand) => (v: unknown) => setField(row.key, field, v)
              return (
                <BeamDetails
                  key={row.key}
                  open={row.open}
                  // Only the details' own toggles: the selects inside it send wa-show/wa-hide that bubble up here too.
                  onWaShow={(e: CustomEvent) => { if (e.target === e.currentTarget) updateCommand(row.key, (r) => ({ ...r, open: true })) }}
                  onWaHide={(e: CustomEvent) => { if (e.target === e.currentTarget) updateCommand(row.key, (r) => ({ ...r, open: false })) }}
                >
                  <span slot="summary" style={{ ...ROW, gap: '0.5rem', flexWrap: 'nowrap', minWidth: 0 }}>
                    <span style={{ ...text, fontWeight: 600, fontSize: '13px', whiteSpace: 'nowrap' }}>{c.name.trim() || 'New command'}</span>
                    <span style={{ ...QUIET_MONO, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{commandSummary(row)}</span>
                  </span>
                  <div style={{ ...STACK, gap: '0.5rem' }}>
                  <div style={{ ...ROW, alignItems: 'flex-end', flexWrap: 'nowrap' }}>
                    <BeamInput size="small" label="Name" placeholder="seed: demo" value={c.name} style={{ flex: 1 }} onValueInput={set('name')} />
                    <BoundSelect label="Runs" defaultValue={row.kind} style={{ width: '11rem' }}
                      onChange={(v: string) => updateCommand(row.key, (r) => ({ ...r, kind: v as CommandKind }))}>
                      <BeamOption value="program">A program</BeamOption>
                      <BeamOption value="shell">A shell script</BeamOption>
                      <BeamOption value="beam">A beam command</BeamOption>
                    </BoundSelect>
                    <BeamButton size="small" appearance="plain" title="Remove this command"
                      onClick={() => setCommands((rows) => rows.filter((r) => r.key !== row.key))}>
                      <BeamIcon name="trash" />
                    </BeamButton>
                  </div>
                  {row.kind === 'program' && (
                    <div style={{ ...ROW, flexWrap: 'nowrap' }}>
                      <BeamInput size="small" label="Program" placeholder="node" value={c.command ?? ''} style={{ width: '12rem' }} onValueInput={set('command')} />
                      <BeamInput size="small" label="Arguments" placeholder="scripts/seed.mjs" value={c.args ?? ''} style={{ flex: 1 }} onValueInput={set('args')} />
                    </div>
                  )}
                  {row.kind === 'shell' && (
                    <div style={{ ...ROW, alignItems: 'flex-end', flexWrap: 'nowrap' }}>
                      <BeamInput size="small" label="Script" placeholder="./scripts/reset.sh" value={c.shell ?? ''} style={{ flex: 1 }} onValueInput={set('shell')} />
                      <BoundSelect label="Shell" defaultValue={c.shellKind || 'default'} style={{ width: '11rem' }}
                        onChange={(v: string) => setField(row.key, 'shellKind', v === 'default' ? undefined : v)}>
                        <BeamOption value="default">This system's</BeamOption>
                        <BeamOption value="sh">sh</BeamOption>
                        <BeamOption value="powershell">PowerShell</BeamOption>
                      </BoundSelect>
                    </div>
                  )}
                  {row.kind === 'beam' && (
                    <BeamInput size="small" label="beam command" placeholder="content publish" value={c.beam ?? ''} onValueInput={set('beam')} />
                  )}
                  <div style={{ ...ROW, flexWrap: 'nowrap' }}>
                    <BeamInput size="small" label="Folder (optional; variables such as ${PortalPath} work)" value={c.dir ?? ''} style={{ flex: 1 }} onValueInput={set('dir')} />
                    <BeamInput size="small" label="Environment (KEY=VALUE, comma separated)" value={row.envText} style={{ flex: 1 }}
                      onValueInput={(v: string) => updateCommand(row.key, (r) => ({ ...r, envText: v }))} />
                  </div>
                  <div style={{ ...ROW, flexWrap: 'nowrap' }}>
                    <BeamInput size="small" label="Ready when this URL answers (optional)" value={c.readyUrl ?? ''} style={{ flex: 1 }} onValueInput={set('readyUrl')} />
                    <BeamInput size="small" label="…or its output contains (optional)" value={c.readyLog ?? ''} style={{ flex: 1 }} onValueInput={set('readyLog')} />
                  </div>
                  <BoundSelect label="Runs after (a step this stack runs)" clearable placeholder="Every other step" defaultValue={c.after ?? ''} onChange={set('after')}>
                    {groupsOf(
                      [...stackSteps, ...(c.after && !stackSteps.includes(c.after) && !commands.some((o) => o.command.name.trim() === c.after) ? [c.after] : [])],
                      [{ title: 'Its own commands', items: commands.filter((o) => o.key !== row.key && o.command.name.trim()).map((o) => o.command.name.trim()) }]
                        .filter((g) => g.items.length > 0),
                    ).map((g, i) => [
                      i > 0 ? <BeamDivider key={`d-${g.title}`} /> : null,
                      <small key={`t-${g.title}`}>{g.title}</small>,
                      ...g.items.map((name) => <BeamOption key={`${g.title}-${name}`} value={name}>{name}</BeamOption>),
                    ])}
                  </BoundSelect>
                  <BeamCheckbox checked={c.once ?? false} onCheckedChange={set('once')}>
                    Runs to completion (a seed, a script) instead of staying up; the next step waits for it
                  </BeamCheckbox>
                  </div>
                </BeamDetails>
              )
            })}
          </div>
          <span style={QUIET}>
            The CLI checks the stack against this machine before writing it, and adds what your picks need (the Docker
            dependencies, the gateway, the services an extension calls).
          </span>
        </div>
        <div slot="footer" style={{ ...ROW, justifyContent: 'space-between' }}>
          <BeamButton appearance="plain" size="small" disabled={saving} onClick={() => loadChoices(true)}>
            <BeamIcon slot="start" name="rotate" /> Rescan workspace
          </BeamButton>
          <span style={{ ...ROW }}>
            <BeamButton appearance="outlined" size="small" disabled={saving} onClick={() => setFormOpen(false)}>Cancel</BeamButton>
            <BeamButton size="small" variant="brand" disabled={saving} onClick={submitForm}>
              {saving ? 'Saving…' : formMode === 'create' ? 'Create' : 'Save'}
            </BeamButton>
          </span>
        </div>
      </BeamDialog>

      {/* ── Log ── */}
      <BeamDialog
        open={logs != null}
        label={`Log of ${logs?.step ?? ''}`}
        with-footer={!!(logs && grafana?.steps[logs.step])}
        onWaHide={(e: Event) => { if (e.target === e.currentTarget) setLogs(null) }}
        style={{ '--width': '64rem' } as CSSProperties}
      >
        {logs && (logs.lines.length ? <FollowLog lines={logs.lines} /> : <span style={QUIET}>Nothing logged yet.</span>)}
        {logs && grafana?.steps[logs.step] && (
          <div slot="footer">
            <BeamButton size="small" appearance="outlined" href={grafana.steps[logs.step]} target="_blank">
              <BeamIcon slot="start" name="chart-line" /> Open in Grafana
            </BeamButton>
          </div>
        )}
      </BeamDialog>

      <BeamConfirmDialog
        open={deleteTarget != null}
        label="Delete stack"
        message={`Delete ${deleteTarget?.name ?? ''} from ${deleteTarget?.layer === 'shared' ? 'the team stacks (committed)' : 'your stacks'}?`}
        confirm-label="Delete"
        confirm-variant="danger"
        onWaConfirm={confirmDelete}
        onWaCancel={() => setDeleteTarget(null)}
      />

      <BeamConfirmDialog
        open={purgeAsk}
        label="Stop and purge"
        message="Stop the stack and delete the local database and logs? The next run creates a new org and realm."
        confirm-label="Stop & purge"
        confirm-variant="danger"
        onWaConfirm={() => { setPurgeAsk(false); act('Stopping', () => stopStack(true)) }}
        onWaCancel={() => setPurgeAsk(false)}
      />

      <Toaster />
    </div>
  )
}
