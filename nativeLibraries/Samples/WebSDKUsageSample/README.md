# Beamable · Web SDK Usage Sample

A minimal **Expo** React Native app that focuses on **one thing**: using the
**Beamable Web SDK** (`@beamable/sdk`) from React Native. It ships an **SDK
Explorer** — a single screen that runs real SDK methods on a live `beam` instance
and shows the result — so you can see exactly how each service behaves.

> This sample has **no** native notifications. For push notifications (token
> registration, device listing, click/convert analytics, native events) see the
> sibling **`../ReactNative`** sample and its `INTEGRATION.md`.

It was extracted from the notifications sample so the Web SDK integration stands on
its own, with no notification-library dependency.

---

## Environment notes

1. **Keep the project on a space-free path.** RN's iOS build scripts don't quote
   paths, so a space anywhere makes `npx expo run:ios` fail. If you move the
   project, keep the `@beamable/sdk` path in `package.json` + `metro.config.js`
   pointing at `BeamableProduct/web`.
2. **Node LTS (20 or 22).** Switch with `nvm use 22` if the CLI misbehaves.

## Prerequisites

- Node LTS (20/22), `npm`
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

Edit `src/beam/config.ts` and set your realm's `cid`, `pid`, and `environment`
(`prod` | `stg` | `dev`). Until then, the Explorer shows "not configured".

## 3. Run

```bash
npx expo start          # then press i / a, or open in a dev build
# or build to a device / simulator:
npx expo run:ios
npx expo run:android
```

---

## The SDK Explorer

`app/index.tsx` renders the catalog defined in `src/beam/sdkCatalog.ts`. Each button
calls a real method on the live `beam` instance and shows the JSON result (or error).

### High-level services

| `beam.*` service | Methods wired in the Explorer | Notes |
|---|---|---|
| `auth` (AuthService) | `loginAsGuest()`, `beam.refresh()`, `loginWithEmail()` | guest works out of the box; email needs an account |
| `player` (PlayerService) | `player.id`, `player.account`, `hasThirdPartyAssociation()` | cached, synchronous |
| `account` (AccountService) | `current()`, `getEmailCredentialStatus()`, `getThirdPartyStatus()`, `addCredentials()` | also add/remove third-party & external identity |
| `stats` (StatsService) | `set()`, `get([key])`, `get(all)` | private stats on self |
| `content` (ContentService) | `getManifestEntries()`, `getById()`, `getByType()` | also `getByIds()`, `refresh()` |
| `announcements` (AnnouncementsService) | `list()`, `refresh()`, `markAsRead()`, `claim()` | also `delete()` |
| `leaderboards` (LeaderboardsService) | `get()`, `getRanks()`, `getAssignedBoard()`, `setScore()` | needs a leaderboard id from your realm |

Some methods need external input the sample can't synthesize (third-party OAuth
tokens, email-verification codes, friend gamertags); those are noted inline in
`sdkCatalog.ts` rather than wired to a button.

### Low-level API layer (`@beamable/sdk/api`)

Below the high-level services, the Explorer also exercises the raw generated REST
bindings (called with `beam.requester`) for the **player-facing** modules: social,
inventory, commerce, mail, presence, cloudsaving, events, tournaments, sessions,
lobby, matchmaking, notifications, push, trials, players/*, groups, party, calendars.

**Intentionally omitted** (server/admin-only — they reject a client guest token):
`PaymentsApi`, `BeamoApi`/`BeamoOtelApi`, `RealmsApi`, `CustomerApi`, `BillingApi`,
`SchedulerApi`, and admin endpoints on otherwise-client modules. They remain
importable from `@beamable/sdk/api` for server / Microservice use.

---

## How the SDK integration works (and RN caveats)

The Beamable Web SDK targets **browser/node**, not React Native. The adaptations that
make it run here — an `AsyncStorage` token store, browser-global polyfills, and Metro
resolution of the SDK's `browser` build — are packaged in **`@beamable/sdk-react-native`**
(same as the `ReactNative` sample), so this sample just consumes them:

1. **Metro** (`metro.config.js`): `withBeamableSdk(getDefaultConfig(__dirname))` resolves
   the SDK's browser build and watches the external `file:` sources.
2. **Polyfills**: `app/_layout.tsx` does `import '@beamable/sdk-react-native/polyfills'`
   before any SDK import (URL / localStorage / structuredClone / DOMException /
   BroadcastChannel + fake-indexeddb; no-op on web).
3. **Token storage**: `RNTokenStorage` (from `@beamable/sdk-react-native`) + `Beam.init({
   tokenStorage })`.

```ts
// src/beam/beamClient.ts
import { RNTokenStorage } from '@beamable/sdk-react-native';
import { hydrateLocalStorage } from '@beamable/sdk-react-native/polyfills';

await hydrateLocalStorage();
const tokenStorage = await RNTokenStorage.create(BEAM_CONFIG.pid);
const beam = await Beam.init({ cid, pid, environment, tokenStorage, gameEngine: 'react-native' });
beam.use([AuthService, AccountService, /* … */]);
beam.player.id; // authenticated guest player
```

> The SDK has no official `react-native` export condition — the adapter package is what
> makes `Beam.init()` succeed in RN. You still add
> `@babel/plugin-transform-class-static-block` to `babel.config.js` (a package can't edit
> your babel config). Treat this as a sample integration, not a Beamable-supported config.

---

## Project layout

```
app/
  _layout.tsx        # Stack(index) + @beamable/sdk-react-native/polyfills import
  index.tsx          # the SDK Explorer
src/
  beam/
    config.ts        # cid / pid / environment (EDIT THIS)
    beamClient.ts    # Beam.init() singleton + high-level services
    sdkCatalog.ts    # data-driven catalog of SDK features
metro.config.js      # resolves the external @beamable/sdk + browser build
```
