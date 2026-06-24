# Configuring APNs for iOS

How to set up Apple Push Notification service (APNs) so remote push works with the
Beamable Notifications SDK across React Native, Unity, and Unreal.

## How push works in this SDK

The SDK uses **raw APNs**. It does **not** send pushes — it only:

1. registers the device with APNs (`registerForRemote()`),
2. surfaces the resulting **device token** (a hex string) via the `tokenReceived`
   callback, and
3. receives + presents the notification, raising callbacks and (optionally) running the
   Notification Service Extension on delivery.

**Sending** is your backend's job (your server or Beamable's push API talking to APNs).

```
 App                         Apple (APNs)                Your backend
  │  registerForRemote()                                      │
  │ ───────────────────────────▶ (register)                  │
  │  ◀── device token (hex) ─────                             │
  │  tokenReceived(token) ──────────────────────────────────▶│ store token
  │                                                           │
  │                          ◀── push (token + payload) ──────│ send
  │  ◀── notification ───────────                             │
  │  callbacks fire / NSE runs on delivery                    │
```

So getting push working is two halves: **(A)** the Apple-side credentials + capabilities
that let APNs accept your registration and your backend authenticate, and **(B)** wiring
the token to your backend and sending a correctly-shaped payload.

---

## A. Apple Developer setup (once per app)

### 1. App ID with Push Notifications

Apple Developer portal → **Certificates, Identifiers & Profiles → Identifiers**:

- Create/select the **App ID** matching your bundle id (e.g. `com.yourco.yourgame`).
- Enable the **Push Notifications** capability.
- If you use closed-app analytics / rich media (the NSE), also create an **App Group**
  (e.g. `group.com.yourco.yourgame`) and enable it on **both** the app ID and the NSE's
  app ID (`<bundleId>.BeamableNotificationServiceExtension`).

### 2. An APNs auth key (recommended) or certificate

**Token-based (.p8) — recommended.** One key works for all your apps, dev + prod, and
never expires:

- Keys → **+** → enable **Apple Push Notifications service (APNs)** → download the
  **`AuthKey_XXXXXXXXXX.p8`** (you can only download it once).
- Record three values your backend needs:
  - **Key ID** — the `XXXXXXXXXX` in the filename.
  - **Team ID** — top-right of the developer portal (10 chars).
  - **Bundle ID** — used as the APNs **topic**.

**Certificate-based (.p12) — legacy.** A per-app certificate split into sandbox/production;
expires yearly. Only use this if your push provider requires it.

### 3. Provisioning profile

Your build's provisioning profile must be generated **after** Push Notifications was
enabled on the App ID. With automatic signing in Xcode this is handled for you; with
manual signing, regenerate the profile.

---

## B. Project capabilities

These must be present in the built app. Each sample/engine already automates them — listed
here so you know what "correct" looks like.

| Capability | Why | Where |
|---|---|---|
| **Push Notifications** (`aps-environment`) | lets the app register with APNs | app entitlements |
| **Background Modes → Remote notifications** | lets silent/`content-available` pushes wake the app | `UIBackgroundModes` += `remote-notification` |
| **App Group** | app ↔ NSE shared container (closed-app analytics + receipts) | app **and** NSE entitlements |
| `BMNAppGroup` Info.plist key | tells the SDK which App Group to use | app **and** NSE Info.plist |

### `aps-environment`: development vs production

This entitlement has two values and **they are not interchangeable** — a token registered
in one environment is rejected by the other:

- `development` — debug builds run from Xcode/Unity onto a device, and the APNs **sandbox**
  (`api.sandbox.push.apple.com`).
- `production` — TestFlight, App Store, and ad-hoc/release builds, and the APNs **prod**
  host (`api.push.apple.com`).

The samples set `development` by default; Xcode/EAS flips it to `production` when you
archive for release. **When you send, point your backend at the matching APNs host.**

---

## C. Per-engine: getting the token

The simulator has **no APNs** — you need a **physical device**. On the device, register
and read the token from the callback:

**React Native**
```ts
import BeamableNotifications from 'beamable-notifications-ios';
BeamableNotifications.addListener('tokenReceived', ({ token }) => sendToBackend(token));
BeamableNotifications.addListener('tokenError', ({ error }) => console.warn(error));
BeamableNotifications.requestPermission();   // user must allow first
BeamableNotifications.registerForRemote();
```

**Unity**
```csharp
BeamableNotifications.OnTokenReceived += token => SendToBackend(token);
BeamableNotifications.OnTokenError   += err   => Debug.LogWarning(err);
BeamableNotifications.RequestPermission();
BeamableNotifications.RegisterForRemote();
```

**Unreal** (the API is a `UGameInstanceSubsystem` with Blueprint-assignable delegates)
```cpp
auto* Notifications = GetGameInstance()->GetSubsystem<UBeamableNotificationsSubsystem>();
Notifications->OnTokenReceived.AddDynamic(this, &AMyActor::HandleToken); // void HandleToken(const FString& Token)
Notifications->RequestPermission();   // bAlert/bBadge/bSound default true
Notifications->RegisterForRemote();
```

The token is the **APNs device token as hex** — exactly what an APNs request addresses.

> No token arriving? See Troubleshooting. The usual causes are: running on a simulator,
> missing Push capability/entitlement, a provisioning profile generated before push was
> enabled, or the user denied permission.

---

## D. Register the token with your backend

Your backend stores the token per player/device and uses it to send. With Beamable, the
React Native sample shows the call (`src/beam/push.ts`):

```ts
import { pushPostRegisterBasic } from '@beamable/sdk/api';
await pushPostRegisterBasic(beam.requester, { provider: 'apns', token });
```

For closed-app funnel analytics, persist the player's auth so the NSE can authenticate the
funnel POST (no webhook/endpoint config — see `docs/notifications-feature.md` §4.3):

```ts
BeamableNotifications.configureAuth({
  accessToken,
  refreshToken,
  accessTokenExpiresAt,   // absolute epoch ms
  cid, pid,
  host: 'https://api.beamable.com',
});
```

Your Beamable realm must have an APNs provider configured (upload the `.p8` + Key ID +
Team ID, or the certificate) for Beamable to send on your behalf.

---

## E. The push payload

A minimal alert:

```json
{ "aps": { "alert": { "title": "Welcome back", "body": "Your energy is full" } } }
```

SDK-aware payload (deep link + NSE features). **`mutable-content: 1` is required** for the
NSE to run — that's what enables rich media and closed-app analytics, even when the app is
killed:

```json
{
  "aps": {
    "alert": { "title": "New event!", "body": "Tap to join" },
    "mutable-content": 1,
    "sound": "default"
  },
  "bmnId": "promo-42",
  "deepLink": "yourgame://events/42",
  "media-url": "https://cdn.example.com/promo.png"
}
```

| Key | Effect |
|---|---|
| `aps.mutable-content: 1` | wakes the NSE on delivery (rich media + analytics + receipts) |
| `bmnId` | stable id used for delivery receipts / dedup |
| `deepLink` | surfaced on the `notificationTapped` callback for routing |
| `media-url` (or `bmn.mediaUrl`) | image/video the NSE downloads and attaches |

---

## F. Send a test push directly (token-based)

Useful to confirm Apple-side setup before involving your backend. Replace the
placeholders; `TOKEN` is the hex token from `tokenReceived`.

```bash
TEAM_ID=XXXXXXXXXX
KEY_ID=YYYYYYYYYY
BUNDLE_ID=com.yourco.yourgame
TOKEN=<device-token-hex>
AUTH_KEY=./AuthKey_YYYYYYYYYY.p8

# Build a JWT for APNs (ES256, signed with the .p8)
JWT=$(python3 - "$TEAM_ID" "$KEY_ID" "$AUTH_KEY" <<'PY'
import sys, time, json, base64, hashlib
# Requires: pip install pyjwt cryptography
import jwt
team, kid, keyfile = sys.argv[1], sys.argv[2], sys.argv[3]
tok = jwt.encode({"iss": team, "iat": int(time.time())},
                 open(keyfile).read(), algorithm="ES256",
                 headers={"kid": kid})
print(tok)
PY
)

# Sandbox host for development builds; api.push.apple.com for production.
curl -v -H "authorization: bearer $JWT" \
     -H "apns-topic: $BUNDLE_ID" \
     -H "apns-push-type: alert" \
     -d '{"aps":{"alert":{"title":"Hi","body":"Test"},"mutable-content":1},"bmnId":"test-1"}' \
     --http2 "https://api.sandbox.push.apple.com/3/device/$TOKEN"
```

A `200` means APNs accepted it. For a `content-available` silent push, set
`apns-push-type: background` and `apns-priority: 5`.

---

## G. Troubleshooting

| Symptom | Likely cause |
|---|---|
| `tokenReceived` never fires | Running on a **simulator**; or Push capability / `aps-environment` missing; or provisioning profile predates the push capability; or permission denied (`requestPermission` first). |
| `tokenError` fires | No network at registration, or the App ID lacks Push Notifications. |
| Token arrives, push never delivered | **Wrong APNs environment** (sandbox vs prod mismatch with the build), wrong `apns-topic` (must be the bundle id), bad/expired key, or the device toggled notifications off. |
| Push delivered but NSE doesn't run | `mutable-content: 1` missing from `aps`; or the user force-quit the app (swiped up) — iOS suppresses the NSE then; or the NSE target isn't in the build. |
| Closed-app funnel POST never arrives | NSE not enabled/built, App Group not enabled on **both** targets, `configureAuth({...})` was never called (no bearer token persisted), or the notification isn't a tracked campaign (missing `campaignId`/`nodeId`/scope/`gamerTag`). Events that can't POST are persisted and replayed on next connect. |
| Local notification while app closed sends nothing | Expected — iOS runs no code for local delivery while closed; only remote pushes (via the NSE) can. |

---

## Checklist

- [ ] App ID has **Push Notifications** (and App Group, if using the NSE).
- [ ] Downloaded the **`.p8`** auth key; recorded **Key ID + Team ID + Bundle ID**.
- [ ] Provisioning profile regenerated after enabling push.
- [ ] App entitlements: `aps-environment`, App Group; Info.plist: `remote-notification`, `BMNAppGroup`.
- [ ] Backend / Beamable realm configured with the APNs key.
- [ ] Tested on a **physical device**: permission granted → token received → registered.
- [ ] Test push reaches the device on the **matching environment** (sandbox for debug).
- [ ] (NSE) pushes include **`mutable-content: 1`**.
