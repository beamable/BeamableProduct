// The bundles API `beam bundles management` serves next to this page (BundlesApi in the CLI). Every operation is a
// job: an existing `bundles` / `deploy` command the CLI runs against its workspace, with its login.

import { api, runJob, type Job } from '../shared/connection'

export { jobError, type Job, type JobProgress } from '../shared/connection'

export interface Pin { name: string; checksum: string }

export interface Workspace {
  cid: string
  pid: string
  zid: string
  host: string
  /** A string: account ids exceed Number.MAX_SAFE_INTEGER. */
  ownerAccountId: string
  ownerEmail: string
  workingDirectory: string
  realmPins: Pin[]
  zonePins: Pin[]
}

/** What `beam bundles list` reports. */
export interface ListResult {
  published: PublishedBundle[]
  local: LocalBundle[]
}

export interface BundleTag { tag: string; checksum: string }

export interface PublishedBundle {
  name: string
  acl: string
  ownerCid: string
  latestChecksum: string
  latestVersion: number
  tags: BundleTag[]
  updatedAt: number
}

export interface LocalBundle {
  name: string
  scope: string
  filePath: string
  serviceNames: string[]
  storageIds: string[]
  portalExtensionNames: string[]
  /** Bundle name to the release range it needs: "1+" (open) or "1-5" (closed). */
  bundleDependencies: Record<string, string>
}

export interface Release {
  version: number
  checksum: string
  publishedAt: number
  comment: string | null
  /** A string: account ids exceed Number.MAX_SAFE_INTEGER (see `jobResult`). */
  publishedByAccountId: string
}

export interface ReleasesResult { releases: Release[]; tags: BundleTag[] }

export interface ComponentChange {
  name: string
  kind: string
  change: string
  localChecksum: string
  publishedChecksum: string
}

export interface PlanResult {
  name: string
  isNoOp: boolean
  publishedChecksum: string
  changes: ComponentChange[]
  planPath: string
}

export interface PublishResult {
  name: string
  checksum: string
  isNew: boolean
  version: number
}

export interface DeployPlanResult {
  success: boolean
  planPath: string
  plan: { changeCount?: number; diff?: unknown } | null
}

export interface JobParams {
  bundle?: string
  tag?: string
  checksum?: string
  comment?: string
  acl?: string
  scope?: string
  flag?: boolean
}

export class CliClient {
  getWorkspace(): Promise<Workspace> {
    return api('GET', 'workspace')
  }

  /**
   * Starts a job and polls it to the end. `onUpdate` gets the job with every log line so far.
   * Resolves with the finished job; check `status`: a failed job does not reject.
   */
  runJob(op: string, params: JobParams = {}, onUpdate?: (job: Job) => void): Promise<Job> {
    return runJob({ op, ...params }, onUpdate)
  }
}

/** Parses a job's result, keeping account ids as strings: they exceed Number.MAX_SAFE_INTEGER. */
export function jobResult<T>(job: Job): T | null {
  if (!job.resultJson) return null
  return JSON.parse(job.resultJson.replace(/"publishedByAccountId":(\d+)/g, '"publishedByAccountId":"$1"')) as T
}
