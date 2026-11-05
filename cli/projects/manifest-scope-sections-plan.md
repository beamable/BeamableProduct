# Plan: Scope-partitioned bundle references in `manifest.beam.json`

## Context

Pinning a bundle writes a content-addressed reference into `.beamable/manifest.beam.json`.
Today that file is a flat map — every pin lands in one `references` dictionary regardless of whether
the bundle is **realm**-scoped or **zone**-scoped:

```json
{
  "schemaVersion": 2,
  "references": { "@alias/my-realm-bundle": "sha256:…", "@alias/my-zone-bundle": "sha256:…" }
}
```

We want the manifest split into a `realm` section and a `zone` section, so a pin goes into the
section matching its scope, and a deploy/release only ever consumes the section matching the deploy's
target scope (dropping the other):

```json
{
  "schemaVersion": 2,
  "realm": { "@alias/my-realm-bundle": "sha256:…" },
  "zone":  { "@alias/my-zone-bundle":  "sha256:…" }
}
```

This is a **breaking change** to the local manifest file. No backward compatibility with the old flat
`references` shape is required (confirmed).

### The core problem this required solving first

To file a pin into the right section, `pin` / `get --pin` must know the bundle's scope. The bundle can
be one that is **not authored in the local workspace** (e.g. `@beamable/defaults`, another customer's
namespace), so the local `*.beam.bundle.json` scope field is not always available. The catalog `Bundle`
model has **no scope field** today.

**Decision (confirmed): Option A — server-authoritative scope, derived not stored.**
The BeamableAPI backend already records `publisherFullScope` (`{cid}.{zid ?? pid}`) on every bundle and
already infers realm-vs-zone from it elsewhere (`RealmInfoProvider.ProvideFromFullScope(...).IsZone`,
used for image mounting). We surface `scope` as a **read-only, derived** field on the bundle read
response — **never persisted, never in the checksum** — and the CLI reads it back from the API and files
the pin accordingly. This removes all local guessing and works for non-local bundles.

Repos touched: **BeamableAPI** (add the derived field), **OpenAPI codegen** (regenerate CLI client),
**BeamableProduct/cli** (manifest reshape + consumers). Codegen regen is acceptable (confirmed).

---

## Part 1 — BeamableAPI: expose derived `scope` on the bundle read response

Repo: `../../BeamableAPI`. All paths below are under
`BeamableShared/Services/Beamo/Bundles/`.

**Constraint:** `scope` must be **inferred at response time, not stored**. The backend already infers
realm/zone from `publisherFullScope`.

### 1.1 Add a `BundleResponse` DTO that carries the derived `Scope` (keep `Bundle` clean)

`scope` is a **response concern**, not persisted state, so rather than putting a field (even a
`[BsonIgnore]` one) on the persistence entity `Bundle`, introduce a dedicated wire projection
`BundleResponse` and have the read response carry that. This keeps `Bundle` a pure Mongo entity.

Implemented (branch state):
- **`BundleResponse`** (`BeamableShared/Services/Beamo/Bundles/BundleResponse.cs`): a `record` mirroring
  `Bundle`'s response fields (`Name`, `Checksum`, the reference lists, `BundleDependencies`,
  `PublisherFullScope`, `PublishedAt`, `Yanked`) **plus `string? Scope`**. A `static BundleResponse
  From(Bundle)` maps the fields and derives `Scope` from the scope segment of `PublisherFullScope`
  using the platform's own rule (`Zone.IsZoneId`; `"zone"`/`"realm"`, null when unknown).
- **`GetBundleResponse`** (`Messages/GetBundle.cs`): its `Bundle` member is now typed `BundleResponse`.
  The JSON field name is unchanged (`bundle`), so the CLI still reads `response.bundle` and now sees
  `scope`. The two single-bundle GETs (`GET beamo/bundles/{ns}/{name}` and `.../checksums/{checksum}`)
  return it.
- **`BundleService.GetByChecksum` / `GetLatest`**: build `new GetBundleResponse(BundleResponse.From(bundle), …)`.
- **`Bundle.cs`** stays a clean entity (no scope, not in `ComputeChecksum`), and `PublishBundleRequest`
  is unchanged.

Builds clean (BeamableShared + gateway + gateway tests, 0 errors); existing tests read
`response.Bundle.{Name,Checksum,ServiceReferences,Yanked}`, all still present on `BundleResponse`.

### 1.2 Do NOT touch these

- **`ComputeChecksum`** — scope stays out; checksum is a content address over component checksums.
  Realm and zone bundles of identical content keep identical checksums.
- **Persistence / migration** — none. `[BsonIgnore]` means the field is never written; existing docs
  are unaffected.
- **`PublishBundleRequest`** — unchanged. Publisher scope is already known server-side at publish
  (`publisherRealm`), and read-time derivation from `publisherFullScope` covers every existing bundle.

### 1.3 Optional (defer unless wanted)

- **`BundleSummary` / `ListBundles`** — `GET beamo/bundles` returns `BundleSummary`, not `Bundle`, so
  it won't show `scope` unless we also add a derived field there. Not needed for pin/deploy; defer.
- **Server-side assembly filter** — `BundleResolutionService.Resolve/Assemble` could refuse to
  assemble a zone bundle into a realm deploy (and vice-versa). The CLI-side filter (Part 3) already
  prevents sending the wrong-scope refs, so this is defense-in-depth; defer unless we want the server
  to enforce it independently.

### 1.4 Verify (not change): publish runs under the correct scope

Confirm the CLI's `bundles publish` (`PublishBundleCommand`) applies the deploy scope
(`ApplyDeployScopeAsync`) for zone bundles the same way `BundlePlanCommand` does, so a zone bundle is
published under `cid.zid` and its `publisherFullScope` resolves to a zone. (Zone bundles must already
build under zone scope, so this is expected to hold — verify, don't assume.)

---

## Part 2 — OpenAPI codegen (CLI's own `oapi generate` flow)

Use the CLI's existing codegen command — no external tooling. It is the same `oapi generate`
(`GenerateSdkCommand`) the web SDK regen uses (see `web/LOCAL_DEV.md`). The C# platform models
(`Models.gs.cs` and the per-service `Beam*.gs.cs`, including `BeamBeamobundle.gs.cs`) are produced by
the `unity` engine generator (`UnitySourceGenerator`) into `beamable.common/Runtime/OpenApi/`, and are
downloaded from whatever `--host` the CLI is pointed at (`SwaggerService.DownloadBeamableApis` uses
`AppContext.Host`).

Run from the `BeamableProduct/` repo root, with the **updated backend running locally** (built from the
Part 1 branch):

```bash
dotnet run -f net10.0 --project ./cli/cli -- \
  --host http://localhost:<port> \
  oapi generate --engine unity \
  --conflict-strategy RenameUncommonConflicts \
  --output ./cli/beamable.common/Runtime/OpenApi
```

Notes:
- Default `--cleaning-strategy RemoveDir` wipes and rewrites the whole output dir — a clean regen.
  Because it regenerates *every* API from the local backend, run against a backend that differs from
  the checked-in generated code **only** by the Part 1 change, then review the diff: ideally only
  `Bundle` (and `GetBundleResponse` if it inlines) gain `scope`. `git restore` anything unrelated.
- After regen, `Bundle.scope` is an `OptionalString` on the generated model consumed by
  `IBeamBeamobundleApi`; rebuild `cli.sln` to confirm.

---

## Part 3 — CLI: reshape the manifest and update all consumers  — IMPLEMENTED

Repo: `cli/`. The flat `references` map becomes two maps, `realm` and `zone`. Builds clean (0 errors);
no existing tests touch the manifest-reference shape. Implementation notes vs. the original sketch:
`ManifestReferences.ForScope(bool isZone)` (bool, not `DeployScope`, to keep the model decoupled from
the Commands namespace); a shared `BundleWorkspace.IsZoneScope(string)` maps a catalog scope string to
the section; `pin` now always fetches the bundle (via `GetBundlesChecksums` when `--checksum` is given)
to read `bundle.scope`; deploy `Plan` selects `ForScope(args.Scope == DeployScope.Zone)`.

### 3.1 Model — `cli/Services/ManifestReferences.cs`

Replace the single `references` dict with `realm` + `zone`, plus a scope selector:

```csharp
public class ManifestReferences
{
    public int schemaVersion = ConfigService.MANIFEST_SCHEMA_VERSION;
    public Dictionary<string, string> realm = new Dictionary<string, string>();
    public Dictionary<string, string> zone  = new Dictionary<string, string>();

    /// <summary>The reference map for a deploy scope.</summary>
    public Dictionary<string, string> ForScope(DeployScope scope) =>
        scope == DeployScope.Zone ? zone : realm;
}
```

(`DeployScope` lives in `cli.DeploymentCommands`; same assembly, add the `using`.)

### 3.2 Config load/save — `cli/Services/ConfigService.cs` (`LoadManifestReferences` / `SaveManifestReferences`, ~1500–1532)

- `Load`: read `realm` and `zone` JObjects into the two dicts. Ignore any legacy `references` key
  (breaking change; no migration).
- `Save`: write `schemaVersion`, `realm`, `zone`.
- Keep `MANIFEST_SCHEMA_VERSION = 2`. Rationale: the **server** contract is unchanged — it still
  receives a single flat `references` map (filtered to the deploy scope, see 3.6) plus `schemaVersion`,
  and the reference *model* is still v2. Only the local file's grouping changed, which doesn't warrant a
  wire-schema bump. (Flag at review if a bump is preferred anyway.)

### 3.3 `pin` — `cli/Commands/BundleCommands/PinBundleCommand.cs`

`pin` must always learn the bundle's scope from the catalog, then write into the matching section:

- Currently, when `--checksum` is given, `pin` skips the API call. Change it to **always** fetch the
  bundle so scope is known: with `--checksum`, call `GetBundlesChecksums(name, checksum, ns)`; otherwise
  `GetBundles(name, ns)` (as today, resolving the tag).
- Read `scope` from the returned `Bundle`, map to section (`"zone"` → `zone`, else `realm`), write
  `manifest.ForScope(scope)[fullName] = checksum`.
- No `--scope` flag is added (scope is authoritative from the server).

### 3.4 `get --pin` — `cli/Commands/BundleCommands/GetBundleCommand.cs`

Already fetches the `Bundle`. Read `scope` (via `BundleInfo`, see 3.7) and write into the matching
section instead of the flat `references`.

### 3.5 `yank` / `prune-yanked`

- `yank` (`YankBundleCommand.cs:42-43`): the pinned-checksum warning must check **both** sections
  (`realm` and `zone`) for `fullName`.
- `prune-yanked` (`PruneYankedCommand.cs`): iterate **both** sections (track which one each key came
  from) so removals delete from the correct dict; the "no references" short-circuit checks both being
  empty.

### 3.6 Deploy plan filter — `cli/Services/DeploymentService.cs` (`Plan<TArgs>`, ~1746-1776)

This is the "filter according to scope" requirement. `Plan` already knows `args.Scope`
(`DeployScope`, realm/zone) and already filters services by it.

- Change local reference loading to select only the target scope's section:

  ```csharp
  var localBundleReferences = hasManifestReferences
      ? configService.LoadManifestReferences()?.ForScope(args.Scope) ?? new Dictionary<string,string>()
      : new Dictionary<string,string>();
  ```

- `remoteBundleReferences` already comes from the scope-correct server manifest (the deploy runs under
  `BEAM_SCOPE = cid.pid` or `cid.zid` via `ApplyDeployScopeAsync`), so no change there.
- The additive/replace merge is unchanged; it now operates on a single-scope set. The wrong-scope
  section is simply never loaded → effectively removed from this deploy.

### 3.7 Release + `BundleInfo`

- **Release** (`DeploymentService.cs:2088-2126`): loads `manifestReferences` only for `schemaVersion`,
  and posts `plan.references` (already scope-filtered by `Plan`). No structural change; just confirm it
  still compiles against the reshaped model (it doesn't touch `.references` on the reshaped type).
- **`BundleInfo`** (`cli/Commands/BundleCommands/BundleInfo.cs`): add a `string scope` field, populated
  in `FromBundle` from `bundle.scope.GetOrElse("")`, so both `get`'s output and the pin path can read it.

---

## Edge cases & transition

- **Missing/empty `scope` from server** (older gateway not yet deployed, or a bundle whose
  `publisherFullScope` is null): default the pin to `realm` and log a warning naming the bundle, so a
  mis-file is visible rather than silent. This only matters in the deploy gap before Part 1 ships; once
  the gateway is updated, every read returns a scope.
- **Same fullName in both sections:** not expected (a bundle has one scope), but load/consumers treat
  the two sections independently, so it degrades gracefully.
- **Existing local manifests with the old flat `references`:** intentionally dropped on next
  load/save (breaking change, confirmed). Users re-pin. Worth a one-line note in the changelog/PR.

## Testing

- Unit: `ManifestReferences` round-trip (realm+zone) through `Save`/`Load`; `ForScope` selection.
- `pin`: realm bundle → `realm` section; zone bundle → `zone` section; `--checksum` path still fetches
  scope. Use the `CLITest` harness with a mocked `IBeamBeamobundleApi` returning a `Bundle` with
  `scope`.
- `prune-yanked` / `yank`: references in either section are found/removed.
- Deploy `Plan`: with pins in both sections, a realm deploy sees only realm refs and a zone deploy only
  zone refs (assert `plan.references`).

## Sequencing

1. **BeamableAPI** (Part 1) — add derived `scope`, verify Swashbuckle emits it, verify publish scope.
2. **Codegen** (Part 2) — regen CLI client; confirm `Bundle.scope` present in `Models.gs.cs`.
3. **CLI** (Part 3) — model + config, then consumers, then deploy filter; tests last.

Steps 1–2 must land (and the gateway be reachable for regen) before Step 3's pin/get can read real
scope, though the CLI model/config/deploy-filter work can proceed in parallel against the regenerated
model.

## Open questions for review

1. ~~Keep `schemaVersion = 2` or bump?~~ — RESOLVED: kept at 2 (server contract unchanged; only the
   local file grouping changed).
2. ~~Backend: computed getter vs service-assigned property~~ — RESOLVED: separate `BundleResponse` DTO
   carries the derived `Scope`; `Bundle` stays a clean entity (1.1).
3. Do we also want the optional server-side assembly filter (1.3) as independent enforcement, or is the
   CLI-side filter sufficient for now?
