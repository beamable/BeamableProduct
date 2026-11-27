# Pre-resolve local storages in `beam project run`

## Problem

Today storages are started lazily, **per microservice, in a grandchild process**:

```
beam project run --ids A,B
  └─ dotnet run A                     (RunProjectCommand.RunService)
       └─ beam project generate-env A --auto-deploy   (MicroserviceStartupUtil.GetLocalEnvironment)
            ├─ GetLocalConnectionString(storage)  → InspectContainer → throws if missing
            └─ catch when autoDeploy → SynchronizeInstanceStatusWithDocker + DeployToLocal(deps)
                                        → RunLocalEmbeddedMongoDb → CreateAndRunContainer
```

Consequences:

- **Race.** When A and B share storage `S`, both `generate-env` processes see `S` missing and both call
  `CreateAndRunContainer("S_mongoDb")`. One of them gets a 409 Conflict. The uncommitted change in
  `BeamoLocalSystem_Docker.cs` works around this after the fact (on a conflict it finds the container, reuses
  it or deletes and recreates it).
- **Repeated Docker work.** Every service's `generate-env` pings Docker, inspects each storage, and on a
  miss runs `SynchronizeInstanceStatusWithDocker`. That call inspects the image of *every* service
  definition one after another, then lists all containers. With ~30 services this adds up.
- **Discovery happens late.** A missing storage only shows up after `dotnet run` has restored, built and
  started the service. Errors show up late and get mixed in with that service's log.

## Goal

`beam project run` finds out **up front** which storages the selected services need, checks them all
with **one** Docker list call, starts the missing ones **in parallel (one task per storage, no
duplicates)**, and passes the resolved connection strings to each service through env vars. The
service's `generate-env` then uses those values and never touches Docker for storages.

## Design

### 1. Work out the required storages (`RunProjectCommand.Handle`)

This runs after `serviceTable` is built and before the launch loop:

- `requiredStorages` = union of `BeamoLocalSystem.GetDependencies(id)` over every `HttpMicroservice` in
  `serviceTable`, plus any `EmbeddedMongoDb` ids passed directly in `--ids`.
- Keep a `storage → [dependent services]` map. It's used for progress and failure reporting.
- If `requiredStorages` is empty, skip the whole step (no Docker ping). This keeps today's behavior for
  projects without storages and for machines without Docker.
- If it isn't empty, run `CheckIsRunning()` once. If Docker is down, throw `CliExceptions.DOCKER_NOT_RUNNING`
  right away, before any `dotnet run`. Today this error only comes out of `generate-env`.

### 2. Check all storages with one call (new `BeamoLocalSystem.GetLocalStorageStates`)

New method in `BeamoLocalSystem_EmbeddedMongoDb.cs`:

```csharp
public async Task<Dictionary<string, LocalStorageState>> GetLocalStorageStates(IEnumerable<string> storageIds)
```

- **One** `_client.Containers.ListContainersAsync(new() { All = true, Filters = name=[each "<id>_mongoDb"] })`.
  Docker ORs several values of the same filter key, so one request covers every storage.
- The name filter matches substrings, so filter again on the exact name `"/" + GetBeamIdAsMongoContainer(id)`.
  This is the same care `FindContainerByName` takes.
- For each storage, return `{ Status = Running | Stopped | Missing, ContainerId, Image, HostPort }`.
  `HostPort` comes from `ContainerListResponse.Ports` (PrivatePort 27017 → PublicPort), so no extra inspect
  is needed.
- Put the matching in a pure static helper
  (`MatchStorageContainers(IList<ContainerListResponse>, IEnumerable<string>)`) so it can be unit-tested
  without Docker.
- This does **not** go through `BeamoRuntime.ExistingLocalServiceInstances` or
  `SynchronizeInstanceStatusWithDocker`. Docker is the source of truth, and we avoid inspecting every image.

No image comparison. A running container counts as ready whatever image it runs, the same as today's
`generate-env` path, which only inspects the port. (The Conflict reconcile would reuse a running container
anyway, so treating an image mismatch as Stopped would never actually recreate it.)

### 3. Start what's missing, in parallel (new `BeamoLocalSystem.EnsureLocalStorage`)

For each storage that isn't `Running`, start **one** task (`Task.WhenAll`). Each task:

1. Runs `PrepareBeamoServiceImage(sd, …)`. This pulls the mongo image if needed, same as `DeployToLocal`.
2. Runs `CreateAndRunContainer(...)` with the same arguments `RunLocalEmbeddedMongoDb` builds today.
   - Refactor: pull the argument-building out of `RunLocalEmbeddedMongoDb` into
     `BuildEmbeddedMongoContainerSpec(sd, protocol)`. `RunLocalEmbeddedMongoDb` keeps its current
     error-swallowing behavior for its other callers. `EnsureLocalStorage` uses the spec and **lets
     exceptions propagate**, so we can tell which storage failed.
3. Reads the host port with `GetStorageHostPort` (inspect), the same readiness check `generate-env` uses
   today (see Decision 3).

Storages that are already `Running` are left alone. They're not restarted, recreated or inspected again.

**Keep the uncommitted 409-Conflict reconcile in `CreateAndRunContainer`** as a safety net. It still
covers paths that don't go through `project run`: an IDE/`dotnet run` launch of two services at once
(each runs `generate-env --auto-deploy`), and `beam local up` steps that run separate `project run`
processes at the same time.

### 4. Pass the result to the service

Build the connection string with the same formula as `GetLocalConnectionString`
(`mongodb://{user}:{pass}@localhost:{hostPort}`). Put the formula in a small shared static so the two
can't drift.

For each `HttpMicroservice` we launch, add one env var per dependent storage to the `envVars` dictionary in
`RunService`:

```
BEAM_LOCAL_STORAGE_CONNSTR_<storageName>=mongodb://…@localhost:<port>
```

Child processes inherit env: `generate-env` is started by the microservice with `UseShellExecute=false` and
only *adds* vars to `StartInfo.EnvironmentVariables`. So the value reaches `generate-env` with **no
microservice runtime changes**.

- The constant goes CLI-side (e.g. `cli.Services.BeamoLocalSystem.ENV_PRERESOLVED_STORAGE_PREFIX`), **not** in
  `Beamable.Common` `Constants`. That keeps Common untouched and avoids the Unity CI shared-code pin problem.
- `RunService` needs the resolved map. Pass it as a new optional parameter,
  `IReadOnlyDictionary<string,string> preresolvedStorageEnv = null`. `RunService` is `public static` and
  has other callers, so the parameter must be optional.

### 5. Use the passed values in `generate-env` (`GenerateEnvFileCommand`)

In `AppendDependencyVars`, for each dependency:

- If `BEAM_LOCAL_STORAGE_CONNSTR_<name>` is set and non-empty, emit `STORAGE_CONNSTR_<name>` with that value
  and **skip** `GetLocalConnectionString`.
- Otherwise, fall back to today's path (inspect, then `--auto-deploy` → `DeployToLocal`).

Also, if *every* dependency was pre-resolved, skip the `CheckIsRunning()` Docker ping.

`--use-remote-deps` still wins, because `AppendDependencyVars` isn't called at all in that case. So a
service that uses `ForceRemoteStorage()` ignores the passed vars and connects remotely, as it does today.

### 6. Progress, failures, `--force-restart`

- Storage ids get `SendUpdate` progress ("checking storage…", "starting storage…", 1.0 when ready). This
  replaces the current `EmbeddedMongoDb` case in the launch loop, which is removed because storages passed
  in `--ids` go through the new pre-step.
- Services wait for the pre-step before the launch loop. The step runs in parallel and is usually a single
  list call, so it's cheap.
- **A storage that fails to start** fails only the services that depend on it. They get a terminal
  `SendUpdate(..., "failed: storage X …", 1)` and an entry in `failedTasks`, and aren't launched. Other
  services start as normal.
- `--force-restart` stops *services* (`DiscoverAndStopServices`), not storages. The pre-step runs after it and
  is unaffected.

## Files touched

| File | Change |
|---|---|
| `cli/Commands/Project/RunProjectCommand.cs` | Pre-step: collect storages, Docker check, resolve/ensure in parallel, pass env map into `RunService`; remove the `EmbeddedMongoDb` launch case |
| `cli/Services/BeamoLocalSystem_EmbeddedMongoDb.cs` | `GetLocalStorageStates`, `MatchStorageContainers` (pure), `EnsureLocalStorage`, `BuildEmbeddedMongoContainerSpec`, shared connection-string builder, env prefix constant |
| `cli/Services/BeamoLocalSystem_HttpMicroservice.cs` | `GetLocalConnectionString` uses the shared connection-string builder |
| `cli/Commands/Project/GenerateEnvFileCommand.cs` | Use `BEAM_LOCAL_STORAGE_CONNSTR_*` before Docker; skip the Docker ping when everything is pre-resolved |
| `cli/Services/BeamoLocalSystem_Docker.cs` | Keep the current uncommitted conflict reconcile (safety net) |

No changes to `microservice/` or `beamable.common/`.

## Tests

- `MatchStorageContainers`: exact name vs substring (`S_mongoDb` vs `S2_mongoDb`), running / exited /
  missing, image mismatch → Stopped, port mapping extraction.
- `generate-env`: with `BEAM_LOCAL_STORAGE_CONNSTR_S` set, the output contains `STORAGE_CONNSTR_S` with that
  value and makes no Docker calls. With `--use-remote-deps`, the pre-resolved var is ignored.
- Manual (`dev.sh` + a project with 2 services sharing one storage, plus a third storage):
  1. Nothing running → exactly one container per storage is created, no 409 in verbose logs.
  2. All running → no creates; `generate-env` logs show no Docker inspect.
  3. One storage stopped → only that one is started.
  4. Docker stopped → `DOCKER_NOT_RUNNING` before any build.

## Decisions (reviewed)

1. **`ForceRemoteStorage()` services.** No change. `project run` creates storages up front even if their
   only dependents use remote storage. Those services still ignore the pre-resolved var because
   `--use-remote-deps` skips `AppendDependencyVars`.
2. **Trust the passed value.** `generate-env` does not re-check the container when
   `BEAM_LOCAL_STORAGE_CONNSTR_<name>` is set. If the container dies in the meantime, the service fails on
   first DB use, which is acceptable.
3. **Readiness uses the same check as today.** Today's `--auto-deploy` path waits for nothing beyond
   Docker: `CreateAndRunContainer` → `RunContainer` returns when `StartContainerAsync` reports true. Then
   `GetLocalConnectionString` reads the mapped 27017 host port with `InspectContainerAsync`
   (`GetStorageHostPort`). The pre-step does exactly that:
   - **Already running:** the container state is `running` and the list result has a host port for 27017.
     That's the same data `GetStorageHostPort` would read.
   - **Started by the pre-step:** `CreateAndRunContainer` returns true, then call `GetStorageHostPort`
     (inspect), exactly as `generate-env` does after `DeployToLocal`.
   - No TCP or mongo `ping` probe, and no Docker healthcheck wait. A running container with no mapped port
     fails the same way it does today ("could not get host port of storage=[X]…"), and only its
     dependent services fail.
