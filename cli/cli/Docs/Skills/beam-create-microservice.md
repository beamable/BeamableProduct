---
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
beam_exec("project new microservice --name <Name> -q")
```
The CLI auto-discovers the nearest `.sln` file relative to the `.beamable` workspace. If none exists, it creates `<Name>/<Name>.sln`.

### 3. Optionally create and link storage
If the microservice needs a database:
```
beam_exec("project new storage --name <StorageName> -q")
beam_exec("project deps add --source <Name> --deps <StorageName>")
```

### 4. Run locally
```
beam_exec("project run --ids <Name>")
```

### 5. Stop when done
```
beam_exec("project stop --ids <Name>")
```

## Federation

Federations allow microservices to integrate with external identity providers and inventory systems. They are auto-managed via interface implementation in the microservice code — there are no manual CLI commands for federation.

### Discovering federation interfaces
```
beam_list_types("federation")
```
This returns all available federation interfaces and their method signatures.

### Available federation interfaces
All require a generic type parameter `T` constrained to `where T : IFederationId, new()`:

- **`IFederatedLogin<T>`** — External authentication. Implement `Authenticate(token, challenge, solution)`.
- **`IFederatedInventory<T>`** — Federated inventory management. Extends `IFederatedLogin<T>`. Adds `GetInventoryState(id)` and `StartInventoryTransaction(...)`.
- **`IFederatedPlayerInit<T>`** — Runs code when new player tokens are created. Implement `CreatePlayer(account, properties)`.
- **`IFederatedGameServer<T>`** — Game server creation. Implement `CreateGameServer(lobby)`.
- **`IFederatedCommerce<T>`** — Commerce integration.

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
