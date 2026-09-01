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
- **deep links** — a tapped notification deep-links into the app (`beamrnsample://details/<id>`);
- **stats → segments** — drive a segment from a stat this app writes itself with `beam.stats`
  (`client.public` / `client.private`, no microservice), or from a **game-private** stat through
  the `PlayerStatsService` microservice, and read back the Portal segments the backend moved the
  player into plus the enter/exit history.

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
# env.local — git-ignored, never committed
VITE_API_BASE=http://192.168.x.x:8080
# Only needed while microservices run via `beam project run` — see below.
BEAM_ROUTING_KEY=<output of `beam fed local-key`>
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
| **Offers** (`offers.tsx`) | Store federation id · Wallet (↻) · Entitlements with the offer, its price and **what the bundle contains** (↻) · **Buy** · Claim · Claim from the last push | the **virtual offer federation** (`IFederatedCampaignVirtualOffer`) end to end, for an offer bought with soft currency. `beam.campaignOffer.getEntitlements(federationId)` → GET `/api/campaign-offer/entitlements` and `beam.campaignOffer.redeem(federationId, grantId)` → POST `/api/campaign-offer/redeem` — the only two routes a player token can reach. The store embeds the whole offer in each entitlement (title, the price as a currency and an amount, and `rewards`), so one call renders the screen. **An offer IS a storefront listing**, and claiming buys it: the provider spends on the player's behalf through `POST /object/commerce/{playerId}/purchase`, which debits the price and credits the bundle in one inventory transaction. "What you received" is a wallet diff around it, not the purchase response — so cost and gain read together. Real-money offers are a separate federation and are not in this sample. Also the deep-link: a campaign that attaches an offer writes the grant id into the push under the reserved `beam_offer_grant` key, and the last section claims straight from it |
| **Segments** (`segments.tsx`) | namespace picker · `CLIENT_LEVEL` card (`beam.stats`) · `PLAYER_LEVEL` card (microservice) · Set/delete any stat in either namespace · Create N players with a stat · My segments (↻) · Recent transitions | the stats → segment loop in both namespaces a rule can read: `beam.stats.set` writes `client.*` directly, `PlayerStatsService.AddToMyStat` writes `game.private`, the backend re-evaluates the Portal rules watching that attribute, and `GET /api/realms/{realmId}/players/{playerId}/segments` (+ `/transitions`) reads the membership back. The screen renders the rule JSON to author, including a cross-namespace one |
| **Analytics** (`analytics.tsx`) | Campaign/Node ID · Track offer clicked · Track offer converted · Clear native auth | native `Clicked`/`Converted` funnel events (iOS + Android) and the closed-app auth handoff |
| **Unity** (`unity.tsx`) | Send message to Unity | the Unity ↔ React WebView bridge. **Web only** — the tab is hidden on native |

> **New to the offer system?** `docs/offer-federation.md` in this repo is a full walkthrough —
> the contract, the gateway routes, how a real-money purchase actually flows, what this tab
> does, and how to run the whole thing locally.

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

## Segments and stat namespaces

A player's stats are split by **namespace** — `{domain}.{visibility}`, one Mongo collection
each — and a segment rule leaf names the namespace its key lives in:

```jsonc
// a stat this app writes
{ "leaf": { "key": "CLIENT_LEVEL", "op": "GTE", "values": [3], "ns": "client.public" } }

// a platform stat — no `ns`, which means game.private
{ "leaf": { "key": "PLAYER_LEVEL", "op": "GTE", "values": [10] } }

// one rule over both
{ "and": { "rules": [ /* the two leaves above */ ] } }
```

An omitted `ns` means `game.private`, so every rule authored before the field existed keeps
meaning exactly what it did.

Which write path the Segments tab uses follows from what a **client token** is allowed to do:

| Namespace | Written by | Why |
|---|---|---|
| `client.public`, `client.private` | this app, `beam.stats.set` | a player may write their own `client.*` stats. A rule leaf naming one now reads it, so no server is involved at all — **the path a shipping game wants** |
| `game.private` | `PlayerStatsService` microservice | the `game` domain needs the privileged identity a microservice runs as, and it is where the platform's own aggregates (`SPEND_*`, `PURCHASES_*`, `SESSIONS_*`) live |

The client path's one caveat is spelled out on screen: its add button does the
read-modify-write **in the client**, because the stats API has no atomic increment and there is
no privileged service in that path — so two devices bumping the same key at once can lose an
increment. `AddToMyStat` does the same work inside the service, in one hop.

## The `PlayerStatsService` microservice

For the `game.private` half, the tab calls **`PlayerStatsService`**, whose `[ClientCallable]`
endpoints always act on the *caller's own* player id:

| Endpoint | Purpose |
|---|---|
| `GetMyStats()` | every game-private stat on the caller (the read path — the client can't read this domain either) |
| `SetMyStat(key, value)` | create/overwrite one stat, absolute value |
| `AddToMyStat(key, amount)` | add to one stat and return the new total (the cards' add buttons) |
| `DeleteMyStat(key)` | remove one stat |
| `CreatePlayersWithStat(count, key, value)` | create N new players carrying one stat, to populate a segment with members (capped at 25 — a QA fixture, not for production clients) |

Writes answer `200` with `success: false` for a rejected key/value, so `src/beam/segments.ts`
throws on that flag rather than treating a resolved call as a success.

The service's source lives in the **agentic-portal** workspace at
`services/PlayerStatsService` (with its own README covering the Portal segment setup), and its
typed client — `src/beam/beamable/clients/PlayerStatsServiceClient.ts`, generated with
`dotnet beam project generate web-client` — is registered via
`beam.use(PlayerStatsServiceClient)` and reached with `getPlayerStatsService()`.

### Reaching a service that runs locally

The service must be reachable in the realm this sample connects to
(`.beamable/config.beam.json`). How it got there decides whether you need one more setting:

- **Deployed** (`beam deploy release`) — it binds with no routing key. Nothing else to do; leave
  `BEAM_ROUTING_KEY` out of `env.local`.
- **Run locally** (`beam project run`, which is also how a local stack starts services) — the
  binding is registered behind a **per-machine routing key**, and the platform routes to it *only*
  for callers presenting that key. Every call otherwise fails with
  `BindingNotFoundException: No binding found for … micro_playerstatsservice`, which reads like
  the service doesn't exist. This applies to **every** microservice, `CampaignService` included —
  it is not specific to stats.

  ```bash
  dotnet beam fed local-key      # → mac-mini-de-felipe_5258…
  # env.local
  BEAM_ROUTING_KEY=mac-mini-de-felipe_5258…
  ```

  `app.config.js` passes it through `extra`, `src/beam/config.ts` exposes it as
  `LOCAL_ROUTING_KEY`, and `beamClient.ts` expands it to
  `X-BEAM-SERVICE-ROUTING-KEY: micro_CampaignService:<key>,micro_PlayerStatsService:<key>`.

  Two safeguards there are deliberate, and worth preserving if you touch that code:

  1. The header is installed **after `Beam.init()`**, by mutating the requester's `defaultHeaders`.
     The SDK also honours a global `BeamBase.env.BEAM_ROUTING_KEY`, but that is read on *every*
     request — including the guest login inside `init()` — so a bad value there breaks
     authentication itself. Applying it afterwards means a wrong key can only break microservice
     calls. (The Portal does the same thing for extension SDK instances.)
  2. The value is validated against `^[A-Za-z0-9_.-]+$` and expanded per service. A malformed key
     is ignored with a console warning rather than sent, because the platform rejects a malformed
     header on every route with `BadRoutingKeyHeaderException`.

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
    offers.tsx       # offer federation: entitlements, claim, claim-from-push
    segments.tsx     # client + game stat cards, namespace picker, membership + transitions
    analytics.tsx    # funnel clicked/converted, native auth
    unity.tsx        # Unity bridge (web only — hidden tab on native)
  details/[id].tsx   # deep-link target screen
src/
  state/             # AppProviders + the three contexts (log / beam / notification)
  ui/                # theme tokens + Section/Button/Field/Collapsible/TabStrip/
                     #   RefreshButton/BalanceRow/OfferCard/
                     #   ConnectionBar/DebugConsole/MessageCard/StatCard
  beam/
    config.ts        # cid / pid / environment (EDIT THIS)
    beamClient.ts    # Beam.init() singleton + getPushService()
    beamable/clients/# generated microservice clients (CampaignService, PlayerStatsService)
    pushNotifications.ts # binds device register/list to CampaignServiceClient
    segments.ts      # namespace model + rule JSON, beam.stats and PlayerStatsService writes, segment reads
    campaignOffers.ts   # offer federation bindings over beam.campaignOffer (entitlements + claim)
    inventory.ts     # currency balances + the wallet diff that answers "what did I receive"
    commerce.ts      # buying a virtual listing: one call, price debited and payout credited together
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
