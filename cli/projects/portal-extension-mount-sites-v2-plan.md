# Plan: Mount sites as resolved URLs, sourced from built extension metadata

This supersedes the approach in [`portal-extension-mount-sites-plan.md`](./portal-extension-mount-sites-plan.md).
That first pass keyed an extension's slots by the **extension name** with a special
`type: "extension"`, and discovered the slots by **regex-scanning the extension source every time**
`list-mount-sites` / `list-extension-options` / `new portal-extension` ran. Both decisions change:
slots become ordinary **`component`** selectors on the site at the extension's **URL**, made
collision-free by **unique selector names** (no owner field); and they are **computed once at build,
stored in metadata**, then read back from running extensions.

## What's wrong with the current behavior

Running `beam portal extension list-mount-sites` in `ProjectTest` currently yields sites whose
`path` is the **extension name** (`"Temaki"`, `"Kare"`) with `type: "extension"`. But a mount site's
`path` must be a **URL match** the Portal can route, exactly like the Portal-defined sites:

```jsonc
// full page
{ "path": "/:customerId/games/:gameId/realms/:realmId/!pathMatch",
  "selectors": [{ "selector": "#extension-page", "type": "page" }] }

// component slots on the players page
{ "path": "/:customerId/games/:gameId/realms/:realmId/players",
  "selectors": [{ "selector": "#top", "type": "component" }, { "selector": "#bottom", "type": "component" }],
  "navContext": ["Engage", "Players"] }
```

So the slots exposed by an extension must be emitted as **component** selectors on the site whose
`path` is **the URL where that extension renders**. Multiple extensions that render at the same URL
contribute their selectors to the **same** site. Example: page extension **A** lives at `/ARoute`
and declares `<BeamExtensionSite selector="top">` and `<BeamExtensionSite selector="bottom">`;
extension **B** is mounted inside A and declares `<BeamExtensionSite selector="B-top">`. Because B
renders inside A, B's slot lives at `/ARoute` too, so everything collapses into one site:

```jsonc
{ "path": "/:customerId/games/:gameId/realms/:realmId/ARoute",
  "selectors": [
    { "selector": "#top",    "type": "component" },   // from A
    { "selector": "#bottom", "type": "component" },   // from A
    { "selector": "#B-top",  "type": "component" }    // from B (rendered inside A)
  ],
  "navContext": ["AGroup", "ALabel"] }                // from A, the page owner
```

The second problem: the slot list is obtained by a **filesystem regex scan of the extension source**
on every invocation. It should instead be **computed once at build time, written into the
extension's metadata**, and **read back from currently-running extensions** when the CLI needs it.

## Core model

### The path is "where the host is mounted"

Every extension already records where it lives in its own `package.json` `beamable.mount`:
- A **page** extension stores its custom route in `mount.page` (e.g. `ARoute`) — the value that
  replaced the `!pathMatch` splat at creation.
- A **component** extension stores the host page path in `mount.page` (e.g. `players`).
- A **nested** child stores its parent's resolved location in `mount.page` (see "Nesting" below).

In all three cases the slot(s) extension A exposes live at the **same URL A occupies**:

```
generatedPath(A) = normalize(A.Properties.Mount.Page)
```

`normalize` = trim + strip leading/trailing `/` (the existing
`BeamoLocalSystem.NormalizePortalExtensionMountPage`). This is the **realm-relative** form, matching
the rest of the in-memory `mountSites` list — `FetchRemotePortalConfig` strips the
`/:customerId/games/:gameId/realms/:realmId/` prefix off remote sites, so generated sites stay
relative for consistency. (`ListMountSitesCommand` already returns this stripped form; the realm
prefix is re-applied Portal-side.) See **Open question 2** on whether to surface the full path.

### Selectors are `component`, and globally unique by name — no owner field

Each `BeamExtensionSite` an extension declares becomes a `{ selector: "#<name>", type: "component" }`
entry on the site at that extension's URL. The special `type: "extension"` is removed; from a child's
point of view, mounting into one of these slots is the same *kind* of operation as mounting into any
Portal component slot.

There is **no `portalExtension` / `hostExtension` owner field**. The earlier draft added one to
disambiguate A's `#top` from the page's own `#top` at the same path. Instead we make the **selector
name itself unique**: an extension namespaces its slot (e.g. B declares `selector="B-top"` →
`#B-top`), so `path + selector` is already globally unique and resolves to exactly one DOM container.
A child that wants B's slot just stores `mount.page = <URL>`, `mount.selector = "#B-top"` — nothing
else. Uniqueness is the extension author's responsibility (the natural convention is to prefix with
the extension name); the CLI/Portal simply collect every declared selector onto the site for that URL
and de-duplicate identical strings.

This keeps `MountSiteConfig` and `PortalExtensionMountProperties` unchanged in shape, and lets
`MergeMountSites` stay a plain merge-by-`path` (union selectors, de-dupe by string) — the original
behavior.

### navContext

A site's `navContext` comes from the **page owner** — the full-page extension (or Portal page) that
defines the route. `navContext = [owner.Mount.NavGroup, owner.Mount.NavLabel]` (skipping unset).
Child extensions mounted into a slot carry no nav fields (those are page-only), so on merge we keep
the first **non-empty** navContext for a path.

### Nesting falls out for free

`generatedPath(X) = normalize(X.Mount.Page)` — the URL where X renders — for every extension, page or
child. B mounted in A (at `/ARoute`) has `mount.page = "ARoute"`, so B's `#B-top` slot lands on the
`ARoute` site alongside A's slots. A grandchild C mounting into B's `#B-top` stores
`mount.page = "ARoute"`, `mount.selector = "#B-top"` and contributes its own (uniquely-named) slots to
`ARoute` as well. The URL stays constant down the chain; unique selector names keep every slot
distinct, with no special-casing and no owner tracking.

## Where the slot list comes from

### 1. Build time — embed the slots into metadata

`PortalExtensionObserver.CreateMetaDataFile` (in
`cli/cli/Services/PortalExtension/PortalExtensionDiscoveryService.cs`) writes
`assets/metadata.json` right after `npm run beam-build`. Extend it to also record the slots this
extension exposes:

- Add `public List<string> ExtensionSites;` to `ExtensionBuildMetaData` — the **selector names this
  extension declares** (e.g. `top`, `bottom`, `B-top`), in source order, de-duplicated. The
  extension's existing `mount.page` (already in `Properties.Mount`) supplies the "path it mounts
  into", so together they say *which selectors live at which path*.
- Populate it in `CreateMetaDataFile` by reusing the existing scanner —
  `RemotePortalConfigService.ScanExtensionSiteSelectors(ExtensionMetaData.AbsolutePath)`. The scan now
  happens **exactly once, at build**, and its result is persisted. (The regex/scan helpers stay in
  `RemotePortalConfigService`; only their *call site* moves. Alternative: move the scan into the
  toolkit's `beam-build` vite step so metadata is authored entirely JS-side — see **Open question 1**.)
- **The scanner must no longer restrict to `top`/`bottom`.** Selector names are now arbitrary
  author-chosen strings (`B-top`, …), so `ParseExtensionSiteSelectors` / `ScanExtensionSiteSelectors`
  return *every* declared selector value (drop `SupportedExtensionSiteSelectors`).

`metadata.json` is already the artifact that gets uploaded on deploy
(`DeploymentService` asset list `["index.js", "style.css", "metadata.json"]`) and served to the
running service, so the slot list rides along to both local and remote consumers.

### 2. Read time — gather metadata from running extensions

The CLI must "check all currently running portal extensions, get their metadata, and generate the
mount-site JSON from that" rather than scanning source. `beam project ps`
(`CheckStatusCommand.CheckStatus`) already enumerates running extensions both **locally** (discovery
host events) and **remotely** (V2 manifest `portalExtensionReferences`).

Introduce a small service method (new `IRemotePortalConfigService` member or a helper on a new
`IPortalExtensionMetadataService`) that:

1. Calls `CheckStatusCommand.CheckStatus(args, mode: DiscoveryMode.ALL)` and collects the
   `ServiceStatus` entries whose `serviceType == "portalExtension"` and that are
   `knownToBeRunning`.
2. Groups instances by extension **name** and, for each, loads its `ExtensionBuildMetaData`. **Remote
   is the source of truth**: if an extension is running both remotely and locally, use the remote
   metadata; only fall back to local when there is no remote instance.
   - **Remote** (preferred): from the V2 manifest `portalExtensionReferences`, take the extension's
     `files[]` entry whose `name == "metadata.json"` → its `contentId` + `version` (the checksum).
     **Check the cache first** (below); on a miss, fetch via the private content system:
     `IContentApi.PostBinaryPrivateUrls(new GetBinaryDownloadUrlsRequest { requests = [ new
     GetContentRequest { contentId, version } ], expirationSeconds = … })` →
     `GetBinaryDownloadUrlsResponse.urls[].url` (a signed download URL) → HTTP `GET` the URL →
     deserialize the body as `ExtensionBuildMetaData`, then **write it to the cache**. (`IContentApi`
     is the same service `DeploymentService` uses for `PostBinary`; resolve via
     `args.Provider.GetService<IContentApi>()`.) Batch all cache-miss `metadata.json` requests into
     **one** `PostBinaryPrivateUrls` call.
   - **Local** (fallback): read `<AbsolutePath>/assets/metadata.json` for the matching local
     `PortalExtensionDef` (no source scan, no microservice round-trip, no cache — it's already on
     disk). The def is found in `BeamoManifest.ServiceDefinitions`.
3. Builds a mount site per extension whose metadata has a non-empty `ExtensionSites`
   (`path = normalize(mount.page)`, selectors `#<name>` `type: "component"`, navContext from the page
   owner), then `MergeMountSites` them into the config by `path`.

> **Failproof deserialization (backward compatibility).** `ExtensionSites` is a *new* field, so any
> `metadata.json` built/deployed before this change won't contain it. Deserialization must tolerate
> that: a missing `ExtensionSites` deserializes to `null`/empty (don't fail), and the extension simply
> contributes no slots and we move on to the next one. More broadly, if a given extension's metadata
> fails to download or parse at all, log it (`Log.Debug`/`Log.Warning`) and **skip that extension**
> rather than aborting the whole `GetRemotePortalConfig` call — consistent with the best-effort
> posture of `FetchRemotePortalConfig`.

#### Caching downloaded remote metadata

Downloads from `content/binary` are cached under the project's `.beamable/temp` folder
(`ConfigService.ConfigTempDirectoryPath`) so repeated `list-*` / `new` invocations don't re-download.

- **Location**: a dedicated subfolder, e.g. `<ConfigTempDirectoryPath>/portal-extension-metadata/`,
  one file per extension (key by extension name, or name + `version` checksum so a redeploy
  naturally misses the stale entry).
- **Invalidation**: a cache entry is valid only if its file's **last write time is within the last 20
  minutes** (make the TTL a named constant, `TimeSpan.FromMinutes(20)`, so it's easy to tune). An
  entry older than the TTL is treated as a miss and re-downloaded. (Keying the filename by `version`
  also invalidates on content change regardless of the timer.)
- **Best-effort**: cache read/write failures must never break the command — fall through to a live
  download (or skip the extension) and `Log.Debug` the problem.

`GetRemotePortalConfig` then becomes: `FetchRemotePortalConfig` (unchanged, best-effort) +
`AppendRunningExtensionMountSites` + `MergeMountSites` (plain merge-by-`path`, union selectors). The
previous `AppendLocalExtensionMountSites` / `BuildLocalExtensionMountSites` are replaced by this
metadata-driven builder.

## Files & changes

### `cli/cli/Services/PortalExtension/PortalExtensionDiscoveryService.cs`
- `ExtensionBuildMetaData` gains `List<string> ExtensionSites` (declared selector names, source order).
- `CreateMetaDataFile` populates it via `ScanExtensionSiteSelectors`.

> No schema change to `RemotePortalConfiguration.MountSiteConfig` or `PortalExtensionMountProperties` —
> the namespaced-selector approach needs no owner field.

### `cli/cli/Services/IRemotePortalConfigService.cs`
- Replace name-based `BuildLocalExtensionMountSites` / `AppendLocalExtensionMountSites` with
  `AppendRunningExtensionMountSites(args, config)`, which (a) enumerates running extensions via
  `CheckStatusCommand.CheckStatus`, (b) gathers each one's `ExtensionBuildMetaData` — **remote
  preferred over local** (remote download below; local reads `assets/metadata.json`), and (c)
  `MergeMountSites` the results in. The pure `BuildMountSiteFromMetadata(ExtensionBuildMetaData)`
  applies `generatedPath = normalize(mount.page)`, selectors `#<name>` `type: "component"`,
  navContext from the page owner.
- Add a remote-metadata fetch helper: resolve the V2 `files[]` `metadata.json` `contentId`/`version`,
  call `IContentApi.PostBinaryPrivateUrls` (batched across all remote extensions), `GET` each signed
  URL, deserialize to `ExtensionBuildMetaData`. New dependency: `args.Provider.GetService<IContentApi>()`
  and `IBeamBeamoApi` (already used by `CheckStatusCommand`) to read `portalExtensionReferences`.
- Add a small cache layer over the remote fetch under `ConfigService.ConfigTempDirectoryPath`
  (`.beamable/temp/portal-extension-metadata/`), 20-minute TTL (named constant), best-effort. Reads
  `args.ConfigService` for the temp path.
- Deserialize `ExtensionBuildMetaData` tolerantly: missing `ExtensionSites` → empty (no failure);
  a per-extension download/parse failure is logged and skipped, never fatal.
- Remove `ExtensionMountType` and stop tagging selectors `"extension"`. Keep `MergeMountSites` as a
  plain merge-by-`path` (union selectors, de-dupe by string + `Log.Warning` on a duplicate selector
  string at the same path; keep first non-empty navContext). Make `ScanExtensionSiteSelectors` /
  `ParseExtensionSiteSelectors` return **all** declared selectors and drop
  `SupportedExtensionSiteSelectors`.

### `cli/cli/Commands/Portal/ListPortalExtensionOptionsCommand.cs`
- Drop `ExtensionMountSiteOption` and the `extensionMountSites` category. Extension-contributed slots
  are now indistinguishable from ordinary component slots (they're just additional uniquely-named
  selectors on a component site), so they flow through the existing `componentExtensions` branch with
  no special handling.

### `cli/cli/Commands/Project/NewPortalExtensionCommand.cs`
- `BuildMountSiteIndex`: remove the `extensionSites` dictionary / `type == "extension"` branch.
  Extension slots are plain component selectors keyed by `path`.
- `RunMountWizard`: remove the third **"Extension"** top-level choice; the new selectors simply appear
  under **"Component"** at their URL path. `ValidateMountArgs`: remove the extension-name branch and
  validate `--mount-page` (URL) + `--mount-selector` against the component sites.
- A child mounting into another extension's slot stores only `mount.page = <URL>` and
  `mount.selector = "#<name>"` — no extra field.

### `cli/cli/Commands/Project/CheckStatusCommand.cs`
- Likely no change to the command itself; `CheckStatus` is reused as the running-extension source.
  Confirm `CheckStatusCommand.CheckStatus` is callable from the config service without circular DI
  (it's already a `static` method taking `CommandArgs`).

### Tests — `cli/tests/PortalExtensionTests/RemotePortalConfigServiceTests.cs`
Rewrite the build/merge tests for the new model:
- `BuildMountSiteFromMetadata`: page extension (`mount.page = "ARoute"`, `ExtensionSites = [top,
  bottom]`) → `path = "ARoute"`, selectors `#top`/`#bottom` `type: "component"`, navContext. Component
  extension (`mount.page = "players"`) → `path = "players"`. Skip when `ExtensionSites` empty / name
  unset.
- `CreateMetaDataFile` writes `ExtensionSites` (all declared selectors, source order, node_modules
  excluded) — adapt the existing scan fixtures.
- `MergeMountSites`: B's `#B-top` (`path = "ARoute"`) folds into A's existing `ARoute` site →
  selectors `[#top, #bottom, #B-top]`, navContext from A preserved. Identical selector strings at the
  same path de-dupe.
- `ParseExtensionSiteSelectors` / `ScanExtensionSiteSelectors` now return **all** selectors (e.g.
  `top`, `bottom`, `B-top`, `sidebar`), not just `top`/`bottom`; the builder assigns `type: "component"`.
- **Failproof deserialization**: a `metadata.json` *without* `ExtensionSites` (old build) deserializes
  with an empty list and contributes no slots — no exception. (Also assert garbage/partial JSON is
  skipped, not fatal.)
- **Cache TTL**: a fresh cache file (< 20 min) is reused without hitting `PostBinaryPrivateUrls`; an
  entry older than the TTL (simulate by back-dating the file's last-write time) is re-downloaded.
  Cache read/write errors fall through to a live fetch.

## Open questions (please confirm before execution)

1. **Where the build-time scan lives.** Reuse the CLI's existing C# regex scanner inside
   `CreateMetaDataFile` (lowest churn, fully self-contained in this repo) **vs.** move detection into
   the toolkit's `beam-build`/vite step so `metadata.json` is authored entirely JS-side (matches how
   the Portal's own `generate-portal-extension-mount-sites.mjs` works, but spans the
   `agentic-portal` repo). **Recommendation: reuse the C# scanner at build time.**

2. **Realm prefix in output.** Keep generated paths **realm-relative** (stripped, consistent with
   every other in-memory site and with how the wizard/validation consume them), and let the Portal
   re-apply the prefix — **vs.** emit the full `/:customerId/.../<route>` path the examples show.
   **Recommendation: stay relative internally**; if `list-mount-sites` should *display* full paths,
   re-prefix only at the command's output boundary.

3. **Remote running extensions.** ✅ Resolved. Download each remote extension's `metadata.json` from
   the private content system using the `contentId` + `version` in its V2
   `portalExtensionReferences[].files[]` entry, via `IContentApi.PostBinaryPrivateUrls` → signed URL →
   `GET` (see "Read time" step 2). When an extension runs both remotely and locally, **remote wins**.

4. **`metadata.json` schema — cross-repo.** The only key that crosses repos is `ExtensionSites` (the
   declared selector names) written into **`metadata.json`**, since the hosted Portal also reads
   metadata at runtime to derive these same sites. Confirm the field name/shape with the Portal team.
   `extension-pages.json` is untouched (it never carries portal-extension sites), and there is no new
   `package.json` `beamable.mount` key.

5. **Selector-name uniqueness.** ✅ Resolved. The model relies on authors giving each
   `BeamExtensionSite` a globally-unique selector (prefix-with-extension-name convention, e.g.
   `B-top`). On a duplicate selector at the same path, the CLI **de-dupes and `Log.Warning`s** the
   collision.

## Verification (to run during execution)
- Unit tests above, plus the full `PortalExtension` suite.
- End-to-end in `ProjectTest` with **Temaki** (page or component) running:
  `dotnet run --framework net10.0 --project cli/cli -- portal extension list-mount-sites`
  → Temaki's selectors appear as **component** entries on the site whose `path` is Temaki's mounted
  URL (not `"Temaki"`); a second extension mounted inside Temaki adds its own uniquely-named selector
  to that same site, and `new portal-extension` mounting **Kare** into it stores
  `mount.page = <URL>` + `mount.selector = "#<name>"` (no owner field).
