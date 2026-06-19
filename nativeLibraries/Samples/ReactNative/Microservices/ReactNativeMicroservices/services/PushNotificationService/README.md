# PushNotificationService

A Beamable microservice that **registers** APNs device tokens and **sends remote
push notifications** to players through Apple's APNs (token-based / `.p8` auth).

## Endpoints

| Method | Attribute | Who can call | Purpose |
|---|---|---|---|
| `RegisterDeviceToken(token, environment)` | `[ClientCallable]` | Any player | Store the caller's APNs device token (de-duplicated; safe to call repeatedly). `environment` is `"sandbox"` or `"production"` (empty → realm default). |
| `UnregisterDeviceToken(token)` | `[ClientCallable]` | Any player | Remove one of the caller's tokens (e.g. on logout). |
| `ListMyDevices()` | `[ClientCallable]` | Any player | List the caller's registered devices (tokens masked). |
| `SendPushToSelf(title, body, deepLink)` | `[ClientCallable]` | Any player | Send a real APNs push to the caller's own device(s) — the easiest end-to-end demo. |
| `SendPushToPlayer(playerId, title, body, deepLink)` | `[AdminOnlyCallable]` | Admin/dev token | Send a push to any player by id (back-office). |

## How it works

- **Token storage** — device tokens are kept in a **private per-player stat**
  (`apns_devices`, a small JSON array). Stats are built into every realm, so no
  MongoDB is needed for this key/value data. Because the microservice runs with a
  privileged identity it can read any player's tokens, which is what lets the
  admin endpoint target an arbitrary `playerId`.
- **Delivery** — `ApnsClient` talks to Apple over **HTTP/2** using a **provider
  JWT** (ES256, signed with your `.p8` key). The JWT is cached for ~50 min and
  reused across requests (Apple throttles tokens that are refreshed too often).
- **Dead-token pruning** — if APNs replies `BadDeviceToken` / `Unregistered`,
  that token is removed from the player's registrations automatically.

## Required Realm Config

Sends fail with a clear "APNs not configured" message until these are set.
In **Portal → your Realm → Config**, add the namespace **`apns_push`**:

| Key | Value |
|---|---|
| `auth_key` | The full contents of your `AuthKey_XXXXXXXXXX.p8` file (PEM, including the `-----BEGIN PRIVATE KEY-----` lines). |
| `key_id` | The 10-char Key ID of that `.p8` key (Apple Developer → Keys). |
| `team_id` | Your 10-char Apple Developer Team ID. |
| `bundle_id` | The app's bundle id / APNs topic — **`com.beamable.rnsample`** for this sample. |
| `default_environment` | Optional: `sandbox` (default) or `production`. |

> The `.p8` secret lives only in Realm Config — never in the repo or the client.
> Dev builds (`expo run:ios`) and TestFlight use **sandbox**; App Store uses
> **production**. The app registers tokens as `sandbox` (see
> `src/beam/pushNotifications.ts` → `APNS_ENVIRONMENT`).

## Run / deploy

```bash
beam project build  --ids PushNotificationService    # compile-check
beam project run    --ids PushNotificationService    # run locally (Docker)
beam project open-swagger PushNotificationService    # try endpoints in Swagger
beam deploy release                                  # deploy to the realm
```

## Regenerating the TypeScript client

```bash
beam project generate web-client -o <tmp>
```

⚠️ The 7.2.0 generator emits a **combined** `types/index.ts` for *all* services and
currently double-emits when a service exists both locally and remotely. The clean,
deduped client + types already live in
`src/beam/beamable/clients/` (`PushNotificationServiceClient.ts` + `types/index.ts`).
If you regenerate, copy only the single per-service client file and the **deduped**
type block — don't blindly overwrite. Each endpoint also uses a **distinctly-named**
return type (`RegisterResult`, `UnregisterResult`, `SendResult`, `AdminSendResult`)
so the generator never produces duplicate `export type` declarations.
