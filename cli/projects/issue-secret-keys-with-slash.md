# Issue: secret keys containing `/` fail through the generated secrets client

**Status:** open, root cause identified, fix not decided.
**Found:** 2026-10-13, while testing `Services.Secrets` (see `microservice-secrets-api-plan.md`).

## What it is

`Services.Secrets.GetSecret("a/b")` (and set/delete) fails with:

```
400 InvalidSecretKey: Invalid secret key: 'a%2Fb'. Must be 1-512 characters.
Allowed characters: alphanumeric (a-z, A-Z, 0-9) and /, _, +, =, ., @, -.
```

Keys without `/` work. This matters because real keys use `/` as a namespace separator. The test
realm (`DE_101281213492111362`) already holds `fcm_push/service_account_json`, `apns_push/auth_key`,
`apns_push/key_id`, `apns_push/bundle_id` and `apns_push/team_id`.

The bug isn't in the new `MicroserviceSecretsApi` wrapper. Any caller of the generated client has
it, in every SDK.

## Root cause

**Client: the generated client encodes the whole key, `/` included.**

- `cli/beamable.common/Runtime/OpenApi/BeamSecrets.gs.cs:77` (and `:91`, `:104` for put/delete):
  ```csharp
  string gsUrl = "/api/secrets/values/{key}";
  gsUrl = gsUrl.Replace("{key}", _requester.EscapeURL(key.ToString()));
  ```
- In microservices, `EscapeURL` is `System.Web.HttpUtility.UrlEncode`:
  - `MicroserviceRequester.cs:703`
  - `HttpSignedRequester.cs:81`

  So `a/b` becomes `a%2Fb`. The other requesters (`MicroserviceHttpRequester.cs:127`,
  `CliRequester.cs:396`, `NoAuthHttpRequester.cs:35`) are the same kind of single-call escape.
- The C# generator emits this pattern for every path parameter: `UnitySourceGenerator.cs:663` / `:679`
  (`nameof(IBeamableRequester.EscapeURL)`).
- The web SDK does the same: `web/src/__generated__/apis/SecretsApi.ts:34/58/82` use
  `endpointEncoder(key)` (`web/src/utils/endpointEncoder.ts`). I haven't verified it, but this is
  very likely the same bug.

**Server: the catch-all route keeps `%2F` literal.**

- BeamableAPI `BeamableGateway/Controllers/SecretsController.cs:27/36/48`:
  `[HttpGet/HttpPut/HttpDelete("values/{*key}")]`. The obsolete `RealmSecretsController.cs:30/40/52`
  uses the same template.
- ASP.NET Core route matching does not decode `%2F` in a path segment. The bound `key` is the
  literal `a%2Fb`.
- `RealmSecretsService.ValidateSecretKey` (`BeamableShared/Services/RealmSecrets/RealmSecretsService.cs`,
  regex `^[a-zA-Z0-9/_+=.@-]+$`) rejects the `%` and throws `InvalidSecretKey`.
- There is no `UnescapeDataString` / `UrlDecode` in either secrets controller.

**Why the portal works:** `core/realm/portal-extensions/secrets/src/lib/realmSecretsClient.ts`
(agentic-portal) encodes **each segment** and keeps the slashes:
`key.split('/').map(encodeURIComponent).join('/')`. The catch-all then receives `a/b`.

## How to reproduce

**Fastest, from any realm microservice:**
```csharp
await Services.Secrets.SetSecret("repro/slash", "x");   // 400 InvalidSecretKey
await Services.Secrets.SetSecret("repro-noslash", "x"); // OK
```

**With the test bed in agentic-portal** (`bundles/services/realms/SecretsTestRealm`,
`bundles/services/zones/SecretsTestZone`, `bundles/extensions/secrets-test`):
1. From `agentic-portal/`, run `dotnet beam project run --ids SecretsTestRealm SecretsTestZone secrets-test --detach -q`.
2. Open `http://localhost:4950/beam-project/zones/ZONE_101282411458731008/secrets-test`. Use the
   `ZONE_`-prefixed zid and the org alias: the numeric `/:cid/zones/:zid/` URL lands on a page
   where zone discovery returns 403.
3. Click **RoundTrip** on any panel. Its keys are `<prefix>/<scope>-<ts>`, so every step fails with
   400 `InvalidSecretKey`.
4. Put a slash-free key in "Key (single ops)", then Set / Get / List / Delete. Everything passes
   (verified for the zone and realm panels).

Environment notes, as of 2026-10-13:
- **otel workaround:** dev's `beamo/otel/auth/writer/config` returned 500 on every call.
  `SecretsTestRealm/Program.cs` therefore adds `--exclude-otel-creds` through `LocalEnvModifier`.
  Remove it when dev recovers.
- **Leftover processes:** if a service fails with "files … are locked by the process N", an
  orphaned `dotnet exec …SecretsTest*.dll` from an earlier run is still alive. Stop it.

## Fix options

1. **Backend: decode the key in `SecretsController` (recommended).** Apply
   `Uri.UnescapeDataString(key)` before validation in the three value actions, or a route-value
   transformer. That fixes every generated client (C#, Unity, Unreal, web) and the deprecated
   realm controller at once. It stays compatible with the portal's per-segment encoding, because
   decoding `a/b` is a no-op.
   - Risk: a key that legitimately contains `%` is already rejected by the regex, so decoding
     changes no valid behaviour.
2. **SDK: per-segment encoding for catch-all parameters.** Teach the generators about `{*param}` /
   `{**param}` routes. OpenAPI loses the `*`, so this needs a vendor extension or a hard-coded
   list. That's larger, touches C#/Unity/Unreal/web generators, and still leaves old SDKs broken.
3. **Wrapper only.** `MicroserviceSecretsApi` calls `IBeamableRequester` directly with per-segment
   encoding. It fixes microservices only, and the generated `IBeamSecretsApi` stays broken.

## Verification once fixed

- Run RoundTrip on all three test-bed panels (zone, realm, zone→AssumeRealm socket). All five steps
  should pass.
- Run the isolation check. It writes `<prefix>/isolation-*` keys, so it also depends on this fix.
- Add a BeamableAPI controller test for `PUT /secrets/values/a%2Fb` → stored under `a/b`.
