# Integrating Beamable push notifications into a fresh React Native app

This guide walks through everything needed to add Beamable push notifications to a
new React Native / Expo app, split into the two halves you must implement:

- **Part A — the Beamable side**: realm config, the Web SDK, and the `CampaignService`
  microservice that stores device tokens and drives delivery.
- **Part B — the notification side**: the native `@beamable/notifications-react-native`
  library, its Expo config plugin, and the runtime flow (permission → token →
  register → events → analytics).

The flows this covers, end to end:

1. **Push-token registration** — get the device's APNs/FCM token and register it with Beamable.
2. **List registered devices** — read back the player's device registrations.
3. **Track `Clicked` / `Converted`** — emit native funnel analytics for a campaign offer (iOS + Android).
4. **Native events** — observe `notificationOpened`, `notificationReceived`, `notificationPresented`, etc.

```
┌── your app (React Native) ─────────────────────────────────────────────┐
│                                                                         │
│  @beamable/sdk (Web SDK)            @beamable/notifications-react-native │
│  ├─ Beam.init() guest login         ├─ initialize() / requestPermission │
│  ├─ CampaignServiceClient           ├─ registerForRemote() ─► token     │
│  │   registerDeviceToken() ◄────────┤  (tokenReceived event)            │
│  │   listMyDevices()                ├─ addListener(...) native events    │
│  └─ tokenStorage (AsyncStorage)     └─ trackOfferClicked/Converted()     │
│                                                                         │
└── Beamable realm ───────────────────────────────────────────────────────┘
     CampaignService microservice · apns_push / fcm_push credentials ·
     Portal Campaign Builder (delivery)
```

Autolinking selects the correct native code per platform — the iOS Swift core (an
`.xcframework`) or the Android `.aar` — so there is **no runtime `Platform.OS`
switch** to pick a package.

---

# Part A — the Beamable side

## A1. Realm credentials

Set your realm's `cid` / `pid` (Beamable Portal → realm settings). In this sample they
live in `src/beam/config.ts`; `environment` is `prod` | `stg` | `dev` (or a custom
name pointing at a local API base via `env.local`).

> **Local stack:** put `VITE_API_BASE=http://<lan-ip>:8080` in an uncommitted `env.local`
> to choose the backend URL, then build with the local variant (`npm run android:local`).
> That variant (`APP_VARIANT=local`) is the single switch that enables Android cleartext
> HTTP via `app.config.js`; it is never committed and never inferred from the URL, so
> plain and remote/release builds always stay TLS-only. See the README's "Pointing at a
> local stack" section.

For **remote** delivery, configure push provider credentials on the realm:

| Platform | Credential namespace | Needed for |
|---|---|---|
| iOS | `apns_push` (APNs cert/key) | delivering to APNs tokens |
| Android | `fcm_push` (FCM service account) | delivering to FCM tokens |

Remote push also requires a **physical device** — neither APNs nor FCM issue a usable
token on a simulator/emulator.

## A2. The Web SDK in React Native — native `react-native` build

The Web SDK (`@beamable/sdk`) ships a first-class **`react-native` build target** with
AsyncStorage-backed token, config, and content storage. Metro selects it automatically via
the package `exports` `"react-native"` condition — `import { Beam } from '@beamable/sdk'`
just works, no adapter package. Two small pieces of wiring remain:

**1. Install:**
```bash
npm install @beamable/sdk @react-native-async-storage/async-storage react-native-url-polyfill
```

**2. `metro.config.js`** — enable package-exports resolution + watch the external `file:`
sources. The helper lives in the notifications plugin:
```js
const { getDefaultConfig } = require('expo/metro-config');
const { withBeamableSdk } = require('@beamable/notifications-react-native/metro');
module.exports = withBeamableSdk(getDefaultConfig(__dirname));
```

**3. Entry + init** — import the polyfills once before any SDK import; no explicit token
storage needed:
```ts
// app/_layout.tsx — very first line (installs the URL polyfill Hermes lacks)
import '@beamable/sdk/react-native/polyfills';

// src/beam/beamClient.ts (abridged)
const beam = await Beam.init({
  cid, pid, environment,
  gameEngine: 'react-native',   // defaults to the AsyncStorage token store
});
beam.use([AuthService, AccountService, /* … */]);
beam.use(CampaignServiceClient);   // adds beam.campaignServiceClient
```

> The guest session persists across app launches automatically: tokens and the
> `beam_cid`/`beam_pid` realm marker both live in AsyncStorage, and the SDK loads them
> (via `TokenStorage.hydrate()` + the async `readConfig`) before `Beam.init()` decides
> whether to reuse or refresh the session — so a cold start no longer looks like a realm
> change.

> The RN build is compiled to ES2021, so Hermes parses it directly — no
> `@babel/plugin-transform-class-static-block` needed.

> **Why here?** Token storage and polyfills are an SDK concern, so they live *inside*
> `@beamable/sdk` (its `react-native` build). The notifications plugin
> (`@beamable/notifications-react-native`) is about push, not the SDK — it only carries the
> `withBeamableSdk` Metro helper, which is build tooling.

## A3. The `CampaignService` microservice

Device registration and listing are player-facing (`[ClientCallable]`) endpoints on a
Beamable microservice. This sample uses **`CampaignService`**, which consolidates
device registration, delivery, and the analytics funnel. The app calls only the
player-facing endpoints — **delivery** is driven from the **Portal Campaign Builder**.

| Endpoint | Attr | Purpose |
|---|---|---|
| `RegisterDeviceToken(token, environment, platform)` | `[ClientCallable]` | store the caller's APNs/FCM token (de-duplicated) |
| `UnregisterDeviceToken(token)` | `[ClientCallable]` | remove a token (e.g. on logout) |
| `ListMyDevices()` | `[ClientCallable]` | list the caller's devices (tokens masked) |

**Generate & register the typed client.** A microservice client is generated from the
service (the Beam CLI emits it) and registered with `beam.use(...)`. In this sample the
generated client lives at `src/beam/beamable/clients/CampaignServiceClient.ts` and
augments the SDK via `declare module '@beamable/sdk'`, giving a typed accessor:

```ts
export class CampaignServiceClient extends BeamMicroServiceClient {
  get serviceName() { return 'CampaignService'; }
  registerDeviceToken(p) { return this.request({ endpoint: 'RegisterDeviceToken', payload: p, withAuth: true }); }
  unregisterDeviceToken(p) { return this.request({ endpoint: 'UnregisterDeviceToken', payload: p, withAuth: true }); }
  listMyDevices() { return this.request({ endpoint: 'ListMyDevices', withAuth: true }); }
}
// after beam.use(CampaignServiceClient): beam.campaignServiceClient.listMyDevices()
```

Each call POSTs to `/basic/{cid}.{pid}.micro_CampaignService/{Endpoint}`. The thin
binding in `src/beam/pushNotifications.ts` (`registerDevice` / `unregisterDevice` /
`listDevices`) is what the UI actually calls.

---

# Part B — the notification side

## B1. Install the package

```jsonc
// package.json — a published version, or a file: path to the plugin source
"@beamable/notifications-react-native": "file:../../EnginePlugins/ReactNative"
```

The package's `react-native.config.js` handles autolinking. Its peer deps are `react`,
`react-native`, and (optionally) `@beamable/sdk` and
`@react-native-async-storage/async-storage`.

## B2. The Expo config plugin

Native push needs platform capabilities that must be applied at prebuild time. The
`@beamable/notifications-react-native` package **ships its own Expo config plugin** (via its
`app.plugin.js`), so you reference it by name in `app.json` — nothing to copy:

```jsonc
"plugins": [
  "expo-router", "expo-dev-client", "expo-asset",
  ["@beamable/notifications-react-native", {
    "appGroup": "group.com.your.app",     // iOS App Group (shared storage for closed-app analytics)
    "enableServiceExtension": true          // iOS Notification Service Extension (rich media / receipts)
  }]
]
```

What the plugin does:

- **iOS**: adds the `aps-environment` + App Group entitlements, the
  `remote-notification` background mode, and a `BMNAppGroup` Info.plist key; disables
  Explicitly Built Modules (an Xcode 16+/iOS 26 workaround); and — when
  `enableServiceExtension` is set — copies the Notification Service Extension sources
  and registers the app-extension target.
- **Android**: registers the receive-time handler
  (`BeamablePushReceivedHandler`, which runs even when the app is killed) via manifest
  meta-data, and injects the `deeplink_scheme` meta-data. You must also add
  `google-services.json` (FCM config) — wired via `app.json` →
  `expo.android.googleServicesFile`, and the Android package must match a client entry
  inside that JSON.

Because these are native changes, the app runs as a **dev build** (`expo run:ios` /
`expo run:android`), not Expo Go.

## B3. Runtime flow

### React hooks (recommended) — how this sample wires it

The package ships three hooks; this sample uses all three (see `app/index.tsx` and
`app/_layout.tsx`). They own subscription lifecycle and expose push state as React state:

```tsx
import {
  BeamNotifications,
  BeamPushNotifications,
  BeamNotificationEvent,
  BeamLaunchNotification,
} from '@beamable/notifications-react-native';

// One hook initializes on mount and tracks support / permission / token / lastOpened:
const push = BeamPushNotifications();

// Both actions RESOLVE with their result (the events still fire too):
const perm = await push.requestPermission();          // PermissionResult
const { token } = await push.registerForRemote();     // APNs (iOS) / FCM (Android)

// Subscribe to any event for the component's lifetime — no manual effect/cleanup:
BeamNotificationEvent('notificationOpened', (n) => { /* route */ });

// Cold-start launch notification, resolved once as state:
const launch = BeamLaunchNotification();
```

> The **same import works on web** — the package ships a platform-resolved web build
> (`@beamable/notifications-react-native`'s `index.web.ts`) that Metro auto-selects, routing
> calls over the built-in Unity-WebView bridge. No per-app `.web.ts` file, and the hooks behave
> identically on native and web.

### Token registration flow (the core loop)

`registerForRemote()` resolves with the token **and** fires the `tokenReceived` event. The
sample forwards the token to `CampaignService` from a `BeamNotificationEvent` handler so
it also covers tokens that arrive unsolicited (e.g. FCM refresh):

```tsx
BeamNotificationEvent('tokenReceived', async ({ token }) => {
  await registerDevice(token, BeamNotifications.devicePushPlatform()); // → campaignServiceClient.registerDeviceToken
});
```

List the player's devices any time with `listDevices()` (→ `listMyDevices()`).

### Native events → deep-link routing

A tapped notification carries a deep link. Route it through the OS both while running
and on cold start (see `app/_layout.tsx`):

```tsx
BeamNotificationEvent('notificationOpened', (n) => {
  const url = BeamNotifications.deepLinkFromNotification(n);   // full deep-link URL, or null
  if (url) Linking.openURL(url);
});

const launch = BeamLaunchNotification();
useEffect(() => {
  if (!launch) return;
  const url = BeamNotifications.deepLinkFromNotification(launch);
  if (url) Linking.openURL(url);
}, [launch]);
```

> **Imperative (non-React) equivalent.** Outside components, call the façade directly:
> `await BeamNotifications.requestPermission()`, `await BeamNotifications.registerForRemote()`,
> and `BeamNotifications.addListener('notificationOpened', …)` / `getLaunchNotification()`.

### Track Clicked / Converted (funnel analytics)

These emit **native** analytics through the Beamable analytics endpoint (iOS + Android)
— they are not Web SDK HTTP calls:

```ts
const intent = { campaignId, nodeId, gamerTag: String(beam.player.id), cidPid: `${cid}.${pid}`, deeplink };
const offer  = { itemId: 'sword_01', value: 100, customData: { tier: 'gold' } };
BeamNotifications.trackOfferClicked(intent, offer);
BeamNotifications.trackOfferConverted(intent, offer);
// outcome arrives on the funnelResult event (Android; iOS follow-up)
```

### Closed-app analytics auth

So the funnel can authenticate when the JS runtime isn't running, hand the player's
tokens to the native side after connecting (this sample does it inside `initBeam()`):

```ts
BeamNotifications.configureAuth({ accessToken, refreshToken, accessTokenExpiresAt, cid, pid, host });
// BeamNotifications.clearAuth() on logout
```

## B4. Event vocabulary

Subscribe with `BeamNotifications.addListener(event, handler)` (or loop over
`BeamNotifications.events`).
The unified TS layer maps the per-platform native names onto one vocabulary:

| Unified event | Payload | iOS native | Android native |
|---|---|---|---|
| `permissionResult` | `{ granted, status }` | `permissionResult` | `onPermissionResult` |
| `tokenReceived` | `{ token }` | `tokenReceived` | `onTokenReceived` |
| `tokenError` | `{ error }` | `tokenError` | `onTokenRefreshError` |
| `notificationPresented` | `NotificationData` | `notificationPresented` | `onMessageForeground` |
| `notificationReceived` | `NotificationData` | `notificationReceived` | *(inert)* |
| `notificationOpened` | `NotificationData` | `notificationTapped` | `onNotificationOpened` |
| `pendingNotifications` | `NotificationData[]` | `pendingNotifications` | — |
| `deliveryReceipts` | `DeliveryReceipt[]` | `deliveryReceipts` | — |
| `funnelResult` | `{ funnelType, ok, statusCode, message }` | *(follow-up)* | `onFunnelResult` |

Android also exposes raw URL-scheme VIEW intents via `addBeamableDeepLinkListener`
(inert on iOS) — expo-router already navigates for those, so the sample only logs them.

---

## Verification checklist

On a **physical device** with a **dev build**:

1. Connect to Beamable (guest login succeeds, `player.id` shown).
2. Request permission → `permissionResult` fires.
3. Register for remote → `tokenReceived` fires; the device auto-registers (a
   `Device registered with CampaignService` line appears).
4. List devices → your token comes back (masked).
5. Fire a local notification, background the app, tap it → `notificationOpened` fires
   and the app deep-links into `details/<id>`.
6. Track clicked / converted → `funnelResult` reports the send.

> Optional: the web build can also run inside a **Unity WebView**, where the same calls route
> to the Unity `com.beamable.notifications` plugin. This is now **built into the package** — its
> `index.web.ts` ships the gree/unity-webview transport, so no app-side web file is required.
> Point a different host at it with `BeamNotifications.setWebTransport(...)`. The sample's
> `src/unity/UnityBridgeSection.tsx` is just a demo panel over the bridge helpers the package
> re-exports.
