# Plan: Fix the verdaccio public-registry fallback for `@beamable/*` local web dev

## Problem

The portal-extension install paths carry defensive workarounds whose sole job is to *avoid*
relying on verdaccio's npmjs uplink — because that fallback is broken today. This plan removes the
cause of the breakage so the fallback works, then removes the workarounds.

The symptom the workarounds paper over: with the local web-dev stack running, a plain `npm install`
for a **released** extension (one pinning a published `@beamable/portal-toolkit`, e.g. `0.4.0`) can
fail to resolve `@beamable` packages through verdaccio, so the CLI forces the registry explicitly.

## Root cause: two contradictory designs were merged

### The original (correct) model — pre-#4799 shell scripts

Before PR #4799 the local web-dev loop lived in the shell scripts and **incremented the version per
publish**. At `073e5be1e` (the commit before #4799):
- `setup-web.sh`: `echo 0 > web-build-number.txt` (reset the counter) and
  `npm config set @beamable:registry <verdaccio>` (point the global `@beamable` scope at verdaccio;
  verdaccio's npmjs uplink then served every released `@beamable` spec — the fallback).
- `dev-web.sh`: read `web-build-number.txt`, `++`, write back, `VERSION="0.0.123-local$N"`,
  `pnpm publish --tag local`. Each publish a unique `0.0.123-local<N>`.

The verdaccio `proxy: npmjs`, the `config.yml`/README comments, and the Portal's `0.0.123-local<N>`
prefix handling were all written for **this** model and remain correct.

### What #4799 changed (the regression)

PR #4799 rewrote the loop into the `beam web publish` / `beam web use` CLI commands and, in the
process:
- **Dropped the increment** — `WebPublishCommand` publishes a **single fixed `0.0.123`**
  (`WebLocalRegistryService.LocalDevVersion`; `WebPublishCommand.cs:141-144`). `web-build-number.txt`
  is now an orphan nothing reads.
- **Added `unpublish --force` before every publish** (`WebPublishCommand.cs:187-205, 259`), needed
  only because a fixed version can't be republished without removing it first.
- **Removed the global-npmrc mutation** (a genuine improvement — the new `setup-web.sh:10-13` says so;
  the old machine-wide override broke installs when the registry went away) and replaced it with
  **per-install registry flags** (`InstallArgsFor`) to compensate.
- Left the config/README/Portal — all written for the incrementing model — untouched.

**The inconsistency is therefore a regression, not two independently-designed halves.** The CLI now
runs fixed-version + `unpublish --force`; everything around it still expects `0.0.123-local<N>`. The
`unpublish --force` against a now-proxied package churns/shadows verdaccio's local metadata for
`@beamable/portal-toolkit` / `@beamable/sdk` (the tombstone the class doc at
`WebLocalRegistryService.cs:19-27` warns about), so released versions of *those two packages* stop
falling through to npmjs — exactly the packages a released extension needs, hence the workarounds.

### Verified against source

- `agentic-portal/src/lib/utils/extensionSdkRegistry.ts:27,46-48` — `isLocalDev(v)` is a pure
  `v.startsWith('0.0.123')`. `normalizeCacheKey` (`:50-54`) returns the **full** version for
  local-dev builds, so `0.0.123-local.5` and `.6` are distinct cache keys — a republish is a new
  key, no stale cache (strictly better than the fixed version, which needs the CDN `__flush`).
- `agentic-portal/src/lib/utils/extensionStorage.ts:342,348` — `includePrefixes: ['0.0.123']`
  matched via `startsWith`; comment already assumes `0.0.123-local<N>`.
- `WebUseCommand.ResolveVersion` (`:176-196`) already resolves the version from the registry's
  `local` **dist-tag**, not the fixed constant, and its class doc (`:54`) says it is built for "the
  incrementing-version model." So even the #4799 re-pin path was written expecting the incrementing
  version, and works unchanged once publishing resumes it.

**Conclusion:** the Portal, the verdaccio config, and even `web use` still expect the incrementing
model. Only `WebPublishCommand` regressed. The fix is CLI-only; **no `agentic-portal` changes are
required.**

## The fix: restore the incrementing model in the CLI

Restore the pre-#4799 behavior — publish `0.0.123-local<N>` (monotonic N from `web-build-number.txt`),
let verdaccio's uplink serve every non-local `@beamable` spec — but implement it inside
`WebPublishCommand` (not only the script) so `beam local up` gets it too, and add a previous-build GC.
The old `dev-web.sh`/`setup-web.sh` at `073e5be1e` are the reference implementation.

### The precedents already exist: old `dev-web.sh` and `.NET dev.sh`

The versioning is a **restoration**, not an invention. The direct reference is the pre-#4799
`dev-web.sh` at `073e5be1e`:

```sh
NEXT_BUILD_NUMBER=$(cat "$WEB_BUILD_NUMBER_FILE")   # web-build-number.txt, git-ignored
((NEXT_BUILD_NUMBER++))
echo $NEXT_BUILD_NUMBER > "$WEB_BUILD_NUMBER_FILE"
VERSION="0.0.123-local$NEXT_BUILD_NUMBER"           # unique per publish
pnpm publish --registry "$REGISTRY" --tag local
```

**Version format note:** the old script used `0.0.123-local<N>` (no dot, e.g. `0.0.123-local5`); the
`config.yml`/README comments write `0.0.123-local.<N>`. Both are valid npm prereleases and both
`startsWith("0.0.123")`, so the Portal contract holds either way — pick one and use it consistently
(recommend matching the old script's `-local<N>` to minimize surprises, or standardize on `-local.<N>`
and update the docs). The old script did **not** GC previous versions (it accumulated them); the GC
below is a deliberate enhancement (decision 4), taken from the .NET side.

The `.NET dev.sh` is the parallel precedent for the counter + previous-build GC, which this mirrors
(`dev.sh:51-58, 76-85`):

```sh
NEXT_BUILD_NUMBER=`cat build-number.txt`   # local, git-ignored counter
PREVIOUS_BUILD_NUMBER=$NEXT_BUILD_NUMBER
((NEXT_BUILD_NUMBER+=1))
echo $NEXT_BUILD_NUMBER > build-number.txt
VERSION=0.0.123.$NEXT_BUILD_NUMBER          # unique per publish
PREVIOUS_VERSION=0.0.123.$PREVIOUS_BUILD_NUMBER
...
dotnet nuget delete <pkg> $PREVIOUS_VERSION  # GC the PREVIOUS build, never the current
```

So `N` comes from a **git-ignored counter file**, incremented per publish; each build is published
exactly once and the *previous* build is garbage-collected. The web counterpart is already
scaffolded: **`web-build-number.txt` exists at repo root and is git-ignored** (`.gitignore:10`,
alongside `build-number.txt:9`), and `config.yml:16-18` states `beam web publish` is meant to
"garbage-collect the previous local build, mirroring how dev.sh deletes PREVIOUS_VERSION." The CLI
just never wired it up.

Two differences from the .NET side:
- **Version format:** npm rejects 4-part versions, so web uses the prerelease form
  `0.0.123-local.<N>` (not `0.0.123.<N>`). Still `startsWith("0.0.123")`, so the Portal contract
  holds.
- **GC is an unpublish, not a delete:** `npm unpublish @beamable/…@0.0.123-local.<N-1> --force`
  against verdaccio.

### The `0.0.123` prefix is a hard, external constraint

The Portal's `startsWith('0.0.123')` in both files above is the contract. The published version
**must** keep the `0.0.123-` prefix. `LOCAL_DEV.md:570-583` documents the failure otherwise: the
Portal silently falls back to `https://unpkg.com`, the local build doesn't exist there, and the
extension 404s at runtime with no error. So the only allowed shape is `0.0.123-local.<N>`.

## Change points (CLI only)

### 1. `WebPublishCommand` — publish a unique version, GC the previous
- **Compute the version from `web-build-number.txt`**, mirroring `dev.sh:51-58`: read the git-ignored
  root counter (default to `0`/`1` when absent), `PREVIOUS = current`, `NEXT = current + 1`, write
  `NEXT` back, publish `0.0.123-local.<NEXT>` instead of `WebLocalRegistryService.LocalDevVersion`
  (`WebPublishCommand.cs:141-144`). `--version` still overrides for one-off publishes.
- **The increment/GC lives in `WebPublishCommand`, NOT in `dev-web.sh`** — deliberately different
  from how `dev.sh` owns the counter in shell. The web flow has **two** entry points to
  `beam web publish`: the `dev-web.sh` wrapper *and* the `beam local up` build step
  (`LocalStackTemplate.cs:771-787`, `arguments = "web publish"`), the latter being the primary
  `--with-web-registry` path. Owning the counter in the shared command means both paths increment;
  owning it in the script would leave `beam local up` publishing the fixed `0.0.123` and re-open the
  bug. `dev-web.sh` stays the thin wrapper it already is. (The .NET side can put it in `dev.sh`
  because `dev.sh` is its only entry point.)
- **Retarget `UnpublishExisting`, don't delete it** (`:187-205`, call at `:259`). Today it
  unpublishes the version about to be published (the fixed `0.0.123`) — that self-conflict + churn
  on a proxied version is the problem. Change it to unpublish the **PREVIOUS** build
  (`0.0.123-local.<PREVIOUS>`), matching `dev.sh`'s `PREVIOUS_VERSION` delete and the
  `config.yml:16-18` comment. A `0.0.123-local.*` version exists nowhere upstream, so unpublishing an
  old one can never disturb an npmjs version; and the current build is never unpublished, so there is
  no republish-over-self. First publish on a machine has no previous → the unpublish is a no-op
  (already handled: "not found" is treated as success, `:195-199`).
- **`StampManifest`** already stamps both the toolkit version and its `@beamable/sdk` peer dep to
  the published version (`:240`); it just receives the new unique version. No structural change.
- **CDN `__flush`** (`FlushCdnCache`, `:175`) becomes unnecessary for correctness (new version = new
  CDN cache key). Safe to keep as belt-and-suspenders, or drop. Recommend: drop, and update the
  comment at `:173-174`.
- **`EvictPackageCaches`** (`WebLocalRegistryService.cs:668-691`) is the analog of `dev.sh:101`'s
  `rm -rf ~/.nuget/packages/beamable.*/$PREVIOUS_VERSION`. Less critical with unique versions, but
  keep it for the previous build to avoid pnpm integrity staleness.
- Both packages still publish at the **same** version (`:147-152`) — unchanged and still required
  (toolkit's SDK peer dep names it).

### 2. `WebLocalRegistryService` — the constant and the doc
- `LocalDevVersion` (`:66`) is no longer the *published* version; it stays as the **prefix
  sentinel** only. Rename/retarget: keep `LocalDevPrefix = "0.0.123"` for `IsLocalDevVersion`
  matching (`:78-80`, still correct — `0.0.123-local.5`.startsWith("0.0.123") is true), and remove
  or repurpose `LocalDevVersion`.
- **Rewrite the stale class doc** (`:19-33`): it currently states `@beamable/*` is "deliberately not
  proxied" — the opposite of the live config. Replace with the Design B rationale.
- `ForceReinstall` (`:96-169`): **simplify** (decision 2). With unique versions the pin changes every
  publish so a plain install refetches, and the `EINTEGRITY` lockfile-drop retry (`:124-168`) is dead
  code (a version's tarball bytes never change, so its lockfile hash can't go stale). Reduce to a
  plain `npm install <pkg>@<version> --registry <verdaccio> --save-exact --no-audit --no-fund`; the
  delete-node_modules step (`:100-112`) is now optional.

### 3. Remove the defensive registry workarounds
The `InstallArgsFor` branch that forces the public registry exists **only** because the fallback was
broken. Once verdaccio proxies correctly, drop it.
- `WebLocalRegistryService.InstallArgsFor` (`:275-286`): the **released-pin** branch returns
  `--@beamable:registry={PublicRegistry}` (`:281`). Change to return empty (inherit defaults). The
  **local-dev** branch (`--registry {verdaccio}`, `:285`) **stays** — that is fundamental, not a
  workaround: `0.0.123-local.<N>` exists only on verdaccio.
- Call sites that then simplify: `PortalExtensionAddLibraryCommand.cs:102`,
  `PortalExtensionDiscoveryService.cs:225`, `PortalExtensionUpdateToolkitCommand.cs:301`.
- `PortalExtensionUpdateToolkitCommand.RunInstallsConcurrently` (`:281-333`): the normal-path
  `--registry {registry}` (`:303`) is entangled — it serves both (a) routing `--local` installs to
  verdaccio and (b) forcing non-local installs off a verdaccio default (**the workaround**). Per
  decision 3, **drop (b)**: stop forcing the registry on the non-local path so a released update
  inherits the default and verdaccio's uplink resolves it. Keep `ResolveTargetVersion`'s registry
  *resolution* for `--local`/`--version` routing (a) — i.e. only pass `--registry` when it's the
  verdaccio/local case. Resolve the entanglement so the two concerns are no longer one shared value.

### 4. Cosmetic / display touch-ups (verify, mostly no-op)
- `WebStatusCommand.cs:79,91` — `Where(IsLocalDevVersion)` still works; `timeKey = localTag ??
  LocalDevVersion` (`:91`) should surface the actual published version, not the sentinel.
- `LocalStackUpCommand.cs:168,179`, `App.cs:1308-1309` — all `StartsWith("0.0.123")` /
  `IsLocalDevVersion`, correct with suffixes; message text may reference the old fixed version.

## Decisions (all resolved)

1. **Version source** — git-ignored `web-build-number.txt`, incremented per publish, GC the previous
   build. Mirrors `dev.sh` + `build-number.txt` exactly (see "The precedent" above). The file already
   exists and is already git-ignored.
2. **Simplify `ForceReinstall`** — YES. With unique versions the `EINTEGRITY` failure class is gone
   (a version's tarball bytes never change, so a lockfile hash can't go stale), so the lockfile-drop
   retry (`WebLocalRegistryService.cs:124-168`) is dead code. Reduce `ForceReinstall` to a plain
   `npm install <pkg>@<version> --registry <verdaccio> --save-exact …`; the delete-node_modules step
   also becomes optional (the changed pin already forces a refetch) — keep it only as a clean-state
   guard or drop it.
3. **Defensive registry forcing** — REMOVE it; let verdaccio handle all fallbacks. Drop both the
   released-pin branch of `InstallArgsFor` (returns empty) and the public-registry forcing on
   `update-toolkit`'s non-local path. Keep only the verdaccio routing for local-dev/`--local` pins
   (fundamental — those versions exist nowhere else).
4. **Previous-build GC unpublish** — KEEP. Matches `dev.sh`'s `PREVIOUS_VERSION` delete and keeps the
   packument small. Not required for correctness, retained for parity and hygiene.

## Migration / compatibility

- **Existing extensions pinned to bare `0.0.123`.** After this change `beam web use` re-pins them to
  `0.0.123-local.<N>` (it already resolves the `local` dist-tag). A stale bare `0.0.123` pin still
  matches `isLocalDev` in the Portal, but that exact version may no longer be published — so an
  un-refreshed extension 404s until `beam web use` runs. Document in `LOCAL_DEV.md`; `beam local up`
  already runs `web use` in its forced set.
- **Dirty verdaccio storage from the #4799 fixed-version regression.** A storage volume holding
  tombstoned `@beamable` metadata (from repeated `unpublish --force` on `0.0.123`) must be wiped once:
  `beam web reset` / `docker compose down -v`. Call this out as a one-time step in the PR/notes.
- **Docs/scripts to update:** `WebLocalRegistryService` class doc, `portal-localdev/README.md:54-59`,
  `web/LOCAL_DEV.md` "How it works: version `0.0.123`" section, and the `config.yml` comment (already
  written for the incrementing model — verify it matches the final version format). Also
  **`dev-web.sh:12-22,83-84,93`** and
  **`setup-web.sh:10-13`**, which currently describe the fixed-`0.0.123`, one-time-pin, CDN-flush
  model and must be rewritten for the per-publish incrementing version (the pin now changes each run,
  so the "ONE-TIME edit" note is wrong).

## Out of scope

- **`agentic-portal` (the Portal repo).** Verified it already handles `0.0.123-local.<N>` by prefix;
  no changes needed. The `0.0.123` prefix is treated here as a fixed external contract.
- The `.NET` `0.0.123.<N>` versioning (`dev.sh`, `VersionInstallCommand`) — unrelated, unchanged.

## Testing

- Unit: `IsLocalDevVersion("0.0.123-local.5")` true; `InstallArgsFor` returns no public-registry
  flag for a released pin and the verdaccio `--registry` for a local-dev pin; the version-`N`
  resolver picks `max+1`.
- Integration (manual, `agentic-portal` sibling as the test bed): with the stack up,
  (a) `beam web publish` twice → two distinct `0.0.123-local.<N>` versions coexist on verdaccio, no
  unpublish; (b) a **released** extension `npm install`s through verdaccio and resolves the
  published `@beamable/portal-toolkit` from the npmjs uplink (the fallback that is broken today);
  (c) a **local-dev** extension loads the latest build in the Portal with no `__flush`.
