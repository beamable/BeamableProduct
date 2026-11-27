# Handoff: make zone-scoped bundles publishable

**Status:** investigation only — not implemented. To be done in a separate session.
**Branch context:** `issue/zones-pes` (zones are new work).
**Related work (this session):** `bundle-discovery-structure-validation-plan.md` adds a
persisted `scope` field to `*.beam.bundle.json` and a single-scope guard. That field is the
recommended input for the fix described here — read that plan first.

All paths are under `cli/cli/` (the nested source dir). Line numbers are from the state of
the branch at handoff; re-confirm before editing.

---

## TL;DR

A bundle whose components are zone-scoped **cannot be published today**, and a bundle that
*mixes* realm and zone components fails with a misleading error. The reason is that
`bundles plan` and `bundles publish` are hard-wired to **realm** scope: they never wire the
deploy `--scope` option, so `args.Scope` stays at its default `DeployScope.Realm`, the build
filters out every zone-scoped component, and then `BundleBuild.SelectComponents` throws
because those components are "missing" from the built plan.

The fix is to let bundle plan/publish operate in **zone** scope. The clean trigger is the new
persisted `scope` field on the bundle file (no new CLI flag — see the naming collision below).

---

## Background: how scope works elsewhere (verified)

### Component scope (realm vs zone)
- The scope source **differs by component kind**: services and storages read it from the
  csproj `<BeamServiceScope>` property (`Services/ProjectContextUtil.cs:831` service,
  `:871` storage); portal extensions read it from `package.json` `beamable.serviceScope`
  (`:886-888`, since portal extensions are TypeScript/React, not csproj projects).
- Both are normalized onto `BeamoServiceDefinition.ServiceScope` / `.IsZoneScoped` —
  null/empty = realm, `"zone"` = zone (`Services/BeamoLocalSystem.cs:776-787`).
- Services, storages, and portal extensions are **all** `BeamoServiceDefinition` entries in
  `BeamoLocalManifest.ServiceDefinitions`, so once the manifest is loaded a single lookup
  resolves scope for every kind regardless of its original source.

### Deploy scope machinery (works correctly for `deploy`, not wired for bundles)
- `enum DeployScope { Realm, Zone }` — `Commands/DeploymentCommands/DeployArgs.cs:22-26`.
- `AddScopeOption` adds the deploy `--scope realm|zone` option and binds it to
  `args.Scope` (`DeployArgs.cs:93-104`). **Deliberately NOT part of `AddPlanOptions`**
  (`DeployArgs.cs:41-80`) — see the collision note.
- `ApplyDeployScopeAsync(args, scope)` (`DeployArgs.cs:167-191`): for `Zone`, resolves the
  effective zid and pins the requester to `BEAM_SCOPE = {cid}.{zid}` via
  `CliRequester.WithBeamScope(...)`, returning an `IDisposable` that restores scope on
  dispose (wrap in `using`). For `Realm` it is a no-op. Throws a clear error if zone was asked
  for but no zid can be resolved.
- `ZoneResolver.ResolveZid(...)` (`Utils/ZoneResolver.cs:25-35`): a selected realm's zone
  binding wins (a realm bound to no zone → `null`); otherwise the local `.beamable` `zid`.
- The actual per-component filter, applied when building the local manifest:
  `CreateReleaseManifestFromLocal` — `Services/DeploymentService.cs:2424-2426`:
  ```csharp
  if (definition.IsZoneScoped != (scope == DeployScope.Zone))
      continue; // realm plan ignores zone services; zone plan includes only them
  ```
  Same predicate for portal extensions at `DeploymentService.cs:1647`, and Docker build gates
  at `:1589` and `:2407`. The relationship is **mutually exclusive** — a plan is *either*
  realm *or* zone, never both.
- Deploy persists the scope into the plan (`DeploymentService.cs:~1960`, `scope = args.Scope`)
  and `deploy release --from-plan` re-adopts it (`ReleaseDeploymentCommand.cs:113-124`), so a
  release re-pins the same target without re-passing `--scope`. `PlanDeploymentCommand` calls
  `ApplyDeployScopeAsync` at `:241`. **Bundles must replicate this plan→publish carry-over.**

There is **no multi-zone concept** — "zone A vs zone B" is not representable. Scope is binary
(realm vs zone); the concrete zone is a single `{cid}.{zid}` resolved from context.

---

## Why bundles are stuck on realm (root cause, verified)

`BundlePlanCommand` (`Commands/BundleCommands/BundlePlanCommand.cs`) and
`PublishBundleCommand` (`Commands/BundleCommands/PublishBundleCommand.cs`):

1. Both `Configure()` call `DeployArgs.AddPlanOptions(this)` but **never** `AddScopeOption`
   (`BundlePlanCommand.cs:60-66`, `PublishBundleCommand.cs:71-85`). So the `DeployScope Scope`
   property (`BundlePlanCommand.cs:27`, `PublishBundleCommand.cs:37`) is never assigned and
   stays at its default `DeployScope.Realm` (enum value 0).
2. Neither `Handle` calls `ApplyDeployScopeAsync`, so BEAM_SCOPE stays `{cid}.{pid}` (realm).
3. Both build via `this.InteractivePlan(provider, args, excludeAuthoredBundleComponents:false,
   includeOnlyBeamoIds: new HashSet<string>(bundle.components))`
   (`BundlePlanCommand.cs:79-80`, `PublishBundleCommand.cs:129-130`). `args.Scope` (Realm)
   flows into `CreateReleaseManifestFromLocal`, where the `:2424` filter **drops every
   zone-scoped component**.
4. Immediately after, `BundleBuild.SelectComponents(plan, bundle)` requires every listed
   component to appear in the built plan or it throws (`Services/Bundles/BundleBuild.cs:68-73`):
   ```
   Bundle=[X] lists components not built as local services/storages/extensions: <zoneComp>
   ```
   The message blames "not built locally" and never mentions scope — the cause is
   non-obvious. A **pure-zone** bundle fails identically (all components dropped); a **mixed**
   bundle fails on its zone components.

Net: only pure-realm bundles can be published today.

---

## ⚠️ Naming collision: two different `--scope` on `bundles publish`

This is the biggest trap for the fix. `PublishBundleCommand` **already** has a `--scope`
option, but it means something completely different — a **visibility/ACL tier**
(`realm` / `org` / `public`), bound to `args.scope` (a `string`, lowercase `s`) and applied
via `BundleAclScope.Resolve` + `PutBundlesAcl` after publish
(`PublishBundleCommand.cs:83-84` and `:251-258`). Note `PublishBundleCommandArgs` has **both**:
- `public string scope;` — visibility tier (`:22`), and
- `public DeployScope Scope { get; set; }` — deploy scope (`:37`, currently always Realm).

`DeployArgs.AddScopeOption` also uses the flag name `--scope` for `DeployScope`
(`DeployArgs.cs:93-104`); its own XML doc (`:82-91`) explicitly says it is kept out of
`AddPlanOptions` *because* the bundle commands already define an unrelated `--scope`.

**Therefore you cannot simply call `AddScopeOption` on the bundle commands** — the flag name
collides with the visibility tier.

### Recommended resolution: derive deploy scope from the bundle file, no new flag
The `scope` field being added to `*.beam.bundle.json` this session (`"realm"` | `"zone"`) is
the natural source of truth. In `bundles plan`/`publish` `Handle`, set:
```csharp
args.Scope = bundle.IsZoneScoped ? DeployScope.Zone : DeployScope.Realm; // from bundle.scope
using var _ = await DeployArgs.ApplyDeployScopeAsync(args, args.Scope);
```
before building. This needs **no** CLI flag, avoids the collision entirely, and is consistent
with the single-scope guard (a bundle is exactly one scope). The visibility-tier `--scope`
stays as-is.

---

## What the fix must do

1. **Set deploy scope from the bundle** in both `BundlePlanCommand.Handle` and
   `PublishBundleCommand.Handle`: `args.Scope = bundle scope`, then wrap the build in
   `using var _ = await DeployArgs.ApplyDeployScopeAsync(args, args.Scope);` so a zone bundle
   pins `BEAM_SCOPE = {cid}.{zid}` and the `:2424`/`:1647` filters keep the zone components
   instead of dropping them.
2. **Carry scope through plan → publish.** `bundles plan` writes a plan file consumed by
   `bundles publish --from-plan` (`BundlePlanUtil`). Persist the scope in the bundle plan and
   re-apply it on `--from-plan` publish, mirroring `deploy release`
   (`ReleaseDeploymentCommand.cs:113-124`). Confirm `DeployablePlan.scope` is already written
   by the shared plan path (`DeploymentService.cs:~1960`) and just needs to be honored here.
3. **Confirm the bundle catalog API is zone-aware.** `PostBundlesPublish(bundle.name, ns,
   request)` (`PublishBundleCommand.cs:242`) and `FetchLatestPublished` currently run under the
   realm scope. Two open questions for the backend/API owner:
   - Does the beamobundle catalog partition bundles by zone at all, or is a bundle a
     customer/namespace-level artifact independent of realm/zone? If the latter, the
     `BEAM_SCOPE` header may be irrelevant to *publishing* and only matters for *building*
     (which images/manifest entries get produced). This changes how much of step 1/2 is needed.
   - How does `BundleNamespace` (the `@<alias>/<name>` namespace) relate to zone? (See memory:
     bundle name format `@alias/bundleName`.) Does zone change the namespace or the ns API
     param?
4. **Update the single-scope guard's messaging** once zone publish works: today the guard
   (this session) turns the misleading `SelectComponents` failure into a clear "bundle is
   zone-scoped" error. When publish supports zone, the guard should *allow* a pure-zone bundle
   through rather than block it.

---

## Key files & anchors

| Concern | File:line |
|---|---|
| `DeployScope` enum | `Commands/DeploymentCommands/DeployArgs.cs:22-26` |
| deploy `--scope` option (binds DeployScope) | `DeployArgs.cs:93-104` |
| `ApplyDeployScopeAsync` (zid resolve + BEAM_SCOPE pin) | `DeployArgs.cs:167-191` |
| `ZoneResolver.ResolveZid` | `Utils/ZoneResolver.cs:25-35` |
| scope filter (services/storages) | `Services/DeploymentService.cs:2424-2426` |
| scope filter (portal extensions) | `Services/DeploymentService.cs:1647` |
| plan carries scope / release re-pins | `DeploymentService.cs:~1960`, `ReleaseDeploymentCommand.cs:113-124` |
| bundle plan: no AddScopeOption, Scope default Realm | `Commands/BundleCommands/BundlePlanCommand.cs:27,60-66,79-80` |
| bundle publish: no AddScopeOption, Scope default Realm | `Commands/BundleCommands/PublishBundleCommand.cs:37,71-85,129-130` |
| bundle publish's *visibility* `--scope` (collision!) | `PublishBundleCommand.cs:22,83-84,251-258` |
| the misleading failure | `Services/Bundles/BundleBuild.cs:68-73` (`SelectComponents`) |
| component scope model | `Services/BeamoLocalSystem.cs:776-787` |
| catalog publish call | `PublishBundleCommand.cs:242` (`PostBundlesPublish`) |

---

## Open questions / risks

1. **Backend zone support for the bundle catalog** (step 3) — the biggest unknown. Determines
   whether zone publish is a pure CLI change or also needs API/backend work.
2. **`--scope` collision** — do NOT reuse the flag for DeployScope on bundle commands. Prefer
   deriving from the bundle's `scope` field (recommended) over adding a differently-named flag.
3. **Multi-zone** — not representable; a zone bundle targets the single context-resolved zone.
   If a customer expects one bundle to deploy to several zones, that is a larger design change,
   out of scope here.
4. **`bundle.scope` vs live component scope drift** — if a component's `<BeamServiceScope>`
   changes after the bundle file was written, the persisted `scope` can go stale. The read-path
   single-scope guard (this session) catches declared-vs-actual mismatch and should error before
   any zone publish proceeds.

## Suggested tests (once implemented)

- Zone-only bundle → `bundles plan`/`publish` builds the zone components (no `SelectComponents`
  throw), pins `{cid}.{zid}`.
- Realm bundle → unchanged behavior (regression guard).
- `bundles publish --from-plan` for a zone bundle re-applies zone scope from the plan.
- Visibility `--scope org|public` still works independently of deploy scope (no collision).
