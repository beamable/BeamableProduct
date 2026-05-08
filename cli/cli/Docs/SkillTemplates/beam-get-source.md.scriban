---
name: beam-get-source
description: Read Beamable SDK source code directly via the MCP tool — returns file content, with pagination for large files
---

# Get Beamable Source Code

## Overview

When you need to understand Beamable SDK internals, look up API signatures, or debug framework behavior, use the `beam_get_source()` MCP tool. It detects the SDK platform, locates source directories, and **reads file content directly** — bypassing sandbox restrictions that prevent agents from accessing system directories like NuGet cache or Unity PackageCache.

## Local Source Locations

| Platform | Local path | How to detect |
|---|---|---|
| Unity | `Library/PackageCache/com.beamable@{version}/` | `Packages/manifest.json` has `com.beamable` |
| Unreal | `Plugins/BeamableCore/` | Directory exists in the project root |
| WebSDK | `node_modules/@beamable/sdk/` | `package.json` has `@beamable/sdk` |
| CLI/Microservice | `~/.nuget/packages/beamable.common/{ver}/content/netstandard2.0/Runtime/` | `.config/dotnet-tools.json` has `beamable.tools` |

### NuGet cache packages (CLI and Microservices)

The NuGet global cache contains additional SDK source:

| Package | Path in cache | Contents |
|---|---|---|
| `beamable.common` | `~/.nuget/packages/beamable.common/{ver}/content/netstandard2.0/Runtime/` | ContentObject, IContentApi, Optional, serialization |
| `beamable.common` (build) | `~/.nuget/packages/beamable.common/{ver}/build/netstandard2.0/` | MSBuild props for the common package |
| `beamable.tooling.common` | `~/.nuget/packages/beamable.tooling.common/{ver}/content/netstandard2.1/` | ClientCallableAttribute, federation interfaces, Microservice base class |
| `beamable.microservice.runtime` | `~/.nuget/packages/beamable.microservice.runtime/{ver}/content/net8.0/` | Microservice runtime, request handling, DI |
| `beamable.microservice.runtime` (build) | `~/.nuget/packages/beamable.microservice.runtime/{ver}/build/` | MSBuild targets for OAPI gen, build validation, collector resolution |

To find the exact version, check the project file (`.csproj`) for PackageReference versions, or check `.config/dotnet-tools.json` for the CLI tool version.

## Workflow

### Step 1: Read a source file directly
Call `beam_get_source` with a filename to search for and read it:
```
beam_get_source(filePath: "ContentObject.cs")
```
The tool auto-detects the platform and version, searches all known SDK directories, and returns the file content.

You can pass:
- **A filename**: `ContentObject.cs` — searches all SDK directories recursively
- **A relative path**: `Runtime/Content/ContentObject.cs` — tries sourcePath then commonPaths
- **An absolute path**: the full path from a previous response — must be under an allowed SDK directory

### Step 2: Inspect the response
The response includes:
- `content` — the file text (present only when the file was found and readable)
- `totalLength` — full file character count
- `offset` — the character offset used
- `hasMore` — `true` if the file was too large for a single response
- `nextOffset` — where to continue reading (only when `hasMore` is true)
- `remaining` — how many characters are left (only when `hasMore` is true)
- `filePath` — the resolved absolute path
- `commonPaths` — all known SDK directories (use these to discover other files)

### Step 3: Paginate large files
If `hasMore` is `true`, call again with `offset` set to `nextOffset`:
```
beam_get_source(filePath: "/full/path/to/LargeFile.cs", offset: 65536)
```
Each chunk returns up to 65,536 characters by default. Use `limit` to request smaller chunks.

### Step 4: Browse available files
If you need to discover which files exist before reading, call `beam_get_source()` without `filePath` to get the `commonPaths` directories, then call back with specific filenames.

## Key Files to Look For

These are the most commonly needed source files when working with Beamable:

### Content system
- `ContentObject.cs` — Base class for all content types; serialization and ID handling
- `IContentApi.cs` — Interface for querying and resolving content at runtime
- `ContentRef.cs` — Typed references to content objects (`ContentRef<T>`)
- `Optional.cs` — `Optional<T>` wrappers for nullable content fields

### Microservice system
- `Microservice.cs` — Base class for microservices; lifecycle and DI
- `ClientCallableAttribute.cs` — `[ClientCallable]`, `[AdminOnlyCallable]`, `[ServerCallable]`, `[Callable]` attributes
- `RequestContext.cs` — Access to `Context.UserId`, `Context.Body`, etc. inside callable methods

### Federation
- `IFederationId.cs` — Marker interface and `[FederationId]` attribute
- `IFederatedLogin.cs` — Login federation interface
- `IFederatedInventory.cs` — Inventory federation interface
- `IFederatedGameServer.cs` — Game server federation interface

### Stats and inventory
- `IStatsApi.cs` — Player and game stats
- `IInventoryApi.cs` — Inventory management

### Build system (MSBuild targets and props)
- `Beamable.Microservice.Runtime.targets` — MSBuild targets for OAPI generation, build cache invalidation, output lock checks, collector resolution
- `Beamable.Microservice.Runtime.props` — MSBuild properties for microservice builds
- `Beamable.Common.props` — MSBuild properties for the common package

## Common Pitfalls
- **File content is read by the MCP server, not the agent.** This bypasses sandbox restrictions — the agent does not need direct filesystem access to system directories.
- **64KB default chunk size.** Each response returns up to 65,536 characters. For larger files, use `offset` and `nextOffset` to paginate. The `remaining` field tells you how much is left.
- **Only SDK directories are readable.** The tool validates that requested paths are under known SDK directories (sourcePath or commonPaths). Paths outside these directories are rejected.
- **Version matters.** API signatures can change between versions. Always check the version number in the package manifest or `.csproj` before relying on source from the cache.
- **NuGet cache path varies by OS.** On Windows it is `%USERPROFILE%\.nuget\packages\`, on macOS/Linux it is `~/.nuget/packages/`.
- **Always pass `-q`** when executing beam commands from MCP to avoid interactive prompts.

## Wrap-Up

After completing the workflow, provide the user with a summary that covers:

1. **What was found**: Which platform was detected, what SDK version is installed, and which source files were read.
2. **Where the source lives**: The resolved file paths from the tool response. If the NuGet cache was used, list the exact package and version path.
3. **Which files were read and why**: Explain why those particular source files were relevant — e.g., "Read `ClientCallableAttribute.cs` to confirm the attribute's constructor parameters and optional flags."
4. **Key findings**: Summarize the relevant API signatures, class hierarchies, or implementation details discovered in the source code.

## CLI Version Awareness

If the CLI version has changed (check `.config/dotnet-tools.json`), re-run `beam_list_commands()` and `beam_get_help()` to get up-to-date command information. Command options and behavior may have changed between versions.
