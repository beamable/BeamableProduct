# Local Web SDK & Portal Toolkit — development guide

How to develop an **unpublished, local build** of `@beamable/sdk` or `@beamable/portal-toolkit` and see it
running in the Portal and in portal extensions.

The main flow publishes both packages to a local Verdaccio registry as version `0.0.123` and serves them
through a local unpkg-style CDN — a real IIFE fetch, so it exercises the production path. There is a
secondary, Docker-free alternative for SDK-only work in
[Appendix: the inject-host alternative](#appendix-the-inject-host-alternative).

- [TL;DR](#tldr)
- [How it works: version `0.0.123`](#how-it-works-version-00123)
- [One-time setup](#one-time-setup)
- [`BEAM_WORKSPACE` — read this before your first run](#beam_workspace--read-this-before-your-first-run)
- [Driving it from `beam local up`](#driving-it-from-beam-local-up)
- [Building the packages](#building-the-packages)
- [The iteration loops](#the-iteration-loops)
- [How the Portal picks up a local build](#how-the-portal-picks-up-a-local-build)
- [Cleaning up — IMPORTANT](#cleaning-up--important)
- [Command reference](#command-reference)
- [The shell scripts](#the-shell-scripts)
- [Troubleshooting](#troubleshooting)
- [How it works under the hood](#how-it-works-under-the-hood)
- [Appendix: the inject-host alternative](#appendix-the-inject-host-alternative)

---

## TL;DR

```bash
# 1. Start the local registry. NOT idempotent — this WIPES every package already published.
cd /path/to/BeamableProduct
./setup-web.sh
#    ...it does not wait for the containers, so give them a moment (see the race below)

# 2. Every iteration: build, publish as 0.0.123, refresh the projects that use it.
#    BEAM_WORKSPACE is effectively REQUIRED — the default rewrites tracked files in THIS repo.
BEAM_WORKSPACE=/path/to/agentic-portal ./dev-web.sh
#    ...add --build if the packages' own dependencies changed
#    ...add --only sdk / --only toolkit to rebuild just one (both are still published)

# 3. Run the Portal and the extension's microservice as usual. NO portal configuration needed.
npm run dev                        # in agentic-portal
beam project run --ids <ext-id>

# 4. When you're done — the pin lives in TRACKED files, so revert it in BOTH repos
./teardown-web.sh
cd /path/to/agentic-portal    && git restore '**/package.json' '**/package-lock.json'
cd /path/to/BeamableProduct   && git status        # nothing web-related should be dirty
```

> ⚠️ If the Portal's `.env.local` sets `VITE_INJECT_HOST_SDK=true`, **comment it out**. That flag silently
> wins over everything on this page: the host SDK is handed to extensions before any fetch happens, so the
> local CDN is never consulted and the build you just published is ignored without a word of warning. See
> the [appendix](#appendix-the-inject-host-alternative).

Or let the local stack do steps 1–3: see
[Driving it from `beam local up`](#driving-it-from-beam-local-up).

Check state at any time with `beam web status`.

---

## How it works: version `0.0.123`

Both packages are always published as the single version **`0.0.123`**:

> Any package published as version `0.0.123` is treated as a local-dev build.

That's the same sentinel `dev.sh` uses for the .NET packages (`0.0.123.<N>`), and a dozen places in the C#
codebase string-match it. The Portal does the same for web packages — it sees a `0.0.123` version and loads
it from the local CDN instead of unpkg.com, **with no environment variable or config**.

Both packages share that version, and the toolkit's `@beamable/sdk` peer dependency points at it — the
Portal reads that peer dep to decide which SDK an extension gets, so they have to agree.

**Why the version never moves:** consumers *pin* it. A version that changed per build would mean rewriting
every extension's `package.json` on every publish. Holding it still means the pin is written **once**, when
you opt in, and reverted when you're done.

**What that costs:** content now changes under a version that already exists, so "is this the latest build?"
becomes a caching question. Every layer that could serve you a stale copy is handled:

| Layer | Keyed by | How it's busted |
|---|---|---|
| npm's view of your tree | version | `beam web use` deletes the installed copy, then installs the explicit spec — npm would otherwise see the version satisfied and do nothing |
| npm cache | version + integrity | the new tarball has a new integrity, so it's a cache miss automatically |
| pnpm store / cache | integrity | **not** self-healing — a stale entry fails the next install with `ERR_PNPM_TARBALL_INTEGRITY`. `beam web reset` / `beam web stop --wipe` evict the two `@beamable` entries (`pnpm store delete` + `pnpm cache delete`) |
| lock file integrity | — | the install rewrites it, keeping the lock consistent with what's on disk |
| local-unpkg's file cache | `pkg@version` | `beam web publish` calls `POST :4874/__flush` — **best-effort, see below** |
| browser HTTP cache | URL | local-unpkg answers `Cache-Control: no-store`, and the Portal fetches local URLs with `cache: 'no-store'` |
| Portal's in-memory module cache | version | cleared only by a **full page reload** |
| extension bundle cache | localStorage | clear the `portal-extension-*` keys |

The first row is the one that bites, and the reason `beam web use` exists rather than a plain `npm install`.
Verified: a plain `npm install` leaves the old build in place; `beam web use` replaces it.

> ⚠️ **The CDN flush is best-effort and never fatal.** `WebLocalRegistryService.FlushCdnCache` logs a
> warning and returns on any failure — a rejected flush, an unreachable container, a timeout. So a publish
> can report success while the CDN keeps serving the build you just replaced. Watch the publish output for
> `Could not flush the local CDN cache` / `rejected the cache flush`. A **stale `local-unpkg` image** is the
> common cause of the latter (the `/__flush` endpoint postdates it); rebuild with
> `docker compose up -d --build` in `portal-localdev/`.

Version pins tolerate range operators: `IsLocalDevVersion` trims a leading `^ ~ > < = v` before matching, so
`^0.0.123` still routes at the local registry. That matters because npm's default save-prefix would
otherwise turn the pin into a caret range and silently stop it being recognised. (`beam web use` installs
with `--save-exact`, so it writes a bare `0.0.123`.)

`0.0.123` is safe to reuse because it exists nowhere on npmjs (404 for both packages), so it can never
collide with the registry's uplink, and removing it can't affect a published version.

---

## One-time setup

### Prerequisites

- Docker Desktop running
- Node ≥ 22.14.0, `pnpm` 10.8.0 (`npm i -g pnpm@10.8.0`)
- .NET SDK (the `beam web` commands are part of the CLI in this repo)

### Start the local registry

```bash
cd /path/to/BeamableProduct
./setup-web.sh
```

This brings up two containers from `portal-localdev/`:

| Service | Port | Role |
|---|---|---|
| Verdaccio | 4873 | npm registry holding your local `@beamable/*` builds; proxies everything else to npmjs |
| local-unpkg | 4874 | unpkg-style file server, so the browser can fetch IIFEs by path |

> ⚠️ **`setup-web.sh` is a clean slate, not "make sure the registry is up".** It runs `beam web reset`,
> which executes `docker compose down -v` **first** — that drops the `verdaccio-storage` volume, so
> **every package you had published is deleted**. It then evicts the cached `@beamable` tarballs from the
> pnpm store as well (pass `--keep-caches` to skip that part).
>
> Run it once at the start of a session, or deliberately when you want an empty registry. To simply bring
> the containers back up without losing anything, use `docker compose up -d` in `portal-localdev/` — or
> `beam local up`, whose registry step is non-destructive.

> ⚠️ **There is a start-up race.** `beam web reset` runs `docker compose up -d` **without `--wait`**, so it
> returns as soon as Docker has accepted the containers — before Verdaccio is listening. A `dev-web.sh`
> fired immediately afterwards can therefore die with *"The local registry at [http://localhost:4873] is not
> reachable"*. It is not a real failure; just re-run it. To avoid it entirely, gate on the registry
> yourself:
>
> ```bash
> ./setup-web.sh
> until curl -sf http://localhost:4873 >/dev/null; do sleep 1; done
> BEAM_WORKSPACE=/path/to/agentic-portal ./dev-web.sh
> ```
>
> `beam local up` does not have this problem — its `docker: web registry` step runs
> `docker compose up -d --wait` and additionally gates on Verdaccio answering.

### Portal configuration

**None.** The `0.0.123` prefix is self-identifying, so the Portal routes those versions to the local CDN
automatically (`LOCAL_DEV_PREFIX` in `agentic-portal/src/lib/utils/extensionSdkRegistry.ts`). See
[How the Portal picks up a local build](#how-the-portal-picks-up-a-local-build).

The one exception is `VITE_INJECT_HOST_SDK=true` in the Portal's `.env.local`, which overrides all of this
silently — comment it out. See the [appendix](#appendix-the-inject-host-alternative).

---

## `BEAM_WORKSPACE` — read this before your first run

`dev-web.sh` takes the repo holding your extensions from `BEAM_WORKSPACE`, and **defaults it to this
repository**:

```bash
WORKSPACE="${BEAM_WORKSPACE:-$SCRIPT_DIR}"      # dev-web.sh
```

That default is almost never what you want. `beam web use` scans the tree for every `package.json` carrying
a `beamable.portalExtension` / `beamable.portalExtensionLib` marker and rewrites its
`@beamable/portal-toolkit` pin to `0.0.123`. Run without `BEAM_WORKSPACE`, and it rewrites **~7 tracked
`package.json` files inside BeamableProduct**:

```
client/extensions/Calculator/package.json
client/extensions/SampleOneHealthCheck/package.json
client/extensions/Test123/package.json
client/extensions/weather/package.json
client/extensions/world-clock/package.json
cli/beamable.templates/templates/PortalExtensionReactApp/package.json      ← CLI scaffolding template
cli/beamable.templates/templates/PortalExtensionCommonLib/package.json     ← CLI scaffolding template
```

The last two are the worst: they are the templates `beam project new` scaffolds from, so committing them
would ship a `0.0.123` pin to every extension anyone creates from that point on.

**So: always set `BEAM_WORKSPACE`**, and **always `git status` this repo** at the end of a session, not just
the extensions repo. See [Cleaning up](#cleaning-up--important).

To publish without repointing anything at all, set `BEAM_SKIP_UPDATE=1`.

---

## Driving it from `beam local up`

`beam local up` can own the whole loop: start the registry, publish your local packages, refresh the
extensions, and then run them against what it just built.

### Opt in, once

```bash
beam local init --with-web-registry
```

That adds three steps to `.beamable/local-stack.json`. The `portal-localdev` path is prompted for with an
auto-detected default (or pass `--web-registry-dir`, which implies the flag); the product dir is derived
from it, and the extensions repo comes from the `--portal-dir` you already give `init`:

| Step | What it does | Runs |
|---|---|---|
| `docker: web registry` | `docker compose up -d --wait` in `portal-localdev/`, gated on Verdaccio answering | always, **first** |
| `build: web packages` | `beam web publish` — build both, publish `0.0.123`, flush the CDN | only with `--build` |
| `build: web extension pins` | `beam web use` — pin `0.0.123` and force-refresh the installs | only with `--build` |

### Use it

```bash
beam local up            # registry + CDN come up; your packages are left alone
beam local up --build    # ...and rebuild + republish + refresh before running the extensions
```

`--build` is the existing convention for "also build the things `local up` doesn't otherwise build" — the
same flag that rebuilds the C# gateway, the Scala services and the portal's node deps. Plain `beam local up`
stays fast.

Skip or isolate individual steps by exact name (comma-separated):

```bash
beam local up --build --skip "build: web packages"                 # refresh only, don't rebuild
beam local up --only "docker: web registry"                        # just the registry
beam local up --build --only "build: web packages","build: web extension pins"
```

**Stopping is non-destructive by default.** The registry step's stop is `docker compose stop`, so published
packages survive a stop/up cycle — unlike `setup-web.sh`. `beam local stop --purge` runs
`docker compose down -v` and wipes them.

### Why the steps sit where they do

- **The registry goes first.** It's independent of everything else and fast to come up, and later steps
  (`build: portal deps`, the extension steps) run npm installs that may have to resolve locally published
  `@beamable` packages.
- **Publish/refresh go after the Scala services.** `beam local up` performs a realm/login before its
  *first* `beam` step, and that authenticates through the Scala `auth` service. Putting the web steps
  earlier would fire that login against a backend that isn't up yet.
- **...and before the extension steps.** `beam project run --ids <ext>` builds the extension, and the
  toolkit is compiled into its bundle — so the refresh has to land first.
- **They abort the stack on failure.** Both are run-to-completion steps, and a non-zero exit tears the
  stack down. Deliberate: quietly running extensions against a stale toolkit after a failed publish is
  worse than stopping.

> Already have a manifest? Re-run `beam local init --with-web-registry` to regenerate it. A manifest created
> *without* the flag is byte-identical to one from before this feature existed.

---

## Building the packages

**You normally don't have to do this by hand.** `beam web publish` (and therefore `./dev-web.sh`) builds
both packages before publishing, running exactly the recipes below. This section is for understanding what
runs, building without publishing, or debugging a build failure.

| | Package | Source | Bundler | Output |
|---|---|---|---|---|
| SDK | `@beamable/sdk` | `web/` | tsdown | `dist/` — node / browser / react-native, ESM+CJS+IIFE, types |
| Toolkit | `@beamable/portal-toolkit` | `beam-portal-toolkit/` | tsdown | `dist/` — CJS+ESM bundles, `dist/types/*.d.ts` |

Both are independent pnpm projects with their own lock files — there is no workspace linking them.

### Build both, via the CLI

```bash
cd /path/to/BeamableProduct

# Build + publish (the normal path)
BEAM_WORKSPACE=/path/to/agentic-portal ./dev-web.sh

# Reinstall dependencies first (after the packages' own deps changed)
BEAM_WORKSPACE=/path/to/agentic-portal ./dev-web.sh --build

# Publish without rebuilding (dist/ is already current)
BEAM_WORKSPACE=/path/to/agentic-portal ./dev-web.sh --skip-build
```

`beam web publish` installs dependencies only when `node_modules` is missing, so a normal iteration doesn't
pay for a `pnpm install`. Pass `--build` (script) / `--force-install` (CLI) when the packages' own
dependencies changed, or the build compiles against stale ones.

> ⚠️ **Every script invocation rebuilds the CLI from source.** `scripts/beam-cli.sh` shells out to
> `dotnet run -f net10.0 --project cli/cli`, which compiles the CLI before running it — on every
> `setup-web.sh` / `dev-web.sh` / `teardown-web.sh`. That is usually the slowest part of an iteration and it
> has nothing to do with the web packages. Skip it with `BEAM_CLI_NO_BUILD=1` (which adds `--no-build`), and
> remember to drop the variable after touching CLI source:
>
> ```bash
> BEAM_CLI_NO_BUILD=1 BEAM_WORKSPACE=/path/to/agentic-portal ./dev-web.sh --only sdk
> ```

### Building the Web SDK (`@beamable/sdk`)

```bash
cd web
pnpm install          # first time, or after dependencies changed
pnpm build            # tsdown -> dist/
```

That's the whole recipe — it mirrors the package's own `prepublishOnly` script.

```bash
pnpm dev              # tsdown --watch, for a fast inner loop
```

The Portal fetches these two files by path at runtime:

```
web/dist/browser/index.iife.js     # the SDK IIFE      (package.json "unpkg" entry)
web/dist/api.iife.js               # the generated API surface IIFE
```

#### Regenerating the SDK's API surface from a local backend

Only needed when your local backend's OpenAPI differs from the checked-in generated code — i.e. you
changed a backend endpoint and want the SDK to know about it. From the repository root, with the backend
running on `:8080`:

```bash
dotnet run -f net10.0 --project ./cli/cli -- \
  --host http://localhost:8080 \
  oapi generate --engine web \
  --conflict-strategy RenameUncommonConflicts \
  --output ./web/src/__generated__
```

Then rebuild (`pnpm build` in `web/`). This **writes tracked files** under `web/src/__generated__` — review
the diff before committing, and `git restore` it if you only wanted a local experiment.

### Building the Portal Toolkit (`@beamable/portal-toolkit`)

The toolkit has an extra step: it generates typed bindings for the Portal's `beam-*` web components before
bundling.

```bash
cd beam-portal-toolkit
pnpm install                      # first time, or after dependencies changed
pnpm sync-components --no-copy    # regenerate src/generated/* from the component manifest
pnpm build                        # tsdown -> dist/
```

That is the package's `prepublishOnly` recipe, which is why `beam web publish` runs both steps.

⚠️ **`pnpm sync-components` writes tracked files.** It rewrites `src/generated/*` and stamps the current
package version into `src/generated/web-types.json`. `beam web publish` snapshots and restores that
directory so a publish never dirties the repo — but running the command **by hand does not**, so check
`git status -- beam-portal-toolkit/src/generated` afterwards. (A `0.0.123-local19` stamp is currently
committed there from an older flow — that's exactly this failure mode.)

A harmless `UNRESOLVED_IMPORT` warning for `@vitejs/plugin-react` may appear; it's an optional dynamic
import in `src/vite.ts`.

#### Why the toolkit and the SDK behave differently at runtime

The single most useful thing to internalise:

- The **SDK is external** to an extension's bundle — the browser fetches its IIFE at runtime. Republish it
  and a full page reload picks it up. **No extension rebuild.**
- The **toolkit is compiled into** each extension's bundle. Republish it and every extension must be
  rebuilt (`beam project run --ids <ext>`) before the change appears.

### Building without publishing

```bash
cd web && pnpm build
cd ../beam-portal-toolkit && pnpm sync-components --no-copy && pnpm build
```

---

## The iteration loops

### Changing the SDK (the common case)

```bash
cd /path/to/BeamableProduct
BEAM_WORKSPACE=/path/to/agentic-portal ./dev-web.sh --only sdk
```

Then fully reload the Portal. **No extension rebuild** — the SDK is fetched at runtime.

`--only sdk` skips the toolkit's *build*, but still publishes it and still repoints your extensions: the
version has to move on both, because the toolkit's peer dep is how the Portal finds the SDK.

> `--only` is a build skip, not a build ban. If the *other* package has no `dist/` directory yet, it is
> built anyway — so the first `--only sdk` in a fresh checkout still pays for the (slower) toolkit build.
> That is deliberate: publishing a `dist/`-less package would produce an empty tarball and 404s at runtime.

### Changing the toolkit

```bash
cd /path/to/BeamableProduct
BEAM_WORKSPACE=/path/to/agentic-portal ./dev-web.sh --only toolkit
beam project run --ids <extension-id>      # rebuilds the extension
```

### Verifying you're on the local build

```bash
beam web status
```

```
registry [http://localhost:4873]  running
cdn      [http://localhost:4874]  running
  @beamable/sdk: 0.0.123   published 2026-07-27T17:44:31.122Z
  @beamable/portal-toolkit: 0.0.123   published 2026-07-27T17:44:32.735Z
```

Since the version never changes, **the publish time is what tells builds apart** — check it matches the
publish you just ran.

In the browser's Network tab, toolkit/SDK requests should go to `/local-unpkg` with the `0.0.123` version and
return 200.

---

## How the Portal picks up a local build

All of this lives in `agentic-portal`. There is **no portal configuration** to switch on;
`src/lib/utils/extensionSdkRegistry.ts` treats any version starting with `0.0.123` as a local developer
build and fetches it from the local CDN instead of unpkg.com:

```ts
const LOCAL_DEV_PREFIX = '0.0.123';
function isLocalDev(version: string): boolean {
  return version.startsWith(LOCAL_DEV_PREFIX);
}
```

```
Extension bundle metadata
  └─ ToolkitVersion: "0.0.123"
       │
       ▼
isLocalDev() → true → LOCAL_CDN_BASE ("/local-unpkg", proxied to :4874 in vite.config.ts)
  └─ GET /local-unpkg/@beamable/portal-toolkit@0.0.123/package.json
       ▼
   peerDependencies['@beamable/sdk'] → "0.0.123"
       ▼
GET /local-unpkg/@beamable/sdk@0.0.123/dist/browser/index.iife.js  (+ dist/api.iife.js)
       ▼
window['@beamable/sdk-0.0.123']
```

Four supporting details, all of which you will eventually trip over:

- **The `/local-unpkg` Vite proxy exists to keep the fetches same-origin.** `LOCAL_CDN_BASE` defaults to the
  path `/local-unpkg`, which `vite.config.ts` proxies to `http://localhost:4874` (stripping the prefix).
  Going straight at the container instead is cross-origin: it does set `Access-Control-Allow-Origin: *` on
  successful file responses, but **its error responses carry no CORS headers at all** — so a 404 for an
  unpublished path arrives in the console as an opaque CORS failure rather than the 404 it is, which is
  exactly the case you need to debug. Override with `VITE_LOCAL_CDN_BASE` if you moved the port (e.g. a
  direct `http://localhost:PORT`, or a different proxy path).

- **A full page reload is mandatory after a republish.** `normalizeCacheKey` keeps the *full* version as the
  cache key for local-dev versions (real versions are collapsed to `x.y.z`). Since the version is fixed,
  that key is identical for every build — so `_moduleCache` / `_apiModuleCache` / `_instanceCache` keep
  handing out whichever build loaded first. Those are module-level `Map`s, so **only a reload clears them**;
  no amount of in-app navigation will. `fetchWithCache` already bypasses the browser Cache API for local
  URLs (and passes `cache: 'no-store'`), and local-unpkg answers `Cache-Control: no-store`, so the reload is
  sufficient.

- **`src/lib/utils/extensionStorage.ts` maps the prefix forward.** `includePrefixes: ['0.0.123']` on the
  newest `STORAGE_BUILDERS` entry means a local build always gets the current `context.storage` shape,
  regardless of how `0.0.123` sorts against real releases. When a new storage shape lands,
  `includePrefixes` has to move to the new newest entry — otherwise local builds silently get the old shape.

- **Extension bundles are cached in `localStorage`.** Clear the `portal-extension-*` keys and reload if a
  bundle itself looks stale.

---

## Cleaning up — IMPORTANT

The local pin lives in **tracked files**, in *both* repos. That is how the model works, but it must never be
committed.

```bash
# 1. Stop the registry (and delete what was published)
cd /path/to/BeamableProduct && ./teardown-web.sh
#    ...or ./teardown-web.sh --keep-packages to stop the containers but keep the packages

# 2. Revert the pins in the repo holding your extensions
cd /path/to/agentic-portal
git restore '**/package.json' '**/package-lock.json'
npm install     # in any extension you actually ran, to restore the published toolkit

# 3. Revert BeamableProduct too — see below for why
cd /path/to/BeamableProduct
git status --porcelain -- '*/package.json' beam-portal-toolkit/src/generated
git restore client/extensions cli/beamable.templates beam-portal-toolkit    # if anything showed up

# 4. Pre-commit sanity check, run in BOTH repos
git status --porcelain | grep -E 'package(-lock)?\.json|pnpm-lock\.yaml|web-types\.json' \
  && echo "LOCAL DEV WIRING STILL PRESENT — do not commit" || echo clean
```

Four things to know:

- **Don't forget this repo.** If you ever ran `dev-web.sh` without `BEAM_WORKSPACE`, `beam web use` pinned
  `0.0.123` into `client/extensions/*` **and the two `cli/beamable.templates/templates/PortalExtension*`
  scaffolding templates**. See
  [`BEAM_WORKSPACE`](#beam_workspace--read-this-before-your-first-run).

- **An aborted publish leaves stamped files behind.** `beam web publish` stamps
  `beam-portal-toolkit/package.json` (version + `@beamable/sdk` peer dep) and lets
  `pnpm sync-components` rewrite `src/generated/*`, then restores both in a `finally` block. Ctrl-C mid-run
  kills the process before that block executes, so the stamps survive. After any interrupted or crashed
  run: `git status` in BeamableProduct, then
  `git restore beam-portal-toolkit/package.json beam-portal-toolkit/src/generated`.

- **Lock files matter as much as manifests.** They pick up `resolved` URLs pointing at
  `http://localhost:4873`, so reverting `package-lock.json` is not optional.

- **There is no global npm config to restore.** This flow never writes an `@beamable` registry override into
  your npmrc; installs are routed per-invocation with `--registry`. (An older version of these scripts did
  mutate the global npmrc, which broke installs machine-wide when the registry went away.)

---

## Command reference

All `beam web` commands are standalone — no Beamable workspace, service manifest or backend connection
required.

### `beam web publish`

Builds both packages and publishes them to the local registry as `0.0.123`, under the `local` dist-tag.

| Option | Meaning |
|---|---|
| `--product-dir <path>` | The `BeamableProduct` checkout. Defaults to searching upwards (and one level down into siblings) from the working directory. |
| `--only sdk\|toolkit` | Rebuild just one package. **Both are still published**, at the same version — see below. |
| `--version <v>` | Publish as this version instead of `0.0.123`. **Read the caveat below before using it.** |
| `--skip-build` | Publish what's already built. |
| `--force-install` | `pnpm install` before building even when `node_modules` exists. `./dev-web.sh --build` maps to this. |
| `--registry <url>` / `--cdn <url>` | Non-default ports. |

#### `--only` rebuilds one package but publishes both

The two versions have to match. The Portal resolves an extension's SDK through the toolkit's
`@beamable/sdk` peer dependency, so a toolkit published at version N names an SDK at version N — publishing
only one would leave that peer dep pointing at a version nothing published, and the SDK fetch 404s at
runtime with the extension failing to mount.

So `--only sdk` rebuilds the SDK, republishes the toolkit's existing `dist/` unchanged, and publishes both
at the same version. You still save the (slower) toolkit build — unless the un-built package has no `dist/`
yet, in which case it is built anyway.

#### ⚠️ `--version <v>` only works for `0.0.123`-prefixed values

`--version` changes what gets published, but **nothing else knows about it**. The Portal's `isLocalDev()`
is a literal `startsWith('0.0.123')` test, so any other value fails it and the Portal falls back to
`https://unpkg.com` — where your local build does not exist. The failure is late and confusing:

- the publish **succeeds**;
- `beam web use` happily pins it (it resolves the `local` dist-tag, which the publish moved);
- `beam web status` **doesn't show it** — it filters versions through `IsLocalDevVersion`, so your build is
  invisible;
- the extension 404s against unpkg at runtime and fails to mount.

The same applies to `extensionStorage.ts`'s `includePrefixes` and to the CLI's install routing. Use
`--version` only for suffixes of the sentinel (e.g. `0.0.123-experiment`), or not at all.

### `beam web use`

Points the extensions under a directory at a locally published build, and installs it.

| Option | Meaning |
|---|---|
| `--workspace <path>` | Directory tree to scan. Defaults to the working directory. `dev-web.sh` passes `BEAM_WORKSPACE`. |
| `--registry <url>` | The registry to install from. Defaults to `http://localhost:4873`. |
| `--version <v>` | The version to pin. Defaults to the registry's `local` dist-tag — i.e. the newest build. |
| `--skip-install` | Rewrite the pins without running `npm install`. |

Discovery is a plain filesystem scan for `beamable.portalExtension` / `beamable.portalExtensionLib` markers,
skipping `node_modules` and `.git`. `beam portal extension update-toolkit --local` does the same rewrite but
discovers extensions via the Beamo service manifest, which makes it authenticate against the configured host
— so it fails when the backend isn't running. `beam web use` works offline; that's why `dev-web.sh` calls
it.

Installs are best-effort: a failed install in one project logs a warning and the command still succeeds,
because `package.json` is the source of truth and the run flow installs again before building.

### `beam web status`

Reachability of the registry and CDN, which local builds exist, and where the `local` dist-tag points —
the pointer `beam web use` follows. First thing to run when an extension loads the wrong thing.

Only `0.0.123`-prefixed versions are listed; the registry proxies npmjs, so its packument otherwise lists
every published release as noise.

### `beam web reset`

`docker compose down -v` then `docker compose up -d` in `portal-localdev/` — **wipes everything published**
and comes back empty. Also evicts the cached `@beamable` tarballs from the pnpm store and cache, since a
wipe means the integrity hashes they hold no longer match anything. `--keep-caches` skips the eviction.

Note the missing `--wait`: see the [race](#start-the-local-registry).

### `beam web stop`

`docker compose down`. `--wipe` uses `down -v` instead, deleting the published packages, and also evicts the
pnpm cache entries.

---

## The shell scripts

Thin wrappers that run the CLI from source via `dotnet run`, so there is one implementation and it behaves
the same on Windows, macOS and Linux.

| Script | Runs | Destructive? |
|---|---|---|
| `./setup-web.sh` | `beam web reset --product-dir <repo>` | **Yes** — deletes every published package, and evicts pnpm cache entries |
| `./dev-web.sh` | `beam web publish --product-dir <repo>` then `beam web use --workspace $BEAM_WORKSPACE` | Edits tracked files in the workspace |
| `./teardown-web.sh` | `beam web stop --product-dir <repo> --wipe` | **Yes**, unless `--keep-packages` |

| Variable | Effect |
|---|---|
| `BEAM_WORKSPACE` | The repo holding your extensions, where `beam web use` runs. **Defaults to this repo — see the warning above.** |
| `BEAM_SKIP_UPDATE` | Publish without repointing any extension. |
| `BEAM_FULL_BUILD` | Same as passing `--build`. |
| `BEAM_CLI_NO_BUILD` | Skip rebuilding the **CLI** on every invocation — much faster, but stale if you changed CLI source. |
| `BEAM_CLI_FRAMEWORK` | Target framework for the CLI, default `net10.0`. |

`dev-web.sh` build flags:

| Flag | Effect |
|---|---|
| *(none)* | Builds both packages, skipping `pnpm install` when `node_modules` exists. |
| `--build` | Full build — reinstalls dependencies first (translated to the CLI's `--force-install`). |
| `--skip-build` | Publishes `dist/` as-is. |

### ⚠️ Passthrough flags reach `web publish` only, never `web use`

`dev-web.sh` collects unrecognised arguments into `PASSTHROUGH` and appends them to the **publish**
invocation. The `beam web use` call that follows is hard-coded to `--workspace "$WORKSPACE"` and nothing
else. So:

```bash
./dev-web.sh --registry http://localhost:5873      # publishes to :5873, then installs from :4873
```

...publishes to your custom registry and then force-reinstalls from the default one — which either has an
older build or nothing at all. Same story for `--version`. When you need a non-default registry or version,
skip the script and run the two commands yourself:

```bash
beam web publish --registry http://localhost:5873
beam web use --workspace /path/to/agentic-portal --registry http://localhost:5873
```

`./setup-web.sh --keep-caches` and `./dev-web.sh --only sdk` work fine, because those flags belong to the
command the passthrough actually reaches.

---

## Troubleshooting

**`beam web publish` says the registry isn't reachable, right after `./setup-web.sh`.**
The [start-up race](#start-the-local-registry) — `beam web reset` doesn't wait for Verdaccio. Re-run
`dev-web.sh`, or poll `curl -sf http://localhost:4873` first. If it persists, check Docker is running and
`docker compose ps` in `portal-localdev/`.

**Everything I published is gone.**
You re-ran `./setup-web.sh`, which is a clean slate (`docker compose down -v` first). Just publish again.
To restart the containers without wiping, `docker compose up -d` in `portal-localdev/`, or `beam local up`.

**An extension still loads the published SDK/toolkit.**

1. Is `VITE_INJECT_HOST_SDK` set in the Portal's `.env.local`? **It wins silently** — comment it out.
2. `beam web status` — is a local build published, is the publish time recent, and where does `local`
   point? Nothing listed? You may have published under a non-`0.0.123` `--version`, which `status` hides.
3. Does the extension's `package.json` pin that version? Re-run `beam web use` if not.
4. Clear the `portal-extension-*` `localStorage` keys and do a **full page reload** (not just a re-render —
   the module caches are module-level `Map`s).

**I published, but my change isn't in the extension.**
Almost always the npm layer: a plain `npm install` sees `0.0.123` already installed and does nothing. Run
`beam web use` (or `beam local up --build`), which deletes the installed copy first. Then check the publish
output for a CDN-flush warning — the flush is best-effort, so a "successful" publish can still be shadowed
by a stale CDN cache. `docker compose restart local-unpkg` (or `POST :4874/__flush`) clears it; if the flush
was *rejected*, the container image predates the endpoint — `docker compose up -d --build`.

**`npm error notarget No matching version found for @beamable/portal-toolkit@0.0.123`**
`0.0.123` exists only on the local registry, so the install has to be routed there. The CLI does that
automatically wherever it installs an extension; if you're running `npm install` by hand, add
`--registry http://localhost:4873`. Also check the registry is actually running.

**`ERR_PNPM_TARBALL_INTEGRITY` / `EINTEGRITY`.**
A cached entry holds the integrity hash of a tarball that has since been replaced. `beam web reset` and
`beam web stop --wipe` evict the two `@beamable` entries; run one, or
`pnpm store delete @beamable/portal-toolkit` by hand.

**`409 Conflict - this package is already present` when publishing by hand.**
Verdaccio won't overwrite a version. Unpublish first — which is exactly what `beam web publish` does:
`npm unpublish <pkg>@<version> --force --registry http://localhost:4873`.

**The build fails inside `beam web publish` / `dev-web.sh`.**
The error is the raw `pnpm` output. Reproduce it directly — `cd web && pnpm build`, or
`cd beam-portal-toolkit && pnpm sync-components --no-copy && pnpm build`. If it complains about missing or
mismatched dependencies, run with `--build`.

**Everything 404s from `/local-unpkg` in the browser.**
Either nothing is published at that version (`beam web status`), or the local-unpkg container is down
(`beam web status` probes it too). A *CORS* error rather than a 404 usually means `VITE_LOCAL_CDN_BASE`
points straight at the container — local-unpkg omits CORS headers on error responses, so the real status is
hidden. Drop back to the `/local-unpkg` proxy path.

**`git status` shows changes under `beam-portal-toolkit/`.**
`pnpm sync-components` regenerates `src/generated/*`, and `beam web publish` stamps `package.json`. The
publish restores both — unless it was interrupted, or you ran `sync-components` by hand.
`git restore beam-portal-toolkit/package.json beam-portal-toolkit/src/generated`.

**`git status` in BeamableProduct shows `0.0.123` in `client/extensions/` or `cli/beamable.templates/`.**
You ran `dev-web.sh` without `BEAM_WORKSPACE`. `git restore client/extensions cli/beamable.templates`, and
set the variable next time.

---

## How it works under the hood

```
PUBLISH   beam web publish            (./dev-web.sh, or `beam local up --build`)
            stamp package.json (+ toolkit sdk peer dep), snapshot src/generated/
            SDK      → pnpm build
            toolkit  → pnpm sync-components --no-copy && pnpm build
            npm unpublish 0.0.123 --force                 ← it already exists; Verdaccio won't overwrite
            npm publish --tag local --ignore-scripts      ← scripts already ran, above
            finally: restore package.json + src/generated ← skipped on Ctrl-C
            POST :4874/__flush                            ← best-effort; or the CDN serves the old files

ADOPT     beam web use
            resolves 0.0.123 from the registry's `local` dist-tag
            scan for beamable.portalExtension{,Lib} markers, skipping node_modules
            ensures the toolkit pin is 0.0.123            ← a no-op after the first run
            rm -rf node_modules/@beamable/portal-toolkit  ← the part that makes it actually refresh
            npm install @beamable/portal-toolkit@0.0.123 --registry http://localhost:4873 --save-exact

RUNTIME   browser → /local-unpkg/@beamable/portal-toolkit@0.0.123/package.json
            → peerDependencies['@beamable/sdk'] = 0.0.123
            → /local-unpkg/@beamable/sdk@0.0.123/dist/browser/index.iife.js (+ api.iife.js)
            → window['@beamable/sdk-0.0.123']
```

Details worth knowing:

**Why `0.0.123` and not `0.0.123.N`.** npm rejects 4-part versions — they aren't semver. The .NET side can
use `0.0.123.<N>` because NuGet allows it; on npm the base version is the whole marker.

**Why the version doesn't move at all.** Consumers pin it, so a moving version means rewriting every
extension's manifest per publish. A fixed version turns that into a one-time edit and makes freshness a
cache problem instead — see the table in [How it works](#how-it-works-version-00123).

**Why publish unpublishes first.** Verdaccio rejects a publish for a version it already holds. Since only
`0.0.123` is ever removed, and the uplink has no such version, this can't disturb a published package. The
unpublish is tolerant of failure: "not found" is the normal first-publish case, and a real problem surfaces
on the publish that follows with a clearer message.

**Why the registry proxies npmjs for `@beamable/*`.** It does — `portal-localdev/verdaccio/config.yml`
gives the scope an npmjs uplink. Extension installs get routed at the local registry, so every other
`@beamable` spec in that install still has to resolve — extensions pinning a published toolkit, extension
libraries, and so on. Proxying is safe because `0.0.123` exists nowhere upstream: publishing a version the
uplink *also* serves is what fails with `409 Conflict`, and that can't happen here.

**Why writes pass a token on the command line.** npm refuses to publish without one even against a registry
that allows anonymous writes, and passing `--//localhost:4873/:_authToken=local` per invocation keeps the
user's global npmrc untouched.

### Files involved

| Path | Role |
|---|---|
| `portal-localdev/docker-compose.yml` | The two containers (`beamable-verdaccio`, `beamable-local-unpkg`) |
| `portal-localdev/verdaccio/config.yml` | Registry config, including the npmjs uplink for `@beamable/*` |
| `portal-localdev/local-unpkg/index.js` | The unpkg-style file server, incl. `POST /__flush` and `Cache-Control: no-store` |
| `portal-localdev/README.md` | The registry stack on its own terms |
| `scripts/beam-cli.sh` | `dotnet run` wrapper the three scripts share |
| `cli/cli/Commands/Web/` | The `beam web` commands |
| `cli/cli/Services/Web/WebLocalRegistryService.cs` | The version constant, force-reinstall, install routing, discovery, cache eviction |
| `cli/cli/Services/LocalStack/LocalStackTemplate.cs` | The `beam local up` steps |
| `agentic-portal/src/lib/utils/extensionSdkRegistry.ts` | `LOCAL_DEV_PREFIX` routing, the module caches, inject-host |
| `agentic-portal/src/lib/utils/extensionStorage.ts` | `includePrefixes` mapping `0.0.123` to the newest storage shape |
| `agentic-portal/vite.config.ts` | The `/local-unpkg` → `:4874` dev proxy |

Related: `docs/developer-help.md` (the .NET `dev.sh` loop this mirrors),
`agentic-portal/CreatingExtensions.md` (writing extensions in the first place).

> The XML doc comments on `WebLocalRegistryService` describe an older "shadow the version consumers already
> pin" design and claim `@beamable/*` is not proxied. Both are stale; the code and
> `portal-localdev/verdaccio/config.yml` are authoritative.

---

## Appendix: the inject-host alternative

A second, **narrower** flow, kept because it needs no Docker at all. Reach for the local registry above
unless you specifically want this.

| | Local registry (main flow) | inject-host (this appendix) |
|---|---|---|
| Mechanism | Publish `0.0.123` to Verdaccio, serve via local-unpkg | The Portal hands extensions its **own bundled** SDK |
| Covers | SDK **and** toolkit | **SDK only** |
| Prod-like? | Yes — a real IIFE fetch from a CDN | No — skips the IIFE/CDN path entirely |
| Needs Docker? | Yes | No |
| Tracked-file edits | Extension manifests + lock files, in both repos | None |

> ⚠️ **inject-host silently takes precedence.** When `VITE_INJECT_HOST_SDK=true`, `loadSdkModule` /
> `loadSdkApiModule` return the host SDK *before any fetch happens*, so the local CDN is never consulted and
> a published `0.0.123` build is ignored with no error, no warning and no network request to notice. If you
> are using the main flow, the flag must be commented out of `.env.local`.

### How it works

In `agentic-portal/src/lib/utils/extensionSdkRegistry.ts`:

- `INJECT_HOST_SDK = import.meta.env.DEV && VITE_INJECT_HOST_SDK === 'true'` — **dev builds only**, never
  active in a production build.
- When enabled, the loaders skip the IIFE fetch and register the Portal's own bundled `@beamable/sdk` onto
  the versioned window globals. `registerHostSdkGlobalsFromBundle` (called from `extensionMountHandler.ts`)
  reads the extension bundle text and registers the host SDK under **whatever** `@beamable/sdk-<V>` /
  `@beamable/sdk/api-<V>` keys the bundle baked.
- **It is therefore version-agnostic.** The regex matches any version suffix, so it doesn't matter which
  toolkit version an extension was built against — an extension built with the *published* toolkit still
  receives the host's SDK at runtime. No local toolkit and no extension `package.json` edit is needed.
- Because the injected SDK is the Portal's own bundled copy, the Portal has to resolve `@beamable/sdk` to
  your local build. That is the only real requirement, and `npm link` satisfies it. Vite dedupes
  `@beamable/sdk` to a single module in the graph, so extensions share the exact instance the host uses.

### Setup

`npm link` creates a symlink under `node_modules` and **does not modify `package.json` or
`package-lock.json`** — so nothing is committable:

```bash
cd /path/to/BeamableProduct/web
pnpm install   # first time only
pnpm build     # produces web/dist
npm link       # registers @beamable/sdk globally

cd /path/to/agentic-portal
npm link @beamable/sdk   # node_modules/@beamable/sdk -> BeamableProduct/web
```

Verify — the manifest should still read its registry pin, and `node_modules` should be a symlink:

```bash
grep '"@beamable/sdk"' package.json          # unchanged registry version — good
readlink node_modules/@beamable/sdk          # -> .../BeamableProduct/web
```

Then add to `agentic-portal/.env.local` (already git-ignored via `*.local`):

```
VITE_INJECT_HOST_SDK=true
```

### The loop

For any change **inside the Web SDK** this is the whole thing — zero tracked-file edits in either repo, no
toolkit rebuild, no extension changes:

```bash
cd /path/to/BeamableProduct/web && pnpm build   # rebuild the local SDK
cd /path/to/agentic-portal && npm run dev       # restart so Vite re-bundles the symlink
```

- **Restart the dev server, not just the browser** — a reload alone won't re-bundle the symlinked SDK.
- Extensions stay on their **published** `@beamable/portal-toolkit` spec. Do not edit their `package.json`.
- Run the extension's microservice so `RequestPortalExtensionData` serves its bundle:
  `beam project run --ids <service-id>`.
- Clear the `portal-extension-*` `localStorage` keys and reload.

Typechecking is the part this doesn't cover: `npx tsc --noEmit` inside an extension resolves
`@beamable/sdk` from *that* folder's `node_modules`, i.e. the registry copy. `npm link @beamable/sdk` in the
extension folder too if you need local types there.

### Teardown

```bash
cd /path/to/agentic-portal
npm unlink @beamable/sdk
npm install                     # restore the registry copy
# then remove VITE_INJECT_HOST_SDK from .env.local
```

### Verifying it worked

1. The extension mounts with **no** *"did not register on window"* error.
2. Add a temporary `console.log` in the local SDK, `pnpm build` in `web/`, restart the Portal, reload — the
   log appears in the extension, with **no** IIFE/CDN fetch in the Network tab, and `git status` is clean of
   any `package.json` or lock-file change in both repos.
