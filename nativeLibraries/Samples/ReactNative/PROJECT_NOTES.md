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
Notifications** native iOS SDK (`beamable-notifications`) alongside
expo-notifications — see the section below.

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

## Beamable Notifications — native iOS SDK (`beamable-notifications`)

Alongside the cross-platform `expo-notifications` path, the app integrates the
**Beamable Notifications** native iOS SDK (a Swift core exposed to RN via a
NativeModule). It lives outside this repo at
`../ClaudeProjects/BeamableNotifications/reactnative` and is wired in as a
`file:` dependency. **iOS only** — every call is a no-op on Android.

### How it's wired

| Piece | Where |
|---|---|
| Package dep | `package.json` → `"beamable-notifications": "file:../ClaudeProjects/BeamableNotifications/reactnative"` |
| Metro | `metro.config.js` adds the package root to `watchFolders` (it lives outside the project root) |
| JS wrapper | `src/notifications/beamableNotifications.ts` — iOS-gated, lazy-requires the native module |
| Tap/launch routing | `app/_layout.tsx` — `notificationTapped` + `getLaunchNotification()` open the payload's `deepLink` URL through the OS |
| Token → backend | `app/index.tsx` `tokenReceived` listener → `registerPushToken(beam, 'apns', token)` (`src/beam/push.ts`) |
| UI | `app/index.tsx` section **2b · Beamable Notifications (native iOS)** |
| Native iOS setup | `plugins/withBeamableNotifications.js` (Expo config plugin), registered in `app.json` |

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

### The config plugin (`plugins/withBeamableNotifications.js`)

`expo prebuild` regenerates `ios/` from scratch, so native capabilities must be
expressed as a plugin or they're lost. This one re-applies on every prebuild:

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

- The package ships its **TS as `lib/`** (`main: lib/index.js`); build it with
  `cd ../ClaudeProjects/BeamableNotifications/reactnative && npx tsc` if missing.
- `npm install` symlinks the `file:` dep into `node_modules/`. If Metro can't
  resolve `beamable-notifications`, recreate the symlink:
  `ln -sfn /abs/path/to/.../reactnative node_modules/beamable-notifications`.
- The package's `prepare` script falls back to the prebuilt `lib/` when `tsc`
  isn't on PATH, so `npm install` doesn't fail on a bare-`tsc` machine.
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
