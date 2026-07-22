# @beamable/portal-toolkit

Utilities and type definitions for building Beamable portal extensions.

- **`Portal.registerExtension()`** — registers your extension with the portal host
- **TypeScript types** for all Beamable web components (`beam-btn`, `beam-data-table`, etc.)
- **Re-exports** of `@beamable/sdk` types

---

## Installation

```bash
npm install @beamable/portal-toolkit
# @beamable/sdk is a peer dependency — install it too
npm install @beamable/sdk
```

Because `@beamable/sdk` is injected as a global by the portal host at runtime, mark it external in your bundler:

```ts
// vite.config.ts
export default {
  build: {
    rollupOptions: {
      external: ['@beamable/sdk'],
    },
  },
};
```

---

## Usage

### Registering an extension

```ts
import { Portal } from '@beamable/portal-toolkit';

Portal.registerExtension({
  beamId: 'MyExtension',
  onMount(container, context) {
    // context.beam   — Promise<Beam> — the Beamable SDK instance
    // context.cid    — customer ID
    // context.realm  — realm string (cid.pid)
    container.innerHTML = `<p>Hello from ${context.realm}</p>`;
    return container; // returned value is passed to onUnmount
  },
  onUnmount(instance) {
    // tear down your UI
  },
});
```

### Persisting data (`context.storage`)

The portal hands every extension a `context.storage` object with three tiers. Values are isolated by your extension **and** the signed-in account automatically, so you never manage global `localStorage` keys or worry about collisions.

| Tier | Lives | Backed by |
|---|---|---|
| `session` | The tab/session | `sessionStorage` |
| `local` | This device, across reloads | `localStorage` |
| `user` | Durable, follows the user across devices | server (**deferred** — rejects until it ships) |

Every tier shares one **async** API, so `local` code is a drop-in for `user` later:

```ts
// per-realm (default), this device, survives reloads
await context.storage.local.set('lastFilter', { seg: 'whales' });
const filter = await context.storage.local.get<Filter>('lastFilter'); // null if unset

// expire a value: TTL in ms, evaluated lazily at read time
await context.storage.local.set('dismissedBanner', true, { ttl: 7 * 864e5 });

// one value shared across every realm in the org (still this account, this device)
await context.storage.local.scope({ scope: 'cid' }).set('compactMode', true);

// keep separate state per mount site when the same bundle mounts in two places
await context.storage.local.scope({ mount: 'instance' }).set('lastFilter', filter);

// react to changes (also fires for another live mount of this extension)
const unsub = context.storage.local.subscribe('lastFilter', (v) => render(v));
```

React authors can use the `useStoredState` hook from `@beamable/portal-toolkit/react`:

```tsx
import { useStoredState } from '@beamable/portal-toolkit/react';

function App({ context }: { context: ExtensionContext }) {
  const [filter, setFilter] = useStoredState(context.storage.local, 'filter', 'all');
  return <FilterBar value={filter} onChange={setFilter} />;
}
```

> **Interface vs. implementation.** This package only *defines* `context.storage` (the `ExtensionStorage` types + the `useStoredState` hook). The portal host *implements* it and builds the object at mount — the same split as the rest of the context (`beam`, `config`, `navigate`). Extension authors just use `context.storage`.

## Development

### Syncing web components from agentic-portal

The custom element definitions live in the `agentic-portal` repo. When that repo adds or changes components, pull them into this package:

```bash
# 1. In the agentic-portal repo, regenerate its CEM:
cd ../../agentic-portal && npm run generate:cem

# 2. Back in this repo, copy the CEM and regenerate all type files:
cd -
pnpm sync-components
```

By default the sync script expects `agentic-portal` to be a sibling directory:

```
~/Documents/Github/
  BeamableProduct/   ← this repo
  agentic-portal/    ← agentic-portal repo
```

Override the path if yours is elsewhere:

```bash
PORTAL_REPO_PATH=/path/to/agentic-portal pnpm sync-components
```

This regenerates:
- `custom-elements.json` — CEM manifest (VS Code HTML IntelliSense)
- `src/generated/globals.ts` — `HTMLElementTagNameMap` augmentations
- `src/generated/web-types.json` — JetBrains/WebStorm autocomplete

Commit all generated files after syncing.

### Never hand author files in the generated folder
The `src/generated` folder is exclusively for generated files created by running the sync-components script. 

### Building locally

```bash
pnpm install
pnpm build
```

`prepublishOnly` runs `sync-components --no-copy && build` automatically before publish, so the
generated files are always up to date without re-copying from `agentic-portal`.

---

## Releasing a new version

### Step 1 — Sync components (if agentic-portal changed)

If the `agentic-portal` repo has new or updated components since the last release, run:

```bash
cd ../../agentic-portal && npm run generate:cem && cd -
pnpm sync-components
```

Review the diff, then commit.

### Step 2 — Update the portal repo version mapping

> **This step is required.** The portal host uses a version map to know which toolkit version to load for a given extension. Without this update, extensions referencing the new version will fail to load.

In the **`agentic-portal` repo**, update the version mapping file (ask the portal team for the exact location if unsure) to add an entry for the new toolkit version.

### Step 3 — Trigger the GitHub Action

Go to **Actions → Portal Toolkit Release** and click **Run workflow**. Fill in:

| Field | Notes |
|---|---|
| `major` / `minor` / `patch` | The new semver components |
| `releaseType` | `production` for a stable release, `rc` for a release candidate, `nightly`/`exp` for pre-release |
| `rcNumber` | Only needed when `releaseType` is `rc` |
| `dryRun` | Set `true` to verify the build without publishing |

The workflow will:
1. Check out the repo at the specified commit
2. Set the package version from the inputs
3. Run `prepublishOnly` (sync + build)
4. Publish to npm with provenance attestation

### Version format

| `releaseType` | npm tag | Version example |
|---|---|---|
| `production` | `latest` | `1.2.3` |
| `rc` | `rc` | `1.2.3-rc.1` |
| `nightly` | `nightly` | `1.2.3-nightly` |
| `exp` | `exp` | `1.2.3-exp` |

### Manual publish (first-time or emergency)

```bash
cd beam-portal-toolkit
nvm use 22.14.0 && corepack enable
pnpm install
# optionally bump version first:
# npm version 0.2.0 --no-git-tag-version
pnpm publish --access public
```

You must be logged in to npm (`npm login`) with publish rights to the `@beamable` org and have an automation token configured (see npm → Access Tokens → Granular Access Token with "bypass 2FA").

---

## Package exports

| Import path | Contents |
|---|---|
| `@beamable/portal-toolkit` | `Portal` namespace, all `@beamable/sdk` types, web component globals |
| `@beamable/portal-toolkit/custom-elements.json` | CEM manifest |
| `@beamable/portal-toolkit/web-types.json` | JetBrains web-types |
