# Project Notes — Beamable · React Native Sample

Working notes on how this project is wired together, what's non-obvious, and the
gotchas worth remembering. The user-facing setup/run instructions live in
[`README.md`](./README.md); this file is the "why it works" companion.

---

## What this project is

An **Expo / React Native** app that exercises the **Beamable Web SDK** end to
end: guest login, the high-level services (auth, account, content, stats,
announcements, leaderboards), local notifications, deep links, and a **custom
C# microservice** (`SampleService`). It also integrates the **Beamable
Notifications** native SDK — iOS via `beamable-notifications-ios`, Android via
`beamable-notifications-android` — alongside expo-notifications. See the section
below.

- **App** — `app/` (expo-router): `index.tsx` is the test panel, `sdk.tsx` is
  the full SDK explorer, `details/[id].tsx` is a deep-link target.
- **Beamable glue** — `src/beam/`.
- **Microservice workspace** — `Microservices/ReactNativeMicroservices/`
  (a `beam` CLI workspace; the C# service lives under `services/SampleService`).

---

## Architecture at a glance

```
React Native app (Expo)
  └─ src/beam/beamClient.ts        Beam.init() singleton + service registration
       ├─ config.ts                cid / pid / environment
       ├─ RNTokenStorage.ts        AsyncStorage-backed TokenStorage
       └─ beamable/clients/        CLI-generated microservice clients
                                     SampleServiceClient.ts + types/
                                          │  HTTPS
                                          ▼
Beamable platform  ──routes──▶  SampleService (C# microservice)
                                  services/SampleService/SampleService.cs
```

---

## Beamable Web SDK in React Native — the key caveats

The Web SDK targets **browser/Node**, not React Native, so the integration
leans on a few shims:

1. **Polyfills load first.** `src/polyfills.ts` is imported at the very top of
   `app/_layout.tsx` and `beamClient.ts` (localStorage / IndexedDB /
   BroadcastChannel / DOMException / structuredClone). `fake-indexeddb/auto` is
   imported inside `beamClient.ts` *after* those shims because it depends on
   them — order matters.
2. **Token storage is custom.** `RNTokenStorage` reproduces the SDK's
   `TokenStorage` shape on top of AsyncStorage so the guest session survives app
   restarts. It's passed via `tokenStorage:` and cast (`as unknown as
   TokenStorage`) because the shapes are structurally compatible but not nominal.
3. **Services must be registered.** Accessors like `beam.stats` /
   `beam.content` throw "Call beam.use(...)" until registered. All registration
   happens once in `beamClient.ts` after `Beam.init()`.
4. **Metro resolves the SDK's browser build** (see `metro.config.js`).

---

## Beamable Notifications — native SDK (iOS + Android)

Alongside the cross-platform `expo-notifications` path, the app integrates the
**Beamable Notifications** native SDK on **both platforms**. Both packages live
in-repo and are wired in as `file:` dependencies:

- **iOS** — `beamable-notifications-ios`
  (`../../iOS/BeamableNotifications/reactnative`): a Swift core compiled into the
  RN pod.
- **Android** — `beamable-notifications-android`
  (`../../Android/BeamableNotifications/reactnative`): a thin JS/Gradle package
  that links the prebuilt unified `.aar` (which carries the `BeamablePush` /
  `BeamableDeeplink` RN bridges) and exposes the **same** API + event names.

The single façade `src/notifications/beamableNotifications.ts` lazy-requires the
right package per platform, so app code is identical on both. Calls are no-ops on
web.

### How it's wired

| Piece | Where |
|---|---|
| Package deps | `package.json` → `beamable-notifications-ios` + `beamable-notifications-android` (both `file:` paths into `../../iOS|Android/BeamableNotifications/reactnative`) |
| Metro | `metro.config.js` adds **both** package roots to `watchFolders` (they live outside the project root) |
| JS façade | `src/notifications/beamableNotifications.ts` — platform-routes the lazy `require`; iOS-only methods are no-ops on Android |
| Tap/launch routing | `app/_layout.tsx` — `notificationTapped` + `getLaunchNotification()` open the payload's `deepLink` URL; Android also logs native VIEW-intent capture via `addBeamableDeepLinkListener` |
| Token → backend | `app/index.tsx` `tokenReceived` listener → `registerDevice(token)` (`src/beam/pushNotifications.ts`) — APNs token (iOS) / FCM token (Android) |
| UI | `app/index.tsx` sections **2b** (native SDK) and **2c** (remote push microservice) |
| Native setup | `plugins/withBeamableNotifications.js` (Expo config plugin) — iOS entitlements/NSE **and** the Android receive-time handler; registered in `app.json` |

### The native module is event-driven

Methods like `requestPermission()` / `registerForRemote()` return **void**; the
result arrives later on an event (`permissionResult`, `tokenReceived`,
`notificationTapped`, …). Subscribe with `addBeamableListener(event, handler)`
(re-exported with the SDK's per-event payload typing) and `.remove()` on unmount.

### Deep links

`scheduleBeamableDeepLink(...)` stashes a full `beamrnsample://details/<id>` URL
under `userInfo.deepLink`. On tap, `_layout.tsx` opens it via `Linking.openURL`,
which expo-router resolves — the same path a real server push carrying a deep
link would take. (The expo-notifications path instead routes a `path` string
through `router.push`; both demonstrate notification → route.)

On **Android**, URL-scheme VIEW intents are also captured natively by the
deeplink module and surfaced via `addBeamableDeepLinkListener`. expo-router
already navigates those, so `_layout.tsx` only **logs** the native capture (to
avoid double-routing).

### Android specifics

The Android bridges already ship inside the unified `.aar`
(`com.beamable.push.react.ReactPushModule` → `BeamablePush`,
`com.beamable.deeplink.react.ReactDeepLinkModule` → `BeamableDeeplink`). The
`beamable-notifications-android` package adds the JS glue:

- **AAR linkage** — `android/build.gradle` links a copy of the `.aar`
  (`android/libs/beamable-notifications-release.aar`) and re-declares its
  transitive deps (`androidx.core`, `firebase-messaging`), since a loose `.aar`
  has no POM. **Refresh that copy** from
  `EnginePlugins/Unity/Plugins/Android/beamable-notifications-release.aar` if you
  rebuild the library (e.g. via `./dev-native.sh`).
- **Autolinking** — `react-native.config.js` points at a single aggregator
  `com.beamable.reactnative.BeamableNotificationsPackage` that registers both
  bridges (Android autolinking takes one package per dependency).
- **API translation** — `src/index.ts` normalizes the Android bridge events
  (`onMessageForeground`, `onNotificationOpened`, `onTokenReceived`, …) to the
  cross-platform event names + `NotificationData`, and translates the iOS
  `LocalRequest` into the Android `NotificationTemplate` JSON (channel
  `deeplink_channel`).
- **Receive-time handler (the Android-only feature)** —
  `plugins/android/BeamablePushReceivedHandler.java` implements
  `com.beamable.push.PushNotificationReceivedHandler` and POSTs to a Slack
  webhook the moment a push arrives — **even when the app is killed** (it's
  instantiated by reflection in a fresh process). The config plugin copies it
  into the generated app package and registers it via the AndroidManifest
  meta-data `com.beamable.push.notification_received_handler`. It fires for
  **local** notifications (no Firebase needed, via `NotificationActionReceiver`)
  and for **remote data-only, high-priority FCM** messages. iOS has no handler
  class — its closest equivalent is the Notification Service Extension.
- **Remote FCM (section 2c — full parity with iOS)** —
  `app.json`'s `expo.android.googleServicesFile` points at `./google-services.json`;
  the plugin applies the google-services Gradle plugin. `registerForRemote()`
  fetches the FCM token; `registerDevice` (`src/beam/pushNotifications.ts`) tags it
  `platform: "fcm"` (vs `"apns"` on iOS) so the `PushNotificationService` routes it
  to `FcmClient`. "Send to myself" then delivers a real FCM push (an FCM
  `notification` message → shows in the tray, opens the app on tap). Requires FCM
  credentials in the realm config (`fcm_push` namespace).
- **Killed-app handler over the air** — the microservice sends a `notification`
  message, which the OS auto-displays in background/killed (so `onMessageReceived`
  — and thus the receive-time handler — does NOT run while killed; it runs on tap →
  launch). To fire the handler from a *fully killed* process, send a **data-only**,
  high-priority FCM message (Firebase console / curl); `PushFirebaseService` then
  invokes `BeamablePushReceivedHandler` directly. Local notifications also trigger
  it with no Firebase at all.

### The config plugin (`plugins/withBeamableNotifications.js`)

`expo prebuild` regenerates `ios/` and `android/` from scratch, so native
capabilities must be expressed as a plugin or they're lost. On **Android** the
plugin copies `BeamablePushReceivedHandler.java` into the app package and adds
the `notification_received_handler` manifest meta-data (both derive the package
from `android.package`). On **iOS** it re-applies, on every prebuild:

1. **Push Notifications** — `aps-environment` entitlement (`development`; Xcode/
   EAS flips to `production` for release signing).
2. **Background Modes** — `remote-notification` in `UIBackgroundModes`.
3. **App Group** — entitlement on the app **and** the NSE, so they share the
   container used for closed-app analytics + delivery receipts.
4. **`BMNAppGroup`** — Info.plist key the SDK reads for that container; default
   `group.com.beamable.rnsample` (set via the plugin's `appGroup` prop in
   `app.json`).
5. **Notification Service Extension target** *(opt-in,
   `"enableServiceExtension": true`)* — copies the SDK's NSE sources
   (`extension/NotificationService.swift` + `ServicePlugins/*.swift`) into
   `ios/BeamableNotificationServiceExtension/` and registers an app-extension
   target (rich media + receipts). Off by default — see the gotcha below.

### How the native side is built (important)

RN does **not** link the prebuilt xcframework that Unity/Unreal use. Vendoring a
Swift-module xcframework through CocoaPods is fragile (the SPM-built framework
ships no importable `Modules/*.swiftmodule`, so `import BeamableNotifications`
fails with "Unable to find module dependency"). Instead the RN pod
(`BeamableNotificationsRN`) **compiles the Swift core from source**:

- The core sources are mirrored into the package at `reactnative/ios/core/`
  (CocoaPods sandboxes `source_files` to the pod root, so they can't be globbed
  from `../core`). Re-mirror after editing the core:
  `./scripts/sync-rn-core.sh`.
- The bridge and core share one Swift module, so `BeamableNotificationsModule.swift`
  references `NotificationManager` / `LocalRequest` / `JSON` **without** an
  `import` — and the file needs `import React` for `RCTEventEmitter`.
- The pod is named **`BeamableNotificationsRN`** (distinct from the core module
  name `BeamableNotifications`).

### Setup / gotchas

- The iOS package ships its **TS as `lib/`** (`main: lib/index.js`); build it with
  `cd ../../iOS/BeamableNotifications/reactnative && npm install && npm run build`
  if `lib/` is missing (it's gitignored). The Android package resolves straight
  from `src/` (`main: src/index.ts`) — Metro transpiles it, so no build step.
- `npm install` symlinks the `file:` deps into `node_modules/`. If Metro can't
  resolve a package, recreate the symlink, e.g.
  `ln -sfn /abs/path/to/iOS/BeamableNotifications/reactnative node_modules/beamable-notifications-ios`.
- Native push (token, rich media, closed-app analytics) needs a **physical
  device**, a real **App Group** enabled on your Apple account, and APNs
  configured on the Beamable realm. The simulator delivers local notifications
  but yields no APNs token.
- **App Group id is hardcoded** to `group.com.beamable.rnsample` in `app.json`'s
  plugin props — change it to one your provisioning profile owns.
- The **Notification Service Extension is opt-in** (`"enableServiceExtension":
  true` in the plugin props) and off by default: the core uses `UIApplication`,
  which an app extension can't compile from source, so the NSE needs the
  extension-API-safe framework path before it can be turned on. Local + remote
  registration work without it.

---

## The microservice (`SampleService`)

A small C# Beamable service demonstrating the three things almost every service
does, one `[ClientCallable]` endpoint each:

| Endpoint     | Demonstrates                                                        |
|--------------|---------------------------------------------------------------------|
| `Add(a,b)`   | plain server-side compute                                           |
| `Greet(name)`| string args + a server-built response                               |
| `WhoAmI()`   | reading the authenticated caller from `Context` (trustworthy id)    |
| `Visit()`    | server-authoritative state — a *protected* stat only the server writes |

Source: `Microservices/ReactNativeMicroservices/services/SampleService/SampleService.cs`

### Server-side API facts (verified against runtime 7.2.0)

- The `Microservice` base exposes protected `Context` and `Services`.
- `Context` is a `RequestContext` with the trustworthy caller info:
  `UserId` (long), `Cid`, `Pid`, `IsAdmin`, `Scopes`, etc. **Never trust a
  player id sent up from the client — read `Context.UserId`.**
- `Services` is an `IBeamableServices` with sub-APIs: `Stats`, `Inventory`,
  `Auth`, `Content`, `Leaderboards`, `Mail`, `Commerce`, `Social`, `Groups`,
  `Events`, `Tournament`, `Notifications`, `Scheduler`, …
- Stats access used here: `Services.Stats.GetProtectedPlayerStat(userId, key)`
  and `SetProtectedPlayerStat(userId, key, value)`. **Protected** stats are
  server-write / client-read — the right choice for a counter that mustn't be
  forged. (**Public** stats are client-writable.)

### Build / run

```bash
cd Microservices/ReactNativeMicroservices
dotnet tool restore          # restores the pinned beam CLI (7.2.0, .config/dotnet-tools.json)
dotnet beam project build    # compile
dotnet beam project run      # run locally  (services run / ps are REMOVED in this CLI)
dotnet beam deploy release -q  # ship to the cloud realm
```

---

## The microservice (`PushNotificationService`) — remote push via APNs

A second C# service that registers APNs device tokens and sends **remote** push
notifications through Apple (token-based `.p8` auth over HTTP/2). This is the
server side of the "2c · Remote push (APNs microservice)" panel in `app/index.tsx`.

| Endpoint | Attr | Purpose |
|---|---|---|
| `RegisterDeviceToken(token, environment)` | `[ClientCallable]` | store the caller's APNs token (de-duplicated) |
| `UnregisterDeviceToken(token)` | `[ClientCallable]` | remove a token |
| `ListMyDevices()` | `[ClientCallable]` | list the caller's devices (masked) |
| `SendPushToSelf(title, body, deepLink)` | `[ClientCallable]` | push to the caller's own device(s) |
| `SendPushToPlayer(playerId, …)` | `[AdminOnlyCallable]` | back-office: push to any player |

Source: `Microservices/.../services/PushNotificationService/` — see its `README.md`
for the **Realm Config** keys you must set (`apns_push` namespace: `auth_key`,
`key_id`, `team_id`, `bundle_id`=`com.beamable.rnsample`, `default_environment`).

- **Storage:** device tokens live in a *private per-player stat* (`apns_devices`,
  a JSON array) — no MongoDB. The privileged service identity can read any
  player's tokens, which powers the admin endpoint.
- **Delivery:** `ApnsClient` signs an ES256 provider JWT (cached ~50 min) and
  POSTs to `api.sandbox.push.apple.com` / `api.push.apple.com`. Dead tokens
  (`BadDeviceToken`/`Unregistered`) are pruned automatically.
- **App flow:** the native `tokenReceived` event → `registerDevice(token)` (in
  `src/beam/pushNotifications.ts`) → server stores it. This **replaces** the old
  built-in PushApi call (`src/beam/push.ts` is now unused by the app).
- **Caveat:** remote push needs a **physical iOS device** (APNs never delivers to
  the Simulator) and the realm config above.

---

## Client generation — important workflow

The TypeScript client is **CLI-generated, not hand-written**. Regenerate it
after changing the service; don't edit the files (they carry a
"DO NOT EDIT" header and are overwritten):

```bash
cd Microservices/ReactNativeMicroservices
dotnet beam project generate web-client --output-dir "../../src/beam"
# writes into a beamable/clients/ subfolder of --output-dir:
#   src/beam/beamable/clients/SampleServiceClient.ts
#   src/beam/beamable/clients/types/index.ts
```

### How the generated client looks / behaves

- Extends `BeamMicroServiceClient`, declares `serviceName = "SampleService"`.
- **Args are objects, not positional**: `add({ a, b })`, `greet({ name })`.
- It augments the SDK via `declare module '@beamable/sdk'` to add a typed
  `beam.sampleServiceClient` accessor — available **after**
  `beam.use(SampleServiceClient)`.
- The constructor is **public** (unlike the abstract base, whose constructor is
  `protected`), so `beam.use(SampleServiceClient)` type-checks and registers it.

### Wiring in the app (`src/beam/beamClient.ts`)

```ts
beam.use([AuthService, AccountService, ContentService, StatsService,
          AnnouncementsService, LeaderboardsService]);
beam.use(SampleServiceClient);   // adds beam.sampleServiceClient

export function getSampleService() {
  return beamInstance?.sampleServiceClient ?? null;
}
```

`beam.use()` is overloaded — it accepts either an array or a single ctor, so the
microservice client can live in its own `use(...)` call or in the array; both
behave identically.

---

## Gotchas discovered

- **Wire format.** A microservice call POSTs to
  `/basic/{cid}.{pid}.micro_{ServiceName}/{Endpoint}`. The runtime reads a
  `payload` field from the body; the generated client handles this for you.
- **`bigint` in results.** Generated result types use `bigint | string` for ids
  (e.g. `WhoAmIResult.userId`). Plain `JSON.stringify` **throws** on bigint —
  `app/index.tsx` uses a replacer (`typeof v === 'bigint' ? \`${v}n\` : v`).
  `app/sdk.tsx` has the same `pretty()` helper.
- **`beam project oapi` fails locally** with a "could not find a project to run"
  error. This is a CLI quirk with the **space in the repo path**
  (`.../Claude Projects/...`), not a code problem. `dotnet build` and
  `beam project build` both succeed.
- **CLI surface changed in 7.x.** `beam services run` / `services ps` are
  `[REMOVED]`; use `beam project run` / `beam project ps`. `beam services ls`
  doesn't exist — it's `beam project list` / `project ps`.

---

## Realm / auth context (as configured)

- **cid** `1752011665993752`, **pid** `DE_1885450253346843`, host
  `https://api.beamable.com` (prod). Set in both `src/beam/config.ts` and the
  workspace `.beamable/config.beam.json`.
- The signed-in CLI user (`beam me`) is an **admin** on this realm.

---

## Quick verification

- App TypeScript: `npx tsc --noEmit` → clean.
- Service: `cd Microservices/ReactNativeMicroservices/services/SampleService &&
  dotnet build` → 0 errors (NU19xx package-vulnerability warnings are noise).
- Manual: launch the app → **Connect to Beamable** → use the
  **4 · Sample microservice** buttons; results appear in the activity log.
