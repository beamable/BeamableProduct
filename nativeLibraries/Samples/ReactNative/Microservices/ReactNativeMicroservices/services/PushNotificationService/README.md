# PushNotificationService

A Beamable microservice that **registers** device tokens and **sends remote push
notifications** to players through both **Apple's APNs** (iOS, token-based / `.p8` auth)
and **Firebase Cloud Messaging** (Android, HTTP v1 / service-account OAuth). Each device
is tagged with a `platform` and the send loop routes it to the matching provider.

## Endpoints

| Method | Attribute | Who can call | Purpose |
|---|---|---|---|
| `RegisterDeviceToken(token, environment, platform)` | `[ClientCallable]` | Any player | Store the caller's device token (de-duplicated; safe to call repeatedly). `platform` is `"apns"` (default, iOS) or `"fcm"` (Android); empty → `"apns"`. `environment` (`"sandbox"`/`"production"`, empty → realm default) applies to APNs only and is ignored for FCM. |
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
- **Delivery (iOS)** — `ApnsClient` talks to Apple over **HTTP/2** using a **provider
  JWT** (ES256, signed with your `.p8` key). The JWT is cached for ~50 min and
  reused across requests (Apple throttles tokens that are refreshed too often).
- **Delivery (Android)** — `FcmClient` talks to Firebase's **HTTP v1 API**. It signs a
  JWT (RS256) with the service-account key, exchanges it for an **OAuth2 access token**,
  caches that token ~50 min, then POSTs each message to
  `…/v1/projects/{project_id}/messages:send`.
- **Routing** — each device carries a `platform` (`"apns"`/`"fcm"`); the send loop sends
  it through the matching client. Provider credentials are loaded lazily, so a player
  with only one platform's devices never fails on the other platform's missing config.
- **Dead-token pruning** — if a provider says a token is dead (APNs `BadDeviceToken` /
  `Unregistered`, FCM `UNREGISTERED` / `INVALID_ARGUMENT`), it's removed from the
  player's registrations automatically.

## Required Realm Config

In **Portal → your Realm → Config**. Configure whichever platform(s) you send to —
each is independent, and a send only needs the config for the platforms it targets.

### iOS — namespace **`apns_push`**

Sends to APNs devices fail with a clear "APNs not configured" message until these are set.

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

### Android — namespace **`fcm_push`**

Sends to FCM devices fail with a clear "FCM not configured" message until this is set.

| Key | Value |
|---|---|
| `service_account_json` | The **full JSON** of a Firebase service-account key (Firebase Console → Project Settings → Service Accounts → **Generate new private key**). It contains `project_id`, `client_email`, `private_key` and `token_uri`. |

> Store the whole JSON blob verbatim in the single key — the service parses out the
> fields it needs. FCM has no sandbox/production split, so the `environment` arg is
> ignored for FCM devices. The service-account secret lives only in Realm Config.

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
