# Local Web SDK & Portal Toolkit — development guide

How to develop `@beamable/sdk` (`web/`) and `@beamable/portal-toolkit` (`beam-portal-toolkit/`) against the Portal
and its extensions, and how they are released.

- [The local package loop](#the-local-package-loop)
- [Building by hand](#building-by-hand)
- [Releasing](#releasing)
- [Appendix: the inject-host alternative](#appendix-the-inject-host-alternative)

---

## The local package loop

Run a stack with the `:web` variant from the portal workspace (agentic-portal). Prerequisites: Docker and a machine
set up with `beam local setup`.

```bash
beam local run csharp-portal:web   # the local backend, the portal, its extensions, and your package builds
beam local run web-dev             # the package loop, the portal and its extensions, against dev
beam local run web-dev:non-publish # type-check loop: build the packages, never publish or install them
beam local stop                    # extensions go back to the versions their lockfiles pin
```

The `:web` variant adds these steps:

| Step | What it does |
|---|---|
| `web: registry` | Starts Verdaccio (:4873) and local-unpkg (:4874) from [portal-localdev](../portal-localdev/README.md) |
| `web: build` | Builds each package whose sources changed, as `<next minor after npmjs>-0.<yyyymmddHHMMSS>`. A new SDK rebuilds the toolkit against it. The toolkit build first regenerates the portal's component manifest (`npm run generate:cem`) and syncs it |
| `web: publish` | Publishes each new version to the local registry, once |
| `web: install` | Writes `.beamable/local/web-overlay.json`: each extension installs the local versions over its lockfile with `--no-save`, so no `package.json` or lockfile changes |
| `web: watch` | Rebuilds, publishes and restarts the extensions on every save in `web/src`, `beam-portal-toolkit/src` or the portal's `src/beam-components` |

The portal starts with `VITE_SDK_CDN_BASE=/local-unpkg`, so extensions load the local SDK the way production loads it
from unpkg. Tracked files in both repositories are put back after every build.

If the portal's `.env.local` has `VITE_INJECT_HOST_SDK=true`, comment it out: that flow
([appendix](#appendix-the-inject-host-alternative)) takes precedence over the local CDN.

**The SDK and the toolkit behave differently at runtime.** The SDK is external to an extension's bundle: the browser
fetches its IIFE, so a new SDK needs only a page reload. The toolkit is compiled into each extension, so a new toolkit
means rebuilding the extensions, which `web: watch` does by restarting them.

To check you're on the local build: the board shows "Local web packages: @beamable/portal-toolkit 0.6.0-0.…,
@beamable/sdk 1.4.0-0.…", and the Network tab shows the SDK coming from `/local-unpkg/@beamable/sdk@<that version>`.

---

## Building by hand

### Web SDK

```bash
cd web
pnpm install          # first time, or after dependencies changed
pnpm build            # tsdown -> dist/ (the package's prepublishOnly)
pnpm dev              # tsdown --watch
```

The Portal fetches `web/dist/browser/index.iife.js` (the SDK) and `web/dist/api.iife.js` (the generated API).

To regenerate the SDK's API surface after changing a backend endpoint, with the local backend on `:8080`, from the
repository root:

```bash
dotnet run -f net10.0 --project ./cli/cli -- \
  --host http://localhost:8080 \
  oapi generate --engine web \
  --conflict-strategy RenameUncommonConflicts \
  --output ./web/src/__generated__
```

This writes tracked files under `web/src/__generated__`; review the diff before committing.

### Portal toolkit

The toolkit generates typed bindings for the Portal's `beam-*` web components from its `custom-elements.json` before
bundling.

```bash
cd beam-portal-toolkit
pnpm install
PORTAL_REPO_PATH=/path/to/agentic-portal pnpm sync-components   # copy the portal's manifest, regenerate src/generated
pnpm sync-components --no-copy                                  # or regenerate from the committed manifest
pnpm build
```

`sync-components` writes tracked files (`custom-elements.json`, `src/generated/*`). Run `npm run generate:cem` in the
portal first if its components changed. A harmless `UNRESOLVED_IMPORT` warning for `@vitejs/plugin-react` may appear.

---

## Releasing

Both packages are released from GitHub Actions, by hand, in one chain:

```
release-web.yml ──▶ release-portal-toolkit.yml ──▶ agentic-portal: update-portal-toolkit.yml
publish the SDK      pin the SDK, sync the           move every extension to the
                     components, publish the          new toolkit, open a PR
                     toolkit, open a PR here
```

**`release-web.yml`** (Web SDK Release):

| Input | |
|---|---|
| `commit`, `major`, `minor`, `patch` | What to release, as `X.Y.Z` |
| `releaseType` | `dev` → `X.Y.Z-dev.<run>`, `nightly` → `X.Y.Z-nightly`, `rc` → `X.Y.Z-rc.<rcNumber>`, `exp` → `X.Y.Z-exp`, `production` → `X.Y.Z`. The npm dist-tag is the type (`latest` for production) |
| `dryRun` | Build only; publish nothing |
| `toolkitVersion` | Also release the portal toolkit as this `X.Y.Z` on the new SDK; empty skips it |
| `toolkitRcNumber` | The toolkit's own rc number |
| `updateExtensions` | After the toolkit, move agentic-portal's extensions to it |

A `dev` release creates no GitHub release and no changelog. With `toolkitVersion`, the SDK run dispatches
`release-portal-toolkit.yml` with the same commit and release type and `sdkVersion` set to the SDK it published.

**`release-portal-toolkit.yml`** (Portal Toolkit Release) can also be run on its own. Besides the same inputs:

- `sdkVersion`: the toolkit's `@beamable/sdk` peer and dev dependency are set to it (the run waits until npm serves
  it). Empty keeps the committed pin.
- It copies `custom-elements.json` from agentic-portal's `main` before building; without the secret below it
  releases the committed copy and warns.
- After publishing (not for `dev`), it opens a PR here with what it changed: the SDK pin, the lockfile, the
  components.
- `updateExtensions`: dispatches agentic-portal's `update-portal-toolkit.yml`, which runs
  `scripts/bump-portal-toolkit.mjs <toolkit> --sdk <sdk>` (every pin and lockfile) and opens a PR there.

**One-time setup** (a repository or organization admin):

1. Secret `AGENTIC_PORTAL_TOKEN` in this repository: a fine-grained token for `beamable/agentic-portal` with
   **Contents: read** and **Actions: read and write**. It reads the components and dispatches the extension update.
2. "Allow GitHub Actions to create and approve pull requests" (Settings → Actions → General) in this repository and in
   agentic-portal.
3. Both packages publish through npm trusted publishing, each from its own workflow file; the chain dispatches rather
   than calls, so that stays true.

---

## Appendix: the inject-host alternative

A narrower flow that needs no Docker: the Portal hands extensions its **own bundled** SDK.

| | Local registry (`:web`) | inject-host |
|---|---|---|
| Covers | SDK **and** toolkit | **SDK only** |
| Prod-like? | Yes: a real IIFE fetch from a CDN | No: skips the IIFE/CDN path |
| Needs Docker? | Yes | No |

> ⚠️ **inject-host silently takes precedence.** When `VITE_INJECT_HOST_SDK=true`, the SDK loaders return the host SDK
> before any fetch, so the local CDN is never consulted, with no error and no request to notice.

### How it works

In `agentic-portal/src/lib/utils/extensionSdkRegistry.ts`, `INJECT_HOST_SDK` is
`import.meta.env.DEV && VITE_INJECT_HOST_SDK === 'true'` (dev builds only). When enabled, the loaders skip the IIFE
fetch and register the Portal's own `@beamable/sdk` under whatever `@beamable/sdk-<V>` / `@beamable/sdk/api-<V>` keys
the extension bundle baked, so it works with any toolkit version. The Portal has to resolve `@beamable/sdk` to your
local build, which `npm link` does without touching `package.json` or the lockfile.

### Setup

```bash
cd /path/to/BeamableProduct/web
pnpm install && pnpm build
npm link                     # registers @beamable/sdk globally

cd /path/to/agentic-portal
npm link @beamable/sdk       # node_modules/@beamable/sdk -> BeamableProduct/web
```

Then add `VITE_INJECT_HOST_SDK=true` to `agentic-portal/.env.local` (git-ignored).

### The loop

```bash
cd /path/to/BeamableProduct/web && pnpm build   # rebuild the local SDK
cd /path/to/agentic-portal && npm run dev       # restart so Vite re-bundles the symlink
```

Restart the dev server, not just the browser. Run the extension's microservice (`beam project run --ids <id>`) and
clear the `portal-extension-*` `localStorage` keys. Type-checking inside an extension still resolves the registry copy;
`npm link @beamable/sdk` there too if you need local types.

### Teardown

```bash
cd /path/to/agentic-portal
npm unlink @beamable/sdk
npm install
# then remove VITE_INJECT_HOST_SDK from .env.local
```
