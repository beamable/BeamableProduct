# @beamable/notifications-react-native

Cross-platform Beamable **push notifications** for React Native — local + remote (FCM/APNs),
deep links, rich media, closed-app analytics, and campaign offer funnel tracking. One
package; autolinking selects the Android AAR or the iOS xcframework per platform.

## Install

```bash
npm install @beamable/notifications-react-native
```

## Usage — React hooks (recommended)

The idiomatic way to consume the SDK from components. The hooks own subscription lifecycle
(subscribe on mount, remove on unmount) and expose push state as reactive React state — no
manual `useEffect` + `addListener` + `useState` dance.

```tsx
import {
  BeamNotifications,
  BeamPushNotifications,
  BeamNotificationEvent,
  BeamLaunchNotification,
} from '@beamable/notifications-react-native';

function Screen() {
  // Initializes on mount; tracks support / permission / token / lastOpened as state,
  // and returns the Promise-returning actions.
  const push = BeamPushNotifications();

  // Subscribe to any event for the component's lifetime (handler needn't be memoized):
  BeamNotificationEvent('notificationOpened', (n) => {
    const url = BeamNotifications.deepLinkFromNotification(n);
    // …route…
  });

  // Cold-start launch notification (resolved once, as state):
  const launch = BeamLaunchNotification();

  return (
    <Button
      title="Enable push"
      onPress={async () => {
        const perm = await push.requestPermission();     // resolves with the outcome
        if (perm.granted) {
          const { token } = await push.registerForRemote(); // resolves with the token
          // …register `token` with your microservice…
        }
      }}
    />
  );
  // push.permission, push.token, push.lastOpened update reactively.
}
```

## Usage — one object, `BeamNotifications` (non-React / imperative)

Import the single façade and call its methods; every method is platform-gated (a safe no-op
on web / unsupported), so you can call it anywhere. **Solicited calls now return Promises**
(the corresponding event still fires too, for unsolicited pushes):

```ts
import { BeamNotifications } from '@beamable/notifications-react-native';
import type { NotificationData } from '@beamable/notifications-react-native';

BeamNotifications.initialize();

const perm = await BeamNotifications.requestPermission();   // Promise<PermissionResult>
const { token } = await BeamNotifications.registerForRemote(); // Promise<{ token }>
// getPermissionStatus(), getPending(), getDeliveryReceipts() are Promises too.

const sub = BeamNotifications.addListener('notificationOpened', (n) => {
  const url = BeamNotifications.deepLinkFromNotification(n); // full deep-link URL, or null
  const { campaignId, nodeId } = BeamNotifications.campaignCoordsFromNotification(n);
  // …route / attribute…
});
// later: sub.remove();

// Schedule a local notification whose tap opens a deep link (you own the URL/scheme):
BeamNotifications.scheduleLocalWithDeepLink({ id: 'x', title: 'Hi', body: '…', url: myUrl });

BeamNotifications.trackOfferClicked(intent, offer);
BeamNotifications.trackOfferConverted(intent, offer);

// Support / platform info:
BeamNotifications.isSupported;                  // boolean
BeamNotifications.events;                        // every event name
BeamNotifications.hostPlatformLabel();           // 'iOS' | 'Android'
BeamNotifications.devicePushPlatform();          // 'apns' | 'fcm'
```

> **Promise semantics.** `requestPermission()` never times out (the OS dialog waits on the
> user). `registerForRemote()` rejects on `tokenError` and times out (default 30s) — a token
> never arrives on a simulator/emulator or without realm push credentials; pass
> `registerForRemote({ timeoutMs })` to tune it. `getPending()` / `getDeliveryReceipts()` are
> iOS-only and resolve `[]` on Android.

`BeamableNotifications` and the default export are aliases of `BeamNotifications`. The
individual flat helpers (`requestBeamablePermission`, `addBeamableListener`, …) remain
exported for back-compat. The full runtime flow is documented in the reference sample's
`INTEGRATION.md` (`nativeLibraries/Samples/ReactNative`).

## Web build (built-in) — Unity WebView & custom hosts

The package ships a **platform-resolved web build** (`src/index.web.ts`), so the same import
works on every platform — Metro auto-selects the native module on iOS/Android and the web build
on web. **There is no per-app `.web.ts` to write.**

```ts
// identical import on iOS, Android, and web:
import { BeamNotifications, BeamPushNotifications } from '@beamable/notifications-react-native';
```

On web, façade calls route over a pluggable **`WebTransport`**. The bundled default is the
**gree/unity-webview** bridge: when your web build runs inside a Unity WebView whose host has the
Beamable notifications plugin, calls reach the real iOS/Android library; in a plain browser the web
build is inert (`BeamNotifications.isSupported` stays `false`). Support is dynamic — it flips `true`
once the host handshake reports native support, and `BeamPushNotifications().isSupported` /
`addSupportListener` reflect that reactively.

To target a **different host**, supply your own transport:

```ts
import { BeamNotifications, type WebTransport } from '@beamable/notifications-react-native';

const myTransport: WebTransport = {
  isSupported: () => /* … */ true,
  getHost: () => ({ os: 'ios', isEditor: false, nativeSupported: true }),
  addSupportListener: (cb) => { cb(true); return { remove() {} }; },
  call: (method, args) => { /* fire-and-forget to your host */ },
  request: (method, args) => Promise.resolve(/* reply */ null as any),
  addEventListener: (name, cb) => { /* forward host events */ return { remove() {} }; },
};
BeamNotifications.setWebTransport(myTransport); // pass null to restore the Unity default
```

The raw gree bridge helpers (`isUnityWebView`, `getUnityHostPlatform`, `sendToUnity`,
`addUnityMessageListener`, `addUnityPlatformListener`, `unityTransport`) are also exported for
diagnostics. On native iOS/Android these are inert. (`setWebTransport` is a no-op on native.)

## Expo config plugin

Native push needs iOS capabilities (App Group, `aps-environment`, background mode, an
optional Notification Service Extension) and Android wiring (receive-time handler + manifest
meta-data). This package **ships its own Expo config plugin** (`app.plugin.js`), so you just
reference it **by name** in `app.json` — nothing to copy into your app:

```jsonc
{
  "expo": {
    "scheme": "yourscheme",
    "plugins": [
      ["@beamable/notifications-react-native", {
        "appGroup": "group.com.your.app",   // iOS App Group (defaults to group.<bundleId>)
        "enableServiceExtension": true       // iOS NSE for rich media + delivery receipts (opt-in)
      }]
    ]
  }
}
```

Then regenerate native projects:

```bash
npx expo prebuild --clean
npx expo run:ios      # or run:android  (physical device for remote push)
```

**Options**
- `appGroup` — iOS App Group id (shared storage the app + NSE use for closed-app analytics).
  Defaults to `group.<ios.bundleIdentifier>`. Must exist in your Apple developer account.
- `enableServiceExtension` — adds the Notification Service Extension target (rich media +
  receipts). Off by default; local + remote-registration push work without it.

The Android deep-link scheme is taken from your top-level Expo `scheme`. FCM also needs
`google-services.json` wired via `expo.android.googleServicesFile`; iOS remote push needs
APNs credentials on your realm.

> The plugin copies a default Android `BeamablePushReceivedHandler` and (when the NSE is
> enabled) the iOS extension sources into your app at prebuild time. Its implementation lives
> in `plugin/`; see `ios/README.md` for how the iOS NSE sources are vendored for publish.

## Web SDK in React Native

Running the Beamable Web SDK (`@beamable/sdk`) in React Native is a separate concern from
notifications, and the SDK now handles it natively: it ships a `react-native` build target
with AsyncStorage-backed token/config/content storage, selected automatically by Metro via
the package `exports` `"react-native"` condition.

This package ships one small piece of that story — a Metro config helper that turns on
package-exports resolution and watches the linked `file:` SDK source during local
development:

```js
// metro.config.js
const { getDefaultConfig } = require('expo/metro-config');
const { withBeamableSdk } = require('@beamable/notifications-react-native/metro');
module.exports = withBeamableSdk(getDefaultConfig(__dirname));
```

Then, once, before importing the SDK (e.g. top of `app/_layout.tsx`):

```ts
import '@beamable/sdk/react-native/polyfills';
```

No explicit token storage is needed — `Beam.init({ ... })` defaults to the AsyncStorage
store. See the samples under `nativeLibraries/Samples/`.
