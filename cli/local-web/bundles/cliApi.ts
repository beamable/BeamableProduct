// The bundles API the CLI serves under /api/bundles (BundlesApi). Every operation is a job: an existing `bundles` /
// `deploy` command the CLI runs against its workspace, with its login.

import { api, runJob, type Job } from '../shared/connection'

export { jobError, type Job, type JobProgress } from '../shared/connection'

export interface Workspace {
  cid: string
  pid: string
  zid: string
  host: string
  /** A string: account ids exceed Number.MAX_SAFE_INTEGER. */
  ownerAccountId: string
  ownerEmail: string
  workingDirectory: string
}

/** What `beam bundles list` reports. */
export interface ListResult {
  /** The org's own bundles in the catalog, private ones included. */
  published: PublishedBundle[]
  /** Bundles authored in this workspace; they may have no release, or no catalog entry, yet. */
  local: LocalBundle[]
}

export interface BundleTag { tag: string; version: number }

export interface BundleNotice {
  id: string
  minVersion: number
  maxVersion: number
  endOfSupport: string
  endOfLife: string
  message: string
  link: string | null
  replacement: string | null
  /** "supported", "deprecated", "unsupported" or "endoflife". */
  status: string
}

export interface PublishedBundle {
  name: string
  ownerCid: string
  /** "private" or "public". */
  visibility: string
  displayName: string
  summary: string
  categories: string[]
  autoUpdateRequired: boolean
  /** The version `latest` points at; 0 when there is no release. */
  latestVersion: number
  tags: BundleTag[]
  notices: BundleNotice[]
}

export interface BundleComponent {
  name: string
  /** "service", "storage" or "extension". */
  kind: string
  /** "realm" or "zone". */
  site: string
  /** Whether orgs that acquire the bundle may get this component's source. */
  source: boolean
}

export interface LocalBundle {
  name: string
  filePath: string
  components: BundleComponent[]
  /** Component group name to its components. */
  componentGroups: Record<string, string[]>
  /** Bundle name to "hard 1+" or "soft 1-5". */
  dependencies: Record<string, string>
}

export interface Release {
  version: number
  checksum: string
  /** Unix seconds. */
  publishedAt: number
  publishedBy: string | null
  yanked: boolean
  irreversibleDataChange: boolean
}

export interface ReleaseContent {
  version: number
  checksum: string
  components: BundleComponent[]
  componentGroups: Record<string, string[]>
  dependencies: Record<string, string>
}

/** What `beam bundles releases` reports; `content` only with a version. */
export interface ReleasesResult {
  name: string
  releases: Release[]
  tags: BundleTag[]
  notices: BundleNotice[]
  content: ReleaseContent | null
}

export interface ComponentChange {
  name: string
  kind: string
  /** "Added", "Removed" or "Changed". */
  change: string
  local: string | null
  published: string | null
}

export interface PlanResult {
  name: string
  isNoOp: boolean
  publishedVersion: number
  changes: ComponentChange[]
  planPath: string
}

export interface PublishResult {
  name: string
  checksum: string
  /** False when the content equals the latest release's, so no version was made. */
  isNewVersion: boolean
  version: number
  warnings: string[]
}

export interface InstallationTarget {
  version: number
  componentGroups: string[]
  /** "all", "none", or the chosen realm ids. */
  realms: string
  autoUpdate: boolean
}

export interface SitePlan {
  site: string
  isZone: boolean
  changes: string[]
}

export interface InstallationPlan {
  bundle: string
  zoneId: string
  change: string
  from: InstallationTarget | null
  to: InstallationTarget | null
  sites: SitePlan[]
  refusals: string[]
  warnings: string[]
  blockers: string[]
  compatible: boolean | null
  autoUpdateLocked: boolean
  hash: string
}

export interface SiteResult {
  site: string
  /** "applied", "pending" or "failed". */
  state: string
  error: string | null
  manifestId: string | null
}

/** What `beam bundles install` / `uninstall` reports: the plan, and with a plan hash what applying it did. */
export interface InstallResult {
  plan: InstallationPlan | null
  applied: boolean
  results: SiteResult[]
  warnings: string[]
}

export interface Override {
  site: string
  component: string
  connectionId: string
  baseVersion: number
  changedIn: number | null
}

export interface Installation {
  bundle: string
  zoneId: string
  target: InstallationTarget | null
  status: string
  notice: BundleNotice | null
  uninstalling: boolean
  autoUpdateLocked: boolean
  sites: SiteResult[]
  update: string | null
  overrides: Override[]
}

export interface InstallationsResult { installations: Installation[] }

export interface LibraryEntry {
  bundle: string
  acquired: boolean
  owned: boolean
  acquiredAt: string | null
  installations: { zoneId: string; version: number; status: string; uninstalling: boolean; update: string | null }[]
}

export interface LibraryResult { bundles: LibraryEntry[] }

export interface AcquireResult {
  name: string
  acquired: boolean
  refusals: string[]
  missingHardDependencies: string[]
  suggestedDependencies: string[]
}

export interface HistoryEntry {
  at: string
  change: string
  actor: string | null
  from: InstallationTarget | null
  to: InstallationTarget | null
  results: SiteResult[]
}

export interface HistoryResult { bundle: string; entries: HistoryEntry[] }

export interface DeployPlanResult {
  success: boolean
  planPath: string
  plan: { changeCount?: number; diff?: unknown } | null
}

export interface JobParams {
  bundle?: string
  tag?: string
  version?: string
  comment?: string
  scope?: string
  visibility?: string
  query?: string
  /** publish: publish exactly what the last plan showed. */
  flag?: boolean
  /** install / uninstall: unset keeps the defaults (new install) or the installed ones (change). */
  componentGroups?: string[]
  realms?: string
  autoUpdate?: boolean
  /** install / uninstall: stop at the plan. */
  planOnly?: boolean
  /** install / uninstall: apply only this plan, the one the page showed. */
  planHash?: string
}

export class CliClient {
  getWorkspace(): Promise<Workspace> {
    return api('GET', 'bundles/workspace')
  }

  /**
   * Starts a job and polls it to the end. `onUpdate` gets the job with every log line so far.
   * Resolves with the finished job; check `status`: a failed job does not reject.
   */
  runJob(op: string, params: JobParams = {}, onUpdate?: (job: Job) => void): Promise<Job> {
    return runJob({ op, ...params }, onUpdate, 'bundles/')
  }
}

export function jobResult<T>(job: Job): T | null {
  if (!job.resultJson) return null
  return JSON.parse(job.resultJson) as T
}
