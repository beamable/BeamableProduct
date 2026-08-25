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
connects on launch, so without this the status bar reports it's not configured; deep
links and local notifications work regardless.

For **remote** push you also need provider credentials on your realm — `apns_push`
(iOS) / `fcm_push` (Android) — plus `google-services.json` at the project root for FCM
(already wired via `app.json` → `expo.android.googleServicesFile`).

### Pointing at a local stack

To run against a local Beamable backend (e.g. `beam local up` over your LAN), do three things.

**1. Point at the realm** — edit `.beamable/config.beam.json` with the cid, pid and host of your
local stack:

```json
{
  "cid": "95633677400146944",
  "pid": "DE_95633677402244098",
  "host": "http://192.168.x.x:8080"
}
```

This file is the **only** thing that sets the SDK's realm and host: `src/beam/config.ts` imports it
directly and `beamClient.ts` passes it to `Beam.init`. `env.local` / `VITE_API_BASE` does **not**
change the SDK host — that path exists for the web/Unity-WebView variant only. Point `env.local` at
a local URL and leave this file on its committed defaults and every request still goes to
`api.beamable.com`, which shows up as `400 InvalidScopeError: Invalid scope: <cid>.<pid>` because
your local realm does not exist there.

`host` must be reachable **from the device**, not from your dev machine — `localhost` means the
phone itself:

| Target | host |
|---|---|
| Android emulator | `http://10.0.2.2:8080` (alias for the host loopback) |
| Physical device / iPad | your dev machine's LAN IP, e.g. `http://192.168.x.x:8080` |

`beam local up` rewrites the *portal's* config on every stack reset, but **not this file** — and a
reset mints a brand-new cid/pid, so re-copy them after one.

**2. Make sure the SDK is built.** The sample links `@beamable/sdk` straight at the monorepo
(`file:../../../web`), and it bundles `web/dist`, which is **git-ignored** — a fresh clone has none.
`dotnet beam local up --build` builds and publishes it (the plain `beam local up` skips that step),
or build it directly:

```bash
cd ../../../web && npm run build
```

A stale `dist` is worse than a missing one: custom-host support lives in `web/src/core/BeamBase.ts`,
so an old bundle silently ignores `host` and falls back to prod. Verify with
`grep -oE "fromHost|findByApiUrl" ../../../web/dist/react-native/index.mjs` — both must appear.

**3. Build with the local variant** — a LAN backend is plain HTTP, which Android blocks by
default, so use the **`:local`** scripts:

```bash
npm run android:local            # dev build against the local stack
npm run android:local:release    # release APK against the local stack
npm run ios:local
```

These set `APP_VARIANT=local`, which is the **single switch** that makes `app.config.js`
inject `expo-build-properties` `usesCleartextTraffic`. It is **not** committed to `app.json`
and is **not** inferred from the URL — so the build *variant*, not a config value, decides
the native security posture. A plain `npm run android` (no `:local`) always stays TLS-only,
and cleartext never reaches remote/release builds.

> If you set an `http://` host but forget `:local`, the app builds fine but Android blocks the
> traffic — run the `:local` variant instead. On iOS the same omission trips App Transport
> Security, which fails the request before it leaves the device, so it looks like a connection
> problem rather than a config one.

> **Config is read at BUNDLE time.** After editing `config.beam.json` or rebuilding the SDK, restart
> Metro with a cleared cache (`node scripts/with-local.js start --dev-client -c`) or you will keep
> running the previous realm.

Re-run `expo prebuild --clean` (or a fresh `expo run:*`) when switching variants so the
regenerated Android manifest picks up the change. (iOS uses ATS and is unaffected by the
cleartext flag.)

### Forcing a full rebuild (`--clean`)

The `:local` scripts build incrementally, and several caches can silently serve stale code —
most notably a changed `@beamable/sdk` dist, which does *not* invalidate Gradle's release
bundling task, so you reinstall a byte-identical APK. Pass `--clean` to rebuild from scratch:

```bash
npm run android:local:release -- --clean
npm run android:local -- --clean          # works for the dev + iOS scripts too
```

It stops the Gradle daemons, rebuilds the web SDK (`pnpm build` in `web/`), deletes the
Gradle transform-cache entries made from `beamable-notifications-release.aar` (a restaged
`.aar` is otherwise ignored — `gradlew clean` does not clear this), clears the Metro/Expo
caches, re-runs `expo prebuild --clean` (restoring `android/local.properties`, which prebuild
would otherwise wipe), and removes the Gradle build output — then runs the normal build.

Reach for it after editing the web SDK, after restaging the notifications `.aar`, or whenever
a change you know you made doesn't show up on device.

## 3. Run (dev build)

This app uses native modules, so it runs as a **dev build**, not Expo Go:

```bash
npx expo run:ios       # builds + launches on iOS
npx expo run:android   # builds + launches on Android
```

The first run runs `expo prebuild` (generates `ios/` and `android/`) and applies the
config plugin (see below). Later runs are fast (`npm start` to just start Metro).

---

## The screens

Beamable connects **automatically on launch** — there is no connect button. A status strip
above the tabs shows `Ready · player <id>`, and offers **Retry connection** if init failed.

Tabs are listed below in bar order; the bar follows the order of the `Tabs.Screen` children in
`app/(tabs)/_layout.tsx`. Push is the `index` route, so the first tab is also the launch screen.

| Tab (`app/(tabs)/…`) | Controls | What it verifies |
|---|---|---|
| **Push** (`index.tsx`) | Request permission · Register for remote · Opt in/out of push · List my devices · **Debug**: fire local now/in 10s, 5 × Live Activity *(iOS only — hidden on Android)* | permission flow, remote registration (token on event), the `push` rail, local notifications that deep-link, Live Activity widgets |
| **Deep links** (`deeplinks.tsx`) | Simulate · Navigate directly · Open any URL · Last received | OS-routed deep link, in-app navigation, `normalizeDeepLink`'s schemeless back-stop |
| **In-game** (`inbox.tsx`) | Opt in/out of in-game · Inbox (auto-refreshes on focus, ↻ in the corner) | the `ingame` rail and the player's Beamable mailbox |
| **Email** (`email.tsx`) | Account (read-only) · Add email to account · Opt in/out of email | `beam.account.current()` guest-vs-credentialed state; `addCredentials` → POST `/basic/accounts/register`; the `email` rail |
| **Analytics** (`analytics.tsx`) | Campaign/Node ID · Track offer clicked · Track offer converted · Clear native auth | native `Clicked`/`Converted` funnel events (iOS + Android) and the closed-app auth handoff |
| **Unity** (`unity.tsx`) | Send message to Unity | the Unity ↔ React WebView bridge. **Web only** — the tab is hidden on native |

A collapsible console is pinned to the bottom of every tab with two streams behind a tab
strip: **Activity** (outcomes of button presses) and **Native events** (every SDK event with
its payload, color-coded).

The device auto-registers as soon as the `tokenReceived` event fires (Push → Remote
registration). Push **delivery** is driven from the Portal Campaign Builder.

> Rail opt-in state is shown as "last action here", not as current status:
> `MessageRailService` exposes only `optIn` / `optOut` — the platform has no endpoint to read a
> player's registration back.

### Shared state (`src/state/`)

Cross-page state lives in three providers, composed by `AppProviders.tsx`:

- `logContext.tsx` — the two log streams. Split into `useLogActions()` (stable `append`) and
  `useLogData()` (the arrays) so a log line doesn't re-render every page.
- `beamContext.tsx` — auto-init, connection status, account, and rail opt-in.
- `notificationContext.tsx` — funnel coordinates, deep-link routing, device registration on
  `tokenReceived`, and the last captured native deep link.

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

`src/state/notificationContext.tsx` uses the package's React hooks:
`BeamNotificationEvent('notificationOpened', …)` opens the payload's deep link via
`Linking.openURL`, and `BeamLaunchNotification()` covers a tap that **cold-launches** the app so
it still routes correctly. Background the app, then tap a notification to test it. Both handlers
also seed the Analytics tab's funnel coordinates — one subscription per event, not two.

### React hooks (the ergonomic API)

The providers consume the package's hooks rather than hand-wiring `addListener`/`useEffect`:

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
  _layout.tsx        # AppProviders + root Stack
  (tabs)/
    _layout.tsx      # ConnectionBar + <Tabs> + DebugConsole (tab order lives here)
    index.tsx        # Push: permission, remote registration, push rail, Debug group
    deeplinks.tsx    # simulate / open any URL / last received
    inbox.tsx        # in-game rail + mailbox (auto-refresh on focus)
    email.tsx        # account, add-email, email rail
    analytics.tsx    # funnel clicked/converted, native auth
    unity.tsx        # Unity bridge (web only — hidden tab on native)
  details/[id].tsx   # deep-link target screen
src/
  state/             # AppProviders + the three contexts (log / beam / notification)
  ui/                # theme tokens + Section/Button/Field/Collapsible/TabStrip/
                     #   ConnectionBar/DebugConsole/MessageCard
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
- **Android `compileDebugKotlin` Compose/Kotlin version mismatch** — pin a `kotlinVersion`
  on the `expo-build-properties` plugin (now injected by `app.config.js`, see "Pointing at
  a local stack") if you hit this after changing Expo/RN versions.
