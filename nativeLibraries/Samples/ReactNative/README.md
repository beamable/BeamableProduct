# Beamable · React Native Sample

A minimal **Expo (dev client)** React Native app that:

- integrates the **Beamable Web SDK** (`@beamable/sdk`) — guest login + player id, using a custom AsyncStorage token store;
- handles **deep links** on **iOS and Android** (`beamrnsample://details/<id>`);
- handles **local push notifications** whose taps **deep-link** into the app;
- ships an in-app **test panel** plus CLI recipes so you can test everything.

> Push notifications here are **local** (on-device). The SDK's server-push
> registration (`PushApi`) is included as an optional, documented helper
> (`src/beam/push.ts`) but is not wired into the default flow — sending real
> server pushes would require an FCM/APNS provider configured in your Beamable
> realm.

---

## ⚠️ One environment note before you start

1. **Keep the project on a space-free path.** React Native's iOS build scripts
   (e.g. the `[CP-User] Generate Specs` codegen phase) don't quote paths, so a
   space anywhere in the path makes `npx expo run:ios` fail with
   `/bin/sh: …: Permission denied`. This project lives at a space-free location
   (`…/Work/ReactiveNative`) for that reason — if you move it, keep the path
   space-free and update the `@beamable/sdk` path in `package.json` +
   `metro.config.js` to keep pointing at `BeamableProduct/web`.
2. **Node version.** If you're on a non-LTS Node, Expo targets **Node LTS
   (20 or 22)** — switch with `nvm use 22` if the CLI misbehaves.

---

## Prerequisites

- Node LTS (20/22), `npm`
- Xcode + an iOS simulator (for iOS), Android Studio + an emulator (for Android)
- CocoaPods (`sudo gem install cocoapods`) for iOS
- The Beamable Web SDK built at `../BeamableProduct/web` (already built in
  this setup). If `dist/` is missing there, build it:
  ```bash
  cd ../BeamableProduct/web && npx pnpm@10.8.0 install && npx pnpm@10.8.0 build
  ```

## 1. Install

```bash
npm install
# align native module versions with the Expo SDK (recommended):
npx expo install --fix
```

## 2. Configure Beamable credentials

Edit `src/beam/config.ts` and set your realm's `cid`, `pid`, and `environment`
(`prod` | `stg` | `dev`). The app runs without this, but **"Connect to
Beamable"** will report it's not configured. Deep links and notifications work
regardless.

## 3. Run (dev client build)

This app uses native modules (the Beamable Notifications SDK, `expo-dev-client`),
so it runs as a **dev build**, not Expo Go:

```bash
npx expo run:ios       # builds + launches on the iOS simulator
# or
npx expo run:android   # builds + launches on an Android emulator/device
```

The first run runs `expo prebuild` (generates `ios/` and `android/`) and
compiles. Subsequent runs are fast (`npm start` to just start Metro).

---

## Testing everything

### SDK Explorer (every service)

Home → **"Explore all SDK features →"** opens the SDK Explorer (`app/sdk.tsx`),
which exercises all 7 high-level services. Each button calls a real method on
the live `beam` instance and shows the result (or error). The catalog lives in
`src/beam/sdkCatalog.ts`.

| `beam.*` service | Methods wired in the Explorer | Notes |
|---|---|---|
| `auth` (AuthService) | `loginAsGuest()`, `beam.refresh()`, `loginWithEmail()` | guest works out of the box; email needs an account |
| `player` (PlayerService) | `player.id`, `player.account`, `hasThirdPartyAssociation()` | cached, synchronous |
| `account` (AccountService) | `current()`, `getEmailCredentialStatus()`, `getThirdPartyStatus()`, `addCredentials()` | also exposes add/remove third-party & external identity, email/password update |
| `stats` (StatsService) | `set()`, `get([key])`, `get(all)` | private stats on self |
| `content` (ContentService) | `getManifestEntries()`, `getById()`, `getByType()` | also `getByIds()`, `refresh()`, `syncContentManifests()` |
| `announcements` (AnnouncementsService) | `list()`, `refresh()`, `markAsRead()`, `claim()` | also `delete()` |
| `leaderboards` (LeaderboardsService) | `get()`, `getRanks()`, `getAssignedBoard()`, `setScore()` | needs a leaderboard id from your realm; also `getFriendRanks()`, `freeze()` |

Some methods need external input the sample can't synthesize (third-party OAuth
tokens, email-verification codes, friend gamertags); those are listed in the
"Notes" column and documented inline in `sdkCatalog.ts` rather than wired to a
button.

#### Low-level API layer (`@beamable/sdk/api`)

Below the high-level services, the Explorer also exercises the raw generated
REST bindings (called with `beam.requester`) for the **player-facing** modules:

| Area | Representative call |
|---|---|
| social | `socialGetMyBasic` |
| inventory | `inventoryGetItemsBasic`, `inventoryGetCurrencyBasic` |
| commerce | `commerceGetCatalogBasic` |
| mail | `mailGetByObjectId(player.id)` |
| presence | `presencePostQuery([player.id])` |
| cloudsaving | `cloudsavingGetBasic` |
| events | `eventsGetRunningBasic`, `eventsGetContentBasic` |
| tournaments | `tournamentsGetBasic`, `tournamentsGetMeBasic` |
| sessions | `sessionGetClientHistoryBasic` |
| lobby | `lobbiesGet` |
| matchmaking | `matchmakingGetTickets` |
| notifications | `notificationGetBasic` |
| push | `pushPostRegisterBasic` (demo token) |
| trials | `trialsGetBasic` |
| players/stats · tickets · presence | `playersGetStatsByPlayerId`, `playersGetMatchmakingTicketsByPlayerId`, `playersGetPresenceByPlayerId` |
| groups / party / calendars | `groupsGetByObjectId`, `partiesGetById`, `calendarsGetByObjectId` (need an id in the `objectId` input) |

**Intentionally omitted** (server/admin-only — they reject a client guest token,
so wiring them would only produce auth errors): `PaymentsApi`,
`BeamoApi`/`BeamoOtelApi`, `RealmsApi`, `CustomerApi`, `BillingApi`,
`SchedulerApi`, and admin endpoints on otherwise-client modules. They remain
importable from `@beamable/sdk/api` for server / Microservice use.

### A. The in-app test panel (Home screen)

| Button | What it verifies |
|---|---|
| **Connect to Beamable** | `Beam.init()` guest login; shows `beam.player.id` |
| **Request permission** | iOS/Android notification permission flow |
| **Fire now → Details #777** | Immediate local notification; tap → deep-links to `/details/777` |
| **Fire in 3s → #888** | Background the app, tap the notification → cold/background deep-link |
| **Simulate deep link → #123** | Opens `beamrnsample://details/123` through the OS |
| **Navigate directly → #55** | Plain in-app navigation |
| **Add / Greet / WhoAmI / Visit** | Calls the `SampleService` microservice (see below) |

### B. Deep links from the command line

iOS simulator:
```bash
xcrun simctl openurl booted "beamrnsample://details/42"
```

Android emulator/device:
```bash
adb shell am start -a android.intent.action.VIEW \
  -d "beamrnsample://details/42" com.beamable.rnsample
```

Cross-platform helper (also in `npm run deeplink:ios` / `deeplink:android`):
```bash
npx uri-scheme open "beamrnsample://details/42" --ios
npx uri-scheme open "beamrnsample://details/42" --android
```

You can also type `beamrnsample://details/42` into the simulator's Safari, or
put it behind a link in Notes/Messages, to confirm real external entry points.

### C. Notification → deep-link routing

`app/_layout.tsx` listens for Beamable notification taps (`notificationTapped`)
and opens the payload's `deepLink` URL via `Linking.openURL`. It also calls
`getLaunchNotification()` so a tap that **cold-launches** the app still routes
correctly. Background the app, then tap a notification to exercise that path.

### D. Beamable native notifications (iOS + Android)

Sections **2** and **3** on the Home screen exercise the **native** Beamable
Notifications SDK (the app's only notification system): iOS via
`beamable-notifications-ios` (Swift core) and Android via
`beamable-notifications-android` (the prebuilt `.aar`'s `BeamablePush` /
`BeamableDeeplink` bridges). One façade
(`src/notifications/beamableNotifications.ts`) routes per platform, so the same
buttons work on both.

**Android-only — receive-time handler (runs even when the app is killed).**
`plugins/android/BeamablePushReceivedHandler.java` implements
`com.beamable.push.PushNotificationReceivedHandler` and POSTs to a Slack
webhook the instant a push arrives. The Expo plugin registers it via manifest
meta-data. Exercise it two ways:

- **Local (no Firebase):** tap **Fire now** in section 2 — the local
  notification fires the handler.
- **Remote, killed app:** fully close the app, then send a **data-only,
  high-priority** FCM message (Firebase console or `curl`) to this device's FCM
  token. The handler fires from a fresh process and posts to Slack.

**Remote push (section 3) works on Android just like iOS.** `registerForRemote`
(section 2) yields an FCM token, which auto-registers with the `PushNotificationService`
microservice tagged `platform: "fcm"`; "Send remote push to myself" then has the
server deliver a real push via Firebase (the service routes each device to APNs
or FCM by its stored platform). Needs `google-services.json` at the project root
(already wired via `app.json` → `expo.android.googleServicesFile`) and FCM
credentials in the realm config (`fcm_push` namespace).

---

## The sample microservice

A small C# Beamable microservice lives in
`Microservices/ReactNativeMicroservices/services/SampleService`. It shows the
three things almost every service does, one `[ClientCallable]` endpoint each:

| Endpoint | Demonstrates |
|---|---|
| `Add(a, b)` | plain server-side compute |
| `Greet(name)` | string args + a server-built response |
| `WhoAmI()` | reading the authenticated caller from `Context` (trustworthy player id) |
| `Visit()` | server-authoritative state — a *protected* stat only the server can write |

### Build / run the service

```bash
cd Microservices/ReactNativeMicroservices
dotnet tool restore          # restores the pinned `beam` CLI
dotnet beam project build    # compile the service
dotnet beam project run      # run locally (or `beam deploy release -q` to ship)
```

### Calling it from the app

The typed client is generated by the `beam` CLI into
`src/beam/beamable/clients/` (`SampleServiceClient.ts` + `types/`) — regenerate
it after changing the service rather than editing it by hand:

```bash
cd Microservices/ReactNativeMicroservices
dotnet beam project generate web-client --output-dir "../../src/beam"
# (the generator writes into a `beamable/clients/` subfolder of --output-dir)
```

`beamClient.ts` registers it with `beam.use(SampleServiceClient)` (which adds
the typed `beam.sampleServiceClient` accessor), and the app reaches it via
`getSampleService()`:

```ts
const svc = getSampleService();          // null until initBeam() resolves
const sum = await svc.add({ a: 2, b: 3 }); // → 5
const me  = await svc.whoAmI();            // → { userId, cid, pid, isAdmin }
```

Under the hood each call POSTs to
`/basic/{cid}.{pid}.micro_SampleService/{Endpoint}`. The four buttons in section
**4 · Sample microservice** on the Home screen call these and log the result.

---

## How the SDK integration works (and RN caveats)

The Beamable Web SDK targets **browser/node**, not React Native, so three small
adaptations make it work here:

1. **Custom token storage** — `src/beam/RNTokenStorage.ts` implements the SDK's
   `TokenStorage` over `AsyncStorage` (the SDK's officially supported extension
   point). Passed via `Beam.init({ tokenStorage })`.
2. **Browser build resolution** — `metro.config.js` forces Metro to resolve the
   SDK's `browser` export condition (fetch-based) instead of the `node` build
   (which imports `node:fs`), and adds the external SDK folder to
   `watchFolders` / module paths (it's a `file:` dependency).
3. **Polyfills** — `src/polyfills.ts` (imported before any `@beamable/sdk`
   import) installs:
   - **`react-native-url-polyfill`** — RN's built-in `URL` mishandles
     `new URL(path, baseUrl)`, which the SDK uses to build *every* request URL;
     without this all SDK network calls fail.
   - `localStorage` (SDK config + token defaults), `DOMException` +
     `structuredClone` (needed by `fake-indexeddb`), and a no-op
     `BroadcastChannel`.
   - `fake-indexeddb` (the SDK's content-manifest cache) is imported in
     `beamClient.ts` right after the polyfills it depends on.

Consuming the SDK (`src/beam/beamClient.ts`):
```ts
const tokenStorage = await RNTokenStorage.create(BEAM_CONFIG.pid);
const beam = await Beam.init({ cid, pid, environment, tokenStorage, gameEngine: 'react-native' });
beam.use([AuthService, AccountService]);
beam.player.id; // authenticated guest player
```

> The SDK has no official `react-native` export condition. The shims above are
> what make `Beam.init()` succeed in RN; treat them as a sample integration, not
> a Beamable-supported configuration.

### Optional: register a native push token with Beamable

See `src/beam/push.ts`. The device token comes from the Beamable Notifications
SDK's `tokenReceived` event — call `registerForRemote()` (see
`src/notifications/beamableNotifications.ts`) on a **physical device** with a
**dev build**, then register it:
```ts
// In the `tokenReceived` listener (FCM on Android / APNS on iOS):
await registerPushToken(beam, Platform.OS === 'android' ? 'fcm' : 'apns', token);
```

---

## Universal / App Links (optional, advanced)

The custom `beamrnsample://` scheme works out of the box. For `https://` links:

- **iOS** — add to `app.json` → `expo.ios.associatedDomains`:
  `["applinks:links.yourgame.com"]`, and host
  `/.well-known/apple-app-site-association` on that domain.
- **Android** — add an `expo.android.intentFilters` entry with
  `"autoVerify": true` for your `https` host, and host
  `/.well-known/assetlinks.json`.

Both require a domain you control, so they're left out of this sample.

---

## Project layout

```
app/
  _layout.tsx        # Stack + notification-tap → deep-link routing
  index.tsx          # Test panel (Beam, notifications, deep links)
  details/[id].tsx   # Deep-link target screen
src/
  polyfills.ts       # localStorage / IndexedDB / BroadcastChannel shims
  beam/
    config.ts        # cid / pid / environment (EDIT THIS)
    RNTokenStorage.ts# AsyncStorage-backed TokenStorage
    beamClient.ts    # Beam.init() singleton + getSampleService()
    beamable/clients/# CLI-generated microservice clients (SampleServiceClient)
    push.ts          # OPTIONAL Beamable native-push registration
  notifications/
    notifications.ts # permissions, channel, local notifications, device token
  linking/
    links.ts         # scheme + URL/path helpers
metro.config.js      # resolves the external @beamable/sdk + browser build
Microservices/ReactNativeMicroservices/
  services/SampleService/
    SampleService.cs # the [ClientCallable] endpoints (Add/Greet/WhoAmI/Visit)
    Program.cs       # service entry point
```

## Troubleshooting

- **Metro: cannot resolve `@beamable/sdk`** — ensure `dist/` exists in
  `../BeamableProduct/web` (build it, see Prerequisites), then
  `npx expo start -c` to clear the cache.
- **iOS build fails with `[CP-User] Generate Specs` → `/bin/sh: …: Permission
  denied`** — a space in the project path. Move the project to a space-free
  directory and update the `@beamable/sdk` path in `package.json` +
  `metro.config.js` (see top of this README).
- **Notifications don't appear in foreground** — they should (handler set in
  `notifications.ts`); on Android confirm the `default` channel was created
  (permission button does this).
- **Android: `expo-modules-core:compileDebugKotlin FAILED` — "Compose Compiler
  (1.5.15) requires Kotlin 1.9.25 but you appear to be using 1.9.24"** — the
  Kotlin Gradle plugin resolves to 1.9.24 (what `react-native-gradle-plugin`
  ships) while the Expo template defaults the Kotlin *version* to 1.9.25, so the
  Compose compiler extension and the actual compiler disagree. Fixed here by
  pinning Kotlin to 1.9.24 via the `expo-build-properties` plugin in `app.json`
  (`android.kotlinVersion`). If you change Expo/RN versions and hit this again,
  set that to whichever 1.9.2x the RN plugin uses.
