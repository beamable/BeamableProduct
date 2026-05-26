# Portal Extension CLI Commands — E2E Test Plan

## Context

The Beamable CLI has several portal extension commands that lack test coverage. This plan adds e2e tests for `project new portal-extension`, `portal extension list-mount-sites`, `portal extension list-extension-options`, `portal extension add-microservice`, `portal extension check`, and `portal extension set-config`.

A prerequisite refactor is needed: `ListMountSitesCommand.GetRemotePortalConfig()` is a static method that creates an `HttpClient` internally, making it impossible to mock. It must be extracted into an injectable service.

**Excluded:** `beam project run --ids <extension>` — deferred.

---

## Part 1: Extract `IRemotePortalConfigService`

### 1a. Define the interface

Create `cli/Services/IRemotePortalConfigService.cs`:

```csharp
namespace cli.Portal;

public interface IRemotePortalConfigService
{
    Task<RemotePortalConfiguration> GetRemotePortalConfig(CommandArgs args);
}
```

### 1b. Create the default implementation

In the same file:

```csharp
public class RemotePortalConfigService : IRemotePortalConfigService
{
    public async Task<RemotePortalConfiguration> GetRemotePortalConfig(CommandArgs args)
    {
        // Body moved from ListMountSitesCommand.GetRemotePortalConfig
        var url = PortalCommand.GetPortalBaseUrl(args) + "/extension-pages.json";
        var client = new HttpClient();
        var json = await client.GetStringAsync(url);
        var config = JsonSerializer.Deserialize<RemotePortalConfiguration>(json, new JsonSerializerOptions
        {
            IncludeFields = true
        });

        var commonPref = "/:customerId/games/:gameId/realms/:realmId/";
        foreach (var mountSite in config.mountSites)
        {
            if (mountSite.path.StartsWith(commonPref))
                mountSite.path = mountSite.path.Substring(commonPref.Length);
        }
        return config;
    }
}
```

### 1c. Register in DI

In `cli/App.cs`, alongside the existing singleton registrations:

```csharp
services.AddSingleton<IRemotePortalConfigService, RemotePortalConfigService>();
```

### 1d. Update consuming commands

Three call sites reference the static method. Each should resolve via DI:

| File | Line | Change |
|------|------|--------|
| `ListMountSitesCommand.cs` | 76 | `var config = await args.Provider.GetService<IRemotePortalConfigService>().GetRemotePortalConfig(args);` |
| `ListPortalExtensionOptionsCommand.cs` | 49 | Same pattern |
| `NewPortalExtensionCommand.cs` | 117 | Same pattern |

### 1e. Remove the static method

Delete `ListMountSitesCommand.GetRemotePortalConfig()` (lines 51–73) once all callers are updated.

---

## Part 2: Test File

Create `tests/PortalExtensionTests/PortalExtensionCommandTests.cs`.

Extends `CLITestExtensions` for standard mock infrastructure.

### Shared mock helper

```csharp
private void MockRemotePortalConfig()
{
    Mock<IRemotePortalConfigService>(mock =>
    {
        mock.Setup(x => x.GetRemotePortalConfig(It.IsAny<CommandArgs>()))
            .ReturnsAsync(GetTestPortalConfig());
    });
}
```

The `GetTestPortalConfig()` method returns a `RemotePortalConfiguration` built from the mock data below, **with paths already stripped of the common prefix** (the real service does this stripping):

```
!pathMatch                      → [#extension-page (page)]
extensions                      → [#realm-extensions (component)]
players                         → [#top (component), #bottom (component)]
players/:playerId               → [#top (component)]
players/:playerId/!pathMatch    → [#extension-page (page)]
```

---

## Part 3: Test Specifications

### Test 1: `portal extension check`

**Purpose:** Smoke test — verifies the command succeeds when Node.js 22+ and Vite are installed on the host.

**Mocks:** `SetupMocks(mockBeamoManifest: false)` only (command implements `ISkipManifest`).

**Flow:**
1. `Run("portal", "extension", "check")`

**Assertions:** Exit code 0.

**Note:** Requires Node.js 22+ and Vite on the test machine.

---

### Test 2: `portal extension list-mount-sites`

**Purpose:** Verifies the command returns the mocked remote configuration.

**Mocks:** `SetupMocks()` + `MockRemotePortalConfig()`.

**Flow:**
1. `Run("portal", "extension", "list-mount-sites")`

**Assertions:**
- Exit code 0
- Output contains expected mount site paths (`!pathMatch`, `extensions`, `players`, etc.)

---

### Test 3: `portal extension list-extension-options`

**Purpose:** Verifies that mount sites are correctly categorized into page extensions (`!pathMatch` paths) and component extensions.

**Mocks:** `SetupMocks()` + `MockRemotePortalConfig()`.

**Flow:**
1. `Run("portal", "extension", "list-extension-options")`

**Assertions:**
- Exit code 0
- Output contains `pageExtensions` and `componentExtensions`
- Page extensions include the two `!pathMatch` entries
- Component extensions include `extensions`, `players`, `players/:playerId`

---

### Test 4: `portal extension set-config`

**Purpose:** Verifies the command persists file extension config to `.beamable/`.

**Mocks:** `SetupMocks(mockAdminMe: false, mockBeamoManifest: false)` for init, then re-setup for command.

**Flow:**
1. Push interactive input (alias, email, password, game, realm)
2. `Run("init", "--save-to-file")`
3. `_mockObjects.Clear()`
4. `SetupMocks(mockBeamoManifest: false)`
5. `Run("portal", "extension", "set-config", "--file-extensions-to-observe", ".md", ".json")`

**Assertions:**
- Exit code 0
- Config file in `.beamable/` contains `.md` and `.json`

---

### Test 5: `project new portal-extension` (Page Extension)

**Purpose:** Verifies scaffolding a page extension with `--mount-page` under a `!pathMatch` prefix.

**Mocks:** `SetupMocks(mockBeamoManifest: false, mockAdminMe: false)` + `MockRemotePortalConfig()`.

**Test cases:** `[TestCase("react")]`, `[TestCase("svelte")]`

**Flow:**
1. Push interactive input
2. `Run("init", "--save-to-file")`
3. `_mockObjects.Clear()`
4. Re-setup mocks + `MockRemotePortalConfig()`
5. `Run("project", "new", "portal-extension", "TestPageExt", "--quiet", "--mount-page", "my-custom-page", "--mount-group", "TestGroup", "--mount-label", "TestLabel", "--template", template)`

**Assertions:**
- `extensions/TestPageExt/package.json` exists
- `package.json` contains mount page `my-custom-page`, selector `#extension-page`, group `TestGroup`, label `TestLabel`

---

### Test 6: `project new portal-extension` (Component Extension)

**Purpose:** Verifies scaffolding a component extension with explicit `--mount-selector`.

**Mocks:** Same as Test 5.

**Flow:**
1. Init + setup
2. `Run("project", "new", "portal-extension", "TestCompExt", "--quiet", "--mount-page", "players", "--mount-selector", "#top", "--template", "react")`

**Assertions:**
- `extensions/TestCompExt/package.json` exists
- `package.json` contains mount page `players` and selector `#top`

---

### Test 7: `portal extension add-microservice`

**Purpose:** Verifies adding a microservice dependency to an existing portal extension.

**Mocks:** `SetupMocks(mockBeamoManifest: false, mockAdminMe: false)` + `MockRemotePortalConfig()`. Re-setup between steps.

**Flow:**
1. Push interactive input + `Run("init", "--save-to-file")`
2. `_mockObjects.Clear()` + re-setup
3. `Run("project", "new", "service", "TestMs", "--quiet")`
4. `_mockObjects.Clear()` + re-setup + `MockRemotePortalConfig()`
5. `Run("project", "new", "portal-extension", "TestExt", "--quiet", "--mount-page", "extensions", "--mount-selector", "#realm-extensions", "--template", "react")`
6. `_mockObjects.Clear()` + re-setup
7. `Run("portal", "extension", "add-microservice", "TestExt", "TestMs")`

**Assertions:**
- Exit code 0 at each step
- `extensions/TestExt/package.json` contains `TestMs` in the microservice dependencies array

---

## Part 4: Implementation Sequence

1. Create `IRemotePortalConfigService` + `RemotePortalConfigService` in `cli/Services/`
2. Register in `cli/App.cs`
3. Update `ListMountSitesCommand`, `ListPortalExtensionOptionsCommand`, `NewPortalExtensionCommand` to use DI
4. Delete the old static method
5. Verify build: `dotnet build cli.sln`
6. Create `tests/PortalExtensionTests/PortalExtensionCommandTests.cs` with all 7 tests
7. Run tests: `dotnet test tests/ --filter "FullyQualifiedName~PortalExtensionCommandTests"`

## Critical Files

| File | Change |
|------|--------|
| `cli/Services/IRemotePortalConfigService.cs` | **New** — interface + default implementation |
| `cli/App.cs` | Add DI registration |
| `cli/Commands/Portal/ListMountSitesCommand.cs` | Use DI, remove static method |
| `cli/Commands/Portal/ListPortalExtensionOptionsCommand.cs` | Use DI |
| `cli/Commands/Project/NewPortalExtensionCommand.cs` | Use DI |
| `tests/PortalExtensionTests/PortalExtensionCommandTests.cs` | **New** — all 7 tests |
