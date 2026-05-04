---
name: beam-create-microservice
description: Create a Beamable microservice with optional storage and federation support.
---

# Create Microservice

## Prerequisites
- A `.beamable` workspace must exist (run `beam init` first)
- .NET 8.0+ SDK installed
- A `.sln` solution file (auto-discovered or created)

## Steps

### 1. Check existing services
```
beam_exec("project ps")
```

### 2. Create the microservice
```
beam_exec("project new service --name <Name> -q")
```
The CLI auto-discovers the nearest `.sln` file relative to the `.beamable` workspace. If none exists, it creates `<Name>/<Name>.sln`.

#### `beam project new service` options
| Option | Type | Description |
|---|---|---|
`--target-framework` | string | The target framework to use for the new project. Defaults to the current dotnet runtime framework
`--sln` | string | Relative path to the .sln file to use for the new project. If the .sln file does not exist, it will be created. When no option is configured, if this command is executing inside a .beamable folder, then the first .sln found in .beamable/.. will be used. If no .sln is found, the .sln path will be <name>.sln. If no .beamable folder exists, then the <project>/<project>.sln will be used
`--service-directory` | string | Relative path to directory where project should be created. Defaults to "SOLUTION_DIR/services"
`--link-to` | Set[string] | The name of the storage to link this service to
`--groups` | Set[string] | Specify BeamableGroups for this service
`--generate-common` | flag | If passed, will create a common library for this project


### 3. Optionally create and link storage
If the microservice needs a database:
```
beam_exec("project new storage --name <StorageName> -q")
beam_exec("project deps add --source <Name> --deps <StorageName>")
```

#### `beam project new storage` options
| Option | Type | Description |
|---|---|---|
`--target-framework` | string | The target framework to use for the new project. Defaults to the current dotnet runtime framework
`--sln` | string | Relative path to the .sln file to use for the new project. If the .sln file does not exist, it will be created. When no option is configured, if this command is executing inside a .beamable folder, then the first .sln found in .beamable/.. will be used. If no .sln is found, the .sln path will be <name>.sln. If no .beamable folder exists, then the <project>/<project>.sln will be used
`--service-directory` | string | Relative path to directory where project should be created. Defaults to "SOLUTION_DIR/services"
`--link-to` | Set[string] | The name of the project to link this storage to
`--groups` | Set[string] | Specify BeamableGroups for this service


### 4. Run locally
```
beam_exec("project run --ids <Name>")
```

### 5. Stop when done
```
beam_exec("project stop --ids <Name>")
```

## Endpoint Callable Types

Before writing endpoint methods, ask the user what access level each endpoint needs. The attribute determines who can call the endpoint and what authentication is required.

| Attribute | Auth Required | Scope | Use Case |
|---|---|---|---|
| `[Callable]` | No | Public (no auth) | Webhooks, health checks, unauthenticated endpoints |
| `[ClientCallable]` | Yes (any user) | Authenticated user | Game client calls — most common for player-facing features |
| `[AdminOnlyCallable]` | Yes (admin only) | `*` (admin scope) | Back-office tools, moderation, data migration |
| `[ServerCallable]` | No (service identity) | `*` (admin scope) | Microservice-to-microservice calls only |

### Examples

**Public endpoint (no authentication):**
```csharp
[Callable]
public string HealthCheck() => "ok";
```

**Player-facing endpoint (requires authenticated user):**
```csharp
[ClientCallable]
public async Task<int> GetPlayerScore()
{
    // Context.UserId is available — the caller is authenticated
    return await Services.Stats.GetStat("score");
}
```

**Admin-only endpoint:**
```csharp
[AdminOnlyCallable]
public async Task ResetAllScores()
{
    // Only callable by admin/developer tokens
}
```

**Service-to-service endpoint:**
```csharp
[ServerCallable]
public async Task<string> InternalSync()
{
    // Only callable from other microservices, not from game clients
}
```

### Skipping client code generation
Add `CallableFlags.SkipGenerateClientFiles` to prevent the CLI from generating typed client code for an endpoint:
```csharp
[ClientCallable(flags: CallableFlags.SkipGenerateClientFiles)]
public Task InternalHelper() { /* ... */ }
```

## Federation

Federations allow microservices to integrate with external identity providers and inventory systems. They are auto-managed via interface implementation in the microservice code — there are no manual CLI commands for federation.

### Discovering federation interfaces
```
beam_list_types("federation")
```
Returns the full list of available federation interfaces with their methods.

### Available federation interfaces
All federation interfaces require a generic type parameter `T` constrained to `where T : IFederationId, new()`.

- **`IFederatedGameServer<T>`** (`where T : IFederationId, new()`)

- **`IFederatedInventory<T>`** (`where T : IFederationId, new()`)

- **`IFederatedLogin<T>`** — Login federation allows you to create federate the login/signup flows of Beamable to one or more third-parties. It also allows you to run arbitrary code every time a user logs in. (`where T : IFederationId, new()`)

- **`IFederatedPlayerInit<T>`** — Player Init Federation allows you to run custom server code when a new player token is created. The player's token request will not complete until the function has finished. (`where T : IFederationId, new()`)


### Declaring a federation identity
Create a class implementing `IFederationId` with the `[FederationId]` attribute:
```csharp
[FederationId("steam")]
public class SteamIdentity : IFederationId { }
```

### Implementing federation in a microservice
The microservice class implements the federation interface directly:
```csharp
public class MyService : Microservice, IFederatedLogin<SteamIdentity>
{
    public async Promise<FederatedAuthenticationResponse> Authenticate(
        string token, string challenge, string solution)
    {
        // Validate the external token and return authentication response
    }
}
```

Federation declarations are automatically picked up by the OpenAPI spec generation — no additional CLI commands are needed.

## Common Pitfalls
- **`services run/stop/build/deploy` are REMOVED.** Use `project run`, `project stop`, `project build`, and `deploy plan`/`deploy release` instead.
- **Storage must exist before linking.** Create the storage first with `project new storage`, then link with `project deps add`.
- **Always pass `-q` (quiet mode)** when executing from MCP to avoid interactive prompts.
- **Solution auto-discovery** looks for `.sln` files near the `.beamable` workspace. Unity and Unreal solutions are automatically excluded.

## Build Warnings

After running `beam project build`, check stderr for Beamable Analyzer warnings. Treat all Beamable-related warnings as errors — fix them before proceeding.

```
beam_exec("project build -q")
```

Common warnings to watch for:
- **Deprecated API usage** — A Beamable API you called has been replaced; the warning message names the replacement.
- **Missing attributes** — Endpoints missing `[ClientCallable]`, `[AdminOnlyCallable]`, or other required attributes.
- **Federation mismatches** — The federation interface implementation does not match the expected method signatures.

Do not ignore these warnings. They indicate code that will fail at runtime or break on the next Beamable upgrade.

## Microservice Storage

Storage objects provide MongoDB access for microservices. Each storage runs as a local MongoDB container during development.

### Creating storage
```
beam_exec("project new storage --name MyStorage -q")
```

Storage is linked to a microservice via project references. Use `project deps add` to create the link:
```
beam_exec("project deps add --source MyService --deps MyStorage")
```


#### `beam project new storage` options
| Option | Type | Description |
|---|---|---|
| `--target-framework` | string | The target framework to use for the new project. Defaults to the current dotnet runtime framework |
| `--sln` | string | Relative path to the .sln file to use for the new project. If the .sln file does not exist, it will be created. When no option is configured, if this command is executing inside a .beamable folder, then the first .sln found in .beamable/.. will be used. If no .sln is found, the .sln path will be <name>.sln. If no .beamable folder exists, then the <project>/<project>.sln will be used |
| `--service-directory` | string | Relative path to directory where project should be created. Defaults to "SOLUTION_DIR/services" |
| `--link-to` | Set[string] | The name of the project to link this storage to |
| `--groups` | Set[string] | Specify BeamableGroups for this service |


### Storage object declaration
```csharp
[StorageObject("MyStorage")]
public class MyStorage : MongoStorageObject { }
```

### Accessing storage in a microservice
```csharp
var db = await Storage.GetDatabase<MyStorage>();
var collection = db.GetCollection<MyDocument>("my-collection");
await collection.InsertOneAsync(new MyDocument { ... });
```

## Calling Other Microservices

Use the `[MicroserviceClient]` attribute on an interface to generate a typed client for another microservice. Inject the generated client via constructor DI, then call the remote service's `[ClientCallable]` methods through it.

```csharp
// In ServiceA, call ServiceB's endpoints:
[MicroserviceClient(typeof(ServiceB))]
public interface IServiceBClient { }
```

**WARNING:** Inter-service calls create tight coupling. Both services must be deployed, and each call adds network latency. Prefer shared libraries (common `.csproj` references) for reusable logic over microservice-to-microservice calls.

## Reading Source Code

Use the `beam-get-source` skill to read Beamable SDK source code when you need to understand microservice base classes, DI services, or available APIs.

## Wrap-Up

After completing the workflow, provide the user with a summary that covers:

1. **What was created**: List each service and storage by name, and their project paths relative to the workspace root (e.g. `services/<Name>/<Name>.csproj`).
2. **Where the files live**:
   - The microservice project: `services/<Name>/` — contains the C# source with endpoint methods.
   - If storage was created: `services/<StorageName>/` — the MongoDB storage project.
   - The solution file: whichever `.sln` was used or created.
   - Service manifest: `.beamable/beamoLocalManifest.json` — tracks all local services and their Docker configuration.
3. **Why specific choices were made** — explain the reasoning behind the decisions:
   - **Callable attribute chosen**: Why `[ClientCallable]` vs `[AdminOnlyCallable]` vs `[ServerCallable]` vs `[Callable]` — what authentication and access scope this gives the endpoint, and why that fits the user's use case.
   - **Storage dependency**: If storage was linked, explain that `project deps add` generates a typed `StorageDocument` accessor in the microservice, and that the storage runs as a local MongoDB container during development.
   - **Federation interface**: If a federation was implemented, explain what external system it integrates with and how Beamable will call the federation methods during player authentication or inventory resolution.
4. **How to iterate**: Remind the user they can `project run --ids <Name>` to start the service locally, open Swagger with `project open-swagger <Name>` to test endpoints interactively, and `project build` to verify compilation before deploying.

## CLI Version Awareness

If the CLI version has changed (check `.config/dotnet-tools.json`), re-run `beam_list_commands()` and `beam_get_help()` to get up-to-date command information. Command options and behavior may have changed between versions.
