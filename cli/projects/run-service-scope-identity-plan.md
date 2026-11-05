# `beam project run` decides each service's scope and secret

## Problem

Realm microservices started in the same `beam project run` as a zone portal extension never authenticate
(`gateway/auth` returns 403 in a loop). This happens in agentic-portal on every run back to at least 09-18.

The chain:

1. Zone portal extensions (`core-hub-player-engagement`, `core-hub-data-platform`, …) run **in-process** in the
   CLI. `BeamoLocalSystem_PortalExtension.ApplyZoneEnvironment` writes `SECRET` (the zone secret), `PID` (the zid),
   `ZID`, `CID`, `HOST` and `BEAM_SKIP_LOCAL_ENV` into the **CLI process env**.
2. Realm microservices launched afterwards (`RunService` → `dotnet run`) inherit that env.
   `BEAM_SKIP_LOCAL_ENV` is already cleared (#4855); nothing else is.
3. Their `generate-env` overwrites `PID`/`CID`/`HOST` with realm values but emits no `SECRET` (unless
   `--include-secret` is passed). The inherited zone secret survives.
4. The SDK sees a non-empty `SECRET` and signs the realm pid with the zone secret. The gateway returns 403.

The same shared env also breaks realm and zone portal extensions running in-process together. Each one's
startup rewrites `PID`/`SECRET`/`BEAM_SKIP_LOCAL_ENV` for all of them, and the SDK reads these straight from
`Environment` (`EnvironmentArgs`).

## Backend facts (BeamableBackend / BeamableAPI)

- **The pid picks the secret.** `gateway/auth` (`ThoriumSocket.scala:390-408`) picks the secret from the pid:
  a `ZONE_` pid (`Zone.isZoneId`) is checked against `Zone.secret`, any other pid against that realm's
  `Project.secret`. There is no customer-level secret and no fallback. The auth body has no `zid` field, so a
  zone can only be named in the pid slot.
- **A realm secret can't authenticate a zone.** The server test `testZoneSignatureAuthRejectsProjectSecret`
  (realm secret on a zone pid) expects 403.
- **A zone secret can't authenticate a realm directly.** A realm context never has a zid. A zone-authenticated
  session *can* act on a bound realm through an `X-BEAM-SCOPE` override (`resolveScopeOverride`). The realm SDK
  path doesn't do that, and supporting it would mean an SDK change, so it's out of scope.
- **The secrets are independent.** Zone and realm secrets are independent GUIDs. Binding a realm to a zone
  doesn't touch either secret.

**Consequence:** one secret can never serve both scopes. A user-supplied `SECRET` override is valid for
exactly one scope.

## Goal

The run command decides, for **each** service it starts, which `CID` / `PID` / `ZID` / `SECRET` the service uses.
It sets them explicitly on that service's process. Rules:

- **Realm services:** `PID` = realm pid, `ZID` removed. `SECRET` = the user's override if one was given,
  otherwise removed (refresh-token auth, or `--include-secret` from `generate-env` as today).
- **Zone services:** `PID` = `ZID` = the resolved zid. `SECRET` = the user's override if one was given,
  otherwise the zone secret from `generate-env`, as today.
- **Override validation:** a `SECRET` override must be valid for every scope in the run. If it doesn't match,
  fail **before anything builds**, with an error naming the scope it doesn't fit.

No microservice SDK changes.

## Design

### 1. Capture the user's inputs at the start of `RunProjectCommand.Handle`

At the top of `Handle`, before any extension can write to the process env, read the user's `SECRET`.
Store it on the args, e.g. `args.secretOverride`. This is the only "override" source today: a `SECRET` env
var in the shell that launches `beam project run`.

### 2. Resolve the identities once (new `RunScopeIdentity` helper)

After `serviceTable` is built, and alongside `EnsureRequiredStorages`:

- `scopes` = the scopes of every service in the table. `serviceDef.IsZoneScoped` covers microservices. For
  extensions, use `PortalExtensionDefinition.Properties.ServiceScope == "zone"` (the same check
  `RunMicroserviceForever` makes). Storages are skipped.
- **Zone present:** resolve the zid once with `ZoneResolver.ResolveZid(provider, cid, pid, localZid)`. If none is
  found, fail with the same message `generate-env` uses today, but up front, before any build.
- **Override present:** check it against the scopes in the run:
  - realm scope present: fetch the realm secret (`IRealmsApi.GetAdminCustomer` → project `secret`) and compare.
  - zone scope present: fetch the zone secret (`IBeamZonesApi.GetZones(cid, zid).secret`) and compare.
  - Any mismatch throws a `CliException` before anything launches, for example:
    - `The SECRET override is not valid for zone [ZONE_x]: zone-scoped services (VipServiceZone, core-hub-…) authenticate with the zone secret. Remove the override or run only realm services.`
    - `The SECRET override is not valid for realm [DE_…]: realm-scoped services (PushMessageRailService, …) authenticate with the realm secret. …`
  - A mixed realm+zone run with an override **always** fails, because no one secret is valid for both. The
    error says so explicitly.
  - If the fetch fails because of permissions (401/403), fail with "cannot verify the SECRET override for
    [scope]: your account can't read that secret". Don't launch services that we know will loop on 403.
- The zone secret is fetched **only** when validating an override. Without an override, zone services keep
  getting it from their own `generate-env`, as today.

Output: `Dictionary<serviceId, ServiceIdentityEnv>`, where each value holds `CID`, `PID`, `ZID` (or none) and
`SECRET` (or none).

### 3. Apply the identity to each child process (`RunService`)

`RunService` already takes `additionalEnvVars` (added for storages). Pass the identity through it as well:
`CID`, `PID` and `HOST` explicitly; `ZID` and `SECRET` set or **removed**.

- `StartProcessUtil.Run` only sets keys today. Add support for removal: a `null` value means
  `psi.Environment.Remove(key)`. This is the difference between "the user wants no secret" and "inherit whatever
  the orchestrator's env currently holds".
- Why this is enough with no `generate-env` changes: `generate-env` runs as a child of the service and inherits
  these values. For realm services it emits `PID`/`CID` (the same values) and no `SECRET`/`ZID`, so what we set
  survives. For zone services it emits `PID`/`ZID`/`SECRET` = the zone values. When there's an override, step 2
  already proved it equals the zone secret, so both agree. `--include-secret` on a realm service likewise
  emits the realm secret, which equals the validated override.

### 4. Portal extensions stay in-process, each with its own args object (no process env writes)

Spawning another `beam` process per extension is too expensive, so extensions keep running in-process. They
stop reading their identity from the shared process env. The SDK already supports this:

- `IBeamServiceConfig.Args` is settable. It defaults to `new EnvironmentArgs()`, which reads the env live. At
  startup the SDK takes **one snapshot**, `configuredArgs = configurator.Args.Copy()`
  (`MicroserviceStartupUtil.cs` ~L86), and everything after that reads the snapshot: `startupCtx.args`, the
  auth daemon's `_env`, and the signed requester.
- With `SkipLocalEnv = true` on that object, the SDK skips `GetLocalEnvironment`. That removes the nested
  `beam project generate-env` subprocess each realm extension spawns today, which is the ~20s step, and the
  `new EnvironmentArgs()` re-read that follows it (L152-163).

So in `RunMicroserviceForever`, inside the existing `.OverrideConfig(...)`, set
`((IBeamServiceConfig)microserviceConfig).Args` to a per-extension object:

```csharp
new EnvironmentArgs().Copy(a =>
{
    // identity decided by the run command (step 2)
    a.CustomerID = identity.Cid;
    a.ProjectName = identity.Pid;          // realm pid, or the zid for a zone extension
    a.Secret = identity.Secret;            // override, the zone secret, or null (refresh-token auth)
    a.Host = identity.Host;
    a.NamePrefix = routingKey;
    a.BeamInstanceCount = 1;
    a.RefreshToken = appContext.RefreshToken;
    a.AccountId = ...; a.AccountEmail = ...;   // what generate-env emits today
    a.SkipLocalEnv = true;
});
```

- The values come from the same logic `generate-env` uses, called **in-process**. Extract the env-building part
  of `GenerateEnvFileCommand.GetResult` (admin/me, zone resolution + zone secret, otel creds) into a reusable
  service method. The otel collector credentials are the same for every realm extension, so they can stay as
  process env.
- **Delete** the process-global writes in `ApplyZoneEnvironment`. The zone secret it fetches goes into the
  zone extension's own args object instead. This also removes the `BEAM_SKIP_LOCAL_ENV` leak that #4855 works
  around in `RunService`.
- Because nothing writes identity into the CLI process env anymore, the orchestrator env stays clean for later
  commands, which also covers the CLI server case. Step 3 still sets identity explicitly on each `dotnet run`
  child, as a guarantee.

**SDK gaps found (not changed, pre-existing):**
- `MicroserviceArgs.Copy` doesn't copy `Zid`, so the snapshot's `Zid` is always null. Zone services still
  authenticate, because the zid rides the `PID` slot. But `ZonedRequestContext` (`RouteSourceUtil.cs:33`) gets a
  null `Zid` today as well. This plan doesn't make it worse. It's worth an SDK ticket.
- The otel path (`MicroserviceStartupUtil.cs` ~L326) builds `new EnvironmentArgs()` to fetch collector creds when
  none are in the env. It reads the process env, not the snapshot. This doesn't matter as long as the CLI keeps
  the collector creds in process env (they're realm-level and shared), and zone extensions skip otel as today.

### 5. `dotnet run` children (microservices)

These use step 3 unchanged: `RunService` sets `CID`/`PID`/`HOST`/`ZID`/`SECRET` explicitly on each child
process, removing the ones that shouldn't be there. This is plain `dotnet run` with per-process env, so no
`beam` overhead is added.

## Files touched

| File | Change |
|---|---|
| `cli/Commands/Project/RunProjectCommand.cs` | Capture the override; resolve identities and validate up front; pass the identity env to `RunService`; pass the identity to `RunLocalPortalExtension` |
| `cli/Commands/Project/RunProjectCommand.cs` (`ServiceScopeIdentity`, `RunScopeIdentity`) | Collect scopes, resolve the zid, validate the override, build per-service identity (pure matching logic kept testable) |
| `cli/Utils/StartProcessUtil.cs` | `null` env value removes the variable from the child |
| `cli/Services/BeamoLocalSystem_PortalExtension.cs` | Set a per-extension `Args` (with `SkipLocalEnv`) in `OverrideConfig`; remove the process-global writes in `ApplyZoneEnvironment` |
| `cli/Commands/Project/GenerateEnvFileCommand.cs` | No change. As implemented, extensions build their args directly in `BuildPortalExtensionArgs` (identity from `RunScopeIdentity`, plus `admin/me` for the account fields). The otel collector creds are fetched once per CLI process by `EnsureOtelCredentialsInEnvironment`, which is simpler than extracting generate-env's logic. |

No changes to `microservice/` or `beamable.common/`.

## Tests

- Unit (`RunScopeIdentity`, with mocked `IRealmsApi` / `IBeamZonesApi`):
  - realm-only, no override → realm identity, no SECRET, no ZID.
  - zone-only, no override → zone PID/ZID, no SECRET set by run (left to generate-env).
  - realm-only + realm-secret override → SECRET = override.
  - zone-only + zone-secret override → SECRET = override.
  - realm-only + zone-secret override → error naming the realm.
  - zone-only + realm-secret override → error naming the zone.
  - mixed + any override → error.
  - zone present, no zid resolvable → early error.
- `StartProcessUtil`: a `null` value removes an inherited variable.
- Manual (agentic-portal, `dev.sh`): full `project run` → the MessageRail services reach "ready for traffic",
  with no `gateway/auth` 403. `VipServiceZone` and the zone hubs still work. `SECRET=<realm secret> beam project
  run` over the mixed set → fails in under a second with the mixed-scope error.

## Decisions (reviewed)

1. **No extra `beam` processes.** Extensions stay in-process, and each gets its own `Args` object (step 4).
   Microservices stay plain `dotnet run` children with per-process env (step 5).

2. **Nothing replaces `--remove-all-except-pid` for extensions.** It already does nothing for in-process
   extensions:
   - The flag was added in `bd85e70d4` (#3682, "stop other running instances"). `generate-env` calls
     `StopProjectCommand.DiscoverAndStopServices({serviceId})` and skips the caller's own process id.
   - For an in-process extension, `serviceId` is `ctx.attributes.MicroserviceName` (`MicroserviceStartupUtil.cs:1055`).
     That's `BeamPortalExtension_{name}_{Guid}` (`PortalExtensionRuntime`), a fresh GUID per run.
   - The match (`StopProjectCommand.cs:66-72`) needs `service.service == serviceId`, or the regex's `serviceName`
     group (`{name}`) `== serviceId`. Another instance has a different GUID, and `{name}` never equals the full
     GUID name, so nothing ever matches.
   - Stopping extensions on purpose is still `project run --force-restart`, which matches on the beamo id.
   - The otel creds that `generate-env` also fetched are shared and realm-level. The run fetches them once
     in-process.
