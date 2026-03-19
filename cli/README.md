[Beamable Docs](https://help.beamable.com/CLI-Latest/)

# Beamable CLI
The Beamable CLI is a .NET tool (targets net8/net10) that manages Content, Microservices, Realms, Auth, and other Beamable systems. The CLI code lives under `cli/cli`; shared runtime types are consumed from the `client/Packages/com.beamable/Common` project.

## Getting started
- [Official docs](https://help.beamable.com/CLI-Latest/cli/guides/getting-started)
- [NuGet tool](https://www.nuget.org/packages/Beamable.Tools)

## Developer tips
Prereqs: `dotnet` (8+ or as required by `global.json`), `docker` only if you plan to run containerized microservice tests or deployments.

- Dev scripts and local feed: See the root `README.md` for the repository-level dev scripts (`setup.sh`, `dev.sh`) and their workflow. These scripts live at the repo root and prepare local package feeds and tooling used by downstream projects.

### When to reference Common as a ProjectReference
The `dev.sh` flow publishes local NuGet packages, but for tight debugging you may want to reference the `Common` project directly. Add a `ProjectReference` to your consuming `.csproj`, for example:

```xml
<ProjectReference Include="..\..\cli\beamable.common\beamable.common.csproj" />
```

Adjust paths to match your workspace layout. This allows stepping into shared code from the CLI or a microservice.

## Projects included in `cli.sln`
The `cli/cli.sln` solution includes several projects useful when working on the CLI and related tooling. High-level list and purpose:

- `cli/cli.csproj` — the CLI application; run with `dotnet run --project cli/cli -- <command>`.
- `tests/tests.csproj` — CLI unit/integration tests; run with `dotnet test cli/tests`.
- `cli/beamable.common` — shared runtime code referenced by the CLI (authoritative sources live in `cli/beamable.common`).
- `beamable.tooling.common` — tooling helpers used by microservice and templates.
- `unityenginestubs` / `unityenginestubs.addressables` — lightweight Unity API stubs used for compiling microservice-related projects without Unity.
- `microservice` / `microserviceTests` — microservice runtime and tests used for runtime development.
- `MicroserviceSourceGen` and related tests — source generator projects used to generate microservice clients.
- `beamable.templates` and template projects (`BeamService`, `BeamStorage`, `CommonLibrary`) — templates used by the CLI to scaffold new projects.
- `beamable.otel.exporter`, `beamable.microservice.otel.exporter`, `beamable.otel.common` — OpenTelemetry exporter and related components.
- `unrealenginestubs` — Unreal Engine stubs used when building Unreal integration code outside Unreal.

Running projects
- Build entire solution: `dotnet build cli/cli.sln`
- Run CLI: `dotnet run --project cli/cli -- <command> [args]`
- Run tests: `dotnet test cli/tests`
- Develop with live Common changes: add a `ProjectReference` to `cli/beamable.common` from the consuming project so you can step through shared code during debugging.

Dev workflow reminder
- For most iterative work consult the root `README.md` for the recommended dev script workflow. The repo-level scripts prepare local package feeds and tooling used by downstream projects.

# Contributing 
This project has the same [contribution policy](https://github.com/beamable/BeamableProduct/tree/main/README.md#Contributing) as the main repository.

# License 
This project has the same [license policy](https://github.com/beamable/BeamableProduct/tree/main/README.md#License) as the main repository.