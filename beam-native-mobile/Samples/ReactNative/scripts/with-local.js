#!/usr/bin/env node
// Runs the Expo CLI with APP_VARIANT=local so app.config.js enables Android cleartext HTTP
// for a local-stack build (see the README's "Pointing at a local stack" section).
//
// This is a zero-dependency, cross-platform replacement for `cross-env APP_VARIANT=local …`:
// npm scripts can't set an env var inline on both Windows and POSIX shells, and Node is
// already a hard requirement of the project, so we set it here and delegate to `expo`.
//
// It also understands one extra flag of its own, `--clean`, which it strips before delegating
// (so the Expo CLI never sees it). `--clean` turns the normal incremental build into a genuine
// from-scratch one; see cleanAll() for what it wipes and why.
//
// Usage (via package.json scripts): node scripts/with-local.js run:android [--variant release]
// Full rebuild:                     npm run android:local:release -- --clean
const { spawnSync } = require('child_process');
const fs = require('fs');
const os = require('os');
const path = require('path');

const SAMPLE_DIR = path.resolve(__dirname, '..');
const ANDROID_DIR = path.join(SAMPLE_DIR, 'android');
// The `@beamable/sdk` dependency is `file:../../../web`, i.e. the repo's web SDK source.
const WEB_SDK_DIR = path.resolve(SAMPLE_DIR, '../../../web');
// The Android SDK ships to the app as a PREBUILT binary — `ReactNative/android/build.gradle` pulls
// it in with `fileTree(dir: "$projectDir/libs")`, so nothing in this build compiles the Kotlin.
const AAR_FILE = path.resolve(
  SAMPLE_DIR,
  '../../ReactNative/android/libs/beamable-notifications-release.aar',
);
const NATIVE_ANDROID_SRC = path.resolve(
  SAMPLE_DIR,
  '../../NativeSources/Android/BeamableNotifications/notifications/src/main',
);

const args = process.argv.slice(2);
// Strip our own flag; everything else is forwarded to `expo` verbatim.
const clean = args.includes('--clean');
const expoArgs = args.filter((a) => a !== '--clean');
// `run:ios` has no Gradle; the Android-only cleanup steps are skipped for it.
const isAndroid = !expoArgs.some((a) => a.endsWith(':ios'));

const env = { ...process.env, APP_VARIANT: 'local' };

function log(msg) {
  console.log(`\n[clean] ${msg}`);
}

/** Run a command, inheriting stdio. Exits the process on failure. */
function run(cmd, cmdArgs, opts = {}) {
  const result = spawnSync(cmd, cmdArgs, {
    stdio: 'inherit',
    shell: true, // needed so `npx`/`pnpm` resolve via the shell on Windows
    env,
    ...opts,
  });
  if (result.error) {
    console.error(result.error.message);
    process.exit(1);
  }
  if (result.status !== 0) {
    console.error(`\n[clean] \`${cmd} ${cmdArgs.join(' ')}\` failed (exit ${result.status}).`);
    process.exit(result.status ?? 1);
  }
}

/** Newest mtime among files under `dir` matching `test`, or null when there are none. */
function newestFile(dir, test) {
  if (!fs.existsSync(dir)) return null;
  let newest = null;
  for (const entry of fs.readdirSync(dir, { withFileTypes: true, recursive: true })) {
    if (!entry.isFile() || !test(entry.name)) continue;
    const full = path.join(entry.parentPath ?? entry.path ?? dir, entry.name);
    const mtime = fs.statSync(full).mtime;
    if (!newest || mtime > newest.mtime) newest = { mtime, file: entry.name };
  }
  return newest;
}

const stamp = (d) => d.toISOString().slice(0, 16).replace('T', ' ');

/**
 * Abort when the vendored `.aar` predates the Kotlin it is built from.
 *
 * Nothing in the app build compiles `NativeSources/Android` — the binary is staged by
 * `dev-native.sh`. So editing Kotlin and rebuilding the app produces a perfectly green build that
 * still runs the OLD bytecode, with no error on the device and no failing test. That drift is what
 * silently posted the funnel to the warehouse-only route for three days.
 *
 * A hard failure rather than a warning: warnings scroll past in an Expo build.
 */
function assertAarIsFresh() {
  if (!fs.existsSync(AAR_FILE)) return; // a consumer without the vendored binary — not our call
  const newestSrc = newestFile(NATIVE_ANDROID_SRC, (n) => n.endsWith('.kt'));
  if (!newestSrc) return; // sources absent (published package) — nothing to compare against

  const aarMtime = fs.statSync(AAR_FILE).mtime;
  if (aarMtime >= newestSrc.mtime) return;

  console.error(
    `\n[stale] The bundled beamable-notifications AAR is older than the Kotlin it is built from.\n` +
      `  aar     ${stamp(aarMtime)}  ${path.basename(AAR_FILE)}\n` +
      `  source  ${stamp(newestSrc.mtime)}  ${newestSrc.file}\n\n` +
      `  The app links the prebuilt .aar, so your Kotlin changes are NOT in this build.\n` +
      `  Run ./dev-native.sh from the repo root, then re-run this command.\n`,
  );
  process.exit(1);
}

/** rm -rf, with retries — Windows briefly holds handles via AV/indexers. */
function rmrf(target) {
  if (!fs.existsSync(target)) return false;
  fs.rmSync(target, { recursive: true, force: true, maxRetries: 3, retryDelay: 200 });
  return true;
}

/**
 * Delete only the Gradle transform/jar cache entries produced from the Beamable notifications
 * `.aar`. That `.aar` is consumed as a loose file (`fileTree(dir: "$projectDir/libs")` in the
 * RN package's build.gradle), and a restaged copy can be silently ignored because the transform
 * cache serves the previously transformed bytecode — `gradlew clean` does not clear it.
 *
 * Deliberately targeted rather than nuking `~/.gradle/caches`: that cache is shared with every
 * other Gradle project on the machine and may be in use by a concurrent build.
 */
function bustAarTransformCache() {
  const cachesDir = path.join(os.homedir(), '.gradle', 'caches');
  if (!fs.existsSync(cachesDir)) return;

  const buckets = fs
    .readdirSync(cachesDir, { withFileTypes: true })
    .filter((e) => e.isDirectory() && /^(transforms|jars)-\d+$/.test(e.name))
    .map((e) => path.join(cachesDir, e.name));

  let removed = 0;
  for (const bucket of buckets) {
    let entries;
    try {
      entries = fs.readdirSync(bucket, { withFileTypes: true });
    } catch {
      continue; // a concurrent build may be mutating the cache
    }
    for (const entry of entries) {
      if (!entry.isDirectory()) continue;
      const entryPath = path.join(bucket, entry.name);
      if (!cacheEntryMentionsBeamableAar(entryPath)) continue;
      try {
        rmrf(entryPath);
        console.log(`  removed ${entryPath}`);
        removed++;
      } catch (err) {
        console.warn(`  could not remove ${entryPath}: ${err.message}`);
      }
    }
  }
  console.log(`  ${removed} cache entr${removed === 1 ? 'y' : 'ies'} removed`);
}

/** True if a `<bucket>/<hash>/` entry holds output transformed from the notifications .aar. */
function cacheEntryMentionsBeamableAar(entryPath) {
  const stack = [{ dir: entryPath, depth: 0 }];
  while (stack.length) {
    const { dir, depth } = stack.pop();
    let children;
    try {
      children = fs.readdirSync(dir, { withFileTypes: true });
    } catch {
      continue;
    }
    for (const child of children) {
      if (/beamable-notifications/i.test(child.name)) return true;
      // Entries are shallow (`<hash>/transformed/<name>`); two levels is enough to reach the name.
      if (child.isDirectory() && depth < 2) {
        stack.push({ dir: path.join(dir, child.name), depth: depth + 1 });
      }
    }
  }
  return false;
}

/**
 * Wipe every cache layer that can silently serve stale code into a local build:
 * the web SDK dist, the Gradle `.aar` transform cache, Metro/Expo caches, and the generated
 * Android project (whose `createBundleReleaseJsAndAssets` output is NOT invalidated by a
 * changed `@beamable/sdk` dist — the classic "identical APK reinstalled" trap).
 */
function cleanAll() {
  const gradlew = path.join(ANDROID_DIR, process.platform === 'win32' ? 'gradlew.bat' : 'gradlew');
  if (isAndroid && fs.existsSync(gradlew)) {
    // First: a live daemon holds locks on android/ and makes the deletes below fail on Windows.
    log('stopping Gradle daemons…');
    // Quoted: `shell: true` would otherwise split an install path containing spaces.
    run(`"${gradlew}"`, ['--stop'], { cwd: ANDROID_DIR });
  }

  // `expo prebuild --clean` wipes android/, and local.properties is uncommitted and never
  // regenerated — losing it breaks the next build with "SDK location not found".
  const localPropsPath = path.join(ANDROID_DIR, 'local.properties');
  const localProps = fs.existsSync(localPropsPath) ? fs.readFileSync(localPropsPath) : null;

  log('rebuilding the web SDK (@beamable/sdk)…');
  if (!fs.existsSync(WEB_SDK_DIR)) {
    console.error(`[clean] web SDK not found at ${WEB_SDK_DIR}`);
    process.exit(1);
  }
  const pnpm = spawnSync('pnpm', ['--version'], { shell: true, stdio: 'ignore' });
  if (pnpm.error || pnpm.status !== 0) {
    console.error('[clean] pnpm is required to build the web SDK. Run `corepack enable` first.');
    process.exit(1);
  }
  run('pnpm', ['build'], { cwd: WEB_SDK_DIR });

  if (isAndroid) {
    log('busting the Gradle .aar transform cache…');
    bustAarTransformCache();
  }

  log('clearing Metro/Expo caches…');
  for (const dir of [path.join(SAMPLE_DIR, '.expo'), path.join(SAMPLE_DIR, 'node_modules', '.cache')]) {
    if (rmrf(dir)) console.log(`  removed ${dir}`);
  }

  log('regenerating the native project (expo prebuild --clean)…');
  run('npx', ['expo', 'prebuild', '--clean'], { cwd: SAMPLE_DIR });

  if (localProps) {
    fs.mkdirSync(ANDROID_DIR, { recursive: true });
    fs.writeFileSync(localPropsPath, localProps);
    console.log(`  restored ${localPropsPath}`);
  }

  if (isAndroid) {
    log('removing Gradle build output…');
    for (const dir of [
      path.join(ANDROID_DIR, 'app', 'build'),
      path.join(ANDROID_DIR, 'build'),
      path.join(ANDROID_DIR, '.gradle'),
    ]) {
      if (rmrf(dir)) console.log(`  removed ${dir}`);
    }
  }

  log('clean complete — starting the build.\n');
}

// Before any of the (slow) clean/build work — a stale binary makes all of it pointless.
if (isAndroid) assertAarIsFresh();

if (clean) cleanAll();

const result = spawnSync('npx', ['expo', ...expoArgs], {
  stdio: 'inherit',
  shell: true, // needed so `npx` resolves via the shell on Windows
  env,
});

if (result.error) {
  console.error(result.error.message);
  process.exit(1);
}
process.exit(result.status ?? 1);
