# portal-localdev

Local development stack for the Beamable portal extension system.

Runs two services:

| Service | Port | Purpose |
|---------|------|---------|
| **Verdaccio** | 4873 | Local npm registry — holds locally-built `@beamable/*` packages |
| **local-unpkg** | 4874 | CDN file server — serves individual files from Verdaccio tarballs, mirroring the unpkg.com URL format used by the Portal at runtime |

You do not normally drive these by hand. **See [WEB_LOCAL_DEV.md](../WEB_LOCAL_DEV.md) for the full
guide**; the short version, from the repository root:

```bash
./setup-web.sh                                            # start the stack, clean slate
BEAM_WORKSPACE=<repo with your extensions> ./dev-web.sh    # build, publish, repoint extensions
./teardown-web.sh                                          # stop and wipe
```

Those scripts are thin wrappers over the CLI, which you can also call directly:

```bash
beam web publish                          # build + publish both packages as 0.0.123
beam web use --workspace <repo>            # pin 0.0.123 there and force-refresh the install
beam web status                            # what's running, what's published, and when
beam web reset                             # wipe and restart empty
beam web stop                              # stop (--wipe to drop packages)
```

## How local packages get picked up: the `0.0.123` marker

> Any package published as version **`0.0.123`** is treated as a local-dev build.

That's the same sentinel `dev.sh` uses for the .NET packages. The Portal string-matches the prefix and routes
those versions to `http://localhost:4874` instead of unpkg.com, with **no environment variable** (see
`LOCAL_DEV_PREFIX` in `src/lib/utils/extensionSdkRegistry.ts`; override the CDN base with
`VITE_LOCAL_CDN_BASE` if you moved the port).

Both packages share that version, and the toolkit's `peerDependencies['@beamable/sdk']` points at it — the
Portal reads that peer dep to pick an extension's SDK, so they have to agree.

Three consequences worth knowing:

- **The version never changes**, so an extension's pin is a one-time edit. What *does* change is the content
  under it, which is why `beam web publish` unpublishes before republishing, flushes this CDN's file cache,
  and `beam web use` deletes the installed copy before reinstalling. A plain `npm install` would see
  `0.0.123` already present and do nothing.
- **`0.0.123` only exists here**, so installs for a project pinning it have to be routed at this registry.
  The CLI does that wherever it installs an extension; by hand, add `--registry http://localhost:4873`.
- **`0.0.123.N` is not valid npm semver** (4-part versions aren't), which is why the web side uses the bare
  base version where the .NET side appends a build number.

### Why `@beamable/*` IS proxied to npmjs

`verdaccio/config.yml` gives the scope an npmjs uplink. It's safe because `0.0.123` exists nowhere upstream —
publishing a version the uplink *also* serves is what fails with `409 Conflict`, and that can't happen here.
And it's required, because installs routed at this registry still have to resolve every other `@beamable`
spec in the tree (extensions on a published toolkit, extension libraries) which would otherwise 404.

---

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for the Docker workflow)
- OR Node.js ≥ 22 (for the non-Docker workflow)
- pnpm

---

## Starting and stopping

### With Docker (recommended)

```bash
# Start both services in the background
docker compose up -d

# Rebuild local-unpkg after changing its source
docker compose up -d --build

# Stop
docker compose down

# Stop and wipe all published packages (clean slate) — or just run `beam web reset`
docker compose down -v
```

`beam local init --with-web-registry` wires this stack into `beam local up`, so it starts and stops with
the rest of the local stack.

### Without Docker

```bash
# Terminal 1 — Verdaccio
npx verdaccio --config ./verdaccio/config.yml

# Terminal 2 — local-unpkg
cd local-unpkg
npm install
node index.js
```

---

## The local dev workflow

```bash
# 1. Start the stack
docker compose up -d

# 2. Build + publish both packages as 0.0.123, from the BeamableProduct checkout
beam web publish

# 3. Point your extensions at it (rewrites their toolkit pin, then installs)
beam web use --workspace /path/to/agentic-portal

# 4. Run the extension's microservice and the Portal. No portal configuration needed.
beam project run --ids <extension-id>

# 5. When done — the pin is in tracked files
git restore '**/package.json' '**/package-lock.json'    # in the extensions repo
```

### Iterating on the SDK

```bash
beam web publish --only sdk        # rebuilds the SDK; publishes both
beam web use --workspace /path/to/agentic-portal
```

Then hard-reload the Portal. The SDK is external to the extension bundle and loaded at runtime, so
**no extension rebuild is needed** — unlike the toolkit, which is compiled in.

---

## How it works

```
Extension package.json
  └─ "@beamable/portal-toolkit": "0.0.123"   ← written once by `beam web use`
       │
       ▼
Portal recognises the 0.0.123 prefix → routes to /local-unpkg (Vite proxy → :4874)
  └─ GET /@beamable/portal-toolkit@0.0.123/package.json
       │   (local-unpkg pulls the tarball from Verdaccio and extracts the file)
       ▼
   peerDependencies['@beamable/sdk'] → "0.0.123"   ← same version, stamped at publish
       │
       ▼
GET /local-unpkg/@beamable/sdk@0.0.123/dist/browser/index.iife.js   (+ dist/api.iife.js)
       ▼
window['@beamable/sdk-0.0.123'] = <SDK IIFE>
```

---

## Troubleshooting

**An extension still loads the published SDK/toolkit**
`beam web status` — is `0.0.123` published, and is the publish time recent? Then check the extension's
`package.json` actually pins it (re-run `beam web use` if not). Finally, if the Portal's `.env.local` sets
`VITE_INJECT_HOST_SDK=true`, comment it out — it takes precedence over the local CDN.

**My change was published but the extension still has the old build**
The npm layer: a plain `npm install` sees `0.0.123` already installed and does nothing. `beam web use`
deletes the installed copy first — use it (or `beam local up --build`) rather than installing by hand.

**`npm error notarget No matching version found for @beamable/portal-toolkit@0.0.123`**
That version exists only here, so the install must be routed at this registry. The CLI does that
automatically; by hand, add `--registry http://localhost:4873`.

**`local-unpkg` returns 404 for a package**
It isn't published at that version. Run `beam web publish`, and check `beam web status`.

**Stale file served by local-unpkg**
Its file cache is keyed by `pkg@version`, and the version never changes, so a republish has to invalidate it.
`beam web publish` does that via `POST :4874/__flush`. If you published by hand, call that endpoint or
`docker compose restart local-unpkg`. If the flush 404s, the container predates the endpoint — rebuild it
with `docker compose up -d --build`.

**`ERR_PNPM_TARBALL_INTEGRITY` / `EINTEGRITY`**
A cached entry holds the integrity hash of a tarball that has since been replaced. `beam web reset` evicts
the `@beamable` cache entries as part of wiping the registry.

**`409 Conflict - this package is already present` when publishing by hand**
Verdaccio won't overwrite a version. Unpublish first — which is exactly what `beam web publish` does:
`npm unpublish <pkg>@<version> --force --registry http://localhost:4873`.

**Port conflict**
Change the ports in `docker-compose.yml`, then pass `--registry` / `--cdn` to the `beam web` commands and
set `VITE_LOCAL_CDN_BASE` in the Portal's `.env.local`.
