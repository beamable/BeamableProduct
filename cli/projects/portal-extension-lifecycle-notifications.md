# Portal Extension lifecycle notifications

## Context

When a portal extension's source files change, `PortalExtensionObserver.OnChanged`
broadcasts a `notify-portalextension` websocket message so the portal web app refreshes
its extension bundle (`cli/cli/Services/PortalExtension/PortalExtensionDiscoveryService.cs:445`).

Today that broadcast **only** happens on a file change. The portal has no signal when an
extension **starts** (so it can mount/refresh) or **stops** (so it can unmount) — it only
finds out lazily via service discovery timeouts. We want the same notification fired on
start and graceful stop, tagged with the lifecycle event so the portal can react correctly.

### Key constraints discovered

- `NotifyServer` only works while the microservice's websocket is alive.
- A portal extension runs **in-process** under `beam project run`; `beam project stop` is a
  separate process that hits `/stop` (→ `Environment.Exit(0)`, the default) or `Process.Kill()`.
- In `BeamableMicroService.OnShutdown` the websocket stays alive from `IsShuttingDown = true`
  (line 249) through line 311; `_connection.Close()` is line 312. `RemoveService()` only
  unregisters routes — the socket requester is still usable. So a shutdown hook invoked early
  in `OnShutdown` can `await` a `NotifyServer` before the socket closes.
- `Environment.Exit(0)` (graceful `/stop`, and the parent-exit path in `RunProjectCommand:245`)
  fires `AppDomain.ProcessExit` → `OnShutdown`. **Force-kill (`stop -k` → SIGKILL) cannot be
  intercepted** — accepted limitation, documented; portal drops it via discovery.
- `READY_FOR_TRAFFIC` (the existing in-process `onProgress(1f)` signal) is the correct moment
  for the "started" notification — that's when the service can actually serve
  `RequestPortalExtensionData`.

## Decisions

- **Start + graceful stop** are covered. Force-kill is **not** (documented limitation).
- Add an **`eventType`** string field (`started` / `stopped` / `changed`) to the payload.
- Add a reusable **`OnShutdown` hook** to the `BeamServer` builder (general-purpose, used here
  for the stop notification). This is the mechanism the rest of the plan hangs off of.

---

## Changes

### A. Microservice runtime — add an `OnShutdown` hook to `BeamServer`

**`microservice/microservice/dbmicroservice/MicroserviceBootstrapper_Config.cs`**
- `IBeamServiceConfig`: add `List<Func<IDependencyProviderScope, Task>> ShutdownHandlers { get; set; }`.
- `BeamServiceConfig`: implement it, initialized to `new List<...>()` (mirror `ServiceInitializers`).
- `BeamServiceConfigExtensions`: add
  `OnShutdown(this IBeamServiceConfig conf, Func<IDependencyProviderScope, Task> handler)`
  that appends to the list (mirror `InitializeServices`).
- `BeamServiceConfigBuilder`: add fluent overloads
  `OnShutdown(Func<IDependencyProviderScope, Task>)`, `OnShutdown(Func<Task>)`, `OnShutdown(Action)`
  (mirror the existing `InitializeServices` overloads).

**`microservice/microservice/dbmicroservice/MicroserviceStartupUtil.cs`**
- In `Begin(...)`, copy `configurator.ShutdownHandlers.ToList()` into `StartupContext`
  alongside the existing `initializers = configurator.ServiceInitializers.ToList()` (line ~99).

**`StartupContext`** (same file/region as `initializers`, `perServiceInitializers`)
- Add `List<Func<IDependencyProviderScope, Task>> shutdownHandlers`.

**`microservice/microservice/dbmicroservice/BeamableMicroService.cs`**
- In `OnShutdown` (line 247), immediately after `IsShuttingDown = true;` (line 249) and before
  the rest of the teardown, invoke the handlers while the socket is still live:
  ```csharp
  foreach (var handler in _startupContext.shutdownHandlers)
  {
      try { await handler(Provider); }
      catch (Exception ex) { Log.Warning("Shutdown handler failed: {message}", ex.Message); }
  }
  ```
  Use the same provider scope that `ServiceInitializers` receive. Handlers must be awaited so
  the `NotifyServer` send completes before `_connection.Close()` (line 312).

### B. CLI — notification payload + observer helpers

**`cli/cli/Services/PortalExtension/PortalExtensionDiscoveryService.cs`**
- `PortalExtensionNotifyPayload`: add `public string eventType;`. Define event-type string
  constants (`"started"`, `"stopped"`, `"changed"`) on the observer (or a small static holder).
- Extract the existing `_notificationsApi.NotifyServer(...)` block (lines 445–451) into:
  ```csharp
  private Promise<EmptyResponse> NotifyPortalExtension(string eventType)
  ```
  building the payload (now including `eventType`) and returning the `NotifyServer` promise.
  Guard against a null `_notificationsApi`.
- `OnChanged` (line 442 area): call `NotifyPortalExtension("changed")` (fire-and-forget, as today).
- Add `NotifyStarted()` → `NotifyPortalExtension("started")`, guarded by a `_startedNotified`
  bool so it fires once.
- Add `NotifyStopped()` → returns `NotifyPortalExtension("stopped")` so the shutdown hook can
  `await` it.

### C. CLI — wire start + stop in the run path

**`cli/cli/Services/BeamoLocalSystem_PortalExtension.cs` (`RunMicroserviceForever`)**
- **Start:** the `started` notification must be sent **only when the service is ready for
  traffic** (`READY_FOR_TRAFFIC` / `onProgress(1f)`) — never at observer init. Two distinct steps:
  - **Capture (plumbing, sends nothing):** declare `PortalExtensionObserver capturedObserver = null;`
    in the method scope, and in `InitializeServices` set `capturedObserver = observer;` after the
    observer is resolved (line 65). This only grabs a reference — `InitializeServices` is the single
    place the observer is resolvable from the DI scope. No notification is broadcast here.
  - **Fire at readiness:** wrap `onProgress` so the notification goes out strictly when the
    readiness signal arrives:
    ```csharp
    void OnProgress(float p, string m)
    {
        onProgress?.Invoke(p, m);
        if (p >= 1f) capturedObserver?.NotifyStarted(); // only at READY_FOR_TRAFFIC
    }
    ```
    Pass `OnProgress` into `AddPortalExtensionProvider(...)` (line 130) in place of `onProgress`.
    (`NotifyStarted()` is itself guarded by `_startedNotified` so it fires exactly once.)
- **Stop:** add to the builder chain (e.g. after `OverrideConfig`, before `RunForever`):
  ```csharp
  .OnShutdown(async provider =>
  {
      var observer = provider.GetService<PortalExtensionObserver>();
      if (observer != null) await observer.NotifyStopped();
  })
  ```

### D. Document the force-kill limitation
- Brief comment in `StopProjectCommand` (the `kill` branch, line ~116) and/or near the
  `OnShutdown` wiring: force-kill (`--kill-task`) sends SIGKILL and cannot notify the portal;
  portal drops the extension via service discovery.

---

## Out of scope / coordination
- The **portal web app** (separate repo) must learn to read `eventType` to mount/unmount/refresh
  intelligently. Until then the new field is ignored harmlessly (portal re-fetches as today).
- No CLI-side posting to `/basic/notification/service` (avoided the user-token-auth question).

## Verification
1. `dotnet build cli.sln` and build the microservice runtime; `dotnet test tests/` (no regressions).
2. From repo root `BeamableProduct/`: `./dev.sh`, then `beam project run` a portal extension.
   - Confirm a `started` `notify-portalextension` broadcast fires at READY_FOR_TRAFFIC
     (add a temporary log in `NotifyPortalExtension`, or observe the portal mounting).
3. Edit a `.tsx`/`.ts` source file → confirm a `changed` notification still fires.
4. `beam project stop` (graceful) → confirm a `stopped` notification fires before the socket
   closes (temporary log / portal unmount).
5. `beam project stop -k` (force-kill) → confirm no notification (expected limitation).