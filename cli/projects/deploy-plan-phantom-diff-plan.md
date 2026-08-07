# Plan: Fix phantom diffs in `beam deploy plan` (duplicate/archived refs + bundle components)

Status: **implemented and verified** (CLI-only). Both the Additive duplicate/archived phantom and the
Replace-mode bundle-component removal are fixed. End-to-end confirmed in ProjectTest: `beam deploy
plan` now reports only `Pinning 5 bundles` (5 pending changes) — no phantom portal-extension diffs.
Unit tests green (8/8).

### Follow-up (Replace-mode) — Option A: pinned-bundle components excluded from the inline diff

First implementation fixed the duplicate/archived phantom "Updating" (Additive mode). A second report
in **Replace** mode surfaced a different issue: `MergeReplacement` archives every remote extension not
present locally, so components of `@beamable/defaults` (`sample/nav-summary-demo/segmentation-*/Tuna`)
— a referenced bundle with **no local `*.beam.bundle.json`** — got swept up as "Removing 10", while
`Pinning 5 bundles` was also shown.

Structural facts (confirmed): `BeamoV2Manifest` keeps bundle contents in a separate `bundles[]`
reverse-index (bundle → component ids), distinct from the inline `*References[]`; the server assembles
pinned bundles at deploy time and does **not** fold their components back into the inline arrays. The
CLI can enumerate a pinned bundle's components on demand via `IBeamBeamobundleApi.GetBundlesChecksums`
→ `BundleInfo`, but `Plan` didn't use it — `bundleComponentIds` came only from local bundle files.

**Decision (user): Option A** — a component delivered by *any* referenced bundle (authored locally OR
remotely pinned) is excluded from the inline diff entirely; only the bundle pin represents it, in both
Additive and Replace.

Changes (superseding the earlier disable-in-place approach):
- New `ResolvePinnedBundleComponentIds(provider, pins, remoteV2)` — resolves each pinned bundle's
  component ids from the realm's assembled `bundles[]` (no network call) or, on a miss, the catalog
  (`GetBundlesChecksums`). Best-effort: a per-bundle failure logs and falls back (components reappear
  inline rather than breaking the plan).
- In `Plan`, after the merge: union those ids into `bundleComponentIds`, then **strip them symmetrically
  from BOTH `remote` and `next`** (services, storages, portal-extension refs). Symmetric removal means
  they neither show as inline changes nor get posted as inline entries — the server assembles them from
  the references. The old `DisableOrDropBundleComponents` helper was removed.

Expected effect on the Replace-mode repro: "Removing 10 portal extensions" disappears; the plan shows
only "Pinning 5 bundles". (Requires the catalog to resolve `@beamable/defaults` etc.; if a lookup
fails, those components fall back to appearing inline and a warning is logged.)

### Implementation summary (first pass — dedup + diff extraction)

`cli/cli/Services/DeploymentService.cs`:
- New `CollapseByName<T>(items, nameOf, isArchived)` — one canonical (live-preferred) entry per name,
  first-appearance order.
- Dedup remote services/storages right after `remote = await remoteTask;` (before the merge/diff, to
  keep the positional array diff aligned).
- Dedup remote portal-extension refs before building `remotePortalRefs`/`remotePortalExtensionRefs`.
- New `DisableOrDropBundleComponents<T>` — bundle-component services/storages that still have a live
  inline entry in remote are rewritten from the remote copy with `enabled = false` (disable-in-place,
  positionally safe); ones with no remote inline entry are dropped. Portal-extension bundle components
  are archived+disabled in `nextPortalExtensionRefs`.
- Extracted the portal-extension + bundle-reference diff into a public `DiffReferences(...)` so it's
  unit-testable (behavior unchanged; braces added to satisfy the project brace rule).

`cli/tests/DeployPlanDiffTests/DeployPlanDiffTest.cs` — regression coverage: collapse dedups/prefers
live (PE + service refs); unchanged extension vs collapsed remote reports nothing; the pre-collapse
phantom is documented; a bundle component disables once then converges; bundle pins still diffed by
checksum.

Expected effect on the ProjectTest repro: the "Updating 13 portal extensions" list disappears
entirely (those are unchanged @beamable/defaults components); "Removing 4" (Fish, uses-hub-test2,
hub-test, CoolDashboard) shows once as the disable of their still-live inline copies, then converges
to no changes after release.


## Symptom

Running `beam deploy plan` in `ProjectTest` reports changes that were already deployed:

```
Updating 13 portal extensions
 - sample, segmentation-builder, segmentation-overview, segmentation-detail,
   segmentation-hub, nav-summary-demo          (each listed TWICE)
 - Tuna
Removing 4 portal extensions
 - Fish, uses-hub-test2, hub-test, CoolDashboard
```

Two complaints:
1. Components that live in bundles show as individual inline diffs (should show only as a bundle-reference change — and here there is *no* bundle-ref change, so they should show nothing).
2. Changes that were already deployed keep re-appearing → the diff isn't comparing against the correct current state.

All change domains are affected in principle (services / storages / portal extensions), so this is general, not portal-extension-specific.

## Evidence (from the saved plan file)

`plan-1786055342565.plan.json` (`mode: Additive`, `scope: Realm`) — the merged
`portalExtensionReferences` (`= nextPortalExtensionRefs`) has **135 entries with heavy duplication
per name**:

```
sample x4, nav-summary-demo x4, segmentation-* x4, Tuna x3,
most players-*/analytics-*/navigator-*/agentnews-* x2, ...
```

Inspecting the duplicate entries for one name:

```
sample: (ea19…, archived:true)  (ea19…, archived:true)   <- two identical archived copies
        (c1af…, archived:true)                            <- older archived copy
        (f54b…, archived:false, enabled:true)             <- the live copy
```

There is **no owner/bundle field** on these entries — they are plain
`{name, checksum, enabled, archived, files}`. So the realm's *current* manifest
(`GET /api/beamo/manifests/current`) is carrying **archived history + the live entry, multiple
copies per name**.

Local extension projects on disk: `cars, CoolDashboard, ferrari, Fish, hub-test, tuna, uses-hub-test,
uses-hub-test2, zone-hub`. Note there is **no** local project for `sample`, `segmentation-*`,
`nav-summary-demo`, or `Tuna` (capital-T) — those come purely from the referenced
`@beamable/defaults` bundle, flattened into the remote manifest.

## Root cause

Diff code is in `cli/Services/DeploymentService.cs`, `DeployUtil.Plan(...)`.

### 1. Portal-extension "Updating" phantoms — duplicate-remote + many-to-one match

- `remotePortalExtensionRefs` is built verbatim from `beamoV2Manifest.portalExtensionReferences`
  (line ~1438-1440) — **no dedup, no archived-collapse**.
- `MergeAdditive(remote, local)` (line 836-857) seeds `final = remote.Select(Copy)` — **it copies
  every duplicate** — then overwrites by *first* name match with the local entry. For names with no
  local project (`sample`, etc.), all remote duplicates survive into `nextPortalExtensionRefs`.
- The diff loop (line 1624-1639) iterates each `nextRef` and compares it to
  `remotePortalExtensionRefs.FirstOrDefault(r => r.name == nextRef.name)` — **always the first
  (archived) copy**:

  ```csharp
  foreach (var nextRef in nextPortalExtensionRefs) {
      var remoteRef = remotePortalExtensionRefs.FirstOrDefault(r => r.name == nextRef.name);
      ...
      else if (nextRef.checksum != remoteRef.checksum) diff.changedPortalExtensions.Add(nextRef.name);
  }
  ```

  Walk `sample` (remoteRef = first copy `ea19`, archived): the `c1af` copy and the live `f54b` copy
  each differ from `ea19` → **`sample` added to `changedPortalExtensions` twice**. `next` is literally
  identical to `remote`, yet it reports "changed." This exactly reproduces the doubled list.

### 2. Bundle components "Removing" phantoms

`Fish, uses-hub-test2, hub-test, CoolDashboard` are local bundle components
(`bundleComponentIds`, from `*.beam.bundle.json`). They are correctly stripped from
`nextPortalExtensionRefs` (line 1608-1616), **but they still exist as live inline entries in the
remote manifest**. The removal loop (line 1643-1648) skips *archived* remote refs but **not** bundle
components:

```csharp
foreach (var remoteRef in remotePortalExtensionRefs) {
    if (remoteRef.archived) continue;
    if (nextPortalExtensionRefs.Any(r => r.name == remoteRef.name)) continue;
    diff.removedPortalExtensions.Add(remoteRef.name);   // fires every plan
}
```

So every plan reports "Removing" them, even though the owning bundle pin is unchanged
(`changedBundleReferences` is empty). Expected: bundle components surface only through their bundle
reference, never as inline add/change/remove.

### 3. The CLI amplifies the duplication on release

`deploy release` posts `plan.portalExtensionReferences` — i.e. `nextPortalExtensionRefs`, dupes and
all — straight back to the server (line 1929-1932). The server persists them, so the next plan reads
even more duplicates. It's a feedback loop the CLI is feeding.

### 4. Same defect latently affects services & storages

`MergeAdditive(ManifestView, ManifestView)` also seeds `final = remote.Copy()` (line 714), and
`FindChanges` (line 534-709) is a **positional JSON array diff** (`DiffStream.FindChanges`). Duplicate
`serviceReferences`/`storageReferences` in the remote manifest would produce phantom
imageId/added/removed changes the same way. `ProjectTest` currently has no microservices in that
state, so it's latent — but the fix should cover it.

## Fix (proposed)

Guiding principle: **the "current remote state" is exactly one canonical entry per name — the live
(non-archived) one if present; archived history is not current state.** Collapse the remote manifest
to that before merging/diffing, and never let bundle components diff as inline components.

1. **Collapse remote portal-extension refs by name.** When building `remotePortalRefs` /
   `remotePortalExtensionRefs`, group by name and keep a single canonical entry — prefer
   `archived == false`; if all archived, keep one (representing "not currently present"). This fixes
   the doubled "Updating" list and stops feeding dupes into `next` (and thus into release).

2. **Exclude bundle components from the removal loop.** In the loop at 1643-1648, `continue` when
   `bundleComponentIds.Contains(remoteRef.name)`. A component now delivered by a bundle should not be
   reported as an inline removal. (Confirm desired behavior with the team — see open questions.)

3. **De-dup on release too (defense in depth).** Before posting, collapse
   `plan.portalExtensionReferences` to one entry per name so the CLI stops persisting duplicates back
   to the realm, breaking the amplification loop.

4. **Generalize to services/storages.** Collapse remote `serviceReferences` / `storageReferences` by
   name (keep the live entry) inside `CreateReleaseManifestViewFromRealmV2` / `ConvertToManifestView`
   so `FindChanges` compares clean single-entry arrays. Guards the same phantom-diff class for
   microservices.

5. **Tests** (`tests/`, deployment plan fixtures): a remote manifest with duplicate archived + live
   entries for a name whose local build matches the live checksum → **no** change reported; a bundle
   component present live in remote but excluded from `next` → **no** inline removal; dedup preserved
   through a plan→release round-trip.

## Finalized design (decisions locked)

Decisions from review:
- **CLI-only.** No server change in this pass; verification happens in a fresh clean realm.
- **Removal semantics = disable-and-converge.** A bundle component that still has a *live* (enabled,
  non-archived) inline entry in the realm is **disabled** by the plan; that disable is the diff shown.
  Once the realm reports it disabled, the condition no longer matches and it disappears from the diff.
  A bundle component with no remote inline entry produces no diff.
- **Applies to all three domains** (services, storages, portal extensions).

### Critical constraint discovered: `FindChanges` diffs arrays positionally

`DiffStream.SerializeArray` keys every element by its **index** (`manifest[i].field`), so
`FindChanges(remote, next)` for services/storages is a **positional** diff. It only works because
`MergeAdditive` seeds `next` from `remote` (same order) and appends local-only entries at the end.
Therefore:
- Remote must be deduped **before** `MergeAdditive` (so `next` inherits the collapsed order and stays
  index-aligned with the collapsed `remote`).
- The bundle-component step must **not** remove any entry at an index `< remote.Length` (would shift
  and misalign everything after it). In-remote bundle components are disabled **in place**; only
  local-only (appended, index `≥ remote.Length`) bundle components are dropped — safe, since remote
  has nothing at those indices so they diff as "added" either way.

### Concrete changes (all in `cli/Services/DeploymentService.cs`)

1. **`CollapseByName<T>(items, nameOf, isArchived)`** helper — one canonical entry per name, preferring
   the non-archived (live) one, preserving first-appearance order.
2. **Dedup remote portal refs** (line ~1438): collapse `beamoV2Manifest.portalExtensionReferences`
   before building `remotePortalRefs` / `remotePortalExtensionRefs`. Kills the doubled "Updating".
3. **Dedup remote services/storages** (right after `remote = await remoteTask;`, ~line 1418): collapse
   `remote.manifest` and `remote.storageReference.Value` before merge/diff.
4. **Disable-or-drop bundle components** (replace the strip block, lines ~1606-1616):
   - Services/storages: `DisableOrDropBundleComponents<T>` — for each bundle-component entry in `next`,
     if remote has it, replace with `remoteEntry.Copy()` and set `enabled = false` (only diff = the
     disable → `disabledServices`/`disabledStorages`, no imageId churn, no upload); if remote lacks it,
     drop it.
   - Portal extensions: for each bundle-component entry in `nextPortalExtensionRefs` (these come from
     the remote copy, since local bundle components aren't built inline), set `archived = true`,
     `enabled = false`. The PE diff loop keys "removed" off `archived`, so this surfaces as
     **"Removing portal extension"** once and converges when the realm reports it archived. (PE has no
     separate "enabled" diff branch, so `archived` is the only lever — see open question on label.)
5. **Release de-dup (defense in depth):** `plan.portalExtensionReferences` is already single-per-name
   after the above, so release stops posting duplicates back to the realm, breaking the amplification
   loop. No extra code needed beyond 2-4; add a test asserting the round-trip stays single-per-name.

Clone uses the existing generic `JsonSerializable.Copy<T>()` (deep copy; no shared refs into `remote`,
which `FindChanges` reads as `old`).

## Open questions (please confirm before I implement)

0. **PE disable label.** For portal extensions the only diff lever is `archived`, which prints
   **"Removing portal extension"** (not "Disabling"). Services/storages print "Disabling". OK to keep
   the existing "Removing" wording for PE, or should I add a distinct "Disabling portal extension"
   line? (Cosmetic; no behavioral impact.)


1. **Is the duplicate/archived accumulation also a server bug?** The CLI can be made robust
   regardless, but if the server is meant to keep only one entry per name, the archived-history
   accumulation should likely also be fixed server-side (and possibly a one-time cleanup of existing
   realm manifests). CLI fix stops the CLI from *adding* to it; it won't retroactively clean the realm.

2. **Removal semantics for a component that moved into a bundle.** When an extension that *was*
   inline becomes a bundle component, the realm's inline entry genuinely needs removing **once**. Do
   we (a) suppress the inline "removed" entirely and rely on the server to reconcile when the bundle
   is applied, or (b) keep showing it until the realm actually drops the inline entry? Option (a)
   matches the user's expectation ("show only as bundle-reference change") but assumes the server
   reconciles.

3. **Scope of this change.** Fix portal extensions only (the acute case), or land the
   services/storages collapse in the same pass (recommended, since it's the same root cause)?
