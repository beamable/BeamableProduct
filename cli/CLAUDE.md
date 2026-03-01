# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

This is the **Beamable CLI** (`beam`), a .NET tool (packaged as `Beamable.Tools` on NuGet) that manages Beamable resources—Content, Microservices, Realms, Auth, and more. The CLI also generates client SDKs for Unity and Unreal Engine from Beamable's OpenAPI specs.

## Commands

All commands run from the repo root (`/BeamableProduct/cli/`):

```bash
# Build
dotnet build cli.sln

# Run CLI locally
dotnet run --project cli/cli -- <beam-args>

# Run all tests
dotnet test tests/

# Run a single test by name
dotnet test tests/ --filter "FullyQualifiedName~TestName"

# Run tests matching a pattern
dotnet test tests/ --filter "TestName=NewProject_AutoInit"

# Pack the CLI as a NuGet tool
dotnet pack cli/cli/
```

The SDK requires .NET 10 (see `global.json`). The CLI targets net8.0, net9.0, and net10.0.

### Manual / end-to-end testing

To test changes against a real project (Unity, Unreal, etc.), run the `dev.sh` script from the **repo root** (`/BeamableProduct/`), not the `cli/` subdirectory:

```bash
cd ..   # from cli/ go up to BeamableProduct/
./dev.sh
```

Run `./setup.sh` once before the first `dev.sh` invocation. `dev.sh` builds all packages, publishes them to a local NuGet feed, installs the CLI globally (`beam`), and restores downstream projects so they pick up your local changes. It accepts flags to skip specific targets:

```bash
./dev.sh --skip-unity    # skip Unity SDK update
./dev.sh --skip-unreal   # skip Unreal SDK update
```

## Architecture

### Solution structure

- `cli/cli.csproj` — The main CLI tool; command name `beam`
- `tests/` — NUnit test project for the CLI
- `beamable.common/` — Shared library (netstandard2.0); also shipped as `Beamable.Common` NuGet package and vendored into the Unity SDK
- `beamable.server.common/` — Shared server-side types (netstandard2.0)
- `beamable.otel.exporter/` — OpenTelemetry exporter
- `beamable.templates/` — dotnet new templates for Beamable microservice/storage projects

The solution also references projects from sibling directories: `../microservice/` (the microservice runtime) and `../templates/` (source generators).

### Command framework

All commands live under `cli/Commands/` and inherit from one of:

- **`AtomicCommand<TArgs, TResult>`** — Returns a single structured result via `GetResult(args)`. The framework automatically reports it to `IDataReporterService`.
- **`StreamCommand<TArgs, TResult>`** — Streams multiple results via `SendResults(result)`. Used for long-running or progressive operations.
- **`CommandGroup`** — A parent command that shows help when invoked directly; used purely for grouping subcommands.

Every command has two abstract members:
- `Configure()` — Declare arguments/options via `AddArgument<T>()` / `AddOption<T>()`, which bind values into the `TArgs` object.
- `Handle(TArgs args)` — Execute command logic.

**Marker interfaces that change framework behavior:**
- `IStandaloneCommand` — Command works without a `.beamable` config directory.
- `ISkipManifest` — Command skips loading `beamoLocalManifest.json` (avoid null refs if accessing it).
- `IHaveRedirectionConcerns<TArgs>` — Command cannot be safely proxied from global→local beam installations.

**Registration** is done in `App.cs` (the large `Configure()` method) using extension methods:
```csharp
builder.AddRootCommand<MyCommand, MyCommandArgs>();
builder.AddSubCommand<MySubCommand, MySubCommandArgs, MyParentCommandGroup>();
```

### DI and `CommandArgs`

`CommandArgs` is the base args class. It exposes key services as properties (resolved lazily from `IServiceProvider`):
- `ConfigService` — reads/writes `.beamable/config.beam.json`
- `BeamoLocalSystem` — Docker-based local microservice management
- `BeamoService` — Beamable Beamo API client
- `ProjectService` — dotnet solution/project file management
- `ContentService` — Beamable Content operations
- `SwaggerService` — OpenAPI generation
- `CliRequester` — authenticated HTTP client for Beamable APIs
- `AppContext` — current CID, PID, host, auth token

### Output protocol

Commands output structured JSON to **stdout** via `IDataReporterService.Report<T>(channelName, data)`. Each message is a `ReportDataPoint<T>` serialized as JSON, separated by a newline delimiter. This protocol is consumed by Unity Editor integrations and other tooling.

- Channel name `"stream"` = default result stream
- Channel name `"error"` = error output (`ErrorOutput` type)
- Channel name `"logs"` = structured log messages

Errors should be thrown as `CliException` (exits with code 1) or `CliException<T>` (typed error payload, exits with a specific non-zero/non-one code).

### Config and workspace

The CLI looks for a `.beamable/` directory (containing `config.beam.json`) to determine the Beamable workspace root. Commands that require this throw if it is absent, unless they implement `IStandaloneCommand`.

Key config fields: `cid`, `pid`, `host`, `cliVersion`.

### Naming conventions (enforced by `NamingPass` test)

- Command names: **kebab-case** (e.g. `my-command`)
- Option names: **kebab-case** (e.g. `--my-option`)
- Descriptions: **start with uppercase**, **do not end with a period**
- Each command must have a **unique description**

### Testing

Tests extend `CLITest` (in `tests/Examples/CLITest.cs`), which:
- Creates a fresh temp directory per test and `cd`s into it
- Provides `Run(params string[] args)` to invoke the CLI in-process
- Provides `Mock<T>(Action<Mock<T>>)` to swap DI registrations via Moq
- Cleans up on teardown; mocks are verified via `VerifyAll()`
