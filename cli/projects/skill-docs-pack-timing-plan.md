# Plan: Pack generated skill docs in the same build

Status: **proposed — awaiting decision on which path to take**

Scope decision (user): change **only** the `RegenerateSkillDocs` target. Do **not** fold in
`RegenerateTypeSchema` or other generators — they have the same latency issue, but that is out of
scope for this change.

## Problem

The MCP skill docs (`Docs/Skills/*.md`) are shipped as **embedded resources**:

- Consumed at runtime via `Assembly.GetManifestResourceStream("cli.Docs.Skills.*.md")`
  (`McpToolExecutor.GetEmbeddedSkills`).
- Embedded by the csproj: `<EmbeddedResource Include="Docs\**\*.md" />`.

Embedded resources are baked into the assembly during **CoreCompile**. But the generation target runs
*after* the compile:

- `RegenerateTypeSchema` — `AfterTargets="Build"`
- `RegenerateSkillDocs` — `AfterTargets="RegenerateTypeSchema"` (so also post-Build)

Ordering on any single build:

1. CoreCompile embeds the **stale** `Docs/Skills/*.md` currently on disk.
2. AfterBuild → the freshly-built `beam` runs `generate-skill-docs`, overwriting the files on disk.
3. The regenerated files are only embedded on the **next** compile.

In `dev.sh` this is worse: `dotnet build` regenerates the docs, then
`dotnet pack --no-build -p:SKIP_GENERATION=true` packs the already-compiled DLL untouched — so the
published `Beamable.Tools` package always carries the *previous* run's docs.

## The core constraint — a chicken-and-egg

- To **generate** the skill docs, you must *run* the `beam` binary — `generate-skill-docs` walks the
  live command tree (`CliGenerator`). So the binary must already be built.
- To **embed** the generated `.md` files into the DLL, they must exist on disk *before* CoreCompile.

So "generate before compile" is impossible on a clean checkout: there is no binary to run yet. A
correct single invocation inherently needs: **build binary → run it → embed output → pack**, which is
two compiles. The goal is to make those two compiles happen automatically inside one `dotnet build` /
`dotnet pack`, not to force the developer to build twice.

## What a "bootstrap sub-build" means

The bootstrap sub-build is how you break the chicken-and-egg. Before the *real* compile, you build a
throwaway copy of the tool, use it to generate the docs, then let the real compile embed them.

Concretely, a `BeforeTargets="CoreCompile"` target does:

1. `<MSBuild>` the CLI project again into a **separate output dir** (e.g. `obj/.../skillgen/`), with
   `SKIP_GENERATION=true` and a marker property `_BeamSkillDocBootstrap=true`.
   → produces a working `beam` binary, but with its own generation targets turned off.
2. Run that throwaway `beam` → writes `Docs/Skills/*.md` to disk.
3. The **outer** CoreCompile (already in progress) then embeds those fresh `.md` files.

The marker property + `SKIP_GENERATION=true` on the inner build are what stop it recursing forever —
the inner build is a distinct MSBuild project instance that skips the generation target entirely, so
it just compiles a binary and stops. It is "throwaway" because we only need it to run the generator;
the outer build produces the real, packaged DLL.

**Cost:** it roughly doubles the CLI's compile work on every build that generates docs (one bootstrap
compile + one real compile). That is the trade-off, and it drives open question #2 below.

## Honest note on scope

The *reason* today's build ships stale docs is precisely that generation is `AfterTargets="Build"`.
There is no way to make one `dotnet build` embed freshly-generated docs without getting a binary built
*before* the compile — i.e. the bootstrap. So a truly "target-only, no restructure" fix that also
produces a correct single-command build does not exist; each option below makes some trade.

## Options

### Option A — Bootstrap inside `RegenerateSkillDocs`, move it `BeforeTargets="CoreCompile"` (recommended)

One `dotnet build` / `dotnet pack` ships correct docs. Keeps the embedded-resource model intact.
Cost: an extra (bootstrap) compile per build.

```xml
<Target Name="RegenerateSkillDocs" BeforeTargets="CoreCompile"
        Condition="('$(TargetFramework)' == 'net8.0' OR ('$(TargetFramework)' == 'net10.0' AND !Exists('$(OutputPath)../net8.0/') AND !Exists('$(OutputPath)..\net8.0\')))
                   AND $(DOTNET_RUNNING_IN_CONTAINER)!=true AND $(SKIP_GENERATION)!=true
                   AND '$(_BeamSkillDocBootstrap)'!='true'">

  <!-- 1. Build a throwaway copy of this tool into a separate output dir.
          _BeamSkillDocBootstrap + SKIP_GENERATION prevent re-entry / recursion. -->
  <PropertyGroup>
    <_BootstrapOut>$(IntermediateOutputPath)skillgen\</_BootstrapOut>
  </PropertyGroup>
  <MSBuild Projects="$(MSBuildProjectFullPath)"
           Targets="Build"
           Properties="TargetFramework=$(TargetFramework);SKIP_GENERATION=true;_BeamSkillDocBootstrap=true;OutputPath=$(_BootstrapOut)" />

  <!-- 2. Run it to (re)generate the skill docs on disk. -->
  <Exec Condition="'$(IsWindows)'!='true'"
        Command="&quot;./$(_BootstrapOut)$(AssemblyName)&quot; generate-skill-docs --template-dir=&quot;$(MSBuildThisFileDirectory)Docs&quot;"
        IgnoreExitCode="true" />
  <Exec Condition="'$(IsWindows)'=='true'"
        Command="&quot;.\$(_BootstrapOut)$(AssemblyName)&quot; generate-skill-docs --template-dir=&quot;$(MSBuildThisFileDirectory)Docs&quot;"
        IgnoreExitCode="true" />

  <!-- 3. Refresh the EmbeddedResource item list so brand-new skill files are embedded too.
          Content changes to existing files are picked up automatically because GenerateResource
          re-reads file content at CoreCompile; only newly-added file *names* need re-globbing. -->
  <ItemGroup>
    <EmbeddedResource Remove="Docs\**\*.md" />
    <EmbeddedResource Include="Docs\**\*.md" />
  </ItemGroup>
</Target>
```

Notes:
- Windows `.\` vs Unix `./` split on the `Exec` path, matching the existing `IsWindows` pattern.
- The bootstrap sub-build sets `SKIP_GENERATION=true`, so it does *not* recurse into this target.
- Keeps a single top-level command working (`dotnet build`, `dotnet pack`, and CI).
- **`dev.sh` impact:** it runs `dotnet build` then `dotnet pack --no-build -p:SKIP_GENERATION=true`.
  With the target at BeforeCompile, the `dotnet build` step embeds fresh docs into the DLL, and the
  existing `pack --no-build` packs that correct DLL. No `dev.sh` change strictly required.

### Option B — Keep `AfterTargets="Build"`, but re-embed + re-pack in place

Leave generation running after the compile (as today), then, once the fresh docs are on disk, have the
target itself invoke a second compile/pack via an `<MSBuild>` call. Still "a second build," just
triggered automatically inside the one command instead of by the developer. Similar cost to A,
arguably uglier (a post-build target that re-drives Build/Pack).

### Option C — Don't embed at all: pack skill docs as loose content files

Pack `Docs/Skills/*.md` as content under `tools/<tfm>/any/` in the nupkg and read them from the tool's
install directory at runtime instead of from embedded resources. Then generation `AfterTargets="Build"`
is fine — no recompile needed, the files just get packed. This is the *cleanest* single-compile answer,
but it touches `McpToolExecutor.GetEmbeddedSkills()` (filesystem read instead of
`GetManifestResourceStream`) and how skills resolve for a global tool. Larger blast radius; outside a
"target-only" change.

## Recommendation

**Option A** — smallest conceptual change, keeps the embedded-resource model, and makes a single
`dotnet build` / `dotnet pack` correct. The only real cost is the extra bootstrap compile.

## Open questions for the final decision

1. Which option — A (bootstrap before compile), B (re-pack in place), or C (loose content files)?
2. If A: run the bootstrap sub-build on **every** non-container, non-`SKIP_GENERATION` build of the
   CLI, or gate it behind an opt-in property so normal inner-loop `dotnet build` stays a single compile
   and only pack/CI pays for the bootstrap?
