import fs from 'node:fs';
import path from 'node:path';

/**
 * The subset of `.beamable/config.beam.json` (or env) that `Beam.init` consumes.
 * All fields are optional: the resolver may find nothing, and callers layer their
 * own further fallbacks (a committed seed, `Beam.init`'s built-in defaults, …).
 */
export interface ResolvedBeamConfig {
  cid?: string;
  pid?: string;
  host?: string;
}

/** Options for {@link resolveBeamConfig}. */
export interface ResolveBeamConfigOptions {
  /** Directory to start the upward search from. Default: `process.cwd()`. */
  from?: string;
  /** Stop searching once this directory has been checked. Default: filesystem root. */
  stopAt?: string;
  /** Fall back to `BEAM_CID`/`BEAM_PID`/`BEAM_HOST` env vars. Default: `true`. */
  env?: boolean;
}

/** Read + parse a `.beamable/config.beam.json`, or `null` if absent/invalid. */
function readConfigFile(filePath: string): ResolvedBeamConfig | null {
  try {
    if (!fs.existsSync(filePath)) return null;
    const raw = fs.readFileSync(filePath, 'utf8');
    const data = JSON.parse(raw) as Partial<
      Record<'cid' | 'pid' | 'host', string>
    >;
    return { cid: data.cid, pid: data.pid, host: data.host };
  } catch {
    // Missing file or malformed JSON: treat as "not found" and keep walking.
    return null;
  }
}

/** Read `{ cid, pid, host }` from `BEAM_*` env vars (undefined-valued keys omitted). */
function readEnvConfig(): ResolvedBeamConfig {
  const { BEAM_CID, BEAM_PID, BEAM_HOST } = process.env;
  const config: ResolvedBeamConfig = {};
  if (BEAM_CID) config.cid = BEAM_CID;
  if (BEAM_PID) config.pid = BEAM_PID;
  if (BEAM_HOST) config.host = BEAM_HOST;
  return config;
}

/**
 * Resolves `{ cid, pid, host }` for `Beam.init` at **build time (Node only)**.
 *
 * This walks the directory tree upward from `from`, using the first
 * `.beamable/config.beam.json` that carries both a `cid` and a `pid` (the
 * CLI writes intermediate configs — e.g. a repo root — that hold only telemetry
 * settings; those are skipped and the walk continues). If no usable file is
 * found, it falls back to the `BEAM_CID`/`BEAM_PID`/`BEAM_HOST` env vars.
 *
 * It is intentionally Node/build-time only: a shipped browser/React Native
 * bundle has no filesystem, so the discovered values must be baked into the
 * bundle by the build (e.g. injected via Expo's `extra` in `app.config.js`).
 *
 * Never throws — returns a possibly-empty object; callers layer any further
 * fallbacks (a committed seed config, `Beam.init`'s built-in environment, …).
 *
 * @example
 * // app.config.js / metro / any Node build step
 * const { resolveBeamConfig } = require('@beamable/sdk/node');
 * const beam = resolveBeamConfig({ from: __dirname }); // { cid, pid, host } | {}
 */
export function resolveBeamConfig(
  opts: ResolveBeamConfigOptions = {},
): ResolvedBeamConfig {
  const { from = process.cwd(), stopAt, env = true } = opts;

  let dir = path.resolve(from);
  const stop = stopAt ? path.resolve(stopAt) : null;

  // Walk up until the filesystem root (dir === its own parent).
  while (true) {
    const config = readConfigFile(
      path.join(dir, '.beamable', 'config.beam.json'),
    );
    // Only accept a config that actually carries the SDK credentials; keep
    // walking past CLI-metadata-only configs (e.g. a repo-root `.beamable`).
    if (config?.cid && config.pid) return config;

    if (stop && dir === stop) break;
    const parent = path.dirname(dir);
    if (parent === dir) break;
    dir = parent;
  }

  // Fallback: env vars (only when they yield a cid — otherwise return {}).
  if (env) {
    const envConfig = readEnvConfig();
    if (envConfig.cid) return envConfig;
  }

  return {};
}
