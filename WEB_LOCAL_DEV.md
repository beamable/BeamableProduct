# Local Web SDK & Portal Toolkit — usage guide

How to develop an **unpublished, local build** of `@beamable/sdk` or `@beamable/portal-toolkit` and see it
running in the Portal and in portal extensions.

- [TL;DR](#tldr)
- [How it works: version `0.0.123`](#how-it-works-version-00123)
- [One-time setup](#one-time-setup)
- [Driving it from `beam local up`](#driving-it-from-beam-local-up)
- [Building the packages](#building-the-packages)
  - [Building the Web SDK](#building-the-web-sdk-beamablesdk)
  - [Building the Portal Toolkit](#building-the-portal-toolkit-beamableportal-toolkit)
- [The iteration loops](#the-iteration-loops)
- [Cleaning up — IMPORTANT](#cleaning-up--important)
- [Command reference](#command-reference)
- [The shell scripts](#the-shell-scripts)
- [Troubleshooting](#troubleshooting)
- [How it works under the hood](#how-it-works-under-the-hood)

---

## TL;DR

```bash
# 1. Once per session: start the local registry (and wipe anything previously published)
cd /path/to/BeamableProduct
./setup-web.sh

# 2. Every iteration: build, publish as 0.0.123, refresh the projects that use it
BEAM_WORKSPACE=/path/to/agentic-portal ./dev-web.sh
#    ...add --build if the packages' own dependencies changed
#    ...add --only sdk / --only toolkit to rebuild just one (both are still published)

# 3. Run the Portal and the extension's microservice as usual. NO portal configuration needed.
npm run dev                        # in agentic-portal
beam project run --ids <ext-id>

# 4. When you're done — the pin lives in TRACKED files, so revert it
./teardown-web.sh
cd /path/to/agentic-portal && git restore '**/package.json' '**/package-lock.json'
```

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
| lock file integrity | — | the install rewrites it, keeping the lock consistent with what's on disk |
| local-unpkg's file cache | `pkg@version` | `beam web publish` calls `POST :4874/__flush` |
| browser HTTP cache | URL | local-unpkg answers `Cache-Control: no-store` |
| Portal's in-memory module cache | version | cleared by a full page reload |
| extension bundle cache | localStorage | clear the `portal-extension-*` keys |

The first row is the one that bites, and the reason `beam web use` exists rather than a plain `npm install`.
Verified: a plain `npm install` leaves the old build in place; `beam web use` replaces it.

Safe to reuse because `0.0.123` exists nowhere on npmjs (404 for both packages), so it can never collide
with the registry's uplink, and removing it can't affect a published version.

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

Prefer it managed with the rest of your local stack? See the next section.

### Portal configuration

**None.** The `0.0.123` prefix is self-identifying, so the Portal routes those versions to the local CDN
automatically (`LOCAL_DEV_PREFIX` in `src/lib/utils/extensionSdkRegistry.ts`).

> One exception: if you have `VITE_INJECT_HOST_SDK=true` in the Portal's `.env.local`, comment it out. That
> is a different approach — it hands extensions the Portal's own bundled SDK before any fetch happens, so
> the local CDN is never consulted. See `agentic-portal/docs/LOCAL_WEBSDK_INJECT_HOST.md`.

---

## Driving it from `beam local up`

`beam local up` can own the whole loop: start the registry, publish your local packages, refresh the
extensions, and then run them against what it just built.

### Opt in, once

```bash
beam local init --with-web-registry
```

That adds three steps to `.beamable/local-stack.json`. Paths are auto-detected — the registry directory from
the `BeamableProduct` checkout next to you, and the extensions repo from the `--portal-dir` you already give
`init`:

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

### Why the steps sit where they do

- **After the Scala services.** `beam local up` performs a realm/login before its *first* `beam` step, and
  that authenticates through the Scala `auth` service. Putting the web steps earlier would fire that login
  against a backend that isn't up yet.
- **Before the extension steps.** `beam project run --ids <ext>` builds the extension, and the toolkit is
  compiled into its bundle — so the refresh has to land first.
- **They abort the stack on failure.** Both are run-to-completion steps, and a non-zero exit tears the stack
  down. Deliberate: quietly running extensions against a stale toolkit after a failed publish is worse than
  stopping.

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

#### If a toolkit change needs a brand-new SDK API

The toolkit's `devDependencies['@beamable/sdk']` is deliberately left pointing at the published version —
it only supplies build-time types, and repointing it drags in lock-file and pnpm-store churn. If you're
adding an SDK API *and* consuming it from the toolkit in the same change, link the SDK by hand
(`link:../web`); see `agentic-portal/extensions/LocalToolkit/README.md`.

#### Why the toolkit and the SDK behave differently at runtime

The single most useful thing to internalise:

- The **SDK is external** to an extension's bundle — the browser fetches its IIFE at runtime. Republish it
  and a hard reload picks it up. **No extension rebuild.**
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

Then hard-reload the Portal. **No extension rebuild** — the SDK is fetched at runtime.

`--only sdk` skips the toolkit's *build*, but still publishes it and still repoints your extensions: the
version has to move on both, because the toolkit's peer dep is how the Portal finds the SDK.

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

## Cleaning up — IMPORTANT

`dev-web.sh` writes the local version into **tracked files** in the repo holding your extensions. That is
how the model works, but it must never be committed.

```bash
# stop the registry
cd /path/to/BeamableProduct && ./teardown-web.sh

# revert the pins, in the repo holding your extensions
cd /path/to/agentic-portal
git restore '**/package.json' '**/package-lock.json'
npm install     # in any extension you actually ran, to restore the published toolkit

# pre-commit sanity check
git status --porcelain | grep -E 'package(-lock)?\.json' \
  && echo "LOCAL DEV PINS STILL PRESENT — do not commit" || echo clean
```

Two things to know:

- The lock files end up with `resolved` URLs pointing at `http://localhost:4873`, so reverting
  `package-lock.json` matters as much as `package.json`.
- A `file:` toolkit spec (a deliberately vendored `.tgz`) is rewritten too. `git restore` brings it back.

---

## Command reference

All `beam web` commands are standalone — no Beamable workspace, service manifest or backend connection
required.

### `beam web publish`

Builds both packages and publishes them under a new local-dev version.

| Option | Meaning |
|---|---|
| `--product-dir <path>` | The `BeamableProduct` checkout. Defaults to searching upwards from the working directory. |
| `--only sdk\|toolkit` | Rebuild just one package. **Both are still published**, at the same version — see below. |
| `--version <v>` | Publish as this exact version instead of incrementing, to republish a specific build. |
| `--skip-build` | Publish what's already built. |
| `--force-install` | `pnpm install` before building even when `node_modules` exists. `./dev-web.sh --build` maps to this. |
| `--registry` / `--cdn` | Non-default ports. |

#### `--only` rebuilds one package but publishes both

The two versions have to match. The Portal resolves an extension's SDK through the toolkit's
`@beamable/sdk` peer dependency, so a toolkit published at version N names an SDK at version N — publishing
only one would leave that peer dep pointing at a version nothing published, and the SDK fetch 404s at
runtime with the extension failing to mount.

So `--only sdk` rebuilds the SDK, republishes the toolkit's existing `dist/` unchanged, and publishes both
at the new version. You still save the (slower) toolkit build. If the un-built package has no `dist/` yet,
it is built anyway.

### `beam web use`

Points the extensions under a directory at a locally published build, and installs it.

| Option | Meaning |
|---|---|
| `--workspace <path>` | Directory tree to scan. Defaults to the working directory. |
| `--version <v>` | The version to pin. Defaults to the registry's `local` dist-tag — i.e. the newest build. |
| `--skip-install` | Rewrite the pins without running `npm install`. |

`beam portal extension update-toolkit --local` does the same rewrite, but discovers extensions via the
Beamo service manifest, which makes it authenticate against the configured host — so it fails when the
backend isn't running. `beam web use` scans the filesystem instead and works offline; that's why
`dev-web.sh` calls it.

### `beam web status`

Reachability of the registry and CDN, which local builds exist, and where the `local` dist-tag points —
the pointer `beam web use` follows. First thing to run when an extension loads the wrong thing.

### `beam web reset`

Wipes the registry and restarts it empty. Also evicts the cached `@beamable` tarballs from the pnpm store,
since a wipe means the integrity hashes they hold no longer match anything.

### `beam web stop`

Stops the containers. `--wipe` also deletes the published packages.

---

## The shell scripts

Thin wrappers that run the CLI from source via `dotnet run`, so there is one implementation and it behaves
the same on Windows, macOS and Linux.

| Script | Runs |
|---|---|
| `./setup-web.sh` | `beam web reset` |
| `./dev-web.sh` | `beam web publish` then `beam web use` |
| `./teardown-web.sh` | `beam web stop --wipe` |

| Variable | Effect |
|---|---|
| `BEAM_WORKSPACE` | The repo holding your extensions, where `beam web use` runs. Defaults to this repo. |
| `BEAM_SKIP_UPDATE` | Publish without repointing any extension. |
| `BEAM_FULL_BUILD` | Same as passing `--build`. |
| `BEAM_CLI_NO_BUILD` | Skip rebuilding the **CLI** between runs — faster, but stale if you changed CLI source. |
| `BEAM_CLI_FRAMEWORK` | Target framework for the CLI, default `net10.0`. |

`dev-web.sh` build flags:

| Flag | Effect |
|---|---|
| *(none)* | Builds both packages, skipping `pnpm install` when `node_modules` exists. |
| `--build` | Full build — reinstalls dependencies first. |
| `--skip-build` | Publishes `dist/` as-is. |

Every other argument passes through, so `./dev-web.sh --only sdk` and `./setup-web.sh --keep-caches` work.

---

## Troubleshooting

**An extension still loads the published SDK/toolkit.**

1. `beam web status` — is a local build published, and where does `local` point?
2. Does the extension's `package.json` pin that version? Re-run `beam web use` if not.
3. Is `VITE_INJECT_HOST_SDK` set in the Portal's `.env.local`? It wins — comment it out.
4. Clear the `portal-extension-*` `localStorage` keys and hard-reload.

**`npm error notarget No matching version found for @beamable/portal-toolkit@0.0.123`**
`0.0.123` exists only on the local registry, so the install has to be routed there. The CLI does that
automatically wherever it installs an extension; if you're running `npm install` by hand, add
`--registry http://localhost:4873`. Also check the registry is actually running.

**I published, but my change isn't in the extension.**
Almost always the npm layer: a plain `npm install` sees `0.0.123` already installed and does nothing. Run
`beam web use` (or `beam local up --build`), which deletes the installed copy first. Confirm with
`beam web status` that the publish time is recent, then hard-reload the Portal so it drops its in-memory
module cache.

**The build fails inside `beam web publish` / `dev-web.sh`.**
The error is the raw `pnpm` output. Reproduce it directly — `cd web && pnpm build`, or
`cd beam-portal-toolkit && pnpm sync-components --no-copy && pnpm build`. If it complains about missing or
mismatched dependencies, run with `--build`.

**`beam web publish` says the registry isn't reachable.**
Run `./setup-web.sh`, or `docker compose up -d` in `portal-localdev/`. Check Docker is running.

**Everything 404s from `/local-unpkg` in the browser.**
The Vite dev server proxies `/local-unpkg` → `localhost:4874`; check that container is up
(`beam web status`).

**`git status` shows changes under `beam-portal-toolkit/src/generated/`.**
`pnpm sync-components` regenerates those. `beam web publish` restores them; running it by hand doesn't.
`git restore beam-portal-toolkit/src/generated`.

---

## How it works under the hood

```
PUBLISH   beam web publish            (./dev-web.sh, or `beam local up --build`)
            unpublish 0.0.123 from the local registry     ← it already exists; Verdaccio won't overwrite
            SDK      → stamp 0.0.123                      → build → publish --tag local
            toolkit  → stamp 0.0.123 + sdk peer dep       → build → publish --tag local
            POST :4874/__flush                            ← or the CDN keeps serving the old files

ADOPT     beam web use
            resolves 0.0.123 from the registry's `local` dist-tag
            ensures the toolkit pin is 0.0.123            ← a no-op after the first run
            rm -rf node_modules/@beamable/portal-toolkit  ← the part that makes it actually refresh
            npm install @beamable/portal-toolkit@0.0.123 --registry http://localhost:4873

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

**Why the registry proxies npmjs for `@beamable/*`.** Extension installs get routed at the local registry,
so every other `@beamable` spec in that install still has to resolve — extensions pinning a published
toolkit, extension libraries, and so on. Proxying is safe because `0.0.123` exists nowhere upstream:
publishing a version the uplink *also* serves is what fails with `409 Conflict`, and that can't happen here.

**Why publish unpublishes first.** Verdaccio rejects a publish for a version it already holds. Since only
`0.0.123` is ever removed, and the uplink has no such version, this can't disturb a published package.

### Files involved

| Path | Role |
|---|---|
| `portal-localdev/docker-compose.yml` | The two containers |
| `portal-localdev/verdaccio/config.yml` | Registry config, including the npmjs uplink |
| `portal-localdev/local-unpkg/index.js` | The unpkg-style file server, incl. `POST /__flush` |
| `cli/cli/Commands/Web/` | The `beam web` commands |
| `cli/cli/Services/Web/WebLocalRegistryService.cs` | The version constant, force-reinstall, install routing, discovery |
| `cli/cli/Services/LocalStack/LocalStackTemplate.cs` | The `beam local up` steps |
| `agentic-portal/src/lib/utils/extensionSdkRegistry.ts` | `LOCAL_DEV_PREFIX` routing to the local CDN |
| `agentic-portal/vite.config.ts` | The `/local-unpkg` → `:4874` dev proxy |

Related: `agentic-portal/docs/LOCAL_WEBSDK.md` (portal-side detail),
`agentic-portal/docs/LOCAL_WEBSDK_INJECT_HOST.md` (the SDK-only alternative that needs no Docker),
`portal-localdev/README.md` (the registry stack itself), `docs/developer-help.md` (the .NET `dev.sh` loop
this mirrors).
