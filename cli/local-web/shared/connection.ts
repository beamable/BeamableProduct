// The page talks to the CLI that serves it, on the same origin, under /api. The CLI opens the page with this run's
// session token in the fragment (#token=…), which browsers never send to a server; it is kept for this tab and taken
// out of the address bar, so a reload still works and the token doesn't travel with a copied URL.

const TOKEN_KEY = 'beam-local-web-token'

export function sessionToken(): string | null {
  const fromHash = new URLSearchParams(window.location.hash.replace(/^#/, '')).get('token')
  if (fromHash) {
    sessionStorage.setItem(TOKEN_KEY, fromHash)
    history.replaceState(null, '', window.location.pathname + window.location.search)
    return fromHash
  }
  return sessionStorage.getItem(TOKEN_KEY)
}

export class ApiError extends Error {
  constructor(message: string, readonly status: number) {
    super(message)
  }
}

/** Calls the CLI's API. Throws {@link ApiError} with the CLI's message for a refused request. */
export async function api<T>(method: 'GET' | 'POST', path: string, body?: unknown): Promise<T> {
  const resp = await fetch(`/api/${path}`, {
    method,
    cache: 'no-store',
    headers: {
      Authorization: `Bearer ${sessionToken() ?? ''}`,
      ...(body !== undefined ? { 'Content-Type': 'application/json' } : {}),
    },
    ...(body !== undefined ? { body: JSON.stringify(body) } : {}),
  })
  const text = await resp.text()
  if (!resp.ok) {
    let message = text
    try { message = JSON.parse(text).message ?? text } catch { /* not JSON */ }
    throw new ApiError(message || `${method} ${path} failed (${resp.status})`, resp.status)
  }
  return (text ? JSON.parse(text) : null) as T
}

// ── Jobs: CLI commands the API runs for the page, one at a time ──

export type JobStatus = 'queued' | 'running' | 'succeeded' | 'failed' | 'cancelled'

export interface JobLog { level: string; message: string; ts: number }

export interface JobProgress {
  name: string
  /** 0..1; below 0 when the step failed. */
  ratio: number
  /** False for steps that only report started/done. */
  isKnownLength: boolean
  serviceName?: string | null
}

export interface Job {
  id: string
  op: string
  commandLine: string
  status: JobStatus
  createdAt: number
  startedAt: number
  finishedAt: number
  logCount: number
  logs: JobLog[]
  errors: string[]
  progress: JobProgress[]
  /** The command's result, as JSON. */
  resultJson: string | null
  /** The command's other channels (progress, listings, build errors), latest payload each. */
  channels: { channel: string; json: string }[]
}

export const TERMINAL: JobStatus[] = ['succeeded', 'failed', 'cancelled']

/** `base` is the API the jobs belong to: '' for the stacks, 'bundles/' for the bundles. */
export function startJob(params: Record<string, unknown>, base = ''): Promise<Job> {
  return api('POST', `${base}jobs`, params)
}

export function getJob(jobId: string, fromLog: number, base = ''): Promise<Job> {
  return api('GET', `${base}jobs/${encodeURIComponent(jobId)}?fromLog=${fromLog}`)
}

export function cancelJob(jobId: string, base = ''): Promise<Job> {
  return api('POST', `${base}jobs/${encodeURIComponent(jobId)}/cancel`)
}

/**
 * Starts a job and polls it to the end. `onUpdate` gets the job with every log line so far.
 * Resolves with the finished job; check `status`: a failed job does not reject.
 */
export async function runJob(params: Record<string, unknown>, onUpdate?: (job: Job) => void, base = ''): Promise<Job> {
  let job = await startJob(params, base)
  const logs: JobLog[] = []
  for (;;) {
    job = await getJob(job.id, logs.length, base)
    logs.push(...job.logs)
    job = { ...job, logs: [...logs] }
    onUpdate?.(job)
    if (TERMINAL.includes(job.status)) return job
    await new Promise((r) => setTimeout(r, 500))
  }
}

/** A channel's latest payload, parsed, or null when the command didn't report one. */
export function jobChannel<T>(job: Job, channel: string): T | null {
  const found = job.channels.find((c) => c.channel === channel)
  return found ? (JSON.parse(found.json) as T) : null
}

export function jobError(job: Job): string {
  return job.errors.length ? job.errors.join('\n') : `${job.op} ${job.status}`
}
