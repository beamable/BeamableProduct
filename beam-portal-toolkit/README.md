# @beamable/portal-toolkit

Utilities and type definitions for building Beamable portal extensions.

- **`Portal.registerExtension()`** — registers your extension with the portal host
- **TypeScript types** for all Beamable web components (`beam-btn`, `beam-data-table`, etc.)
- **Svelte element types** for `.svelte` template autocomplete
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

### Svelte autocomplete

Add one line to your project's `app.d.ts`:

```ts
/// <reference types="@beamable/portal-toolkit/svelte" />
```

This gives `.svelte` templates full autocomplete for all `beam-*` components.

---

## Development

### Syncing web components from Portal

The custom element definitions live in the Portal repo. When Portal adds or changes components, pull them into this package:

```bash
# Copies Portal's beam-components.json and regenerates all type files
pnpm sync-components
```

By default the script expects Portal to be a sibling directory:

```
~/Documents/Github/
  BeamableProduct/   ← this repo
  Portal/            ← Portal repo
```

Override the path if yours is elsewhere:

```bash
PORTAL_REPO_PATH=/path/to/Portal pnpm sync-components
```

This regenerates:
- `custom-elements.json` — CEM manifest (VS Code HTML IntelliSense)
- `src/generated/globals.ts` — `HTMLElementTagNameMap` augmentations
- `src/generated/svelte-elements.ts` — `SvelteHTMLElements` augmentations
- `src/generated/web-types.json` — JetBrains/WebStorm autocomplete

Commit all generated files after syncing.

### Building locally

```bash
pnpm install
pnpm build
```

`prepublishOnly` runs `sync-components --no-copy && build` automatically before publish, so the
generated files are always up to date without re-copying from Portal.

---

## Releasing a new version

### Step 1 — Sync components (if Portal changed)

If the Portal repo has new or updated components since the last release, run:

```bash
pnpm sync-components
```

Review the diff, then commit.

### Step 2 — Update the Portal repo version mapping

> **This step is required.** The portal host uses a version map to know which toolkit version to load for a given extension. Without this update, extensions referencing the new version will fail to load.

In the **Portal repo**, update the version mapping file (ask the Portal team for the exact location if unsure) to add an entry for the new toolkit version.

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
| `@beamable/portal-toolkit/svelte` | `SvelteHTMLElements` augmentations (triple-slash reference only) |
| `@beamable/portal-toolkit/custom-elements.json` | CEM manifest |
| `@beamable/portal-toolkit/web-types.json` | JetBrains web-types |
