# Issue: `Context.Zid` is always null in a zone microservice

**Status:** fixed 2026-10-13 (`Zid` added to `Copy()`; `MicroserviceArgsCopyTests`). Verified on the local
stack: zone `Diagnostics` returns `zid=ZONE_104688289826486272`; `RealmDiagnostics`/`RealmRoundTrip` (socket and
signed) still pass.
**Found:** 2026-10-13, while testing `Services.Secrets` (see `microservice-secrets-api-plan.md`).

## What it is

In a `ZoneMicroservice`, `Context.Zid` (`ZonedRequestContext.Zid`) is `null` on every request, even
though the service is correctly running in its zone.

Zone requests still go to the right scope, because the requester uses `RequestContext.Pid`, and a
zone service carries its zone id in the PID slot (`ZONE_<zid>`). So nothing breaks for SDK calls.
Any user code that reads `Context.Zid`, though, gets `null`.

## Root cause

`MicroserviceArgs.Copy()` does not copy `Zid`.

- `microservice/beamable.tooling.common/Microservice/MicroserviceArgs.cs:76-130`: `Copy()` builds a
  new `MicroserviceArgs` field by field (about 50 fields) and never sets `Zid`.
  - `MicroserviceArgs.Zid` is a settable property (`:20`).
  - `EnvironmentArgs.Zid` reads the `ZID` env var (`:174`).
- Startup copies the args several times:
  - `MicroserviceStartupUtil.cs:88`: `configurator.Args.Copy()` → `startupCtx.args`;
  - `:202`: `startupCtx.args.Copy(...)`;
  - `:215`: per-instance `args.Copy(...)`.

  After the first copy, `Zid` is `null` for good.
- `microservice/beamable.tooling.common/Microservice/RouteSourceUtil.cs:33` builds the request
  context from the copied args:
  `new ZonedRequestContext(args.CustomerID, args.Zid, ...)` → `Zid = null`.
- `ZoneServicesImpl.RealmScopeFactory` (`realmEnv = _env.Copy(a => a.ProjectName = pid)`) also drops
  it. That's harmless there, since the fork is a realm scope.

The env side is fine:
- `GenerateEnvFileCommand.cs:415-418` sets `PID = ZID = <zoneId>` for zone services;
- `RunProjectCommand.cs:108` sets `ZID` for containers.

## How to reproduce

**Test bed in agentic-portal:**
1. From `agentic-portal/`, run `dotnet beam project run --ids SecretsTestZone secrets-test --detach -q`.
2. Open `http://localhost:4950/beam-project/zones/ZONE_101282411458731008/secrets-test`.
3. Click **Diagnostics** in "Zone service". The result shows `zid=-` (null), while
   `X-BEAM-SCOPE pid slot=ZONE_101282411458731008`.

**Or in any zone service:** `[ServerCallable] public string Zid() => Context.Zid;` returns `null`.

**Unit-level:** `new MicroserviceArgs { Zid = "z" }.Copy().Zid == null`.

## Fix

1. Add `Zid = args.Zid,` to the initializer in `MicroserviceArgs.Copy()`.
2. Add a test in `microservice/microserviceTests`: `Copy()` preserves `Zid`.
   - Better, add a reflection test that every public settable property of `MicroserviceArgs`
     survives `Copy()`, so the next new arg can't be dropped silently.
   - No test currently covers `Copy()`. `TestArgs.cs:186` has `Zid = "testzid"`, which could feed
     a `RouteSourceUtil` test asserting `ZonedRequestContext.Zid`.
3. Rebuild the local packages (`./dev.sh`) and re-run the Diagnostics repro. `zid` should show the
   zone id.

## Notes

- **Value format:** `ZONE_<zid>` vs bare zid. `GenerateEnvFileCommand` writes the same value to
  `PID` and `ZID` (`zoneId`). The PID slot shows `ZONE_101282411458731008`, so `Context.Zid` will
  probably carry the `ZONE_` prefix too. Decide whether `ZonedRequestContext.Zid` should strip it
  for consistency with the portal's numeric zone ids.
- **Related TODOs:** `MicroserviceAuthenticationDaemon.cs:215/232/257` (`TODO(zones)`) also read
  `_env.Zid`. Those read the original env object, so they aren't affected by `Copy()`. Note them
  when touching this area.
