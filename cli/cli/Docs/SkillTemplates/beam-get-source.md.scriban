---
name: beam-get-source
description: Find and read Beamable SDK source code locally or via MCP tool
---

# Get Beamable Source Code

## Overview

When you need to understand Beamable SDK internals, look up API signatures, or debug framework behavior, read the SDK source directly. The source is already on disk in most projects — you just need to know where to look.

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
| `beamable.tooling.common` | `~/.nuget/packages/beamable.tooling.common/{ver}/content/netstandard2.1/` | ClientCallableAttribute, federation interfaces, Microservice base class |
| `beamable.microservice.runtime` | `~/.nuget/packages/beamable.microservice.runtime/{ver}/content/net8.0/` | Microservice runtime, request handling, DI |

To find the exact version, check the project file (`.csproj`) for PackageReference versions, or check `.config/dotnet-tools.json` for the CLI tool version.

## Workflow

### Step 1: Detect the platform
Check for platform indicators in the project:
- **Unity**: Look for `Packages/manifest.json` with `com.beamable`
- **Unreal**: Look for `Plugins/BeamableCore/` directory
- **WebSDK**: Look for `package.json` with `@beamable/sdk`
- **CLI/Microservice**: Look for `.config/dotnet-tools.json` with `beamable.tools` or `.csproj` files with Beamable PackageReferences

### Step 2: Read local source
Once you know the platform, navigate to the appropriate path and read the source files directly. No CLI commands are needed — just read the files from disk.

### Step 3: Fallback to MCP tool
If local source is unavailable (e.g., the package cache is empty or the platform is not detected), use the `beam_get_source()` MCP tool to retrieve source code remotely.

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

## Common Pitfalls
- **NuGet cache path varies by OS.** On Windows it is `%USERPROFILE%\.nuget\packages\`, on macOS/Linux it is `~/.nuget/packages/`.
- **Unity package cache is read-only.** The `Library/PackageCache/` directory is managed by Unity and regenerated from `Packages/manifest.json`. Do not modify files there.
- **Version matters.** API signatures can change between versions. Always check the version number in the package manifest or `.csproj` before relying on source from the cache.
- **Always pass `-q`** when executing beam commands from MCP to avoid interactive prompts.

## Wrap-Up

After completing the workflow, provide the user with a summary that covers:

1. **What was found**: Which platform was detected, what SDK version is installed, and which source files were read.
2. **Where the source lives**:
   - The specific path where Beamable SDK source was found on disk.
   - If the NuGet cache was used, list the exact package and version path.
3. **Why specific choices were made** — explain the reasoning:
   - **Local vs remote source**: Local source is preferred because it matches the exact version the project uses. The `beam_get_source()` MCP tool is a fallback when local files are unavailable.
   - **Which files were read**: Explain why those particular source files were relevant to the user's question — e.g., "Read `ClientCallableAttribute.cs` to confirm the attribute's constructor parameters and optional flags."
4. **Key findings**: Summarize the relevant API signatures, class hierarchies, or implementation details discovered in the source code.

## CLI Version Awareness

If the CLI version has changed (check `.config/dotnet-tools.json`), re-run `beam_list_commands()` and `beam_get_help()` to get up-to-date command information. Command options and behavior may have changed between versions.
