# Microservice Secrets API (realm + zone)

## Goal

Make the platform **secrets store** (`/api/secrets/*`) a first-class SDK feature in microservices,
available in both scopes:

- realm services: `Services.Secrets` (on `IBeamableServices`)
- zone services: `Services.Secrets` (on `IZoneServices`)

Today nothing in the microservice SDK registers or uses the secrets API. The generated client
`IBeamSecretsApi` exists in `cli/beamable.common/Runtime/OpenApi/BeamSecrets.gs.cs`, but it is not
in any DI container and has no hand-written wrapper.

## Backend facts this relies on (BeamableAPI)

- `SecretsController` (`/secrets`) addresses the target through the `X-BEAM-SCOPE` header
  (`RequestContext.RealmInfo`). Storage, data keys and audit are keyed on `RealmInfo.Scope`
  (`ZoneId ?? RealmId`), so a `cid.zid` request reads and writes **zone** secrets and a `cid.pid`
  request reads and writes **realm** secrets. No backend change is needed.
- The path-based `RealmSecretsController` (`/customers/{cid}/realms/{pid}/secrets`) is
  `[Obsolete]`. We will **not** use `IBeamRealmsecretsApi`.
- Rules the wrapper should document, not re-implement:
  - The `beamable/` key prefix is reserved for SuperAdmin.
  - A key can be at most 512 characters and a value at most 64 KiB.
  - A missing key returns 404 `SecretNotFound`.
- Permissions:
  - Reading a value (`GET values/{key}`) needs `RealmSecrets:Write`.
  - Listing needs `RealmSecrets:Read`.
  - On a zone-scoped request, any Write also needs the zone Admin role
    (`ScopeAuthorization.ValidateZoneWritePermission`).

## Design

### 1. Contract — `IMicroserviceSecretsApi`

New file: `microservice/beamable.tooling.common/SharedRuntime/Api/Secrets/IMicroserviceSecretsApi.cs`.
This follows the existing one-API-per-folder convention (`Api/Push`, `Api/Payments`, ...).

- **No `[RealmScoped]` / `[ZoneScoped]` attribute.** The same contract is valid in both containers.
  `ScopedDependencyBuilder` only rejects types that are annotated, so leaving it unannotated is what
  allows registration in both scopes.
- Scope is implicit: the API acts on whatever scope the current request or requester is in. The doc
  comments say this explicitly ("in a zone service this reads zone secrets; use `AssumeRealm` to
  reach a realm's secrets").

Proposed surface. It wraps the generated models, so no new DTO classes:

```csharp
public interface IMicroserviceSecretsApi
{
    /// Value of a secret. Fails (404 SecretNotFound) when the key does not exist.
    Promise<string> GetSecret(string key);

    /// Creates or replaces a secret.
    Promise SetSecret(string key, string value);

    /// Deletes a secret.
    Promise DeleteSecret(string key);

    /// One page of secret metadata (key, dataKeyId, algorithm, updatedAt) — never values.
    Promise<RealmSecretInfoCursorPagedResult> ListSecrets(string cursor = null);
}
```

Data-key listing/rotation and audit logs are operator concerns. They are left out of this first
iteration (see Decisions). They stay reachable through the raw generated client, which is
registered in DI as well (step 3).

### 2. Implementation — `MicroserviceSecretsApi`

New file: `microservice/microservice/Api/Secrets/MicroserviceSecretsApi.cs`. It follows the
`MicroservicePushApi` pattern: a thin class that takes the generated `IBeamSecretsApi` in its
constructor and maps the calls:

| Wrapper            | Generated call                         |
|--------------------|----------------------------------------|
| `GetSecret(key)`   | `GetValues(key)` → `.value.Value`      |
| `SetSecret(k, v)`  | `PutValues(k, new PutRealmSecretRequest { value = v })` |
| `DeleteSecret(k)`  | `DeleteValues(k)`                      |
| `ListSecrets(c)`   | `GetValues(cursor: c)`                 |

### 3. DI registration (`MicroserviceStartupUtil.cs`)

Register both the generated client and the wrapper, **scoped**, in each scope method:

```csharp
.AddScoped<IBeamSecretsApi, BeamSecretsApi>()
.AddScoped<IMicroserviceSecretsApi, MicroserviceSecretsApi>()
```

- **`ConfigureRealmServices`:** add the two lines. Add
  `Secrets = provider.GetRequiredService<IMicroserviceSecretsApi>()` to `ExtractSdks`.
- **`ConfigureZoneServices`:** add the two lines. Change the `IZoneServices` factory to pass the
  secrets API into `ZoneServices`.
  - Scoped is required here: the requester resolves `RequestContext` from the per-request
    (`cid.zid`) scope, the same constraint documented for `IZoneCustomerApi` and the storage provider.
- **Not in `OpenApiRegistration.RegisterOpenApis`.** That file is shared with the CLI and Unity, and
  under the pinned Unity CI nuget it gets replaced anyway. Registering in the microservice startup
  keeps the change local.

### 4. `AssumeRealm` fork (`ZoneServicesImpl.cs` → `RealmScopeFactory.CreateRealmScope`)

The fork starts from the **zone** root and re-runs `ConfigureRealmServices`. The builder keeps one
registration per type: a second `AddScoped` of the same interface logs
`"Failed to add scoped interface=[...] because it already existed"`, and the first registration
wins. Since secrets are now registered in both scopes, the fork must drop the zone copies first,
next to the existing requester removals:

```csharp
builder.RemoveIfExists<IBeamSecretsApi>();
builder.RemoveIfExists<IMicroserviceSecretsApi>();
```

With that, `using (var realm = AssumeRealm(pid)) realm.Services.Secrets...` hits the realm's secrets
through the realm requester. That works for both the socket and the signed-HTTP paths.

### 5. Services accessors

- `IBeamableServices` (`beamable.tooling.common/SharedRuntime/IBeamableServices.cs`): add
  `IMicroserviceSecretsApi Secrets { get; }` with a doc comment. Add the property to
  `BeamableServices` (`microservice/microservice/dbmicroservice/BeamableServices.cs`).
- `IZoneServices` (`beamable.tooling.common/SharedRuntime/ZoneServices.cs`): add
  `IMicroserviceSecretsApi Secrets { get; }`. In `ZoneServices` (`ZoneServicesImpl.cs`), add a
  constructor parameter and the property. Update the `ZoneMicroservice.Services` doc comment, which
  currently says "Use `Services.Customer`...".

### 6. Tests (`microservice/microserviceTests`)

- In `ZoneMicroserviceTests`, add one scope-validation test for each container:
  - `ZoneContainer_AcceptsSecretsApi`: `ScopedDependencyBuilder(Zone)` with
    `IMicroserviceSecretsApi` builds without throwing.
  - `RealmContainer_AcceptsSecretsApi`: the same check on a realm container.
  - These guard against someone later adding a `[RealmScoped]` attribute and breaking zones.
- Add a `MicroserviceSecretsApi` unit test with a fake `IBeamSecretsApi`, checking the mapping:
  - Get returns the `value`.
  - Set sends a `PutRealmSecretRequest`.
  - List passes the cursor through.
- If the existing realm socket harness (`TestSetup`) has standard handlers for an HTTP-over-socket
  request, add a round-trip `ClientCallable` that calls `Services.Secrets.GetSecret`. Skip this if
  it needs new harness work.

### 7. Changelog

`microservice/microservice/CHANGELOG.md` → **Added**: "`Services.Secrets` for realm and zone
microservices: get, set, delete and list secrets scoped to the service's realm or zone."

### 8. Manual test bed (agentic-portal)

Unit tests can't answer the main open question: whether the gateway lets a microservice's own
requests read and write secrets. So the end-to-end check is two plain microservices plus one zone
portal extension in `agentic-portal/bundles/`. This is test scaffolding only:
- no `*.beam.bundle.json`;
- not part of any feature bundle;
- it follows the existing `bundles/extensions/hot-reload-test` extension and the
  `bundles/services/{realms,zones}/` service layout.

`bundles/.gitignore` only ignores `assets/` and `node_modules/`, and the root `.gitignore` only
ignores `bin/`/`obj/`, so the sources are tracked.

**Prerequisite:** the services reference `Beamable.Microservice.Runtime` at the CLI version pinned
in agentic-portal's `.config/dotnet-tools.json`. Run `./dev.sh` from BeamableProduct first, so the
local `0.0.123.x` runtime with `Services.Secrets` is published and picked up.

#### 8a. `SecretsTestRealm` — `bundles/services/realms/SecretsTestRealm/`

- A realm `Microservice` (`[Microservice("SecretsTestRealm")]`) with default realm scope. The
  csproj is a copy of `PlayerStatsService`'s, with the name changed.
- All endpoints are `[ServerCallable]`: the extension calls them with an operator token after
  `assumeRealm`.

#### 8b. `SecretsTestZone` — `bundles/services/zones/SecretsTestZone/`

- A `ZoneMicroservice` (`[Microservice("SecretsTestZone")]`, `<BeamServiceScope>zone</BeamServiceScope>`).
  The csproj is based on `VipServiceZone`'s, without the storage reference.
- `[ServerCallable]` endpoints, like `VipServiceZone`.

#### Endpoints (same shape on both services)

Every endpoint returns a `SecretsDebugResult`, which captures everything we need to debug a failure:

| Field | Source / meaning |
|---|---|
| `scope` | `realm` or `zone` |
| `cid`, `pid` / `zid` | from `Context` (realm `RequestContext` / zone `ZonedRequestContext`) — the scope the call should hit |
| `secretsApiType`, `generatedApiType` | concrete types resolved for `Services.Secrets` and `Provider.GetService<IBeamSecretsApi>()` (null → the DI registration is missing) |
| `steps[]` | one entry per secrets call: `op`, `key`, `ok`, `value` / `records`, `elapsedMs` |
| `steps[].error` | on failure: HTTP status, Beamable error code (`SecretNotFound`, `Forbidden`, `ReservedSecretKeyPrefix`, ...), message |

Endpoints:

- `Diagnostics()`: scope, ids and resolved DI types only, with no secrets call. This is the first
  thing to check if anything else fails.
- `ListSecrets(string cursor)`, `GetSecret(string key)`, `SetSecret(string key, string value)`,
  `DeleteSecret(string key)`: single operations, for poking at it by hand.
- `RoundTrip(string keyPrefix)`: runs set → get (compare) → list (key present) → delete → get
  (expect 404) on `<keyPrefix>/<scope>-<timestamp>` and reports each step. This is the main smoke
  test.

Zone-only endpoints, which exercise the `AssumeRealm` fork (plan step 4):

- `RealmDiagnostics(string pid, bool useSignedRequests)`: same as `Diagnostics()`, but read from
  `AssumeRealm(pid, 0, useSignedRequests).Services`.
- `RealmRoundTrip(string pid, string keyPrefix, bool useSignedRequests)`: `RoundTrip`, run through
  `AssumeRealm`. Run it with both `useSignedRequests` values, because the socket and signed-HTTP
  requesters are different code paths in the fork.

#### 8c. `secrets-test` extension — `bundles/extensions/secrets-test/`

- A copy of `hot-reload-test`: `serviceScope: "zone"`, navGroup `Testing`, its own page mount
  (`secrets-test`), and the same `.gitignore`/vite/tsconfig.
- `microserviceDependencies: ["SecretsTestZone", "SecretsTestRealm"]` so `beam` generates both
  clients.
- **Realm client:** depends on `web-client-service-scope-plan.md`, which lands first. With that
  fix, the generated `SecretsTestRealmClient` is typed `BeamBase` and is used as is:
  `realmBeam.use(SecretsTestRealmClient)`. `SecretsTestZoneClient` is typed `BeamZoneSdk` and is
  used on `context.beam`. There are no hand-written clients or casts. If either client comes out
  with the wrong type, that's a bug in the fix, not something to work around here.
- **Realm picker:** use `useRealmScope` from `extensions-libs/portal-ui`, as `message-rail-debug`
  does (`context` → realm list → `assumeRealm(pid)`). It's the only extra dependency over
  `hot-reload-test`.
- **Type check:** run `tsc --noEmit` on the extension as part of verification. `beam-build` is
  only `vite build` and won't catch type errors.

Page layout (single page, no routing):

1. **Inputs:** key prefix (default `secrets-test`), realm picker, a `useSignedRequests` toggle.
2. **Zone service panel:** buttons for `Diagnostics`, `RoundTrip` and the single ops.
3. **Realm service panel:** the same buttons, called on `SecretsTestRealm` through `assumeRealm`.
4. **Zone → realm panel:** `RealmDiagnostics` and `RealmRoundTrip` on `SecretsTestZone`.
5. **Ground truth panel:** lists `/secrets` directly through the operator requester:
   - the zone SDK's requester (zone scope);
   - `realmBeam.requester` (realm scope).
   
   This is the same call `core/realm/portal-extensions/secrets` makes, and it's independent of the
   microservice SDK. It confirms where each service's writes actually landed.
6. **Isolation check button:** writes one key via the zone service and one via the realm service,
   then checks with the ground-truth lists that each key appears only in its own scope. Deletes both
   afterwards.

Each result renders as a step table (op / key / ok / status / error code / ms). The raw
`SecretsDebugResult` JSON goes in a collapsible block below it.

#### What "working" means

- **DI:** `Diagnostics` on both services shows non-null `secretsApiType` / `generatedApiType`.
  `RealmDiagnostics` shows the same, and the zone service log has no
  `"Failed to add scoped interface=[IMicroserviceSecretsApi] because it already existed"`.
- **Round trips:** `RoundTrip` passes on both services. `RealmRoundTrip` passes with
  `useSignedRequests` both false and true.
- **Isolation:** zone keys appear only in the zone list, realm keys only in the realm list.
- **Errors:** any `Forbidden` on get/set answers risk 1 below. Record the role and scope from the
  result, and that becomes the follow-up.

## Files touched

| File | Change |
|---|---|
| `microservice/beamable.tooling.common/SharedRuntime/Api/Secrets/IMicroserviceSecretsApi.cs` | new |
| `microservice/microservice/Api/Secrets/MicroserviceSecretsApi.cs` | new |
| `microservice/beamable.tooling.common/SharedRuntime/IBeamableServices.cs` | `Secrets` property |
| `microservice/microservice/dbmicroservice/BeamableServices.cs` | `Secrets` property |
| `microservice/beamable.tooling.common/SharedRuntime/ZoneServices.cs` | `Secrets` on `IZoneServices` |
| `microservice/beamable.tooling.common/SharedRuntime/ZoneMicroservice.cs` | doc comment |
| `microservice/microservice/dbmicroservice/ZoneServicesImpl.cs` | `ZoneServices` ctor/property; fork `RemoveIfExists` |
| `microservice/microservice/dbmicroservice/MicroserviceStartupUtil.cs` | registrations in both scopes + `ExtractSdks` |
| `microservice/microserviceTests/...` | tests above |
| `microservice/microservice/CHANGELOG.md` | entry |
| `agentic-portal/bundles/services/realms/SecretsTestRealm/` | new test service (step 8a) |
| `agentic-portal/bundles/services/zones/SecretsTestZone/` | new test service (step 8b) |
| `agentic-portal/bundles/extensions/secrets-test/` | new test extension (step 8c) |

## Risks / things to verify

1. **Permissions for microservice-originated calls.** Reading a value needs `RealmSecrets:Write`,
   and zone writes need Admin. I haven't confirmed which role the gateway gives a microservice's
   websocket request, for either scope. The step 8 test bed answers this.
2. **Calls outside a request.** The zone requester gets its `RequestContext` from the per-request
   scope, so calling `Services.Secrets` from the zone root (e.g. at startup) won't have a scope. This
   is the same limitation `Services.Customer` has today. Document it; don't fix it here.
3. **Public interface additions.** `IBeamableServices` and `IZoneServices` get a new member. Any
   external code implementing them (custom test doubles) has to add the property. This is low risk
   but worth a changelog note.

## Decisions

1. **Wrapper surface: keep the first iteration as simple as possible.** Only `GetSecret`,
   `SetSecret`, `DeleteSecret` and `ListSecrets` (one page, cursor passed through). Data-key
   list/rotate and audit logs are left out; they can be added later. Until then they're reachable
   through the generated `IBeamSecretsApi`, which is registered anyway because the wrapper depends
   on it.
2. **Missing key: `GetSecret` throws.** It surfaces the backend's 404 `SecretNotFound`, and there's
   no `TryGetSecret` in this iteration.
3. **Naming:** `Services.Secrets`, with `GetSecret` / `SetSecret` / `DeleteSecret` / `ListSecrets`.

## Follow-ups (next iterations)

- **`TryGetSecret`:** expected to be needed. It returns no value instead of throwing on a 404.
  Decide its shape when we add it (`Promise<Optional<string>>` vs a `(bool, string)` result).
- **Data-key and audit-log operations** on `IMicroserviceSecretsApi`, if services need them.
