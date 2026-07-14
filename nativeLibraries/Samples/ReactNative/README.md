# Beamable · React Native Push Notifications Sample

A focused **Expo (dev client)** React Native app that demonstrates the Beamable
**native push notifications** library (`@beamable/notifications-react-native`) together
with the minimum **Web SDK** (`@beamable/sdk`) integration it needs. One screen shows,
end to end:

- **push-token registration** — request permission, register for remote, and register
  the device's APNs/FCM token with Beamable;
- **listing registered devices** for the player;
- **tracking `Clicked` / `Converted`** funnel analytics (iOS + Android);
- **native events** — `notificationOpened`, `notificationReceived`,
  `notificationPresented`, token, delivery receipts, funnel results — in a live log;
- **deep links** — a tapped notification deep-links into the app (`beamrnsample://details/<id>`).

> 📘 **New to this?** [`INTEGRATION.md`](./INTEGRATION.md) is the step-by-step guide to
> adding Beamable push to a **fresh** React Native app — the Beamable side and the
> notification side.
>
> 🧭 Want to explore the rest of the Web SDK (auth, stats, content, leaderboards, the
> low-level API…)? That moved to the sibling **[`../WebSDKUsageSample`](../WebSDKUsageSample)** sample.

---

## Environment notes

1. **Keep the project on a space-free path.** RN's iOS build scripts don't quote paths,
   so a space anywhere makes `npx expo run:ios` fail with `/bin/sh: …: Permission
   denied`. If you move it, keep the path space-free and update the `@beamable/sdk` +
   `@beamable/notifications-react-native` paths in `package.json` + `metro.config.js`.
2. **Node LTS (20 or 22).** Switch with `nvm use 22` if the CLI misbehaves.

## Prerequisites

- Node LTS (20/22), `npm`
- Xcode + iOS simulator/device, Android Studio + emulator/device
- CocoaPods (`sudo gem install cocoapods`) for iOS
- The Beamable Web SDK built at `../../../web`. If `dist/` is missing there:
  ```bash
  cd ../../../web && npx pnpm@10.8.0 install && npx pnpm@10.8.0 build
  ```

## 1. Install

```bash
npm install
npx expo install --fix   # align native module versions with the Expo SDK
```

## 2. Configure Beamable credentials

Edit `src/beam/config.ts` and set your realm's `cid`, `pid`, and `environment`. The app
runs without this, but **"Connect to Beamable"** will report it's not configured; deep
links and local notifications work regardless.

For **remote** push you also need provider credentials on your realm — `apns_push`
(iOS) / `fcm_push` (Android) — plus `google-services.json` at the project root for FCM
(already wired via `app.json` → `expo.android.googleServicesFile`).

## 3. Run (dev build)

This app uses native modules, so it runs as a **dev build**, not Expo Go:

```bash
npx expo run:ios       # builds + launches on iOS
npx expo run:android   # builds + launches on Android
```

The first run runs `expo prebuild` (generates `ios/` and `android/`) and applies the
config plugin (see below). Later runs are fast (`npm start` to just start Metro).

---

## The single screen (`app/index.tsx`)

| Section | Buttons | What it verifies |
|---|---|---|
| **1 · Web SDK** | Connect to Beamable | `Beam.init()` guest login; shows `beam.player.id` |
| **2 · Permission & remote** | Request permission · Register for remote · Fire local now/in 10s | permission flow, remote registration (token on event), local notifications that deep-link |
| **3 · Devices** | Register this device · List my registered devices | registers the APNs/FCM token with `CampaignService`; lists registrations |
| **4 · Analytics** | Track offer clicked · Track offer converted · Clear native auth | native `Clicked`/`Converted` funnel events (iOS + Android) |
| **5 · Deep links** | Simulate deep link · Navigate directly | OS-routed deep link and in-app navigation |
| **Native events** | *(live log)* | every SDK event with its payload, color-coded |
| **Activity log** | *(text feed)* | outcomes of the button presses above |

The device auto-registers as soon as the `tokenReceived` event fires (section 2 →
section 3). Push **delivery** is driven from the Portal Campaign Builder.

### Deep links from the command line

```bash
# iOS simulator
xcrun simctl openurl booted "beamrnsample://details/42"
# Android emulator/device
adb shell am start -a android.intent.action.VIEW -d "beamrnsample://details/42" com.beamable.rnsample
# cross-platform helper
npx uri-scheme open "beamrnsample://details/42" --ios   # or --android
```

### Notification → deep-link routing

`app/_layout.tsx` uses the package's React hooks: `BeamNotificationEvent('notificationOpened', …)`
opens the payload's deep link via `Linking.openURL`, and `BeamLaunchNotification()` covers a tap
that **cold-launches** the app so it still routes correctly. Background the app, then tap a
notification to test it.

### React hooks (the ergonomic API)

Both screens consume the package's hooks rather than hand-wiring `addListener`/`useEffect`:

- `BeamPushNotifications()` — initializes on mount and returns reactive `{ isSupported, permission,
  token, lastOpened }` plus the Promise-returning `requestPermission` / `registerForRemote`.
- `BeamNotificationEvent(event, handler)` — a lifecycle-managed subscription to any event.
- `BeamLaunchNotification()` — the cold-start payload, as state.

Permission and remote registration `await` their result (`await push.requestPermission()`,
`await push.registerForRemote()`); the matching events still fire too and feed the live event log.

### Android receive-time handler (runs even when killed)

The package's config plugin installs a default `BeamablePushReceivedHandler` (implements
`com.beamable.push.PushNotificationReceivedHandler`) that runs the instant a push arrives
(it just logs). Exercise it by tapping **Fire local now**, or by sending a data-only,
high-priority FCM message to a fully-closed app. Customize it by editing the copy the
plugin writes into `android/app/src/main/java/<pkg>/` after `expo prebuild`.

---

## The `CampaignService` microservice

The app registers each device's push token with the **`CampaignService`** microservice
via three player-facing (`[ClientCallable]`) endpoints; delivery is driven from the
Portal Campaign Builder:

| Endpoint | Purpose |
|---|---|
| `RegisterDeviceToken(token, environment, platform)` | store the caller's APNs/FCM token (de-duplicated) |
| `UnregisterDeviceToken(token)` | remove a token |
| `ListMyDevices()` | list the caller's devices (masked) |

The typed client (`src/beam/beamable/clients/CampaignServiceClient.ts`) is registered
via `beam.use(CampaignServiceClient)` and reached with `getPushService()`. See
[`INTEGRATION.md`](./INTEGRATION.md) for how it's generated and wired.

---

## Project layout

```
app/
  _layout.tsx        # Stack + notification-tap → deep-link routing
  index.tsx          # the single screen (all flows + native-event log)
  details/[id].tsx   # deep-link target screen
src/
  beam/
    config.ts        # cid / pid / environment (EDIT THIS)
    beamClient.ts    # Beam.init() singleton + getPushService()
    beamable/clients/# generated microservice client (CampaignServiceClient)
    pushNotifications.ts # binds device register/list to CampaignServiceClient
  linking/links.ts   # scheme + URL/path helpers
  unity/UnityBridgeSection.tsx  # demo panel over the package's Unity-bridge helpers (web only)
app.json             # registers the "@beamable/notifications-react-native" config plugin
metro.config.js      # withBeamableSdk() from @beamable/notifications-react-native/metro
```

> Screens import `BeamNotifications` + the hooks **directly from
> `@beamable/notifications-react-native`** on every platform — there is no app-side notifications
> wrapper or `.web.ts` file. The package ships its own platform-resolved web build (its
> `index.web.ts`, routed over the built-in Unity-WebView bridge), which Metro auto-selects on web.

> The Expo config plugin (iOS NSE + Android setup) also ships **inside**
> `@beamable/notifications-react-native` (its `app.plugin.js`) — referenced by name in
> `app.json`, no local `plugins/` folder needed.

## Web SDK caveats (short version)

The Web SDK now ships a native `react-native` build target (AsyncStorage-backed token,
config, and content storage), selected automatically by Metro via the package `exports`
`"react-native"` condition. This sample only: (1) imports `@beamable/sdk/react-native/polyfills`
once before the SDK (installs the URL polyfill Hermes lacks), and (2) applies the
`withBeamableSdk` Metro helper from `@beamable/notifications-react-native/metro`. No explicit
token storage — `Beam.init()` defaults to the AsyncStorage store. Full details and the
ordering rules are in [`INTEGRATION.md`](./INTEGRATION.md) § A2.

## Troubleshooting

- **Metro: cannot resolve `@beamable/sdk`** — ensure `dist/` exists in `../../../web`
  (build it, see Prerequisites), then `npx expo start -c`.
- **iOS build fails at `[CP-User] Generate Specs` → `Permission denied`** — a space in
  the project path; move to a space-free directory.
- **"Register for remote" never yields a token** — remote push needs a **physical
  device** + realm push credentials (`apns_push` / `fcm_push`); on Android the package
  name must match a client entry in `google-services.json`.
- **Android `compileDebugKotlin` Compose/Kotlin version mismatch** — Kotlin is pinned to
  1.9.24 via `expo-build-properties` in `app.json`; adjust if you change Expo/RN versions.
