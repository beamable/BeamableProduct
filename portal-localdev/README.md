# portal-localdev

Local development stack for the Beamable portal extension system.

Runs two services:

| Service | Port | Purpose |
|---------|------|---------|
| **Verdaccio** | 4873 | Local npm registry — install packages from here instead of npmjs |
| **local-unpkg** | 4874 | CDN file server — serves individual files from Verdaccio tarballs, mirroring the unpkg.com URL format used by the Portal at runtime |

## How the Portal knows to use local services

Any package published as version **`0.0.123`** is treated as a local-dev build.
When the Portal sees a `ToolkitVersion` of `0.0.123` in an extension manifest, it
automatically routes all fetches for that version to `http://localhost:4874`
(local-unpkg) instead of `https://unpkg.com`. No environment variables needed.

This extends transitively: if the local toolkit's `peerDependencies['beamable-sdk']`
is also `0.0.123`, the SDK IIFE is fetched from localhost too. If it references a
real published version like `0.6.0`, that version is still fetched from the real CDN.

> **Port override:** `localhost:4874` is the default. Set `LOCAL_CDN_BASE_URL` in
> the Portal's `.env.local` if you run local-unpkg on a different port.

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

# Stop
docker compose down

# Stop and wipe all published packages (clean slate)
docker compose down -v
```

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

### Step 1 — Start the local stack

```bash
docker compose up -d
```

### Step 2 — Build and publish local packages as `0.0.123`

**Toolkit only** (testing toolkit changes with the real published SDK):

```bash
cd beam-portal-toolkit

# Set version to 0.0.123 in package.json (peerDependencies.beamable-sdk stays as the real version)
pnpm version 0.0.123 --no-git-tag-version

pnpm build
pnpm publish --registry http://localhost:4873 --no-git-checks
```

**Toolkit + SDK** (testing both locally):

```bash
# 1. Publish SDK as 0.0.123
cd beamable-sdk
pnpm version 0.0.123 --no-git-tag-version
pnpm build
pnpm publish --registry http://localhost:4873 --no-git-checks

# 2. Publish toolkit as 0.0.123, referencing local SDK
cd beam-portal-toolkit
# Update peerDependencies.beamable-sdk to "0.0.123" in package.json
pnpm version 0.0.123 --no-git-tag-version
pnpm build
pnpm publish --registry http://localhost:4873 --no-git-checks
```

> Verdaccio accepts re-publishing the same version. Restart `local-unpkg` after
> re-publishing to bust its in-memory file cache:
> `docker compose restart local-unpkg`

### Step 3 — Set your extension to use `0.0.123`

In your extension's manifest (e.g. `extension.json` or similar):

```json
{
  "ToolkitVersion": "0.0.123"
}
```

In your extension's `package.json` devDependencies, point at the local registry:

```json
{
  "devDependencies": {
    "@beamable/portal-toolkit": "0.0.123",
    "beamable-sdk": "0.0.123"
  }
}
```

Then install from the local registry:

```bash
pnpm install --registry http://localhost:4873
```

Or add a `.npmrc` in the extension project:

```ini
registry=http://localhost:4873
```

Build the extension normally:

```bash
pnpm build
```

### Step 4 — Run the Portal

```bash
cd Portal
pnpm dev
```

That's it. No environment variables needed. The Portal detects `ToolkitVersion: "0.0.123"`
and automatically fetches from `localhost:4874`.

---

## How it works

```
Extension manifest
  └─ ToolkitVersion: "0.0.123"   ← local-dev signal
       │
       ▼
Portal detects 0.0.123 → routes to http://localhost:4874
  └─ GET http://localhost:4874/@beamable/portal-toolkit@0.0.123/package.json
       │   (local-unpkg fetches tarball from Verdaccio, extracts file)
       ▼
   peerDependencies.beamable-sdk

   Case A — "0.6.0" (real SDK)        Case B — "0.0.123" (local SDK)
       │                                    │
       ▼                                    ▼
GET https://unpkg.com/beamable-sdk@0.6.0/  GET http://localhost:4874/beamable-sdk@0.0.123/
    dist/browser/index.global.js               dist/browser/index.global.js
       │                                    │
       └──────────────┬─────────────────────┘
                      ▼
          window['beamable-sdk-<version>'] = <SDK IIFE>
```

---

## Troubleshooting

**`local-unpkg` returns 404 for a package**
The package hasn't been published to Verdaccio yet. Run `pnpm publish --registry http://localhost:4873 --no-git-checks` from the package directory.

**Stale file after republishing**
`local-unpkg` caches files in memory. Restart it to clear the cache:
`docker compose restart local-unpkg`

**Extension still loads from unpkg**
Check that `ToolkitVersion` in your extension manifest is exactly `0.0.123`
(or a pre-release variant like `0.0.123-1`). Any other version routes to the real CDN.

**Port conflict**
Change the ports in `docker-compose.yml`. Add `LOCAL_CDN_BASE_URL=http://localhost:<your-port>`
to the Portal's `.env.local` to override the local-dev CDN base.
