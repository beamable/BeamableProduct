# BeamableNotifications — Feature Reference

The native notification stack ships push + deep-link support for **Android** (Kotlin `.aar`) and
**iOS** (Swift xcframework), a single shared **React Native** package, and a **Unity** C# package —
all over one cross-platform contract. The two native platforms are first-class equals: every
capability documented here exists on both, with the same public/bridge-facing names.

This document is the authoritative feature reference. Section numbers (`§1`–`§8` and their
subsections) are referenced directly from `§x.y`-anchored comments throughout the code, so the
numbering scheme is stable.

**Related guides:** [`../LIBRARY_GUIDE.md`](../LIBRARY_GUIDE.md) (per-platform feature walkthrough,
ABI/parity tables), the iOS docs under `iOS/BeamableNotifications/docs/`, and the Unity package
under `EnginePlugins/Unity`.

---

## Repository layout

| Area | Location |
|---|---|
| Android core (`com.beamable.push`, `com.beamable.deeplink`) | `Android/BeamableNotifications/notifications/src/main/java/com/beamable/` |
| iOS core (Swift package → xcframework) | `iOS/BeamableNotifications/core/Sources/BeamableNotifications/` |
| iOS Notification Service Extension (NSE) | `iOS/BeamableNotifications/extension/` |
| Unity package (`Beamable.Notifications`) | `EnginePlugins/Unity/` |
| Unified React Native package | `EnginePlugins/ReactNative/` |
| Unreal plugin (`BeamPlatformNotifications`) | `EnginePlugins/Unreal/` |
| Sample microservice (push delivery + Sent event) | `Samples/ReactNative/Microservices/ReactNativeMicroservices/services/PushNotificationService/` |
| React Native sample app | `Samples/ReactNative/` |

---

## 1. Android — multiple receive handlers + file layout

### 1.1 Multiple `PushNotificationReceivedHandler` instances

A **receive-time handler** (`com.beamable.push.PushNotificationReceivedHandler`) runs natively on
every delivery, including when the app is fully killed (the FCM process starts fresh and dispatches
through it). Android supports **N handlers**, mirroring iOS's `PluginRegistry`, and resolves them
from two additive sources (`PushManager.resolveHandlers`):

- **Programmatic** handlers, registered while the app process is alive via
  `PushManager.addNotificationReceivedHandler(handler)` and removed via
  `PushManager.removeNotificationReceivedHandler(handler)`. These are held in a thread-safe
  `CopyOnWriteArrayList` and are only present while the process lives — a closed-app push starts a
  fresh process with an empty list.
- **Manifest-declared** handlers, which **always participate** (they are the only handlers present in
  a freshly-spawned closed-app process). Because the Android manifest merger rejects duplicate
  `<meta-data>` names on one component, handlers are declared with the shared key used as a
  **prefix**:

  ```xml
  <meta-data android:name="com.beamable.push.notification_received_handler"   android:value="com.game.HandlerA" />
  <meta-data android:name="com.beamable.push.notification_received_handler.1" android:value="com.game.HandlerB" />
  <meta-data android:name="com.beamable.push.notification_received_handler.2" android:value="com.game.HandlerC" />
  ```

  `resolveHandlers` scans every `<meta-data>` whose name is the base key (index `-1`, sorts first) or
  the base key plus a `.N` non-negative-integer suffix, instantiates each by reflection once per
  process, and caches the result. Non-numeric suffixes (e.g. `.enabled`) are ignored silently.

The combined list (programmatic first, then manifest, deduped) is dispatched by
`PushManager.dispatchNotificationReceived` from `PushFirebaseService` (delivery) and
`NotificationActionReceiver` (tap). **Each handler's failure is isolated** — a throwing handler
routes to `dispatchError` and does not block the others.

There is no single-handler setter and no legacy/back-compat path: registration is exclusively
add/remove, and manifest handlers always run.

### 1.2 File layout

Cohesive types are grouped into a small number of files (Kotlin allows multiple top-level
declarations per file):

- **`PushModels.kt`** — data models: `PushReceivedEvent`, `NotificationTemplate`,
  `NotificationChannelSpec`, and the cross-platform `NotificationIntentData` / `NotificationOffer`
  schema (§3.3).
- **`PushContracts.kt`** — interfaces: `PushListener`, `PushNotificationReceivedHandler`,
  `EngineBridge`, `DeepLinkListener`.
- **`DeepLinkManager.kt`** — deep-link manager with the intent-extraction and activity-observer
  internals folded in.
- **`BeamableAnalytics.kt`** — the native funnel POSTer (§4).
- Framework-instantiated entry points stay in their own files (`PushFirebaseService`,
  `NotificationActionReceiver`) — the Android framework instantiates them by name.
- Per-engine adapters live in their own packages (`push/unity/`, `push/unreal/`, and the React
  Native side, which now ships from `EnginePlugins/ReactNative`).

**Constraint:** public/JNI-facing class and object names (`UnityNotifications`, `UnrealPush`, …) are
referenced by fully-qualified name from Unity/Unreal via reflection/JNI and must not be renamed.

---

## 2. iOS — file layout and ABI

iOS is already compact. `NotificationManager` (a singleton `UNUserNotificationCenterDelegate`) is the
central object; `PluginRegistry` discovers `NotificationPlugin` implementations from the app's
Info.plist `BMNPlugins` and dispatches to all of them. `Models.swift` holds the model types
(including the §3.3 `CampaignIntentData` / `NotificationOffer` and the §4 `AuthConfig` /
`FunnelEvent`). The small singletons (`TemplateStore`, `CategoryStore`, `LaunchTracker`,
`SharedConfig`) stay as-is. The NSE (`extension/` + `ServicePlugins/`) compiles into a separate
target.

**Constraint:** `CABI.swift` `@_cdecl` function names (`bmn_*`) and the React Native
`RCT_EXTERN_MODULE` method names are an ABI consumed by Unity, Unreal, and React Native — they must
not be renamed or moved.

---

## 3. Cross-platform parity, unified RN package, intent-data schema

### 3.1 Name / method parity

The public/bridge-facing vocabulary (the JS API, the C# API, the event strings) is unified across
platforms; internal native class names may differ where renaming would break JNI/ABI.

| Concept | Unified name / behavior |
|---|---|
| Receive-time hook | N handlers on both platforms — Android `PushNotificationReceivedHandler` list, iOS `NotificationPlugin` via `PluginRegistry` (§1.1) |
| Foreground message event | `onMessageForeground` |
| Tap / open event | `onNotificationOpened` |
| Deep-link key | canonical `deeplink`; iOS keeps tolerant reads of `deepLink` / `deeplink` / `deep_link` |
| Offer tracking | `trackOfferClicked` / `trackOfferConverted` (§4.7) |

### 3.2 Unified React Native package — `EnginePlugins/ReactNative`

A single React Native package contains both native sides plus the shared TypeScript API (sibling to
`EnginePlugins/Unity`):

```
EnginePlugins/ReactNative/
  android/                       # Android native module (BeamablePush, BeamableDeeplink)
  ios/                           # iOS native module (BeamableNotificationsModule, RCTEventEmitter)
  src/                           # shared TypeScript API (single index + helpers)
  package.json                   # one package: @beamable/notifications-react-native
  BeamableNotificationsRN.podspec, react-native.config.js
```

It replaces the two former packages (`beamable-notifications-android` + `beamable-notifications-ios`)
with one package whose native code is platform-conditional (autolinking selects per platform). It
consumes the **built binaries**, not source copies:

- **iOS** — the core via the **xcframework/podspec** (the podspec vendors
  `BeamableNotifications.xcframework`).
- **Android** — the built **AAR** (`beamable-notifications-release.aar`).

`dev-native.sh` copies the AAR into `EnginePlugins/ReactNative/android/` and the xcframework into
`EnginePlugins/ReactNative/ios/` (the iOS copy is macOS-only, matching the Unity copy steps). It
also stages the Unreal plugin's binaries into `EnginePlugins/Unreal/ThirdParty/`: the AAR under
`ThirdParty/Android/`, and (macOS-only) the **dynamic** `BeamableNotifications.embeddedframework.zip`
built via `iOS/BeamableNotifications/scripts/build-xcframework-dynamic.sh`.

### 3.3 Notification Intent Data — JSON schema

One canonical schema, shared by Android, iOS, and the engines, embedded in the notification's data
payload (FCM `data` / APNs `userInfo`). Logical shape:

```jsonc
{
  "campaignId":  "string",          // campaign identifier (gates funnel tracking, §4.2)
  "nodeId":      "string",          // node identifier   (gates funnel tracking, §4.2)
  "gamerTag":    "string",          // Beamable dbid
  "accountId":   "string",          // Beamable account id
  "cidPid":      "string",          // "<cid>.<pid>" realm scope
  "offers": [                       // optional array
    {
      "itemId":     "string",
      "value":      "string|number",
      "customData": { }             // free-form object (generic T at the SDK layer)
    }
  ],
  "campaignData": { },              // free-form object (generic T at the SDK layer)
  "deeplink":     "string"          // raw deeplink, passed through verbatim (schema-less)
}
```

**Wire format.** The payload travels as a **flat string→string map** on both platforms. Scalar fields
(`campaignId`, `nodeId`, `gamerTag`, `accountId`, `cidPid`, `deeplink`) are plain strings; the nested
`offers` array and `campaignData` object are sent **JSON-encoded as strings** and parsed back into
typed objects at the engine/SDK layer. On the wire it looks like:

```jsonc
{
  "campaignId": "summer_sale",
  "nodeId": "node_7",
  "gamerTag": "1234567890",
  "accountId": "acct_42",
  "cidPid": "1657892323.DE_1657892324",
  "deeplink": "game://store/offer/42",
  "offers": "[{\"itemId\":\"gems_100\",\"value\":\"4.99\",\"customData\":{\"sku\":\"g100\"}}]",
  "campaignData": "{\"theme\":\"summer\"}"
}
```

- `offers[].customData` and `campaignData` are free-form (the spec's generic `T`). Native (Kotlin
  `NotificationOffer`/`NotificationIntentData`, Swift `NotificationOffer`/`CampaignIntentData`)
  surfaces them as opaque JSON; they are typed only at the C#/TS layer.
- `value` may be a string or a number on the wire; native keeps it untyped (Kotlin surfaces a raw
  string, Swift a `JSONValue`).
- iOS reads nested fields tolerantly: a stringified value (the canonical form) **or** an
  already-decoded object/array (for locally-scheduled notifications), so engine code is identical
  across platforms.
- `deeplink` is passed through verbatim and is intentionally schema-less.

### 3.4 Deep-link behavior under the schema

The existing deep-link behavior keeps working with the schema present.

- **Android** reads `deeplink` from `remoteMessage.data["deeplink"]` and from intent extras. The
  schema fields are additive string keys; because the FCM data map is string-only, `offers` /
  `campaignData` are carried as JSON strings (the intent reader only copies `String` extras).
- **iOS** lifts the deep link tolerant of key spelling (`deepLink` / `deeplink` / `deep_link`), and
  parses the full campaign intent data out of `userInfo`.
- Cold-start and warm-start paths (Android consumed-once intent markers; iOS `LaunchTracker`) surface
  the **whole** intent-data schema, not just the deep-link string, so a launch from a tracked-campaign
  notification carries its full context.

---

## 4. Analytics funnel

Funnel tracking records a campaign notification's progress through five stages by POSTing Beamable
`CoreEvent`s. There is no webhook and no `configureAnalytics`/`AnalyticsConfig` surface — the funnel
authenticates with the player's persisted bearer token and posts directly to Beamable.

### 4.1 Notifications handled natively

Event capture runs natively so it works when the engine VM is dead — the iOS NSE on the closed-app
path, and Android `PushFirebaseService` / `NotificationActionReceiver`. Scheduling and permission UX
also live natively; the engine SDKs are thin pass-throughs to the native scheduling/permission APIs.
Because that logic is native, any per-game permission-prompt copy or scheduling rules must be
expressed through the native API surface (parameters/config) rather than owned in managed code.

### 4.2 Gating — campaign tracking requires `campaignId` + `nodeId`

A notification is part of a **tracked campaign** only when its intent data carries both `campaignId`
and `nodeId` (Android `NotificationIntentData.isTrackedCampaign`, iOS
`CampaignIntentData.isTrackedCampaign`). A funnel event additionally requires a realm scope
(`cidPid`) and a `gamerTag` so the POST can be authenticated and routed (Android
`hasFunnelCredentials`, iOS `canEmitFunnel`). When any of these are absent the native side is inert —
no event is emitted.

### 4.3 Auth, native POST, and persist-and-replay

Native funnel events are POSTed directly to:

```
POST {host}/report/custom_batch/{cid}/{pid}/{gamerTag}
```

The `{gamerTag}` in the path is routing only; the request must carry a credential. The native side is
a pure reader of a **persisted player bearer token** — the SDK writes the player's session into
native-readable shared storage and keeps it updated on login/refresh, clearing it on logout. The
write is done via `configureAuth` (canonical credential object):

```jsonc
{
  "accessToken":          "string",
  "refreshToken":         "string",
  "accessTokenExpiresAt":  1782134720704,   // absolute epoch MILLISECONDS
  "cid":                  "string",
  "pid":                  "string",
  "host":                 "https://api.beamable.com"
}
```

Keys are individually optional; present ones overwrite, absent ones are left untouched. Malformed
JSON is logged/ignored and never crashes.

- **Android** stores these in the `beamable_notifications_auth` SharedPreferences
  (`PushManager.configureAuth` → keys read by `BeamableAnalytics`); the FCM handler reads them because
  it runs in the app process.
- **iOS** stores them in the App Group `SharedConfig` (decoded as `AuthConfig`), readable by both the
  app and the NSE.

**On POST**, native attaches `Authorization: Bearer {accessToken}` and `X-BEAM-SCOPE: {cid}.{pid}`
(scope from the intent-data `cidPid`, falling back to the stored `cid`/`pid`). All POSTs are
fire-and-forget with short timeouts (well inside the iOS NSE ~30s and Android FCM ~10s budgets). If
the access token is stale it refreshes first (`POST {host}/basic/auth/token` with the refresh token,
no bearer); a 401/403 triggers at most one refresh-and-retry per send. No realm secret is ever
embedded in the client.

**Persist-and-replay.** On an unrecoverable auth/transport failure (no host yet, no usable token,
refresh failed, or any non-2xx after the single retry) the event is **persisted** rather than lost:

- Android: a capped JSON array under the `pending_funnel` pref (deduped by
  `funnelType|campaignId|nodeId|offerItemId`).
- iOS: pending `FunnelEvent`s in the App Group (same dedup key).

Persisted events are **auto-replayed** when the app connects to Beamable: `configureAuth` flushes on
both platforms once fresh credentials are written, and `initialize` flushes on startup for a returning
player whose credentials are already persisted. Replay is best-effort and does not re-persist on
failure (so it can't loop unboundedly).

### 4.4 Microservice "Sent" event

The sample microservice `PushNotificationService.DeliverToPlayer(...)` emits a funnel **Sent** event
via `IMicroserviceAnalyticsService` once per logical send (once at least one of the player's devices
accepts the push), gated on `campaignId` + `nodeId` (§4.2). It is a `CoreEvent`
(`category = "notification_funnel"`, `eventName = "Sent"`) whose params follow §4.6, with `offerData`
set to the first offer the message carried (if any). The authoritative `gamerTag` is always in the
params because the path id is routing only.

### 4.5 Funnel stages

- **Sent** — emitted by the microservice after a successful provider send (§4.4).
- **Received** — fired natively on delivery (foreground and closed-app); from Android
  `PushFirebaseService`, iOS NSE `AnalyticsServicePlugin` / in-app `AnalyticsPlugin`.
- **Opened** — fired natively when the user **taps the notification** (Android
  `onNotificationOpened`, iOS tap handler).
- **Clicked** — fired by the in-app offer-tracking helpers (§4.7) when the user clicks an offer from
  the campaign **inside the app** — distinct from tapping the notification.
- **Converted** — fired by the offer-tracking helpers (§4.7) when the offer click converts.

### 4.6 Event payload (`CoreEvent`)

Each funnel event is a Beamable `CoreEvent` (`op = "g.core"`, `e = <funnelType>`,
`c = "notification_funnel"`). The native POSTs a batch (a JSON array of one event). The `p` (params)
bag:

```jsonc
{
  "campaignId": "string",
  "nodeId":     "string",
  "gamerTag":   "string",
  "accountId":  "string",
  "cidPid":     "string",
  "offerData":  {                    // optional — the SINGLE offer this event concerns
    "itemId":     "string",
    "value":      "string|number",
    "customData": { }
  },
  "deeplink":   "string",
  "funnelType": "Sent|Received|Opened|Clicked|Converted"
}
```

`offerData` is the single offer relevant to this event (the one clicked/converted), drawn from the
§3.3 `offers` array — never the whole array. It is omitted for events with no offer context
(Received/Opened emit no `offerData` to avoid mis-attributing a carried offer). Empty fields are
omitted rather than serialized as explicit nulls, on every platform.

Full envelope as POSTed:

```jsonc
[
  {
    "op": "g.core",
    "e":  "Received",
    "c":  "notification_funnel",
    "p":  { "campaignId": "summer_sale", "nodeId": "node_7", "gamerTag": "1234567890",
            "cidPid": "1657892323.DE_1657892324", "deeplink": "game://store",
            "funnelType": "Received" }
  }
]
```

### 4.7 Offer / conversion helpers

Each SDK exposes a small helper API so a game can record that a user clicked or converted on an offer
and attribute it back to the originating campaign:

- Native: `PushManager.trackOfferClicked(intentDataJson, offerJson)` /
  `trackOfferConverted(...)` (Android); `NotificationManager` offer-track methods over the
  `OfferTrackRequest` shape (iOS); the `bmn_*` offer-track C ABI.
- Unity: `BeamableNotifications.TrackOfferClicked(...)` / `TrackOfferConverted(...)`.
- React Native: `BeamableNotifications.trackOfferClicked(...)` / `trackOfferConverted(...)`.

The helper accepts the campaign context that arrived in the notification's intent data (so the
`Clicked`/`Converted` event carries the originating `campaignId`/`nodeId`) plus the single offer the
user acted on. It is a no-op unless `campaignId` + `nodeId` + scope + `gamerTag` are present (§4.2).

---

## 5. Unity SDK

### 5.1 JSON serialization (no Newtonsoft)

The Unity package serializes with the same JSON stack as the Beamable Unity SDK (no Newtonsoft
dependency). DTOs in `Runtime/Payloads.cs` implement
`Beamable.Serialization.JsonSerializable.ISerializable` and serialize through
`JsonSerializable.ToJson` / `FromJson<T>`, backed by `Beamable.Serialization.SmallerJSON`. Null
omission is replicated with conditional serialization (`if (value != null) s.Serialize(...)`); free-form
`Dictionary<string,object>` payloads (stringified `offers`/`campaignData`, `userInfo`) map to
Beamable's `MapOfObject`.

### 5.2 No push-received handler scaffolding

Receive-time analytics is native (§4), so the package no longer owns or auto-wires a push-received
handler (and no longer auto-wires the old Discord-webhook demo). Setup instead **generates a sample**
`PushNotificationReceivedHandler` file for the user to customize (§5.4).

### 5.3 Depend on `com.beamable`

The package depends on `com.beamable` via the scoped registry
`https://nexus.beamable.com/nexus/content/repositories/unity` and references the Beamable assemblies
(`Beamable.SmallerJSON`, `Unity.Beamable.Runtime.Common`, plus analytics) for the §5.1 serializer and
§4 analytics helpers. `com.beamable` is pinned to `5.1.0` for now (a temporary pin, expected to
auto-adjust to the consumer's installed Beamable version later).

### 5.4 Editor window (`BeamableNotificationsWindow`)

A single setup/validation window exposes:

- **Deep-link / intent-data schema setup and change** (ties to §3.3, §3.4) — the §3.3 schema fields
  are shown as a read-only contract reference.
- **Validation + run setup**, runnable as "all" or per item, covering Android and iOS post-build setup
  (decomposed from the former `BeamableAndroidSetup.Validate()/ApplySettings()`).
- **Push-received handler sample generation** — instead of auto-wiring, it generates a sample
  handler file and opens it.
- **`google-services.json` guidance** — an inline guide/link for obtaining and placing the file.

---

## 6. React Native sample

The reusable logic now lives in the unified `EnginePlugins/ReactNative` package, leaving the sample
with only app-specific glue (realm config, screen wiring, the `beamrnsample` deep-link scheme):

- Device register/unregister/list/send-to-self wrappers and platform/env resolution → package.
- `TokenStorage` over AsyncStorage (generic RN session persistence) → package.
- The cross-platform notification façade → package (minus the removed demo webhook).
- The unused `pushPostRegisterBasic` legacy wrapper was deleted.

The sample consumes the package and keeps app-specific configuration only.

---

## 7. Design decisions

These resolved choices are folded into the design above; recorded here for context.

| Topic | Decision |
|---|---|
| Android multi-handler | Repeated, prefix-keyed `<meta-data>` entries (the manifest merger rejects duplicate exact names). Add/remove only — no legacy single-handler setter. (§1.1) |
| Unified RN package binaries | Consume the iOS xcframework/podspec + the Android AAR; `dev-native.sh` copies both into `EnginePlugins/ReactNative`. (§3.2) |
| Intent-data wire format | Stringify nested objects on both platforms (flat string→string map). (§3.3, §3.4) |
| "All notifications native" | Includes scheduling + permission UX, not just event capture. (§4.1) |
| Funnel auth | Direct native POST to `/report/custom_batch/...`, authenticated with a persisted player bearer token (`Authorization: Bearer` + `X-BEAM-SCOPE`); refresh-on-expiry; persist-and-replay on unrecoverable failure, auto-flushed on connect. Realm-secret signature rejected (it would leak a secret in the client); webhook/`configureAnalytics` removed. (§4.3) |
| Opened vs Clicked | `Opened` = notification tap (native); `Clicked` = in-app offer click (helper) — distinct stages. (§4.5, §4.7) |
| Unity serializer | Beamable's `JsonSerializable` / `SmallerJSON` from `com.beamable`. (§5.1) |
| Unity handler scaffolding | Removed; the setup generates a sample handler file instead. (§5.2, §5.4) |
| `com.beamable` version | Pinned to `5.1.0` for now; auto-adjust to the consumer's version later (temporary). (§5.3) |

---

## 8. Build & tooling

`dev-native.sh` (repo root) builds the native artifacts and copies them into the engine plugins: the
Android AAR and iOS xcframework into `EnginePlugins/Unity/Plugins/`, and (per §3.2) into the
`EnginePlugins/ReactNative` package (the iOS copy guarded to macOS). The prebuilt artifacts under
`EnginePlugins/Unity/Plugins/iOS/BeamableNotifications.xcframework/` are generated outputs and may lag
the source between rebuilds.
