# Plan: real build + diff for `beam bundles plan` / `publish` (#2)

## Context

`beam bundles plan` and `publish` should mirror `beam deploy plan` / `release` — the design doc
says bundle `plan`/`publish` reuse the `DeploymentService` artifact pipeline, differing only in
target (the bundle catalog vs the realm). Today:

- **`bundles plan`** is a lightweight, build-free preview: it resolves the authored
  `*.beam.bundle.json`, checks each listed component exists locally, and lists them. It does **not**
  build or diff against what's published. (Name over-promises; it's really "validate + list".)
- **`bundles publish`** builds via `InteractivePlan`, filters the built components to the bundle,
  uploads changed service images, converts Basic→canonical, and POSTs `PublishBundleRequest`. It
  does not compute or print a diff, and doesn't tell you whether the publish is a no-op.

Goal: make `plan` produce a real added/removed/changed diff against the published bundle, and make
`publish` print that same plan (like `release` does) before pushing.

## The checksum / diff model (settled)

Bundles are **content-addressed**. Two checksum levels (verified against the backend impl):

1. **Component checksum** — each `ServiceReference` / `ServiceStorageReference` carries a `.checksum`
   = hash of `serviceName;imageId;enabled;templateId` (services) / `id;storageType;enabled;templateId`
   (storages). Computed by the shared `BeamoExtensions.ComputeChecksum` / `ResetChecksum` (MD5). The
   client and server use the **same** formula, so component checksums are directly comparable.
2. **Bundle checksum** — server's `Bundle.ComputeChecksum` = `"sha256:" +` SHA-256 over the sorted,
   concatenated component checksums.

**Consequence:** a modified local bundle differs from the unmodified catalog one transitively —
change a service → new `imageId` → different component checksum → different bundle checksum. But the
component checksum depends on `imageId` (the built image digest), so **the checksum is not derivable
from the authored config alone — you must build first.** That's why a freshly-`new`'d bundle shows
`"checksum": ""` and why `plan` is inherently build-first.

**Diff strategy — component-level (source of truth), bundle-checksum (summary only):**

- Diff by component name/id after building:
  - in local, not in published → **added**
  - in published, not in local → **removed**
  - in both, different component `.checksum` → **changed**
- Optionally roll up a local bundle checksum and compare to the published one for a one-line
  "identical (no-op) / differs" headline.
- **Do the component diff as the source of truth; treat the bundle-level SHA-256 as a summary
  indicator only.** Reason: the component checksum reuses shared `beamable.common` code (correct by
  construction), whereas re-implementing the server's exact bundle-hash (sort order + concatenation
  format) client-side is fragile — if it ever drifts it yields false "changed" results. So we do NOT
  gate correctness on the client-side bundle hash.

The build pipeline already computes component checksums for us: `DeployUtil.Plan`'s
`EnsureEntriesHaveChecksums` transform calls `ResetChecksum` on every built service/storage, so
`plan.manifest.manifest[*].checksum` is populated after the build.

## What to diff against

The published bundle to compare with = the bundle's **`@latest`** in the catalog
(`IBeamBeamobundleApi.GetBundles(name, ns)` returns the latest entry). If the bundle was never
published, the published side is empty → everything shows as **added**. (Diffing against a
manifest-pinned checksum is a possible future option; `@latest` is the natural "what would change if
I publish now" baseline.)

## Implementation

### 1. Shared diff — `cli/Services/Bundles/BundleDiff.cs` (new)
A small pure function + result types (CLI-assembly, so they satisfy the output-contract test):

```
BundleComponentChange { string name; string kind /* service|storage */; ChangeType change /* Added|Removed|Changed */;
                        string localChecksum; string publishedChecksum; }
BundleDiffResult { List<BundleComponentChange> changes; bool isNoOp; /* no changes */
                   string publishedChecksum; /* @latest, or "" if never published */ }
```
`BundleDiff.Compute(localServices, localStorages, publishedBundle)` — match services by
`serviceName`, storages by `id`; classify Added/Removed/Changed by comparing `.checksum`. `isNoOp`
= no changes. (`localServices`/`localStorages` are the built, checksum-stamped refs from the plan,
filtered to the bundle's components.)

### 2. `bundles plan` — rewrite to build + diff (no upload, no POST)
Mirror `PublishBundleCommand`'s build path (it already does the hard part):
- Make `BundlePlanCommandArgs : IHasDeployPlanArgs` + solution flag (like publish), so it can call
  `this.InteractivePlan(provider, args)` to build.
- `BundleWorkspace.Require` the authored bundle; filter the built services/storages to its components
  (reuse the exact filtering logic in `PublishBundleCommand.Handle`).
- Fetch `@latest` via `GetBundles(name, ns)` (tolerate 404 / never-published → empty published side).
- `BundleDiff.Compute(...)`; return a `BundlePlanCommandOutput` with the bundle name, the diff
  (added/removed/changed), and the no-op/ differs headline. Keep the PE-in-bundle guard (throw
  "not yet supported") consistent with publish.
- **Factor the build+filter+PE-guard block out of `PublishBundleCommand` into a shared helper** (e.g.
  `BundleBuildUtil.BuildBundleComponents(this AppCommand, provider, args, bundle) → (services, storages)`)
  so `plan` and `publish` share one implementation and can't drift.

### 3. `bundles publish` — print the plan, then publish
- Reuse the shared build+filter helper and `BundleDiff.Compute` to produce the same diff.
- Print it (added/removed/changed) the way `release` prints `PrintPlanInfo`, before uploading.
- If `isNoOp` (identical to `@latest`), say so and skip the upload/POST (publishing identical content
  is already a server-side no-op — `isNew=false` — but short-circuiting avoids needless image
  uploads). Keep a `--force`/confirm path if we want to allow re-publish anyway.
- Otherwise proceed as today: upload changed images → `PostBundlesPublish` → (existing `--scope`
  widen). Include the diff in the command output alongside `checksum`/`isNew`.

### Reuse map
- Build: `this.InteractivePlan` + `DeployUtil.Plan`/`CreateReleaseManifestFromLocal` (already used by publish).
- Component checksums: already stamped by `EnsureEntriesHaveChecksums` during the plan build.
- Basic→canonical: `ConvertToBundleReference` (for the publish request; diff works on Basic refs' `.checksum`).
- Catalog read: `IBeamBeamobundleApi.GetBundles(name, ns)` → `GetBundleResponse.bundle` (canonical refs w/ `.checksum`).
- Image upload: `ServiceUploadUtil.Upload` (already used by publish).

## Edge cases
- **Never published** → `GetBundles` 404: treat published side as empty → all components "added"; publish proceeds.
- **No-op** → local bundle checksum-equivalent to `@latest`: `plan` reports "no changes"; `publish` skips.
- **Portal extensions in a bundle** → still unsupported this pass (PE asset upload is coupled to realm
  deploy); keep the clear `CliException`. (Tracked as a follow-up.)
- **Client bundle-hash drift** → don't gate on it; component diff is authoritative.

## Verification
1. Build clean; `NamingPass` + `OutputTypesAreDeclaredInTheCLiAssembly` pass (new output/diff types are CLI-assembly).
2. Author a bundle, build a component, `bundles plan` against a never-published name → all "added".
3. `bundles publish`, then `bundles plan` again with no local change → "no changes / no-op".
4. Modify a component (force a new imageId), `bundles plan` → that component shows "changed" with
   differing local/published checksums; `publish` prints the diff then pushes a new checksum.
5. Against a real scratch realm/catalog (checksum equality is only fully meaningful end-to-end).

## Out of scope (this pass)
- Portal-extension components inside a bundle (asset upload path).
- Client-side re-implementation of the server's exact bundle-level SHA-256 as a correctness gate.
- Diffing against a manifest-pinned checksum instead of `@latest`.
