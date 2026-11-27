# Issue: portal-extension client generation never writes `beamable/clients/types/index.ts`

**Status:** open, root cause identified, fix not started.
**Found:** 2026-10-13, while building the `secrets-test` extension (see `microservice-secrets-api-plan.md`).

## What it is

`beam portal extension add-microservice <ext> <service>` and `beam project generate
portal-extension-clients` (`pe-client`) write `beamable/clients/<Service>Client.ts`. Every generated
client starts with:

```ts
import type * as Types from './types';
```

Neither command writes `beamable/clients/types/index.ts`, so the extension fails type checking:

```
beamable/clients/SecretsTestRealmClient.ts(7,29): error TS2307: Cannot find module './types' ...
beamable/clients/SecretsTestZoneClient.ts(7,29): error TS2307: Cannot find module './types' ...
```

`vite build` / `beam-build` still succeed, because the imports are `import type` and are stripped
at build time. So the problem only shows up with `tsc` or in the editor, with request/response
types missing.

Existing agentic-portal extensions (e.g. `bundles/features/vip/.../beamable/clients/types/index.ts`)
do have the file, with the "AUTO-GENERATED" header. Its origin is unknown: probably
`beam project generate web-client` run by hand, or an older CLI. Git history shows no point where
either extension command called `GenerateClientTypes`.

## Root cause

- `cli/cli/Services/Web/WebClientCodeGenerator.cs`:
  - each `new WebClientCodeGenerator(...)` adds its types to **static** `ClientTypes` /
    `ModuleTypes` (`ProcessClientTypes()` in the constructor);
  - `GenerateClientCode(dir)` writes only the client file;
  - `public static string GenerateClientTypes(string typesOutputDirectory)` writes `index.ts`,
    deduplicates by type name, then **clears** the static sets.
- Only `cli/cli/Commands/Project/GenerateWebClientCommand.cs:69` calls `GenerateClientTypes`. That
  command generates clients for **every** service in the workspace into one output directory, so
  it isn't a workaround for one extension.
- `cli/cli/Commands/Portal/PortalExtensionAddDependencyCommand.cs`, `GenerateDependenciesClients`:
  generates clients for the extension's dependencies, never types. It's also run on every
  `project run`.
- `cli/cli/Commands/Project/GeneratePortalExtensionClientsCommand.cs`: generates per microservice,
  in `Parallel.ForEachAsync` (8 at a time), into each dependent extension, never types. It locks
  per output path for client writes only.

**Side effect of the static state:** the types collected by these two commands are never written
or cleared. They pile up for the life of the process, which matters for the long-lived MCP server.
The next `generate web-client` in that process would then emit types from unrelated earlier runs.

## How to reproduce

From `agentic-portal/`, with any built microservice `Svc` that has callables taking or returning a DTO:
```
dotnet beam project new portal-extension repro-types --zone --mount-page repro-types --mount-group Testing --mount-label Repro -q
# the CLI ignores --service-directory and writes to extensions/repro-types (separate bug)
dotnet beam portal extension add-microservice repro-types Svc -q
ls extensions/repro-types/beamable/clients          # SvcClient.ts, no types/
cd extensions/repro-types && npm install && npx tsc --noEmit -p .   # TS2307 './types'
```

Existing instance: `agentic-portal/bundles/extensions/secrets-test` (clients for `SecretsTestZone`
and `SecretsTestRealm`, no `types/`).

## Fix sketch

For each extension, generate all of its clients and then its types together, so `index.ts`
contains exactly that extension's dependency types:

1. **`GenerateDependenciesClients`:** after the loop over the dependencies, call
   `WebClientCodeGenerator.GenerateClientTypes(Path.Combine(extensionPath, "beamable/clients/types"))`.
2. **`GeneratePortalExtensionClientsCommand`:** this one is per *service*, but types must be per
   *extension*. Options:
   - **(a) Invert the loop:** per affected extension, regenerate all of that extension's dependency
     clients and then its types. Same as (1), and it can reuse `GenerateDependenciesClients`. The
     extension count is small.
   - **(b) Keep it per service, then regenerate types per extension at the end.** This needs every
     dependency's OpenAPI doc for that extension anyway, so (a) is simpler.
3. **Static state:** `ClientTypes`/`ModuleTypes` are shared across threads in the parallel command.
   - Either make the type collection an instance of the generator, or a per-extension collector
     passed in.
   - Or serialize generation per extension under the existing path lock, and clear the statics
     before generating each extension.
   - Making it instance state is the cleaner fix. It also removes the MCP cross-run leak.
4. **Tests (`cli/tests`):** extend `Web/WebClientServiceScopeTests.AZoneExtensionGetsAClientPerServiceScope`,
   or add a sibling test with a DTO-returning operation in the OpenAPI doc, asserting
   `beamable/clients/types/index.ts` exists and declares the DTO. Add a second extension in the same
   process to check its types don't leak into the first.

## Related

- `project new portal-extension` ignores `--service-directory`.
  `ProjectService.CreateNewPortalExtension` always writes to `<workspace>/extensions`
  (`cli/cli/Services/ProjectService.cs:282`). The option is advertised in `--help`. Track it
  separately.
- The client host type (`BeamBase` vs `BeamZoneSdk`) is chosen per service since commit `69fcb94a1`
  (`web-client-service-scope-plan.md`). Keep that behaviour when restructuring the loop.
