# Issue: `AssumeRealm(pid, useSignedRequests: true)` gets 401 on `/api/secrets`

**Status:** open, root cause **not** identified. Hypotheses below.
**Found:** 2026-10-13, while testing `Services.Secrets` (see `microservice-secrets-api-plan.md`).

## What it is

From a zone service, calling a realm's secrets through the **signed** `AssumeRealm` path fails on
every operation:

```
IncorrectAuthentication: signed-requester. method=[put]
uri=[https://dev.api.beamable.com/api/secrets/values/secrets-test%2frealm-1791907788228] code=[401]
payload=[{"status":401,"error":"IncorrectAuthentication","description":"You are missing or have provided invalid authentication credentials"}]
```

The same happens for get, list (`GET /api/secrets/values`, no key) and delete.

What does work:
- The same call through the **socket** `AssumeRealm` path (`useSignedRequests: false`) reaches the
  backend with `X-BEAM-SCOPE` = the realm.
- The realm service's own `Services.Secrets`.
- The zone service's `Services.Secrets`.

So the DI fork re-registers the secrets API correctly. Only the signed HTTP requester is rejected.

This likely affects **every** `/api/*` (.NET gateway) call made through signed `AssumeRealm`, not
only secrets. Nothing else had been tried through that path when this was found (see
"Next steps").

## How to reproduce

Test bed in agentic-portal: `bundles/services/zones/SecretsTestZone` has the `RealmRoundTrip`
endpoint, and `bundles/extensions/secrets-test` has the UI.
1. From `agentic-portal/`, run `dotnet beam project run --ids SecretsTestRealm SecretsTestZone secrets-test --detach -q`.
   - **otel workaround:** dev's otel writer endpoint was returning 500, so `SecretsTestRealm/Program.cs`
     adds `--exclude-otel-creds`.
   - **Locked files:** if a build reports "files … are locked by the process N", stop the orphaned
     `dotnet exec …SecretsTest*.dll`.
2. Open `http://localhost:4950/beam-project/zones/ZONE_101282411458731008/secrets-test`.
3. Pick realm `BeamableDev-dev` (`DE_101281213492111362`) and turn on **"AssumeRealm with signed
   requests"**. The switch's thumb intercepts pointer events; click the label text.
4. Click **RoundTrip** in the "Zone service → C# AssumeRealm" panel. Every step shows 401.
5. With the switch off, the same button reaches the backend. Its steps currently fail only with the
   400 from `issue-secret-keys-with-slash.md`.

Direct code path: `ZoneMicroservice.AssumeRealm(pid, 0, true)` →
`RealmScopeFactory.CreateRealmScope` → `realm.Services.Secrets.ListSecrets()`.

## Code involved

**Client** (BeamableProduct):
- `microservice/microservice/dbmicroservice/ZoneServicesImpl.cs`, `RealmScopeFactory.CreateRealmScope`.
  With `useSignedRequests`, it:
  - pins `IRealmInfo` to `realmEnv` (ProjectName = pid);
  - replaces `ISignedRequesterConfig` with `Host = realmEnv.Host` (ws→http, `/socket` stripped)
    and `RealmSecretGenerator = () => realmSecret.Value`;
  - routes `IBeamableRequester`/`IRequester` to `HttpSignedRequester`.
- `ZoneServicesImpl.FetchRealmSecret`: reads the secret from
  `GET /basic/realms/admin/customer` → `customer.projects[name == pid].secret`, using the zone
  scope's `MicroserviceRequester`.
- `microservice/beamable.tooling.common/HttpSignedRequester.cs:128-180`:
  - path normalized to a leading `/`;
  - signature = `SignedRequesterHelper.CalculateSignature(Pid, _secret, path, body, version: "1")`;
  - headers: `X-BEAM-SCOPE: {Cid}.{Pid}` plus the signature header; with `includeAuthHeader`, a
    gamertag header only (no bearer token).
- `cli/beamable.common/Runtime/Api/SignedRequesterHelper.cs:88`: MD5 of
  `secret + pid + version + uriPathAndQuery + body`, base64. GET/DELETE bodies are `null`
  (`GetBodyToSign`).

**Server** (BeamableAPI):
- `BeamableShared/Auth/SchemeHandlers/BeamableAuthenticationSchemeHandler.cs`:
  - a bearer token wins (`HandleAuthenticateWithToken`); the signature path runs only without one;
  - `HandleAuthenticateWithSignature` resolves `RealmInfo` from the scope header;
  - for a realm it signs with `realm.Secret` + `realm.Name`; for a zone, `zone.Secret` + `zone.ZoneId`;
  - it compares against `CalculateSignature(secret, name, $"{PathBase}{Path}", body)`, and also
    against `Path` only (the legacy Unreal workaround);
  - on mismatch it returns `AuthenticateResult.Fail(InvalidSignatureException)`.
- `BeamableShared/Legacy/Requester/LegacyBeamableRequester.cs:150`: `MD5(secret + realmId + "1" + urlPath + body)`.
  This is the same formula as the client.

## Hypotheses (unverified)

1. **Secret or name mismatch.**
   - The secret from `/basic/realms/admin/customer` may not be what the .NET gateway reads as
     `realm.Secret`. That could be a different source, a rotated secret, or a cached `Customer`.
   - The client signs with `Pid` = `DE_101281213492111362`, while the server uses `realm.Name`.
     Confirm `realm.Name` is the pid string.
2. **Path mismatch.**
   - The client signs the generated path (`/api/secrets/values/...`, UrlEncoded key, lowercase
     `%2f`).
   - The server signs `{PathBase}{Path}`. If the gateway's `PathBase` is `/api` and `Path` is
     decoded or re-cased, the strings differ.
   - The list call has no key and still fails, so encoding alone doesn't explain it. A
     `PathBase`/prefix difference still could.
3. **Body for GET.** The client appends nothing (`null`); the server appends `""` from the empty
   body stream. These are equivalent, so this is unlikely.
4. **Error shape.** `IncorrectAuthentication` with "missing or have provided invalid
   authentication credentials" may come from a layer other than the signature handler (e.g. the
   Scala gateway, or an earlier auth step). Confirm which service produced the response, for
   example from response headers or BeamableAPI logs for that request.

## Next steps

1. **Is this secrets-specific, or general to the .NET gateway?** Through signed `AssumeRealm`, call
   a legacy `/basic/...` endpoint (e.g. `realm.Services.Stats`) and another `/api/...` endpoint.
2. **Compute both sides by hand.** Log the client's `dataToSign` inputs in `HttpSignedRequester`
   (secret redacted except its hash) and compare with the server's for the same request. Run
   BeamableAPI locally, or add temporary logging to the handler.
3. **Check the secret.** Compare the secret `FetchRealmSecret` returns with the one `beam` sees
   (`beam project generate-env <realm service> --include-secret` writes `SECRET`).
4. **Regression test.** Once found, add a microservice test that signs a request the way
   `HttpSignedRequester` does and validates it with the server formula.
