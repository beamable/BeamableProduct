# Plan: `bundles pin` → `install` / `uninstall`, fold `force-inject` into `install --force`

## Context

Two bundle-related commands change shape:

- **`beam bundles pin`** (`PinBundleCommand`) — writes a content-addressed reference
  (`@ns/name → sha256:…`) into the scope-matching section of the local
  `.beamable/manifest.beam.json`. Purely local; no server call beyond resolving the checksum/scope.
- **`beam deploy admin force-inject`** (`ForceInjectBundleCommand`, the only subcommand under the
  `admin` group) — sets/clears a **server-side** forced-bundle override (PUT/DELETE against
  `IBeamBeamoforcedbundleApi`), scoped to a realm (`--target-realm`) or CID-wide. Beamable-internal
  (`IsForInternalUse`, SuperAdmin token). Already accepts `--namespace`, `--target-cid`,
  `--target-realm`, `--checksum`, `--unset`.

### Requested changes

1. Replace `pin` with a **`install` / `uninstall`** pair. `install` = today's `pin` (write the
   reference). `uninstall` = the inverse (remove the reference from the manifest).
2. `install` gains a **`--force`** option that runs the force-inject logic (server-side override),
   and can also target a namespace/cid/realm.
3. Once `install --force` exists, **delete the `force-inject` command**: its file is removed and it is
   no longer registered in `App.cs`. The *logic* survives (extracted to a shared helper) and is called
   from `install`.

---

## Design

### Two behaviors under `install`, selected by `--force`

- **`beam bundles install <bundle-name> [--tag|--checksum]`** (no `--force`) → today's `pin`:
  resolve checksum + scope from the catalog, write into the `realm`/`zone` section of the local
  manifest. Unchanged logic.
- **`beam bundles install <bundle-name> --force [--namespace|--target-cid|--target-realm|--checksum]`**
  → today's `force-inject` set path: PUT the forced-bundle override server-side. Does **not** touch the
  local manifest (mirrors current force-inject, which is server-only).

This matches the ask literally ("`install --force` basically does the same thing that `force-inject`
does, but now you can also pass a target namespace"). Note `force-inject` **already** has
`--namespace`; the `--force` path simply carries the same options across.

### `uninstall` — the inverse pair

- **`beam bundles uninstall <bundle-name>`** (no `--force`) → remove the reference from the local
  manifest. Look up `fullName` in both `realm` and `zone` sections, remove wherever present, save.
  Report whether anything was removed.
- **`beam bundles uninstall <bundle-name> --force [--namespace|--target-cid|--target-realm]`** →
  today's `force-inject --unset`: DELETE the forced-bundle override server-side.

> **Open question (1):** Is the symmetric `uninstall --force` (= clear the server-side override)
> wanted, or should `uninstall` be manifest-only and clearing an override stay elsewhere? The ask
> only explicitly names `install --force`. Symmetric is the clean reading of "install/uninstall"
> and avoids leaving `--unset` homeless once `force-inject` is deleted — but confirm.

### Where the force-inject logic lives

Extract the current `ForceInjectBundleCommand.GetResult` body into a shared helper so both
`install --force` and `uninstall --force` call it (and `force-inject` can be deleted):

`cli/Services/Bundles/BundleForcedInject.cs`

```csharp
public class BundleForcedInjectResult
{
    public string bundleName;   // fully-qualified @ns/name
    public string cid;
    public string realmId;
    public string checksum;
    public bool removed;
}

public static class BundleForcedInject
{
    /// Set (unset=false) or clear (unset=true) a server-side forced-bundle override.
    public static async Task<BundleForcedInjectResult> Apply(
        CommandArgs args, string bundleName, string explicitNs,
        string cid, string realmId, string checksum, bool unset)
    { /* verbatim move of the current force-inject logic */ }
}
```

The logic moves verbatim: validate name, resolve ns (`explicitNs` → `FromAlias`, else
`BundleNamespace.Get`), default cid to `AppContext.Cid`, realm-scoped vs CID-wide branch,
resolve latest checksum when omitted, PUT/DELETE via `IBeamBeamoforcedbundleApi`.

### `install` command shape

`cli/Commands/BundleCommands/InstallBundleCommand.cs` (rename of `PinBundleCommand.cs`):

- Command name `"install"`, description e.g. `"Install a bundle: pin its checksum locally, or (with --force) force-inject it server-side"`.
  (Descriptions must be unique, start uppercase, no trailing period — `NamingPass`.)
- Keep `ISkipManifest`.
- Args: `bundleName`, `tag`, `checksum` (existing) **plus** `force` (bool), `ns`, `cid`, `realmId`.
- Options:
  - `--tag`, `--checksum` — as today (pin path).
  - `--force` — "Force-inject the bundle server-side instead of pinning it locally (Beamable-internal)".
  - `--namespace`/`-ns`, `--target-cid`, `--target-realm`/`-r` — as today's force-inject, used only on the `--force` path.
- `GetResult`: `if (args.force) return <map from BundleForcedInject.Apply(...)>; else <existing pin body>;`
- Output type covers both paths — either reuse a shared shape or an `InstallBundleCommandOutput`
  with `name`, `checksum`, and force-only `cid`, `realmId`, `forced`. Final field set TBD at
  implementation; keeps `name`+`checksum` so existing pin-output consumers (if any) still read them.

> **Open question (2):** `--force` is a customer-invisible/internal action but `install` is a
> public command. Keep the internal-use gating (the old command had `IsForInternalUse => true`)?
> A per-command flag can't be per-option, so options: (a) leave `install` public and just document
> `--force` as internal, (b) keep a hidden internal path. Recommend (a) — simplest, and the server
> still enforces SuperAdmin.

### `uninstall` command shape

`cli/Commands/BundleCommands/UninstallBundleCommand.cs` (new):

- Command name `"uninstall"`, unique description e.g. `"Uninstall a bundle: remove its local pin, or (with --force) clear its server-side force-injection"`.
- `ISkipManifest`.
- Args: `bundleName`, `force`, `ns`, `cid`, `realmId`.
- No-force path: `BundleNamespace.SplitName` + `Resolve` + `Qualify`, load manifest, remove `fullName`
  from whichever section(s) contain it, save, log/report removed=true/false.
- Force path: `BundleForcedInject.Apply(..., unset: true)`.

### Deletions

- **`cli/Commands/DeploymentCommands/Admin/AdminCommand.cs`** — this file holds *both* the `admin`
  `CommandGroup` and `ForceInjectBundleCommand`/args/output. `force-inject` is the only subcommand
  under `admin`, so the group becomes empty. Plan: **delete the whole file** and both `App.cs`
  registrations (the `AdminCommand` group at `App.cs:802` and `ForceInjectBundleCommand` at `:803`).

> **Open question (3):** Removing the now-empty `admin` group is a small scope addition beyond
> "delete force-inject". It's the right call (a dangling empty group is dead surface), but if any
> future admin subcommand is imminent, we could instead keep `AdminCommand` in its own file and only
> drop `force-inject`. Recommend deleting both.

- Remove `App.cs:815` `PinBundleCommand` registration, add `InstallBundleCommand` +
  `UninstallBundleCommand` registrations under `BundlesCommand`.
- Delete `PinBundleCommand.cs`.

### Registration diff (`App.cs`)

Remove:
```csharp
Commands.AddSubCommand<AdminCommand, CommandGroupArgs, DeploymentCommand>();                       // :802
Commands.AddSubCommandWithHandler<ForceInjectBundleCommand, ForceInjectBundleCommandArgs, AdminCommand>(); // :803
Commands.AddSubCommandWithHandler<PinBundleCommand, PinBundleCommandArgs, BundlesCommand>();       // :815
```
Add (under `BundlesCommand`):
```csharp
Commands.AddSubCommandWithHandler<InstallBundleCommand, InstallBundleCommandArgs, BundlesCommand>();
Commands.AddSubCommandWithHandler<UninstallBundleCommand, UninstallBundleCommandArgs, BundlesCommand>();
```

---

## Not in scope (flag for confirmation)

- **`beam bundles get --pin`** (`GetBundleCommand`) still uses the word "pin" for the same
  manifest-write behavior. The ask didn't mention it. **Open question (4):** rename its flag to
  `--install` for consistency, or leave `--pin`? Recommend renaming for consistency, but confirm.
- No `IBeamBeamoforcedbundleApi` / backend change; only the CLI surface moves.

## Testing

- No existing tests reference `pin` / `force-inject` bundle commands (grep-confirmed), so nothing
  breaks. `NamingPass` will enforce kebab-case names + unique, uppercase, period-free descriptions
  for the two new commands.
- Add `CLITest` coverage: `install` (pin path) writes the right scope section; `uninstall` removes it;
  `install --force` / `uninstall --force` call the forced-bundle API (mock `IBeamBeamoforcedbundleApi`).

## Sequencing

1. Add `BundleForcedInject` helper (verbatim logic move).
2. Rename `PinBundleCommand` → `InstallBundleCommand`; add `--force` + target options; branch in `GetResult`.
3. Add `UninstallBundleCommand`.
4. Delete `AdminCommand.cs`; update `App.cs` registrations.
5. Tests; `dotnet build cli.sln` clean; `NamingPass` green.

## Open questions for review

1. Symmetric `uninstall --force` (clear server override) — wanted? (recommend yes)
2. `install` public with an internal `--force`, or keep an internal gate? (recommend public)
3. Delete the now-empty `admin` group along with `force-inject`? (recommend yes)
4. Rename `bundles get --pin` → `--install` for consistency? (recommend yes)
