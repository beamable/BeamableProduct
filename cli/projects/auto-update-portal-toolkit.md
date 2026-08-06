# Plan: Auto-update Portal Toolkit in Projects

## Context

Portal extension apps and portal extension libraries each declare a dependency on
`@beamable/portal-toolkit` in their `package.json`. Today there is no way to bump that
version across a workspace — a developer must hand-edit every extension and library file.

This adds a CLI command, **`beam portal extension update-toolkit`** (class
`PortalExtensionUpdateToolkitCommand`), that scans every portal extension *and* every portal
extension library in the workspace and rewrites their `@beamable/portal-toolkit` reference to a
target version. The target is the latest published npm version by default, an explicit
`--version`, or the locally-published verdaccio version via `--local`.

This is the next step of the broader portal-extension tooling work (`add-library`,
`add-dependency`, etc.) and reuses their existing scan/IO patterns.

## Confirmed decisions

- **npm install**: after rewriting each project's `package.json`, run `npm install` best-effort
  (warn on failure, never fail the command) — mirrors `AddLibraryToExtension`.
- **Verdaccio**: default to `http://localhost:4873` (matches `setup-web.sh`/`dev-web.sh`), with an
  optional `--registry` override.
- **Missing dependency**: if a scanned project's `package.json` has no `@beamable/portal-toolkit`
  entry in any dependency block, skip it and log that it was skipped. Never insert it.

## How it works

1. Resolve the **target version** from the options (see "Version resolution").
2. **Scan extensions**: `args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions` where
   `Protocol == BeamoProtocolType.PortalExtension`, taking each `PortalExtensionDefinition`
   (`AbsolutePackageJsonPath`). Same filter used in
   `PortalExtensionAddLibraryCommand.Handle` (cli/Commands/Portal/PortalExtensionAddLibraryCommand.cs:51).
3. **Scan libraries**: enumerate all workspace `package.json` files marked
   `beamable.portalExtensionLib == true`, reusing the existing `LocateLibrary` scan logic.
4. For each scanned `package.json`, update the `@beamable/portal-toolkit` entry **wherever it
   already exists** — extension apps keep it under `devDependencies`
   (PortalExtensionReactApp/package.json:28), libraries under `peerDependencies`
   (PortalExtensionCommonLib/package.json:23). If absent in all blocks, skip + log.
5. Write back with `Newtonsoft.Json.Formatting.Indented` (same as existing commands), then run
   best-effort `npm install` in that project directory.
6. Report which projects were updated (old → new version) and which were skipped.

## Version resolution

A new method on `cli/Services/VersionService.cs` (already DI-registered singleton, App.cs:244)
queries npm-format registry metadata for `@beamable/portal-toolkit`:

- Public npm: `https://registry.npmjs.org/@beamable%2Fportal-toolkit`
- Verdaccio: `{registry}/@beamable/portal-toolkit` (default `http://localhost:4873`)

The response JSON exposes `dist-tags` (e.g. `latest`, `local`) and a `versions` map. Model after
`GetBeamableToolPackageVersions` (VersionService.cs:28) — `HttpClient.GetAsync`, check
`IsSuccessStatusCode`, `JsonConvert.DeserializeObject`, throw `CliException` on failure.

Resolution rules:
- **No options** → npm registry `dist-tags.latest`.
- **`--version X`** → check `X` exists in npm registry `versions`; if not, check verdaccio
  `versions`; if found in either, use `X` verbatim; otherwise throw `CliException`.
- **`--local`** → verdaccio `dist-tags.local` (the tag `dev-web.sh` publishes under,
  e.g. `0.0.123-local<n>`). Requires verdaccio reachable; throw a clear `CliException` if not.

The version string is written verbatim (exact pin, no caret) to match the existing template
convention (`"0.1.6"`).

## Files to modify / create

- **Create** `cli/Commands/Portal/PortalExtensionUpdateToolkitCommand.cs`
  - `PortalExtensionUpdateToolkitCommandArgs : CommandArgs` with `string Version`,
    `bool Local`, `string Registry`.
  - `AtomicCommand<TArgs, TResult>` (returns a result listing updated/skipped projects), or
    `AppCommand` if a structured result is unneeded — prefer `AtomicCommand` for tooling output.
  - `Configure()`: `--version`, `--local`, `--registry` options (kebab-case, descriptions start
    uppercase, no trailing period — enforced by `NamingPass`).
  - `Handle`/`GetResult`: orchestrate resolve → scan → update → install → report.
  - Reuse `LocateLibrary` and `ComputeFileSpecifier` patterns; put the package.json field-update
    helper here (update across `dependencies`/`devDependencies`/`peerDependencies`).
- **Edit** `cli/Commands/Portal/PortalExtensionAddLibraryCommand.cs`
  - Add a `LocateAllLibraries(workspace)` static method that factors out the existing
    `Directory.EnumerateFiles(... "package.json" ...)` + `PortalExtensionPackageInfo` scan from
    `LocateLibrary` (LocateLibrary:133), returning every library dir (and package.json path)
    where `IsPortalExtensionLib == true`. Refactor `LocateLibrary` to call it, so the scan logic
    lives in one place (CLAUDE.md "search for existing solutions" rule).
- **Edit** `cli/Services/VersionService.cs`
  - Add npm/verdaccio metadata query method(s) + a small DTO for `dist-tags` and `versions`.
- **Edit** `cli/App.cs` (~line 666, after the `add-library` registration)
  - `.AddSubCommandWithHandler<PortalExtensionUpdateToolkitCommand, PortalExtensionUpdateToolkitCommandArgs, PortalExtensionCommand>();`

## Reused existing code

- Extension enumeration filter — PortalExtensionAddLibraryCommand.cs:51-54
- Library scan — `LocateLibrary` (PortalExtensionAddLibraryCommand.cs:133)
- package.json read/modify/write + best-effort `npm install` — `AddLibraryToExtension`
  (PortalExtensionAddLibraryCommand.cs:74)
- Registry HTTP + JSON + `CliException` pattern — `VersionService.GetBeamableToolPackageVersions`
  (VersionService.cs:28)
- Property-name constants (`@beamable/portal-toolkit` lives in standard npm blocks; the
  `beamable.*` constants are in `PortalExtensionConstants.cs` if needed)

## Verification

1. Build: `dotnet build cli.sln`.
2. Create a scratch workspace with a portal extension app and a portal extension lib (via
   `beam project new portal-extension` / `portal-extension-lib`).
3. `dotnet run --project cli/cli -- portal extension update-toolkit` → asserts both
   package.json files now pin npm's `latest`.
4. `... update-toolkit --version 0.1.6` → both updated to `0.1.6`; a bogus
   `--version 9.9.9` throws a clear error.
5. With verdaccio running (`./setup-web.sh` + `./dev-web.sh` from `BeamableProduct/`):
   `... update-toolkit --local` → both pin the `0.0.123-local<n>` version; confirm
   `npm install` resolves it via the `@beamable` scoped registry.
6. Confirm a project lacking the toolkit dependency is skipped with a log line.
7. Add an NUnit test under `tests/PortalExtensionTests/` (alongside
   `PortalExtensionLibraryTests.cs`) covering: field-update across dev/peer dependencies,
   skip-when-missing, and `--version` not-found error. Run with
   `dotnet test tests/ --filter "FullyQualifiedName~PortalExtension"`.