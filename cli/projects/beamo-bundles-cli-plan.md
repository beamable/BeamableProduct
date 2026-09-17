# Plan: Client (CLI) support for Beamo Manifest Bundles

## Context

Beamo's manifest is evolving from one flat per-realm document into a **manifest-of-manifests**:
a realm manifest keeps its inline `manifest[]` / `storageReferences[]` / `portalExtensionReferences[]`
arrays but gains two additive fields — `schemaVersion` (int) and `references` (a
`bundleName → sha256:checksum` map) — that pull in named, content-addressed **bundles** from a
shared catalog. Full feature spec: `DesignDocs/infra/beamo-manifest/beamo-manifest-redesign.md`.
Backend is implemented through Phase 3 (`impl/phase1-foundation.md`, `phase2-force-injection.md`,
`phase3-resolution-pinning.md`): the **server resolves and assembles bundles at deploy time** — the
client only sends `references` + inline arrays and reads back an assembled view. Phase 4 (PRs) is
not documented as built.

The OpenAPI regen against `dev.api.beamable.com` already landed the new models and three new
generated clients (`IBeamBeamobundleApi`, `IBeamBeamoforcedbundleApi`, `IBeamBeamopullrequestApi`),
and in doing so **broke the CLI build** (~93 errors) because `ManifestView` was re-pointed onto the
new `BeamoBasic*` type family. So the work starts by adopting the regen, then layers the feature on.

**Scope of this plan** (confirmed): Phase 0 build-green + Phase 1 v2 manifest plumbing + Phase 2
`bundles` sub-group + Phase 3 `admin force-inject`. **Out of scope:** the `prs` sub-group (backend
not ready) and any `deploy release --bundle` flag (dropped). Tests are a separate later effort.

Backwards compatibility is the hard constraint: with **no `.beamable/manifest.beam.json`**, every
existing command must behave exactly as today (v1 wire format). `beamable.common` ships into the
Unity SDK, so changes there must be additive (add overloads, don't remove/retype public members).

---

## Type-family landscape (the crux)

The regen produced three parallel reference families in `Models.gs.cs`. Getting these straight is
what makes the rest mechanical:

| Family | Example | Field shape | Used by |
|---|---|---|---|
| **Basic** | `BeamoBasicServiceReference` (13079) | plain `bool`/`string`, has `arm`, `containerHealthCheckPort:long`, **no** `origin`/`logProvider` | `ManifestView` (legacy GET manifest read) |
| **canonical** | `ServiceReference` (3673) | all `Optional<T>`, has `logProvider`, `origin` | **bundle catalog** (`Bundle`, `PublishBundleRequest`, `GetBundleResponse`) |
| **V2** | `BeamoV2ServiceReference` (2197) | all `Optional<T>`, has `logProvider`, `origin` | `BeamoV2Manifest` / `BeamoV2PostManifestRequest` (v2 realm read/publish, carry `references`+`schemaVersion`) |

`Optional<T>` has implicit operators **both** directions (`Optionals.cs:48-49`), so plain↔optional
assignment compiles freely — cross-family converters are just member copies. The realm
plan/merge/diff pipeline operates on whatever `ManifestView` holds (**Basic**); release converts
**Basic → V2** and posts. Bundle publish needs **canonical**. So local build output is produced as
**Basic** (to feed the pipeline) and converted to **canonical** only at bundle-publish time.

---

## Phase 0 — Adopt the regen, get the build green

Prerequisite; nothing else compiles until this is done. Approach validated against the actual
errors. Some edits already exist in the working tree (noted).

1. **`beamable.common/Runtime/Content/Optionals.cs`** — add `OptionalArrayOfArrayOfString :
   OptionalArray<string[]>` (fixes the original `AudienceRequest.exclude` CS0246). *(done)*
2. **`LegacyConverterExtensions.cs`** — the forward `ConvertToLegacy(BeamoV2* → …)` converters must
   produce the **Basic** types `ManifestView` now holds. *(done)*
3. **`ConvertToBeamoV2` + checksum helpers** — add **new overloads** for the Basic family, keeping
   the existing canonical-type overloads (decision: keep old, add overloads — the canonical types
   are live via the bundle API):
   - `LegacyConverterExtensions.cs`: add `ConvertToBeamoV2(this BeamoBasicServiceReference[] / …ServiceReference / …ServiceStorageReference[] / … / …ServiceComponent[] / …ServiceDependencyReference[])` paralleling `:229-305`. Bodies identical (implicit `Optional<T>` conversions).
   - `BeamoExtensions.cs`: add `ComputeChecksum`/`ResetChecksum` for `BeamoBasicServiceReference` and `BeamoBasicServiceStorageReference` paralleling `:19-43`.
4. **`cli/Services/DeploymentService.cs`** — migrate the manifest logic to Basic via the alias block
   (`:31-32`). Repoint `ServiceReference`/`ServiceStorageReference` aliases to `BeamoBasic*` and add
   aliases for `ServiceComponent`, `ServiceDependencyReference`, `OptionalArrayOfServiceStorageReference`,
   `OptionalArrayOfServiceComponent`, `OptionalArrayOfServiceDependencyReference`. All merge/validate/
   transform/build sites then compile unchanged (fields are supersets; the local-build constructors
   assign plain values that fit Basic). Remove the dead `using PostManifestRequest` alias *(done)*.
5. **Name-collision fixes** from new `Models.IPAddress`/`AddressFamily` types: fully-qualify
   `System.Net.*` in `CollectorManager.cs` *(done)*, `MicroserviceStartupUtil.cs` *(done)*, and
   `cli/Services/DiscoveryService.cs:572`.
6. **`cli/Services/Content/ContentService.cs:1971`** — regen inserted an `omitTags` param between
   `id` and `uid` on `GetManifestPublicJson`; change the positional call to `GetManifestPublicJson(manifestId, uid: manifestUid)`.
7. **`tests/DiffTests/DiffTest.cs`** — add `using ServiceReference = …BeamoBasicServiceReference;`
   so the `ManifestView` it builds compiles (fields used are plain on Basic).
8. **DI registration — `beamable.common/Runtime/OpenApiSystem.cs`** (`RegisterOpenApis`): register the
   new clients used in scope, mirroring the `AddOrOverrideScoped<IBeamBeamoApi, BeamBeamoApi>()`
   pattern (`:87`) plus the matching `using`:
   `IBeamBeamobundleApi → BeamBeamobundleApi` and `IBeamBeamoforcedbundleApi → BeamBeamoforcedbundleApi`.
   (Skip `IBeamBeamopullrequestApi` — out of scope.)

**Exit criteria:** `dotnet build cli.sln` → 0 errors; existing deploy commands behave as before.

---

## Phase 1 — v2 manifest plumbing (consume bundles), backwards compatible

Goal: a workspace can **reference** catalog bundles by checksum and have `deploy plan/release`
carry `schemaVersion` + `references` through to the server, which does all resolution. No authored
bundles yet, no partitioning — every local component stays inline (identical to today when the file
is absent).

1. **`.beamable/manifest.beam.json` read/write.** Add a small serializable type
   `ManifestReferences { int schemaVersion; Dictionary<string,string> references; }` and helpers on
   **`ConfigService`** (`cli/cli/Services/ConfigService.cs`) following the Otel/PortalExtension
   helper-region pattern (`:1421-1448`): a `const string MANIFEST_FILE = "manifest.beam.json"`,
   `GetConfigPath(MANIFEST_FILE)`, and `LoadManifestReferences()` / `SaveManifestReferences(...)` /
   `ExistsManifestReferences()`. It is a standalone file under `.beamable/` (sibling to
   `config.beam.json`), not a key inside `config.beam.json`. Read via `JObject` (per the
   `optional-converter-throws-on-null` note — avoid `JsonConvert<T>` on Beamable JSON).
2. **Wire into release.** In `DeployUtil.Deploy` where `BeamoV2PostManifestRequest` is built
   (`DeploymentService.cs:1714-1728`): if `ExistsManifestReferences()`, set `schemaVersion` and
   `references` on the request from the loaded file. If absent, leave both unset → server treats it
   as v1 (unchanged behavior). `references` passes straight through as the `bundleName→checksum` map.
3. **Wire into plan.** `DeployUtil.Plan` already fetches the V2 remote (`CreateReleaseManifestFromRealmV2`
   → `BeamoV2Manifest`, which carries `references`/`schemaVersion`). Surface the local file's
   references in `DeployablePlan` (extend its `Serialize`, `:157-195`) and in `PrintPlanInfo`
   (`:1001`) so `plan` shows referenced bundles. No resolution client-side.
4. **Provenance in read commands.** `BeamoV2Manifest` refs carry `origin: BundleOrigin`
   (Inline/Referenced/Forced) and the manifest carries `references`/`schemaVersion`. Extend
   `deploy get` (`GetDeploymentCommand`) and `deploy status` output to show, per component, its
   `BundleOrigin` and the `references` map (prefer the V2 read where richer). `ManifestView` (Basic)
   lacks `origin`, so provenance display reads from the V2 endpoint.

**Exit criteria:** with a hand-written `manifest.beam.json` referencing a published checksum, `deploy
release` deploys the referenced bundle (server-resolved) alongside inline services; without the file,
byte-identical behavior to today.

---

## Phase 2 — `beam deploy bundles` sub-group (author + publish + catalog)

New `CommandGroup` `bundles` under `DeploymentCommand`, registered in `App.cs` (`:761-768` block),
backed by `IBeamBeamobundleApi`. Namespaced names (`@acme/social`) split into `(ns="@acme",
name="social")` for the generated `(bundleName, ns)` params — add a small `SplitBundleName` helper.

**Authored bundle discovery.** Add discovery of `*.beam.bundle.json` files (scan the workspace like
`ProjectContextUtil.FindPortalExtensionProjects`, `ProjectContextUtil.cs:414`). A bundle config =
`{ name, components: string[] (beamoIds), peerDependencies?: map }`. Validate the partition every
run: a beamoId in ≥2 bundles → error; components must exist in `BeamoLocalManifest.ServiceDefinitions`.
Components that belong to a bundle are **excluded from the realm manifest's inline arrays** (they
reach the realm via `references`); components in no bundle stay inline. This partitioning extends the
Phase 1 inline-build path in `CreateReleaseManifestFromLocal`.

**Building a publish request.** Reuse `CreateServiceReference`/`CreateStorageReference` (produce
Basic) + the portal-extension build path, then convert **Basic → canonical** for
`PublishBundleRequest` (`serviceReferences: ServiceReference[]`, `storageReferences`,
`portalExtensionReferences`, `schemaReferences`, `peerDependencies`, `tag`). Add a
`ConvertToBundleReference` extension (Basic → canonical `ServiceReference`/`ServiceStorageReference`)
in `LegacyConverterExtensions.cs`, same member-copy style.

**Commands** (each an `AtomicCommand`/`StreamCommand`; args in kebab-case):

| Command | API call | Notes |
|---|---|---|
| `bundles new <name>` | none | scaffold a `*.beam.bundle.json` locally |
| `bundles plan <name>` | none | preview components/peer-deps that would publish |
| `bundles publish <name> [--tag <t>]` | `PostBundlesPublish` | build → canonical → publish; print returned checksum/isNew |
| `bundles list` | `GetBundles()` | ACL-filtered catalog list |
| `bundles get <name>[@<tag\|checksum>]` | `GetBundles(name,ns)` / `GetBundlesChecksums` | bundle + tag map |
| `bundles history <name>` | `GetBundlesHistory` | publish history |
| `bundles tags <name>` | `GetBundles(name,ns)` (tag map) | show tag→checksum |
| `bundles tag <name>@<checksum> <tag>` | `PostBundlesTags` | advance a tag |
| `bundles yank <name>@<checksum>` | `PostBundlesChecksumsYank` | |
| `bundles pin <name> [--tag <t>\|--checksum <ck>]` | `GetBundles`/`GetBundlesChecksums` read + local write | resolve tag→checksum, write into `manifest.beam.json` `references` (reuse Phase 1 `SaveManifestReferences`); **no server mutation** |
| `bundles acl <name>@<checksum> --scope <cid.pid\|cid\|*>` | `PatchBundlesChecksumsAcl` | ⚠ generated client sends `GET`-with-body to `/acl` (`BeamBeamobundle.gs.cs:242`) — verify against the OpenAPI/host before relying on it; may be a generator bug to fix in the spec/regen |

**Optional release coupling (design note, decide at build time):** the design doc has `release`
auto-publish locally-changed authored bundles before applying the manifest (UC3). Recommend keeping
publish an explicit `bundles publish` step in this round (simpler, matches the "publish and pin are
decoupled" principle) and revisiting auto-publish-on-release later.

---

## Phase 3 — `beam deploy admin force-inject` (Beamable-internal)

Small `admin` sub-group (or a single command) under `DeploymentCommand`, backed by
`IBeamBeamoforcedbundleApi`, for the UC13 preview workflow. `force-inject <name> --realm <realmId>
--checksum <ck>` → `PutRealmsForcedBundles(name, cid, ns, realmId, {checksum})`; a delete/unset
variant → `DeleteRealmsForcedBundles`. These are SuperAdmin-gated server-side (only `cid:beamable`
tokens), so mark the command `IsForInternalUse=true`. `cid` comes from `AppContext`; both `cid` and
`realmId` are explicit route params. Register `IBeamBeamoforcedbundleApi` (done in Phase 0).

---

## Files touched (summary)

- `beamable.common/Runtime/Content/Optionals.cs`, `.../OpenApiExtensions/LegacyConverterExtensions.cs`,
  `.../OpenApiExtensions/BeamoExtensions.cs`, `.../OpenApiSystem.cs` — types, converters, checksums, DI.
- `cli/Services/DeploymentService.cs` — alias migration (P0), references/schemaVersion wiring (P1),
  partitioning + publish-request building (P2).
- `cli/Services/ConfigService.cs` — `manifest.beam.json` helpers (P1).
- `cli/Services/DiscoveryService.cs`, `cli/Services/Content/ContentService.cs`, microservice files — P0 fixes.
- `cli/Commands/DeploymentCommands/` — new `bundles` + `admin` command files; `App.cs` registration.
- `cli/Services/ProjectContextUtil.cs` (or a new discovery helper) — `*.beam.bundle.json` discovery (P2).
- `tests/DiffTests/DiffTest.cs` — P0 alias (only test touched; broader tests are a later effort).

## Explicitly out of scope
- `prs` sub-group (`IBeamBeamopullrequestApi`) — backend Phase 4 not confirmed built.
- `deploy release --bundle` flag — dropped per direction.
- Writing/expanding the test suite — deferred to a Phase-2 testing effort.

## Verification
1. `dotnet build cli.sln` → 0 errors after Phase 0.
2. Backwards-compat: in a workspace with **no** `manifest.beam.json`, `deploy plan`/`release`/`get`/
   `status`/`list` produce the same output/wire request as before (diff against current behavior).
3. Phase 1: hand-author `manifest.beam.json` with a real published checksum against a scratch realm;
   `deploy release`; confirm via `deploy get` that the referenced bundle's components appear in the
   assembled view with `BundleOrigin=Referenced`.
4. Phase 2: `bundles new` → `bundles publish` (check printed checksum + `isNew`), `bundles list/get/
   history/tags`, `bundles pin` (confirm it writes `references` into `manifest.beam.json`), then
   `deploy release`. Manually verify the `bundles acl` GET-with-body quirk against the host.
5. Phase 3: `admin force-inject` against a preview realm with a Beamable SuperAdmin token.
6. Run the existing suite (`dotnet test tests/`) to confirm no regressions from the Phase 0 migration.
