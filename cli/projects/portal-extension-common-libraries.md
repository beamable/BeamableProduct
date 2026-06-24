# Portal Extension Common Libraries

## Context

Today each Beamable Portal Extension (a React + TypeScript app under `extensions/`) is an
independent, self-contained npm package. There is no supported way to share UI/logic across
multiple extensions — code gets copy-pasted. This feature adds a **shared TypeScript library**
("portal extension common lib") that any number of extensions can depend on, import with full
type support, and bundle into their build. A shared lib can export plain TS utilities *and*
React components (the "recursive rendering" use case: a component implemented once in the lib
and rendered inside many extensions).

The work mirrors patterns the CLI already has:
- `beam project new portal-extension` scaffolds an extension from a `dotnet new` template.
- `beam portal extension add-microservice` records a dependency in the extension's `package.json`.
- The C# `beam project new common-lib` scaffolds a shared C# library (different ecosystem).

### Locked decisions (confirmed with user)

| Decision | Choice |
|---|---|
| Scaffold command name | `beam project new portal-extension-lib` (alias `pe-lib`) |
| On-disk location | `<workspace>/extensions-libs/<Name>/` (sibling of `extensions/`) |
| Packaging | **Raw TS source** shipped as a `file:` npm package (no `dist/` build step) |
| Dev loop | **Auto-watch**: editing the lib triggers a rebuild of dependent extensions |
| `file:` path integrity | Validated on every run; **auto-repaired** if the lib moved, comprehensive error if missing (section 7) |

## Mechanism (why this works)

- The lib is a real npm package whose `package.json` `exports`/`types` point at `./src/index.ts`.
  It is added to a consuming extension as `"<Lib>": "file:../../extensions-libs/<Lib>"`. `npm install`
  symlinks it into the extension's `node_modules`.
- The extension build is `vite build` in lib/IIFE mode (`npm run beam-build`). **Build mode uses
  Rollup, not esbuild dep pre-bundling** (pre-bundling is dev-server only), so a source dep
  bundles cleanly. Vite's default `resolve.preserveSymlinks: false` resolves the symlink to the lib's
  **real path outside `node_modules`**, so `@vitejs/plugin-react` (default `exclude: /node_modules/`)
  **does** transform the lib's `.tsx`, and plain `.ts` is transpiled by Vite's esbuild transform.
  → satisfies "types" + "bundled along with the extension".
- `portalExtensionPlugin({ react: true })` externalizes react/react-dom/jsx-runtime at the Rollup
  `external` level, which applies to the **whole module graph including lib code**. The lib must
  therefore declare react/react-dom as **`peerDependencies`** (never `dependencies`) so npm does not
  install a second React copy → avoids "Invalid hook call".
- The `file:` entry in `package.json` `dependencies` is also the signal the CLI reads to know which
  libs to auto-watch.

This is strictly more than "a folder of TS files imported by relative path": it gives named imports
(`import { X } from 'MyLib'`), a `package.json`-declared dependency (a stated requirement), reuse
across extensions, and the dependency record that auto-watch depends on.

## Implementation

### 1. New template — `cli/beamable.templates/templates/PortalExtensionCommonLib/`

The templates `.csproj` already globs `templates\**\*`, so a new folder ships automatically — no
csproj change required. Mirror `PortalExtensionReactApp`'s template config style.

Files to create:

- `.template.config/template.json` — mirror PortalExtensionReactApp's manifest with new
  `identity: Beamable.Templates.PortalExtensionCommonLib`, `shortName: portalextensioncommonlib`,
  `sourceName: PortalExtensionCommonLib`, `language: TypeScript`, same `TemplateVersion` symbol bind.
- `package.json`:
  ```jsonc
  {
    "name": "PortalExtensionCommonLib",
    "private": false,
    "version": "1.0.0",
    "type": "module",
    "beamable": { "version": "1.0.0", "portalExtensionLib": true },
    "main": "./src/index.ts",
    "module": "./src/index.ts",
    "types": "./src/index.ts",
    "exports": { ".": { "types": "./src/index.ts", "import": "./src/index.ts" } },
    "scripts": { "typecheck": "tsc --noEmit" },
    "peerDependencies": { "react": "^19.0.0", "react-dom": "^19.0.0" },
    "devDependencies": { "@types/react": "^19.0.0", "@types/react-dom": "^19.0.0", "typescript": "^5.6.3" }
  }
  ```
- `tsconfig.json` — mirror the extension tsconfig (`moduleResolution: bundler`, `jsx: react-jsx`,
  `noEmit: true`, `include: ["src/**/*"]`).
- `src/index.ts` — barrel re-exporting the samples.
- `src/sample.ts` — a plain TS util (e.g. `export function greet(name: string)`), proves `.ts` bundling.
- `src/SampleWidget.tsx` — a React component importing `react`, proves `.tsx` transform + react
  externalization (the recursive-rendering use case).
- `.gitignore` — same node ignores as the extension template.

### 2. Scaffold command — `cli/Commands/Project/NewPortalExtensionLibCommand.cs`

Mirror `NewCommonLibraryCommand` / the front half of `NewPortalExtensionCommand` (no mount/remote
config logic):
- `NewPortalExtensionLibCommandArgs : SolutionCommandArgs`.
- Command name `"portal-extension-lib"`; add alias via `AddAlias("pe-lib")` in `Configure()`
  (`AppCommand` derives from `System.CommandLine.Command`).
- `Configure()`: `AddArgument(new ServiceNameArgument(), ...)` + `SolutionCommandArgs.Configure(this)`.
- `Handle()`: run `PortalExtensionCheckCommand.CheckPortalExtensionsDependencies()` (node/npm guard),
  `await args.CreateConfigIfNeeded(_initCommand)`, then `await args.ProjectService.CreateNewPortalExtensionLib(args)`.

Add `ProjectService.CreateNewPortalExtensionLib` in `cli/Services/ProjectService.cs`, mirroring
`CreateNewPortalExtension` (line 274) — but output to `extensions-libs/` and **no** sln/reference work:
- `EnsureCanUseTemplates(version)`.
- `outputPath = Path.Combine(_configService.BeamableWorkspace, "extensions-libs")`.
- `RunDotnetCommand($"new portalextensioncommonlib -n {ProjectName} -o {servicePath.EnquotePath()}")`.
- Rewrite `package.json` `name` → `ProjectName.Value` (same `JObject` block as lines 288-297, reusing
  `EXTENSION_NAME_PROPERTY_NAME`).
- `await args.BeamoLocalSystem.InitManifest()`.

### 3. Add-library command — `cli/Commands/Portal/PortalExtensionAddLibraryCommand.cs`

Mirror `PortalExtensionAddDependencyCommand` but write standard npm `dependencies` (not the
`microserviceDependencies` array), and **do not** generate clients (that is microservice-only).

This command and the run-time self-heal step (section 7) share the SAME logic, so it lives in static
helpers on this class — exactly the precedent set by `PortalExtensionAddDependencyCommand.GenerateDependenciesClients`,
which is a static method reused by both the command and the run flow. Do not duplicate path logic.

**Shared static helpers (reused by section 7):**
- `LocateLibrary(workspace, libName) → string?` — scans `<workspace>/extensions-libs/*/package.json`
  for an entry whose `name == libName` AND `beamable.portalExtensionLib == true`; returns its absolute
  dir or null. (Locating by *name*, not just by path, is what makes self-heal possible when a lib moved.)
- `ComputeFileSpecifier(extensionDir, libAbsPath) → string` —
  `"file:" + Path.GetRelativePath(extensionDir, libAbsPath).Replace('\\','/')`.

`Handle()`:
1. Find the extension in `args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions`
   (filter `Protocol == BeamoProtocolType.PortalExtension`), throw `CliException` if missing
   (mirror the existing "Couldn't find a Portal Extension" message).
2. `libAbsPath = LocateLibrary(workspace, LibraryName)`; if null, throw a comprehensive `CliException`
   (lib name + expected `extensions-libs/<name>` location + hint to run `project new portal-extension-lib`).
3. `specifier = ComputeFileSpecifier(extension.AbsolutePath, libAbsPath)`.
4. Read extension `package.json` (`JObject`), upsert into the `dependencies` object
   (`deps[LibraryName] = specifier`) — upsert (not "skip if exists"), so re-running the command
   **updates a stale path**. Write back with `Formatting.Indented` (same write style as
   `PortalExtensionAddDependencyCommand`).
5. Run `npm install` inline in the extension dir via `StartProcessUtil.Run("npm","install",
   useShell:true, workingDirectoryPath: extension.AbsolutePath)` so the symlink + types are
   immediately available (mirrors `PortalExtensionObserver.InstallDeps`).

### 4. Constants — `beamable.common/Runtime/Constants/Implementations/PortalExtensionConstants.cs`

Add:
```csharp
public const string EXTENSION_LIB_PROPERTY_NAME = "portalExtensionLib";
public const string EXTENSION_NPM_DEPENDENCIES_PROPERTY_NAME = "dependencies";
```
Use `EXTENSION_LIB_PROPERTY_NAME` as the lib discovery marker and
`EXTENSION_NPM_DEPENDENCIES_PROPERTY_NAME` for the package.json edits.

### 5. Registration — `cli/App.cs`

- After the `NewPortalExtensionCommand` registration (~line 595):
  `Commands.AddSubCommand<NewPortalExtensionLibCommand, NewPortalExtensionLibCommandArgs, ProjectNewCommand>();`
- After the `PortalExtensionAddDependencyCommand` registration (~line 662):
  `Commands.AddSubCommandWithHandler<PortalExtensionAddLibraryCommand, PortalExtensionAddLibraryCommandArgs, PortalExtensionCommand>();`

### 6. Auto-watch — `cli/Services/PortalExtension/PortalExtensionDiscoveryService.cs`

The observer currently watches only the extension dir (`AppFilesPath`) and ignores any path
containing `node_modules/` (`OnChanged`). To rebuild on lib edits:
- In `StartExtensionFileWatcher`, after creating the extension watcher, read the extension's
  `package.json` `dependencies`, select `file:` specifiers that resolve under `<workspace>/extensions-libs/`,
  and for each resolved **real lib path** create an additional `FileSystemWatcher` (same filters,
  `IncludeSubdirectories = true`) wired to the existing `OnChanged` handler.
- Keep these watchers alive for the lifetime of the watch loop (track them in a list so they aren't
  GC'd / disposed early; the existing single `using var watcher` pattern must be widened to dispose
  all of them at loop end).
- `OnChanged` already early-returns on `node_modules/`, `assets/`, and `beamable/clients` — this also
  correctly ignores the lib's own `node_modules`, and on a real lib-src change it calls
  `BuildExtension()` (re-runs `vite build`, which re-transpiles current lib source). No other change
  to `OnChanged` is needed.

### 7. Validate & self-heal the `file:` paths on run — `cli/Services/BeamoLocalSystem_PortalExtension.cs`

**Requirement:** the `file:` relative path stored in the extension's `package.json` is computed and can
drift (the extension or the lib gets moved/renamed/relocated). Re-running the extension must first
verify each lib path resolves; if it does not, **auto-fix it when the lib can be relocated by name**,
otherwise **throw a comprehensive error**. Manual editing must never be the only recovery path.

Add a static `PortalExtensionAddLibraryCommand.ValidateAndRepairLibraryDependencies(extensionPath, workspace)`
(reusing the `LocateLibrary` / `ComputeFileSpecifier` helpers from section 3, no duplication). For each
entry in the extension `package.json` `dependencies` whose value starts with `file:`:
1. Resolve the recorded target: `Path.GetFullPath(Path.Combine(extensionPath, relPath))`.
2. **Valid** if that dir exists and its `package.json` has `portalExtensionLib == true` → leave as-is.
3. **Repairable** if invalid but `LocateLibrary(workspace, depKey)` finds the lib elsewhere → recompute
   `ComputeFileSpecifier`, rewrite `package.json` (`Formatting.Indented`), and `Log.Warning` that the
   path was auto-repaired (old → new). This is the "automatically fix it if it just needs updating" case.
4. **Fatal** if invalid and the lib cannot be located anywhere under `extensions-libs/` → throw a
   `CliException` naming: the extension, the dependency key, the recorded `file:` value, the resolved
   absolute path that does not exist, and the fix (`beam portal extension add-library <ext> <lib>` or
   restore/recreate the lib).

**Hook point:** call it in `RunMicroserviceForever`'s `InitializeServices` block, **immediately before
`observer.InstallDeps()`** (alongside the existing `GenerateDependenciesClients` call) — so the paths
are correct before `npm install` symlinks them and before the first `vite build`. Wrapping in the
existing `try/catch (CliException)` there means a fatal mismatch surfaces as a clean startup error.

## Verification (end-to-end)

Run the CLI from source (`dotnet run --project cli/cli -- <args>`) or via `./dev.sh`, against a
scratch workspace that has `.beamable/config.beam.json`:

1. `beam project new portal-extension-lib MyLib` → assert
   `extensions-libs/MyLib/{package.json,tsconfig.json,src/index.ts,src/SampleWidget.tsx}` exist and
   `package.json.name == "MyLib"`.
2. `beam project new portal-extension MyExt --mount-page players/ --mount-selector "#root" -q`.
3. `beam portal extension add-library MyExt MyLib` → assert `extensions/MyExt/package.json`
   `dependencies.MyLib == "file:../../extensions-libs/MyLib"` and `npm install` linked it
   (`extensions/MyExt/node_modules/MyLib` symlink exists).
4. In `extensions/MyExt/src/App.tsx` add `import { greet, SampleWidget } from 'MyLib'` and use both;
   confirm editor intellisense/types resolve.
5. In `extensions/MyExt`: `npm install && npm run beam-build`. Assert exit 0 and that `assets/index.js`
   **contains** a unique string from the lib (proves bundling) and references react via the host
   window-global form (proves react stays externalized — no inlined React copy).
6. Auto-watch: run the extension via the normal CLI dev flow, edit a string in
   `extensions-libs/MyLib/src/SampleWidget.tsx`, and confirm the extension rebuilds and the rendered
   output updates.
7. Self-heal (section 7): manually corrupt `extensions/MyExt/package.json` `dependencies.MyLib` to a
   wrong `file:` path, then run the extension → confirm it auto-rewrites the path back to the correct
   one and logs a warning. Then delete `extensions-libs/MyLib` entirely and run again → confirm a
   comprehensive `CliException` naming the missing lib + recorded path + fix command.

## Testing (NUnit, in `tests/`)

Mirror existing portal-extension command tests (`CLITest` base + `Mock<T>` for DI):
- **Scaffold test**: invoke `NewPortalExtensionLibCommand` in a temp workspace; assert the lib dir,
  `package.json.name`, and `exports.types == "./src/index.ts"`. Match how existing scaffold tests
  satisfy `EnsureCanUseTemplates` (template availability).
- **add-library test**: scaffold an extension + a lib, run `PortalExtensionAddLibraryCommand`, re-read
  the extension `package.json`, assert `dependencies.<Lib>` equals the expected `file:` specifier;
  assert re-run upserts (updates, no duplicate) and a `CliException` for an unknown extension/library name.
- **self-heal test**: write a wrong `file:` path into the extension `package.json`, call
  `ValidateAndRepairLibraryDependencies`, assert the path was rewritten to the correct relative value;
  then point it at a non-existent lib and assert a `CliException` is thrown.

## Critical files

- Create: `cli/beamable.templates/templates/PortalExtensionCommonLib/**`
- Create: `cli/Commands/Project/NewPortalExtensionLibCommand.cs`
- Create: `cli/Commands/Portal/PortalExtensionAddLibraryCommand.cs`
- Edit: `cli/Services/ProjectService.cs` (add `CreateNewPortalExtensionLib`, mirror line 274)
- Edit: `cli/Services/PortalExtension/PortalExtensionDiscoveryService.cs` (auto-watch in
  `StartExtensionFileWatcher`)
- Edit: `cli/Services/BeamoLocalSystem_PortalExtension.cs` (call `ValidateAndRepairLibraryDependencies`
  in `RunMicroserviceForever` before `observer.InstallDeps()`)
- Edit: `cli/App.cs` (two registrations, ~lines 595 and 662)
- Edit: `beamable.common/Runtime/Constants/Implementations/PortalExtensionConstants.cs` (two constants)
- Reference (do not change): `cli/Commands/Project/NewPortalExtensionCommand.cs`,
  `cli/Commands/Portal/PortalExtensionAddDependencyCommand.cs`,
  `cli/beamable.templates/templates/PortalExtensionReactApp/**`
