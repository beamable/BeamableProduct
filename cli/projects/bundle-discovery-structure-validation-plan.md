# Plan: Validate `*.beam.bundle.json` structure during discovery

**Status: implemented** (build green, 27 new unit tests + existing naming/diff/discovery
tests pass). Files changed: `BundleWorkspace.cs`, `BundleBuild.cs`, `NewBundleCommand.cs`,
`DeploymentService.cs`, `beam-bundles.md.scriban`, new `tests/BundleTests/BundleWorkspaceValidationTests.cs`.

## Goal

When the CLI discovers authored bundle files, it should not treat *any* file whose name
ends in `.beam.bundle.json` as a valid bundle. It should **validate the file's structure**
and **hard-error (naming the offending file)** when a matched file is malformed or does not
match the bundle schema.

A second, related guard is added: a bundle must be **single-scope** (all components realm or
all zone — never mixed), the bundle's **scope is persisted as a field** in the json, and the
declared scope must match the components' actual scope. This is enforced both at **creation**
(`bundles new`) and when **reading** bundles for a build/deploy.

Scanning behavior and the default create location are unchanged:
- Discovery already scans the whole workspace tree recursively for `*.beam.bundle.json`
  (`BundleWorkspace.EnumerateBundleFiles`, `cli/Services/Bundles/BundleWorkspace.cs:162`),
  skipping `bin`/`obj`/`node_modules`/`.git`/`.beamable`. **No change.**
- `bundles new` still creates the file at the workspace root
  (`NewBundleCommand.cs:57`). **No change.**

## Background: how component scope works

A component's scope (realm vs zone) is **not** in the bundle file — it comes from the
component's project, and the **source differs by component kind**:
- Services and storages: the csproj `<BeamServiceScope>` property
  (`ProjectContextUtil.cs:831` service, `:871` storage).
- Portal extensions: the `package.json` `beamable.serviceScope` field
  (`ProjectContextUtil.cs:886-888`; portal extensions are TypeScript/React, not csproj).

Both sources are normalized onto `BeamoServiceDefinition.ServiceScope` and surfaced as
`BeamoServiceDefinition.IsZoneScoped` (`BeamoLocalSystem.cs:776-787`; null/empty scope =
realm, `"zone"` = zone). Services, storages, **and** portal extensions are all
`BeamoServiceDefinition` entries in `BeamoLocalManifest.ServiceDefinitions`, so once the
manifest is loaded a single lookup over `ServiceDefinitions` resolves scope for every
component kind — the guard does not need to know which source a given component came from.
Resolving scope therefore requires the `BeamoLocalManifest`.

Deploy is scope-exclusive: `deploy --scope zone` includes only zone components, the default
realm deploy only realm components (`DeploymentService.cs:2424`). Bundle plan/publish are
currently realm-only (they never wire `--scope`), so a bundle containing any zone component
fails today with a misleading "components not built as local services" error
(`BundleBuild.SelectComponents`, `BundleBuild.cs:68-73`). The single-scope guard turns that
into a clear, early error and records the bundle's scope explicitly.

## Current behavior (the gap)

`BundleWorkspace.Discover` (`BundleWorkspace.cs:89-123`) parses each matched file leniently:

- `JsonConvert.DeserializeObject<JObject>(text) ?? new JObject()` — only guards `null`;
  malformed JSON throws a raw `JsonReaderException` (no file name), and a non-object root
  (e.g. a top-level array) also throws unhelpfully.
- `obj["components"] as JArray` — a wrong-typed `components` (object, string) silently
  becomes an **empty list**.
- Element/`min`/`max` values are read with lenient coercion; wrong types silently degrade
  to defaults.

Net effect: a typo'd or malformed file (e.g. `"component"` instead of `"components"`, or
`components` as an object) is silently accepted as an **empty** bundle instead of being
reported. The cross-file rules in `DiscoverAndValidate` (`:129`) — unique names, one
beamoId per bundle — stay as-is; they are orthogonal to per-file structure.

## Proposed change

### 1. Extract a pure, strict parser

Add a static method to `BundleWorkspace`:

```csharp
// displayPath is a workspace-relative path used only in error messages.
public static BundleConfigFile ParseBundleFile(string displayPath, string text)
```

**Approach: deserialize-then-validate.** It first deserializes the text into a strict wire DTO
(`BundleFileDto` / `BundleDepRangeDto`) with `JsonSerializerSettings {
MissingMemberHandling = MissingMemberHandling.Error }`, catching `JsonException` and rethrowing
as `CliException` naming `displayPath`. Deserialization is what surfaces the *structural* faults
(malformed JSON, non-object root, an unknown property at any level, a field of the wrong shape
such as `components` as an object). It then validates the resulting field **values**. `Discover`
calls it, passing the workspace-relative path (`Path.GetRelativePath`, falling back to the file
name) and setting `filePath`/`name` on the returned object as today.

Keeping the parser pure (text in, model or throw) makes it directly unit-testable without a
`ConfigService` or a real workspace.

### 2. Validation rules

Structural (enforced by deserialization into the strict DTO):
1. `text` must deserialize — malformed JSON, a `null`/empty result, or a top-level array/scalar
   errors. (An **empty / whitespace-only** file is guarded explicitly and errors, rather than
   silently becoming an empty bundle — intended, per the hard-error decision.)
2. An **unknown property at any level** errors (`MissingMemberHandling.Error`) — this is what
   catches a `"component"`/`"dependencies"` typo, and an unknown key inside a dependency range.
3. A field of the wrong container shape errors: `components` not an array, `bundleDependencies`
   not an object, a dependency value not an object.

Value validation (after a successful deserialize):
4. `scope` (optional persisted field): if present, must equal `"realm"` or `"zone"`
   (case-insensitive on read; written lowercase). Absent is tolerated (older files) and treated
   as realm by the manifest-aware guard.
5. `components`: each element a **non-empty, non-whitespace string**; no duplicates.
6. `bundleDependencies`: each dependency name non-empty; `min` defaults to `0` and must be
   `≥ 0`; `max` (absent = open-ended) must be `≥ min`.

**Coercion caveat (accepted).** Because this uses typed deserialization, Newtonsoft *coerces*
some scalars rather than rejecting them: a JSON number in a string position becomes its string
form (e.g. a component `1` → `"1"`, still value-checked), and a quoted or fractional number in
`min`/`max` is coerced to an integer (`"1"` → 1, `1.5` → 1). We validate the resulting values
(range, emptiness, scope), not the original JSON token type. This is the intended trade-off of
the deserialize-then-validate design over hand-walking the JSON.

All messages follow the existing `CliException` style (`"Bundle file=[{path}] ..."`).

### 3. Where validation runs

Validation lives inside `Discover`, so **every** caller enforces it consistently:
`PublishBundleCommand`, `BundlePlanCommand`, `ListBundlesCommand`, `NewBundleCommand`
(dup-check), `DeploymentService.DiscoverAndValidate`, `PortalExtensionUpdateToolkitCommand`.
This is the intended consequence of the hard-error decision: one malformed file fails any
bundle command until fixed (including `bundles new`, whose dup-check calls `Discover`).

Per-file **name** validation (`ValidateName`, the file-stem check) stays in
`DiscoverAndValidate` where it is today — it is a cross-file/name concern, not file
structure, and moving it would change `Require`'s input-name semantics. (Open question below.)

### 4. Single-scope guard + persisted `scope` field (manifest-aware)

`BundleConfigFile` gains a serialized field:

```csharp
public string scope; // "realm" | "zone"; the bundle's deploy scope. Optional in old files.
```

Its **value** is validated structurally (rule 3a). Whether it **matches the components'
actual scope** requires the `BeamoLocalManifest`, so it is a separate, manifest-aware check:

```csharp
// New in BundleWorkspace. Resolves each component's scope from the manifest.
public static void ValidateComponentScope(BeamoLocalManifest manifest, BundleConfigFile bundle)
```

Rules:
- Resolve each component that exists in `manifest.ServiceDefinitions` to its `IsZoneScoped`.
- **Single-scope**: if the resolved components include both zone and realm, throw a
  `CliException` that names the file and lists the realm side and the zone side, e.g.
  *"Bundle=[x] mixes realm and zone components; a bundle must be one scope. realm: a, b —
  zone: c. Split into separate bundles or fix `<BeamServiceScope>`."*
- **Declared vs actual**: if `scope` is present, it must equal the resolved common scope;
  a mismatch throws (names the file, the declared scope, and the offending components). If
  `scope` is absent (older file), no error for absence — the resolved scope is authoritative.
- Components not found in the manifest are left to `ValidateComponentsExist` (existence is a
  separate concern); this method only judges scope for the ones it can resolve.

**Creation (`bundles new`)** — `NewBundleCommand`:
- Drop `ISkipManifest` so the `BeamoLocalManifest` loads (it does not today, `:24`).
- When `--component`s are given, run `ValidateComponentsExist` + `ValidateComponentScope`
  → hard-errors on a mix (this is the "already reads the manifest and errors on mix" flow).
- Derive the bundle scope (`any zone ? "zone" : "realm"`) and **write it** into the json as
  `"scope"`. With no components given, default to `"realm"` (the only publishable scope
  today, and the default deploy scope). The written file now looks like:
  ```json
  { "scope": "realm", "components": ["a","b"], "bundleDependencies": {} }
  ```

**Reading (build/deploy)** — call `ValidateComponentScope` at the manifest-aware read points:
- `BundlePlanCommand` / `PublishBundleCommand`: they already call
  `BundleBuild.ValidateComponentsExist(BeamoManifest, bundle)` (`BundlePlanCommand.cs:72`).
  Fold the scope check in there (or call it immediately after) so plan + publish are covered
  with no per-site churn.
- `DeploymentService` bundle path (`~:1530`, `DiscoverAndValidate`): call
  `ValidateComponentScope` per discovered bundle where the manifest is in hand.
- `bundles list` stays informational (no manifest, no hard scope error); it may surface the
  declared `scope` field in its output.

## Files touched

- `cli/Services/Bundles/BundleWorkspace.cs`
  - New `ParseBundleFile(string displayPath, string text)` — deserialize into the strict
    `BundleFileDto`/`BundleDepRangeDto` (private nested), then value-validate.
  - `BundleConfigFile.scope` field (serialized) + `IsZoneScoped` helper.
  - `Discover` rewired to call `ParseBundleFile` (relative display path via `Path.GetRelativePath`).
  - New `ValidateComponentScope(BeamoLocalManifest, BundleConfigFile)`.
- `cli/Commands/BundleCommands/NewBundleCommand.cs`
  - Remove `ISkipManifest`; validate components exist + single scope; write derived `scope`.
- `cli/Services/Bundles/BundleBuild.cs` — `ValidateComponentsExist` also enforces scope (or a
  sibling call), so plan/publish inherit the guard.
- `cli/Services/DeploymentService.cs` — call `ValidateComponentScope` on the deploy read path.
- `cli/Docs/SkillTemplates/beam-bundles.md.scriban` — MCP skill docs (see below).
- `tests/` — new test file (see below). No public callers change existing signatures.

## Docs / MCP skill template updates

The bundle MCP skill is generated from `cli/Docs/SkillTemplates/beam-bundles.md.scriban`.
Update it so the documented file shape and rules match the new behavior:

- **Generated-file example** (`:53-59`): add the `scope` field, e.g.
  ```json
  { "scope": "realm", "components": ["PaymentService", "OrdersStorage"], "bundleDependencies": {} }
  ```
- **Field description** (`:47`, `:60`): document `scope` (`"realm"`/`"zone"`, the bundle's
  deploy scope; written by `bundles new` from the components' scope — the csproj
  `<BeamServiceScope>` for services/storages, or `package.json` `beamable.serviceScope` for
  portal extensions), and that `bundles new` now validates its components exist and share one
  scope.
- **Common Pitfalls** (`:279-286`): add entries for the new hard errors —
  (a) a malformed `*.beam.bundle.json` (invalid JSON, wrong-typed/empty fields, unknown
  top-level keys, duplicate component) now fails *any* bundle command with a message naming
  the file; (b) a bundle mixing realm + zone components is rejected — a bundle is single-scope.
- **Maintainer Notes** (`:295`): extend the `BundleWorkspace` bullet to mention per-file
  structure validation, the `scope` field, and `ValidateComponentScope` (single-scope guard).
- Cross-check `beam-zones.md.scriban` — if it discusses bundles, note that a bundle is
  single-scope. (Confirm whether it references bundles at implementation time.)

## Tests

New `tests/BundleWorkspaceValidationTests.cs`, driving the pure `ParseBundleFile` directly
(no workspace needed) — one case per rule:

- Valid file (components + bundleDependencies with open and closed ranges) → parses; fields
  correct.
- Valid minimal file (`{}` — no keys) → empty bundle, no throw.
- Malformed JSON / empty file / whitespace / top-level array → throws, message contains the path.
- Unknown top-level key (`"component"`) → throws (deserialization, `MissingMemberHandling.Error`).
- `components` as object → throws; an empty-string element → throws; a duplicate → throws.
- `bundleDependencies` as array; value not an object; unknown key inside a value → throws.
- `min` negative; `max` < `min` → throws.
- `scope` present with value `"realm"`/`"zone"` (any case) → parses; a bad value (incl. a
  number coerced to a non-scope string) → throws.
- Coercion cases are intentionally NOT asserted to throw (a numeric component, `"min":"1"`,
  `"max":1.5`) — see the coercion caveat.

Scope guard (`ValidateComponentScope`, unit tests with a hand-built `BeamoLocalManifest`):
- All-realm and all-zone bundles → no throw.
- Mixed realm+zone components → throws, message lists both sides.
- `scope: "realm"` with a zone component (declared≠actual) → throws.
- `scope` absent, single-scope components → no throw.
- Component absent from the manifest → not judged here (no throw from this method).

CLI-driven (`CLITest`):
- Malformed `x.beam.bundle.json` → `bundles list` exits non-zero, file name in output.
- `bundles new` with two components of differing scope → non-zero exit, mix error.
- `bundles new` with realm components → file written containing `"scope": "realm"`.

## Behavior changes to call out (for review)

- Empty / whitespace `*.beam.bundle.json` → now an error (was a silent empty bundle).
- Wrong-typed `components`/`bundleDependencies` → now an error (was silently ignored).
- Unknown top-level keys → now an error. If future bundle features add top-level fields, the
  allowed-key set must be extended in lockstep.
- `bundles new` now **loads the manifest** (loses `ISkipManifest`) and **validates components
  exist + share a scope** when components are passed. Previously it scaffolded without any
  component validation. Scaffolding with *no* components still works (writes `scope: realm`).
- Every newly-created bundle file now carries a `"scope"` field.

## Decisions (confirmed)

1. **Unknown top-level keys** → **hard-error** (catches typos). Confirmed.
2. **Empty / whitespace file** → **hard-error** (was a silent empty bundle). Confirmed.
3. **Duplicate component within one file** (rule #6) → **rejected**. Confirmed.
4. **Single-scope guard** enforced at **creation and on read** (build/deploy). Confirmed.
5. **Persisted `scope` field**, written at creation, validated on read. Confirmed.

## Open question (minor)

- **Move `ValidateName` into per-file parsing** so the file-stem name is validated during
  `Discover` too, not only `DiscoverAndValidate`? Plan leaves it where it is unless you'd
  prefer otherwise.

## Out of scope

- The recursive scan and ignored-dir set (already correct).
- The default create location (already the root).
- The file suffix stays `.beam.bundle.json` (not changed to `*.bundle.json`).
- Cross-file rules in `DiscoverAndValidate` (unique names, one beamoId per bundle).
- **Making zone bundles publishable.** This plan lets a bundle *record* `scope: zone` and
  keeps it single-scope, but `bundles plan`/`publish` are still realm-only — a zone bundle
  will still fail to publish until `--scope` is wired through bundle publish (separate work).
  The guard just makes that failure honest instead of a misleading "not built" error.
