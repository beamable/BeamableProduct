# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Load shedding: a service instance now answers with a `503` (`serviceBusy`) instead of queueing when more than `BEAM_MAX_CONCURRENT_REQUESTS` (default 500) client requests are in flight, so the gateway retries on another instance right away. `0` disables the limit.
- Requests that are already past the deadline the gateway stamped on them (`X-BEAM-DEADLINE`) are dropped without doing any work, since the gateway has already timed them out. `BEAM_DISABLE_STALE_REQUEST_DROP=true` restores the old behaviour. The deadline also sizes the request's cancellation token.
- Requests from the service to the platform time out after `BEAM_PLATFORM_REQUEST_TIMEOUT_SECONDS` (default 30) instead of waiting forever for a response that will never arrive. `0` disables the timeout.
- The thread pool minimum is raised to `BEAM_MIN_THREADPOOL_THREADS` (default 32) worker and IO threads. On a 0.25 vCPU task the runtime default is a single thread, which let one blocking call stall every other request.
- On `net9.0` and later the websocket aborts and reconnects when the gateway does not answer a ping within `WS_KEEP_ALIVE_TIMEOUT_SECONDS` (default 30), instead of waiting for the OS to give up on a dead TCP connection. `0` disables the check.

### Changed

- A request received while the instance is draining for shutdown is answered with a `503` (`draining`) instead of being silently dropped.
- Reconnecting to the gateway starts after 250ms (with jitter) instead of 5 seconds, since every second offline is lost capacity.
- Writes to a websocket that has closed fail immediately instead of hanging the handler that produced them; pending platform requests are failed when their connection is lost, so request handlers awaiting them can finish.
- The websocket read loop no longer slows down when the process is busy. Throttling reads never reduced the work to be done, it only pushed it past the gateway's deadline; overload is handled by the explicit load shedding above.
- A request the gateway rejects with a `403` re-authenticates and retries at most 3 times before the error is surfaced, instead of retrying forever.
- Replace `xunit` dependency with `xunit.v3.mtp-off` in version `4.0.1`.
- Update `Microsoft.CodeAnalysis.CSharp` dependencies to `4.8.0`.
- Update `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing` dependencies to `1.1.3`.
- Update `Microsoft.CodeAnalysis.CSharp.CodeFix.Testing` dependencies to `1.1.3`.
- Update Microservices `OpenTelemetry` dependencies to `1.18.0`.

### Fixed

- The shutdown handler registered for `ProcessExit` ran asynchronously and returned at its first `await`, so the process could exit in the middle of the drain. It now blocks until the drain completes.
- Receiving a message larger than 85KB no longer schedules a full compacting garbage collection.
- `Services.Inventory.GetCurrent()` and `GetCurrent("")` now omit the empty scope query parameter, which could leave inventory requests pending on runtimes 7.1.0 through 7.2.3. `GetCurrent(null)` also treats the scope as omitted instead of throwing before sending the request.

## [7.2.3] - 2026-08-26

### Added

- Cache OpenAPI docs schema on first use.

## [7.2.1] - 2026-06-30

### Fixed
- Microservice content fetches now retry with bounded exponential backoff and jitter. This applies to both manifest and content entry fetches.

## [7.2.0] - 2026-05-23

### Changed
- `Context.Headers` are not case-sensitive
- Changed `Func<ILogger> IBeamServiceConfig.LogFactory` to `Action<ILoggingBuilder> IBeamServiceConfig.AddLoggerProvider`, which enables to override the C#MS logs to your own implementation of `ILoggerProvider`

### Fixed
 - Local running microservice failing to get otel credentials when otel is enabled for local development

## [7.0.0] - 2026-02-19
### Added
- `net10` support
- Zstandard (zstd) compression for WebSocket messages between microservices and the gateway, negotiated automatically during authentication

### Changed
- Lowered log level of `/docs` endpoint from `DEBUG` to `TRACE` [4461](https://github.com/beamable/BeamableProduct/issues/4461)

### Fixed
- Cannot add duplicate custom telemetry attributes [4352](https://github.com/beamable/BeamableProduct/issues/4352)
- Warning log is only printed _once_ when a Microservice sets the `DisableAllBeamableEvents` flag.  

## [6.1.0] - 2025-10-31
### Fixed
- Call for `Services.Stats.GetStats( StatsDomainType.Game, StatsAccessType.Private, Context.UserId)` no longer yields `NoReadAccess`error

### Changed
- New msbuild property `BeamLogProvider` allows microservice switch between Cloudwatch and Clickhouse logs providers.

## [6.0.0] - 2025-10-14

### Added
- Microservices can source `[Callable]` methods from multiple base classes
- Microservice initialization flow uses builder pattern
- Microservice support dynamic log level configurations per request 
- Microservices include Open Telemetry collector as part of the build process

### Fixed
- Project references without assembly names no longer cause `NRE`
- `StringBuilderPool` no longer throws rare concurrency exception in high traffic cases
- new projects include `.gitignore` and `.dockerignore`

### Changed
- Microservices use new Open Telemetry log system
- Local development uses the developer's access token to authenticate the Microservice
- `Log.Verbose` is obsolete. Please use `Log.Trace` instead.
- `Log.Fatal` is obsolete. Please use `Log.Critical` instead.
- Source generator no longer treats non-partial Microservice classes as an error.
- Source generator no longer treats multiple Microservice classes as an error.
- new projects no longer require `[Microservice]` attribute

## [5.4.0] - 2025-08-27
no changes

## [5.3.0] - 2025-08-05
no changes

## [5.2.0] - 2025-07-30
### Added
- `BeamableBootstrapper.AdjustWorkingDirectory<T>()` function will auto-correct working directory for Microservices

### Fixed
- `Lobby` type includes `.data` field when used in `[ClientCallable]` methods.


## [5.1.0] - 2025-07-23
### Fixed
- Non `async` Callables can return `Task` types [4156](https://github.com/beamable/BeamableProduct/issues/4156)

## [5.0.3] - 2025-06-24
no changes

## [5.0.2] - 2025-06-23
no changes

## [5.0.1] - 2025-06-18
no changes

## [5.0.0] - 2025-06-06

### Fixed
- Callable methods can have `void` signatures
- Scheduler can invoke callable methods that return `Promise` with more than 1 argument

### Added
- Beam scheduler jobs have a new `SuspendedAt` property
- `BeamScheduler.GetSuspendedJobs()` returns recently suspended jobs
- Beam scheduler jobs can be unique
- Add the ability to use remote storage in local microservices. Can be done by calling `MicroserviceBootstrapper.ForceUseRemoteDependencies<TMicroservice>()` in the `Program.cs` right after the call for `await MicroserviceBootstrapper.Prepare<TMicroservice>()`.
- Callable methods' `Context` field have access to `AccountId`, `GamePid`, and `BeamContext` properties.
- Microservices can emit open telemetry data when standard OTLP environment variables are enabled

### Changed
- `BeamScheduler.GetJobs` is obsolete and `GetAllJobs` should be used instead
- `BeamScheduler.GetJobActivity` is obsolete and `GetAllJobActivity` should be used instead
- Microservice logging uses `Zlogger` instead of `Serilog`

## [4.3.0] - 2025-05-08

### Added
- `[Callable]` methods have access to a `SignedRequester` property which may execute signed requests.

### Fixed
- `Analytics.SendAnalyticsEvent` calls now use signed requests 

## [4.2.0] - 2025-04-04
### Fixed
- Fix local routing key in ServiceCallBuilder

## [4.1.5] - 2025-03-26
### Fixed
- SSL bypass logic allows for custom host string containing `/` characters 

## [4.1.4] - 2025-03-20
no changes

## [4.1.3] - 2025-03-07

### Fixed
- Content preloading uses configured host string in SSL logic, rather than hard coding "beamable.com"

## [4.1.0] - 2025-02-20

### Fixed
- Content can be loaded from referenced assemblies [#3888](https://github.com/beamable/BeamableProduct/issues/3888)

## [4.0.0] - 2025-01-24

### Added
- Providing `BEAM_REQUIRE_PROCESS_ID` environment variable will cause microservice to quit when configured process terminates. [#3839](https://github.com/beamable/BeamableProduct/issues/3839)
- Player initialization support through `IFederatedPlayerInit` interface. [#3838](https://github.com/beamable/BeamableProduct/issues/3838)
- Added `CallableFlags` to all `Callable` attributes. You can now use this to opt-out of client-code generation for specific `Callables` (mostly used for hiding `AdminOnlyCallables` from client source). 

### Changed
- Updated MongoDb.Driver reference to 2.19.2 in accordance with [known security vulnerability](https://github.com/advisories/GHSA-7j9m-j397-g4wx)

### Removed
- No longer support net6.0 or net7.0

## [3.0.1] - 2024-12-09

### Fixed
- Fixed issue that caused the OAPI Generation to fail with an exception if you put `BeamGenerateSchema` on a type that was already used in a `Callable` signature.
  - This implies that you can now reuse `BeamGenerateSchema` types for both custom notifications AND `Callable` signatures.

## [3.0.0] - 2024-12-04

### Added
- `Net8.0` support
- Microservices can accept federation traffic locally without needing to be deployed
- Microservices can have local developer settings in the `.beamable/temp/localDev` folder
- Microservice federations must exist in a special config file, `federations.json`
- Added `BeamGenerateSchemaAttribute` that can be used to include custom types declared inside microservices in the service's OpenAPI spec's schema list.
  This includes the type in the list of types generated by client-code generation.
  This can be used to avoid having to sync schemas between Unreal (or any non-C# product) and the services/storages that use them.
- `admin/metadata` route will return sdk version and other metadata about a running service.

### Changed
- Dockerfiles are simplified. Deployment builds occur on the host machine and are copied into build image.
- Microservices classes must be marked with the `partial` keyword
- `IThirdPartyCloudId` renamed to `IFederationId`
- `IThirdPartyCloudId` subtypes require a `[FederationId]` attribute with a string argument equal to the old `.UniqueName` property
- Microservice projects should reference `Beamable.Microservice.SourceGen`
- Microservice uses `routingKey` instead of `prefix` for HTTP routing
- `AssumeUser` was renamed to `AssumeNewUser`
- `RequestDataHandler` was renamed to `UserRequestDataHandler`

### Fixed
- services running locally will use adaptive port bindings to avoid port collisions
- JoinTournaments api now propagates the tournamentId to the inner GetPlayerStatus call, which reduces the amount of data fetched
- calling `Context.Services.Inventory.GetItems()` will no longer break when items reference deleted content
- Microservices have improved thread-safety when sending messages to Beamable.
- `AssumeUser` now returns a disposable object, memory usage improvements
- `[Callable]` methods no longer produce `AccountNotFoundError` errors when emitting Beamable API calls with valid playerIds.
