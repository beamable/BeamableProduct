# Storage name length validation (CLI + BeamableAPI)

The work ships in two phases:

| Phase | Repo | What it does |
|---|---|---|
| **1. CLI** | BeamableProduct | Client-side checks: creation, manifest init (covers local runs), deploy, microservice runtime |
| **2. Backend** | BeamableAPI | Authoritative check on every manifest post (realm, zone, promote, pull request) |

Phase 1 is implemented and tested (automated + manual) before phase 2 starts.

## Problem

Each storage object maps to a MongoDB database named

```
{cid}{scope}_{storageName}      scope = pid for realm storages, zid for zone storages
```

MongoDB rejects database names of 64 bytes or more, so the limit is **63**. Nothing checks
this today:

- CLI: `ServiceName` (`cli/beamable.common/Runtime/Semantics/ServiceName.cs`) only checks
  the regex `^[A-Za-z][A-Za-z0-9_-]*$`, with no length limit.
- BeamableAPI: `PostManifestRequest.ThrowIfInvalid` and `ManifestItemValidation` don't check
  `StorageId`'s length or format. A name that is too long only fails later, inside the
  Atlas role/user provisioning (`MongoAtlasAdmin.GrantStorageAccess`) or on the first write
  from the microservice. Both errors are hard to trace back to the storage name.

**Local runs break too, not only deploys.** `beam project run` and `generate-env` give the
service the real `CID` and `PID` from the current context (`PID = zid` for zone services). The
service then builds the same name and uses it against the local `mongo:7.0` container. The
limit is enforced by the MongoDB server, not by Atlas. Verified on `mongo:7.0`: a 63-character
name works, while a 64-character name fails both inserts and reads with
`InvalidNamespace - db name must be at most 63 characters`. Locally, the containers and the
service start normally; the failure only appears on the first database call, and the error
doesn't mention the storage. Because the prefix comes from the selected realm or zone, a name
that fits one realm can fail after switching to another. The checks below must therefore fail
local runs as well as deploys.

Both sides already build the name the same way:

| Where | Code |
|---|---|
| BeamableAPI | `MongoAtlasAdmin.MicroStorageDatabasePrefix` → `$"{info.CustomerId}{info.Scope}"`, then `$"{prefix}_{storageId}"` (`GrantStorageAccess`, `GetPerformanceByDatabase`) |
| Microservice runtime | `StorageObjectConnectionProvider.GetDatabaseByStorageName` → `$"{CID}{PID}_{storageName}"`; for zone services the CLI sets `PID = zid` |

### How much room the storage name gets

The ids are snowflake longs from `BeamIdGenerator` (at most 19 digits). The table shows the
budget for the storage name:

| Scope | Prefix today | Worst-case prefix | Storage name budget today | Worst case |
|---|---|---|---|---|
| Realm, legacy (`16` + `DE_`+16) | 35 | – | 27 | – |
| Realm, new (`18` + `DE_`+18) | 39 | 41 | 23 | 21 |
| Zone (`18` + `ZONE_`+18) | 41 | 43 | 21 | 19 |

(budget = 63 − prefix − 1 for the `_`)

The budget depends on the target cid/scope, so a single fixed limit would either be too
strict for legacy realms or too loose for zones. The plan validates against the
**actual target** wherever the target is known.

## Shared rule

The rule is `len($"{cid}{scope}_{storageName}") <= 63`. Both phases use it and the same
error message. The message names the storage, the database name it would produce, its
length, and the maximum storage-name length for that target, for example:

> Storage `PlayerInventoryStorage` is too long for zone `ZONE_1234…`: the MongoDB database name
> `1234…ZONE_1234…_PlayerInventoryStorage` would be 63+ characters (limit 63). Storage names in this
> zone can be at most 21 characters.

---

## Phase 1: CLI (BeamableProduct)

### Changes

1. **Helpers.** The rule is split across two files:
   - `StorageDatabaseName` (inside `microservice/beamable.tooling.common/SharedRuntime/StorageObjectConnectionProvider.cs`) holds the rule
     itself: `MaxLength = 63`, `Compose`, `IsTooLong`, `MaxStorageNameLength` and
     `DescribeTooLong`. Both the microservice runtime and the CLI (which already references
     `beamable.tooling.common`) use it. Unity doesn't compile this project, and nothing is
     added to `beamable.common`, so the Unity shared-code pin is unaffected.
   - The CLI-side checks are static members of `ProjectService`
     (`cli/cli/Services/ProjectService.cs`), next to `CreateNewStorage`:
     - `FindTooLongStorageNames(definitions, cid, pid, zid, includeOnlyBeamoIds)`, which
       doesn't throw. It checks only local storages and skips everything when the cid isn't
       numeric.
     - `ThrowIfStorageNamesTooLong`, which throws one `CliException` listing every offender.
     - `ValidateNewStorageName`, which throws when the target is known and warns using the
       worst-case budget (realm 21, zone 19) when it isn't. `CreateNewStorage` calls it first,
       resolving the target scope with `ResolveNewStorageScope`.
2. **`beam project new storage`** (`NewStorageCommand.Handle`, before
   `ProjectService.CreateNewStorage`): validate against the current context, using `cid` plus
   `zid` when `--zone` is passed and `pid` otherwise.
   - If there is no cid or scope yet (not logged in, or `--zone` without a resolvable zid), log a
     warning that uses the worst-case budget instead of failing.
   - Unity's editor creates storages through this command, so it gets the check too.
3. **Manifest initialization** (`BeamoLocalSystem.InitManifest`, after `GenerateLocalManifest`,
   next to the `PortalExtensionNameValidator` pass). This catches storages renamed by hand
   (folder, csproj or `BeamId` edits), not through the CLI. `App.cs` runs `InitManifest` before
   every command that isn't `ISkipManifest`, and it already rethrows failures, so a bad name
   stops the command.
   - Cost: in-memory only, with no network calls. It loops over `ServiceDefinitions` where
     `Protocol == EmbeddedMongoDb`, and uses each definition's `BeamoId` and `IsZoneScoped`.
   - Realm storages: check against `_ctx.Cid` + `_ctx.Pid`.
   - Zone storages: check against `_ctx.Cid` + the **local** zid
     (`ConfigService.CFG_JSON_FIELD_ZID`). `ZoneResolver.ResolveZid` is deliberately not called,
     because it makes an HTTP request whenever a pid is selected. If no local zid is set, the
     zone storage is skipped with a `Log.Verbose`; the deploy check (item 4) and the runtime
     guard (item 5) still catch it.
   - If cid/pid aren't available (no `.beamable` config, or not logged in), skip silently. The
     init code already returns early when there is no config directory.
   - Collect every offending storage and throw **one** `CliException` listing them all, each
     with its database name, its length, and the maximum for that scope.
   - This also guards local runs: `beam project run` (and Unity/IDE flows that call the
     CLI) initialize the manifest first, so the command fails before any container starts.
     No separate check is needed in `RunProjectCommand`.
   - Fixing a name requires no CLI command: rename the project (folder/csproj/`BeamId`) or
     archive it, then run any command again.
4. **Deploy plan** (`DeploymentService.CreateReleaseManifestFromLocal`, which already filters
   by `DeployScope`): validate every storage reference it emits against the **resolved**
   target cid and pid/zid. `ApplyDeployScopeAsync` already resolves the zid, so this adds no
   network call. This is still needed after item 3: the deploy target's zid can differ from
   the local config zid (a realm's zone binding takes precedence), and init skips zone
   storages when no local zid is set. Report all offenders in one error before any image
   build starts. Until phase 2 ships, this is the last check before the manifest is posted.
5. **Runtime guard** (`microservice/beamable.tooling.common/SharedRuntime/StorageObjectConnectionProvider.cs`):
   in `GetDatabaseByStorageName`, throw a clear exception when the composed name is longer
   than 63, before the first database call. This covers services started without the CLI
   (for example F5 from the IDE with an existing `.env`), and services whose zone storage the
   init check skipped because no local zid was set. The message names the storage, the
   composed name, its length, and the limit.
6. ~~Fill in `StorageObjectAttribute.AreValidNameForType`~~. **Dropped:** `StorageObjectAttribute`
   isn't registered with any reflection-cache system, so `AreValidNameForType` is never called.
   Filling it in would have no effect.

### Phase 1 testing

**Automated** (`cli/tests`, plus `microservice/microserviceTests` for the runtime guard):
- `StorageNaming`: realm and zone prefixes, exactly 63 characters (passes), 64 (fails), and the
  message content.
- `FindTooLong` over a built `BeamoLocalManifest`:
  - realm and zone storages are each checked against the right scope,
  - zone storages are skipped when no local zid is set,
  - non-storage protocols are ignored,
  - several offenders are reported together.
- Runtime guard: `GetDatabaseByStorageName` throws the clear exception for a 64-character
  composed name and returns a database for a 63-character one.

**Manual** (in a test project logged into a real realm, plus a zone-bound realm):

| # | Scenario | Expected |
|---|---|---|
| 1 | `beam project new storage <name over budget>` (realm) | Fails before any files are created; the message shows the max for this realm |
| 2 | Same with `--zone` | Fails with the zone's (smaller) max |
| 3 | `beam project new storage <name at budget>` | Succeeds |
| 4 | Rename an existing storage by hand (folder/csproj/`BeamId`) to a name over budget, then run any command (`beam project ps`) | Fails at manifest init and lists the storage |
| 5 | Same as 4, then `beam project run` | Fails before any container starts |
| 6 | Zone storage with no local `zid`, realm bound to a zone, name fits the realm but not the zone; `beam deploy plan --scope zone` | Init passes; deploy fails before the build |
| 7 | Service started from the IDE with an `.env` that composes a 64-character name | First storage call throws the clear runtime message, not `InvalidNamespace` |
| 8 | Not logged in, `beam project new storage <long name>` | Warns and creates the storage |
| 9 | Rename back to a valid name | All commands work again |

**Done when:** automated tests pass, every manual scenario behaves as expected, and the PR
is merged. Then phase 2 starts.

---

## Phase 2: Backend (BeamableAPI, C#; authoritative)

This phase starts after phase 1 is verified. It enforces the same rule on the server, so
manifests posted without the CLI (Portal, direct API calls, older CLI versions) are rejected
too. The error message reuses the phase 1 wording.

### Changes

1. **Single source for the name.** `MongoAtlasAdmin` (which already owned the prefix) now has
   the public static `MicroStorageDatabasePrefix(RealmInfo)`, `MicroStorageDatabaseName(RealmInfo,
   storageId)` and `MaxDatabaseNameLength = 63`. `GrantStorageAccess` and
   `GetPerformanceByDatabase` use them, so the validation and the provisioning code can't drift
   apart.
2. **New error** `BeamableShared/Services/Beamo/Errors/StorageIdsTooLong.cs` (one error per file,
   following the repo convention): `BadRequest(nameof(StorageIdsTooLong), msg)` (HTTP 400). It
   lists every id that is too long, along with the scope and the maximum id length.
3. **Validation step** `EnsureStorageDatabaseNamesFit(info)` in
   `BeamableShared/Services/Beamo/Messages/PostManifest.cs`, called from `ThrowIfInvalid` right
   after `ManifestItemValidation.ValidateActive`. It checks every **active** (`!Archived`)
   storage reference against `info` (which already knows the realm or zone scope). This covers:
   - `POST /beamo/manifests` for both realms and zones (same endpoint, scope comes from the request),
   - promote (`PromoteCurrentManifestFromScope` goes back through `PostManifest`),
   - pull requests (`ValidateProposal` → `ThrowIfInvalid`).

   Archived references are skipped, so a manifest that already contains a bad name can still
   be fixed by archiving it.
4. **Bundle publish** (`BundleService.Publish`) gets no check (see Decisions).

### Phase 2 testing

- `BeamableShared.Tests/.../PostManifestRequestTest.cs`: `WhenStorageIdIsTooLong_ThrowsStorageIdsTooLong`,
  `WhenStorageIdIsAtTheLimit_DoesNotThrow`, `WhenTooLongStorageIdIsArchived_DoesNotThrow`,
  `WhenStorageIdFitsRealmButNotZone_ThrowsStorageIdsTooLong`.
- `BeamableGateway.Tests/Controllers/BeamoZoneManifestTest.cs`: a zone manifest whose storage
  id is one character over the zone's budget returns 400 with `Error == nameof(StorageIdsTooLong)`,
  and an id exactly at the budget is accepted (`PostManifest_ForZone_WithTooLongStorageId_ReturnsBadRequest`).
- Manual: post a manifest with a storage name that is too long directly against the local
  stack, skipping the CLI checks. Expect 400 `StorageIdsTooLong`.
- Before rollout: query Atlas for existing databases whose names are 64 characters or longer
  (there shouldn't be any, since Mongo rejects them) and active manifests with storage ids
  over budget. Because of decision 1, any such manifest will reject posts until it is fixed.

---

## Not doing

- No length limit in `ServiceName` itself. It is shared by microservices, portal extensions and
  groups, and the limit depends on the cid and scope, which `ServiceName` doesn't know.
- No change to how the name is composed, such as adding a separator or hashing long names.
  That would break existing databases.

## Decisions

1. Active storages with names that are already too long block every manifest post until they
   are archived or renamed.
2. When `new storage` runs without a cid/scope, it only warns.
3. Bundle publish has no check. A bundle's storages are deployed as part of a realm or zone
   deployment, so the creation, init, deploy and backend checks all cover them.
4. Local runs must fail on a name that is too long, the same as deploys (manifest init plus
   the runtime guard).
5. Phase 1 (CLI) ships and is verified before phase 2 (backend) begins.
6. Manifest init checks zone storages only against the **local** `zid`. It doesn't resolve the
   realm's bound zone (that takes an HTTP call) and doesn't assume a zid length.
   - Why not assume a length: a zid is `ZONE_` plus a `BeamIdGenerator` id, which gains digits
     over time. Zones created 2025-05-09 → 2026-09-16 have 22-character zids; zones created
     from 2026-09-17 → 2040-04-22 have 23; 24 is the maximum. A fixed assumption would either
     block every command over a valid name (if too strict) or miss some names (if too lenient).
   - Zone storages that init skips, or checks against a stale local zid, are caught by the
     deploy check (which has the resolved zid) and by the runtime guard.
