# Plan: Generate mount sites from local Portal Extensions' `BeamExtensionSite` declarations

## Context

A Portal Extension can host **child** extensions inside itself by rendering
`<BeamExtensionSite selector="top|bottom" />` (from `@beamable/portal-toolkit`). A child extension
targets that slot by setting its own `package.json` `mount.page` to the **host extension's name**
and `mount.selector` to `#top`/`#bottom`.

Real example (in `ProjectTest`): extension **Temaki** renders `<BeamExtensionSite selector="top"/>`;
extension **Kare** mounts into it via `"mount": { "page": "Temaki", "selector": "#top" }`.

The remote mount-site catalog (`extension-pages.json`, fetched by
`RemotePortalConfigService.GetRemotePortalConfig`) only knows about Portal-defined "page" and
"component" slots — never the slots a sibling local extension declares. So when creating a new
extension or listing mount sites, those local slots are invisible. This change discovers them and
surfaces them everywhere `GetRemotePortalConfig` is consumed.

## Model

These are a third kind of mount site — neither "page" nor "component". An extension's site lives
**wherever that extension is mounted**, so it's addressed by the **host extension's name**, not a
Portal route. Per local extension A that declares `BeamExtensionSite`(s), we generate:

- **path** = `A.Name` (what a child sets as its `mount.page`).
- **selectors** = one per `top`/`bottom` found, as `{ selector: "#top"|"#bottom", type: "extension" }`.
  Only `top`/`bottom` are recognized; order is `#top` then `#bottom`.
- **navContext** = `[A.navGroup, A.navLabel]` (skipping unset), for display.

`type: "extension"` (`RemotePortalConfigService.ExtensionMountType`) is the marker that
distinguishes these from remote page/component slots throughout the CLI.

## Files & changes

### `cli/cli/Services/IRemotePortalConfigService.cs`
- `GetRemotePortalConfig` = `FetchRemotePortalConfig` + `AppendLocalExtensionMountSites`.
- **`FetchRemotePortalConfig`** — the remote fetch is now **best-effort**: on any failure (e.g. an
  environment that 404s the catalog, or no network) it logs `Log.Warning` and returns an empty
  config so local sites still surface instead of the whole command throwing. *(Behavior change: the
  fetch used to be fatal.)*
- **`AppendLocalExtensionMountSites`** — reads the **already-loaded** `BeamoManifest` (no
  `InitManifest`: callers reach here without `ISkipManifest`, so the framework loaded it), takes the
  local portal-extension defs, and `MergeMountSites` the built sites in. Best-effort try/catch logs
  a warning rather than breaking the remote-config path.
- **`BuildLocalExtensionMountSites`** (pure) — path = `ext.Name`; selectors from the scan; navContext
  from nav fields. Skips an extension with no name or no sites.
- **`MergeMountSites`** (pure) — merges by `path`, de-duping selectors by string. Extension names are
  normally distinct from remote routes so each is appended; merging guards the rare path clash that
  would otherwise collide in `NewPortalExtensionCommand.BuildMountSiteIndex` (last-writer-wins dict).
- **`ScanExtensionSiteSelectors`** / **`ParseExtensionSiteSelectors`** — scan `.tsx/.jsx/.ts` under the
  extension (excluding `node_modules`/`dist`/`assets`/`build`/`.git`); regex extracts `top`/`bottom`
  selector values, emitted as `#top`/`#bottom` with `type: "extension"`.

### `cli/cli/Commands/Portal/ListPortalExtensionOptionsCommand.cs`
New `extensionMountSites` result category (`ExtensionMountSiteOption { extensionName, selectors }`).
Sites whose selectors are `type: "extension"` go here; page (path ends `!pathMatch`) and component
sites are unchanged.

### `cli/cli/Commands/Project/NewPortalExtensionCommand.cs`
- `BuildMountSiteIndex` gains an `extensionSites` dictionary (keyed by host name; detected by
  selector `type == "extension"`).
- `ValidateMountArgs` validates `--mount-page` against extension names + their selectors.
- `RunMountWizard` adds a third top-level choice **"Extension"** (shown only when extension sites
  exist): pick a host extension, then its available site (auto-selected if only one).

## Verification (done)
- Unit tests: `tests/PortalExtensionTests/RemotePortalConfigServiceTests.cs` — parse, scan
  (ordering + exclusions), build (path = name, `extension` type, navContext, skip cases), merge.
  All green; full `PortalExtension` suite (43) green.
- End-to-end in `ProjectTest`:
  `dotnet run --framework net10.0 --project cli/cli -- portal extension list-extension-options`
  → `extensionMountSites: [{ extensionName: "Temaki", selectors: [{ selector: "#top", type: "extension" }] }]`
  (remote catalog 404'd; local site surfaced anyway via the best-effort fetch).
