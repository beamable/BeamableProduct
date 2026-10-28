// The stacks API `beam local management` serves next to this page (LocalStackManagementApi in the CLI): reads from the
// workspace's settings, stack files and run state; edits and previews as jobs; start/stop as one CLI operation at a time.

import { api, jobChannel, jobError, runJob, type Job } from '../shared/connection'

export type Layer = 'built-in' | 'shared' | 'local'

export interface Workspace {
  workspace: string
  settings: string
  hasSettings: boolean
}

export interface ListedVariant { name: string; description: string | null; layer: Layer }

export interface ListedStack {
  name: string
  description: string | null
  layer: Layer
  /** The backend it runs against (local, dev, …). */
  target: string | null
  /** Layers that change a stack defined in another one (a personal tweak of a built-in stack). */
  changedBy: Layer[]
  variants: ListedVariant[]
}

export interface Listing {
  stacks: ListedStack[]
  /** Variants every stack has (build, web, …). */
  builtInVariants: string[]
  /** Backends, each also a variant (:dev). */
  targets: string[]
  /** What each built-in variant and each backend variant does. */
  descriptions: Record<string, string | null>
}

export interface StackDef {
  description?: string | null
  extends?: string | null
  target?: string | null
  include?: string[] | null
  exclude?: string[] | null
  /** Microservices, portal extensions and service groups of the workspace the stack runs. */
  extensions?: string[] | null
  /** The stack's own commands, by name. */
  steps?: Record<string, { label?: string | null; command?: string | null; arguments?: string | null; beam?: boolean } | null> | null
}

export interface Definition {
  name: string
  layers: { layer: Layer; file: string; definition: StackDef; commands: OwnCommand[] }[]
}

export interface Choices {
  stacks: string[]
  steps: { name: string; label: string | null; tags: string[] }[]
  tags: string[]
  services: string[]
  extensions: string[]
  groups: string[]
  targets: string[]
  builtInVariants: string[]
}

export interface StepStatus {
  name: string
  label: string | null
  group: string | null
  kind: string | null
  running: boolean
  state: string | null
  elapsedSeconds: number
  port: number
  detail: string | null
  once: boolean
}

export interface StackStatus {
  stack: string | null
  attached: boolean
  host: string | null
  portalUrl: string | null
  backendHealthy: boolean
  up: boolean
  steps: StepStatus[]
}

export interface Operation {
  op: string
  commandLine: string
  startedAt: number
  finishedAt: number
  finished: boolean
  succeeded: boolean
  exitCode: number
  output: string[]
}

export interface Status {
  stack: StackStatus
  operation: Operation | null
  /** Where the stack's logs are in Grafana; null when it doesn't forward them. */
  grafana: { dashboards: string; all: string; steps: Record<string, string> } | null
}

export interface DryRun {
  stack: string
  description: string | null
  steps: { name: string; label: string | null; runs: string | null; selected: boolean; port: number; folder: string | null }[]
  checks: { name?: string; ok?: boolean; message?: string }[]
}

/**
 * A command of the stack's own, in the shape `beam local create --commands` takes: one of `command` (with `args`),
 * `shell` (with `shellKind`) or `beam`, and what the `--step-*` options set.
 */
export interface OwnCommand {
  name: string
  command?: string
  args?: string
  shell?: string
  shellKind?: string
  beam?: string
  dir?: string
  env?: string[]
  once?: boolean
  readyUrl?: string
  readyLog?: string
  port?: number
  timeout?: number
  label?: string
  tags?: string
  after?: string
  first?: boolean
}

/** What the stack form sends: every field, so an edit replaces what it shows. */
export interface StackForm {
  name: string
  shared: boolean
  extends: string
  include: string[]
  exclude: string[]
  services: string[]
  extensions: string[]
  groups: string[]
  target: string
  description: string
  /** All the stack's own commands; an edit replaces the ones it has. Left out, they stay as they are. */
  commands?: OwnCommand[]
}

export const getWorkspace = () => api<Workspace>('GET', 'workspace')
export const getStacks = () => api<Listing>('GET', 'stacks')
export const getDefinition = (name: string) => api<Definition>('GET', `stacks/${encodeURIComponent(name)}`)
/** The steps a stack runs, by name. */
export const getStackSteps = (stack: string) =>
  api<{ stack: string; steps: string[] }>('GET', `steps?stack=${encodeURIComponent(stack)}`)
export const getChoices = (refresh = false) => api<Choices>('GET', `choices${refresh ? '?refresh=true' : ''}`)
export const getStatus = () => api<Status>('GET', 'status')
export const getLogs = (step: string, tail = 300) =>
  api<{ step: string; lines: string[] }>('GET', `logs/${encodeURIComponent(step)}?tail=${tail}`)

export const startStack = (stack: string) => api<Operation>('POST', 'run', { stack })
export const stopStack = (purge: boolean) => api<Operation>('POST', 'stop', { purge })
export const stepAction = (step: string, action: 'restart' | 'start' | 'stop') =>
  api<Operation>('POST', `steps/${encodeURIComponent(step)}/${action}`)

/** Runs a create/edit/delete/dry-run job and throws its error when it fails. */
async function job(params: Record<string, unknown>): Promise<Job> {
  const done = await runJob(params)
  if (done.status !== 'succeeded') throw new Error(jobError(done))
  return done
}

export const saveStack = (op: 'create' | 'edit', form: StackForm) => job({ op, ...form })
export const deleteStack = (name: string, shared: boolean) => job({ op: 'delete', name, shared })

export async function dryRun(stack: string): Promise<DryRun> {
  const done = await job({ op: 'dry-run', stack })
  const result = jobChannel<DryRun>(done, 'dryRun')
  if (!result) throw new Error(`\`${done.commandLine}\` reported nothing.`)
  return result
}
