# Beamable Notifications — Android (`com.beamable.push` + `com.beamable.deeplink`)

One engine-agnostic Android library (one Gradle module → one `.aar`) holding **both**:
- **Push notifications** (`com.beamable.push`) — **local** notifications (AlarmManager), **optional
  remote** push (FCM), notification **templates** + **channels**, **permission** request,
  **launch-intent** reading, a **receive-time handler** that runs even while the app is killed, with
  callbacks via `PushListener`.
- **Deep links** (`com.beamable.deeplink`) — native `VIEW`-intent capture (cold + warm start) with
  callbacks via `DeepLinkListener`.

The two keep their original package names but build into a single module named `notifications`
(namespace `com.beamable.notifications`), mirroring the single iOS `BeamableNotifications` framework.
The engine adapters (Unity / Unreal / React Native) for both ship **inside the same `.aar`** under
`unity/`, `unreal/`, `react/`. They are thin routing layers over the shared cores
(`PushManager`/`DeepLinkManager` + their listeners); per-engine code is simply unused in the other
engines' apps.

---

## Build

```bash
gradle wrapper --gradle-version 8.2     # once, if gradle/wrapper/gradle-wrapper.jar is absent
./gradlew :notifications:assembleRelease
# → notifications/build/outputs/aar/notifications-release.aar
```
Or just run `./dev-native.sh` from the repo root, which builds this and copies the result to
`nativeLibraries/EnginePlugins/Unity/Plugins/Android/beamable-notifications-release.aar` (the shared
Unity package ships the binary).

Toolchain: AGP 8.1.4, Gradle 8.2, Kotlin 1.9.22, `compileSdk 34`, `minSdk 24`, Java 11.
The React adapters compile against a `compileOnly` `com.facebook.react` dependency (not bundled).
Firebase is always linked (the push core needs it); a deep-link-only consumer simply never calls the
push API.

---

## API (shared core)

Channels are registered with explicit fields —
`registerChannel(id, name, description, importance)` (`importance` uses the
`NotificationManager.IMPORTANCE_*` constants, 4 = HIGH). The notification **template** is passed as
JSON to `scheduleLocal*`:

```jsonc
// notification template
{
  "id": 0,                       // 0 = auto-assign
  "title": "Hello",
  "body": "Tap to open",
  "smallIconResName": "icon_0",  // drawable/mipmap name; optional
  "channelId": "default",
  "deepLinkUrl": "myapp://path", // optional; merged into dataPayload["deeplink"]
  "dataPayload": { "k": "v" }    // optional; delivered back on tap
}
```

Operations (names are consistent across engines): `initialize(enableRemote)`,
`registerChannel(id, name, description, importance)`, `requestPermission()`, `hasPermission()`,
`scheduleLocal(templateJson, delayMillis) → id`, `fetchToken()`, `subscribeTopic(t)`,
`unsubscribeTopic(t)`, `consumeLaunchIntent()` (was app opened from a notification?),
`cancel(id)`, `cancelAll()`, `setForeground(bool)`,
`scheduleLocalExact(templateJson, delayMillis) → id`,
`scheduleLocalAt(templateJson, year, month, dayOfMonth, hourOfDay, minute, second, useUtc, exact) → id`,
`canScheduleExactAlarms()`, `requestExactAlarmPermission()`.

**Exact alarms & absolute time.** `scheduleLocal*` defaults to **inexact** doze-friendly alarms
(no permission). `scheduleLocalExact` (and `scheduleLocalAt(..., exact=true)`) use an **exact** alarm,
which needs `SCHEDULE_EXACT_ALARM` — the consuming app declares it; on API 33+ the user grants it in
Settings. The library checks `canScheduleExactAlarms()` and **falls back to inexact** (dispatching a
`schedule_exact_denied` error) when it isn't available. `scheduleLocalAt` schedules at an absolute
wall-clock time interpreted as **UTC** (`useUtc=true`) or the **device-local** zone, converted to an
epoch by the library. (`USE_EXACT_ALARM` is intentionally not used — Play restricts it to
alarm/calendar apps.)

Callbacks (`PushListener`): `onTokenReceived`, `onTokenRefreshError`,
`onMessageReceivedForeground`, `onNotificationOpened`, `onPermissionResult`,
`onLocalNotificationScheduled`, `onError`.

**Remote (FCM) is optional.** With a `google-services.json` in the consuming app it's enabled;
without it the library auto-detects no Firebase and runs **local-only** (token/topic calls no-op).

---

## Deep links (`com.beamable.deeplink`)

The deep-link half of the library captures native `ACTION_VIEW` intents for the app's URL scheme —
**cold start** (`DeepLinkManager.getInitialLink(activity)`) and **warm start**
(`handleNewIntent(intent)`, de-duped by `ActivityIntentObserver`). The consuming app declares the
`VIEW` intent-filter + scheme in its manifest; the library carries none. Callbacks arrive via
`DeepLinkListener.onDeepLink(url, isColdStart)`.

Its engine adapters live alongside the push ones in the same `.aar`:

| Package | Inbound | Outbound |
|---|---|---|
| `com.beamable.deeplink.unity` | `UnityDeepLink` (`@JvmStatic`) | `UnityDeepLinkBridge` → `UnitySendMessage("OnNativeDeepLink", url)` |
| `com.beamable.deeplink.react` | `ReactDeepLinkModule` (`@ReactMethod`, `ActivityEventListener`) | `RCTDeviceEventEmitter` `onDeepLink` |
| `com.beamable.deeplink.unreal` | `UnrealDeepLink` (`@JvmStatic`) | `UnrealDeepLinkBridge` → JNI `nativeOnDeepLink` |

> In Unity this is consumed through `DeepLinkManager.cs` in the shared package (which also handles
> iOS `Application.deepLinkActivated`, WebGL, and Windows). Notification-carried deep links surface
> separately as `NotificationData.DeepLink` on the push events.

---

## Adapter packages — what the push `unity/`, `unreal/`, `react/` `.kt` are

These are **thin routing layers** over the shared core (`PushManager` + `PushListener`). They
contain **no push logic** — only how each engine calls *in* and how results are routed *back*:

| Package | Inbound (engine → core) | Outbound (core → engine) | Engine dependency |
|---|---|---|---|
| `unity/` | `UnityNotifications` — `@JvmStatic` facade (iOS-matching) C# calls via `AndroidJavaClass` | `UnityNotificationsBridge` → `UnityPlayer.UnitySendMessage` (reflection) | none |
| `react/` | `ReactPushModule` — `@ReactMethod` | `ReactPushModule` → `RCTDeviceEventEmitter` | React (compileOnly) |
| `unreal/` | `UnrealPush` — `@JvmStatic` facade UE C++ calls via JNI | `UnrealPushBridge` → JNI `external` funcs (impl'd in the UE plugin's C++) | none |

All three target the **identical core**; in an app for one engine, the other engines' classes are
simply never loaded.

## ProGuard / the `com.facebook.react` dependency

`build.gradle` declares:
```gradle
compileOnly 'com.facebook.react:react-android:0.73.4'
```
The `react/` adapter needs React classes to **compile**, but `compileOnly` means React is **not
bundled** into the `.aar` and is **not** a transitive dependency — so Unity/Unreal apps never pull
it. Because the kept `react/` classes reference React types that are absent in a non-RN app, a
minifying (R8) consumer would otherwise log "missing class" warnings, so `proguard-rules.pro`
adds:
```
-dontwarn com.facebook.react.**
```
This is safe: the React module is only ever instantiated by React Native's package system, so it's
never loaded — and thus never references those classes — in a Unity/Unreal app. (The `unreal/`
adapter needs no such rule: it reaches Unreal only through reflection + JNI `external` functions,
with no compile-time references to UE classes.)

---

## Use in Unity

Unity consumers use the shared cross-platform package **`Beamable.Notifications`** (in
`nativeLibraries/EnginePlugins/Unity/`, same API as iOS) — they do **not** call the facade directly.
On Android that package routes to the `com.beamable.push.unity.UnityNotifications` `@JvmStatic`
facade and receives callbacks via an auto-spawned relay GameObject.

```csharp
using Beamable.Notifications;

BeamableNotifications.OnNotificationTapped += n => Route(n.DeepLink);
BeamableNotifications.Initialize();
BeamableNotifications.RequestPermission(new PermissionOptions());
BeamableNotifications.ScheduleLocal(new LocalRequest {
    Id = "welcome", Title = "Hi", Body = "Yo",
    Trigger = TriggerSpec.After(5),
    UserInfo = new Dictionary<string, object> { ["deepLink"] = "beamable://x" }
});
```

1. Build the `.aar` (above); the editor build processor injects the transitive deps
   (`kotlin-stdlib`, `androidx.core`, `firebase-messaging`) automatically.
2. Inbound facade methods (called by the C# layer): `initialize(gameObject)`,
   `requestPermission(optionsJson)`, `getPermissionStatus()`, `scheduleLocal(requestJson)`,
   `cancelLocal(id)`, `cancelAllLocal()`, `clearDelivered()`, `getPending()`,
   `registerForRemote()`/`unregisterForRemote()`, `setBadge(count)`, `getLaunchNotification()`,
   `setForeground(bool)`.
3. Outbound callbacks (via `UnitySendMessage` to the relay): `OnNotificationReceived`,
   `OnNotificationTapped`, `OnNotificationPresented` (`NotificationData` JSON), `OnTokenReceived`,
   `OnTokenError`, `OnPermissionResult`, `OnPendingNotifications`, `OnDeliveryReceipts`.

## Use in React Native

The native module + `ReactPackage` ship in the `.aar` (`com.beamable.push.react`).
1. Add the `.aar` to the app (e.g. via an RN package whose `android/` depends on it) and register
   the package with autolinking — `react-native.config.js`:
   ```js
   module.exports = { dependency: { platforms: { android: {
     packageImportPath: 'import com.beamable.push.react.ReactPushPackage;',
     packageInstance: 'new ReactPushPackage()' } } } };
   ```
2. Call it from JS (native module name **`BeamablePush`**):
   ```ts
   import { NativeModules, NativeEventEmitter } from 'react-native';
   const { BeamablePush } = NativeModules;
   const emitter = new NativeEventEmitter(BeamablePush);

   BeamablePush.initialize(true);
   BeamablePush.registerChannel('default', 'General', '', 4); // importance 4 = HIGH
   BeamablePush.requestPermission();
   emitter.addListener('onTokenReceived', (t) => console.log('token', t));
   emitter.addListener('onNotificationOpened', (json) => route(json));
   const id = await BeamablePush.scheduleLocal(
     JSON.stringify({ title: 'Hi', body: 'Yo', channelId: 'default' }), 5000);
   ```
   Events: `onTokenReceived`, `onTokenRefreshError`, `onMessageForeground`,
   `onNotificationOpened`, `onPermissionResult`, `onLocalScheduled`, `onError`.

## Use in Unreal

The Kotlin facade (`com.beamable.push.unreal.UnrealPush`) and JNI bridge
(`UnrealPushBridge`) ship in the `.aar`. The UE plugin supplies the C++/UPL glue:
1. UPL imports `beamable-notifications-release.aar` + its gradle deps into the Android build.
2. C++ calls the facade via JNI (`CallStaticVoidMethod` / `CallStaticIntMethod`), e.g.
   `UnrealPush.initialize(enableRemote)`, `UnrealPush.scheduleLocal(json, delayMillis)`.
3. Implement the JNI callbacks the bridge declares (marshal to the Game thread, then broadcast a
   UE delegate):
   `Java_com_beamable_push_unreal_UnrealPushBridge_nativeOnToken`,
   `…_nativeOnNotificationOpened`, `…_nativeOnMessageForeground`,
   `…_nativeOnPermissionResult`, `…_nativeOnTokenError`, `…_nativeOnLocalScheduled`,
   `…_nativeOnError`.

---

## How notifications are detected (remote + local)

Both delivery paths converge on the **same** receive-time handler,
`com.beamable.push.PushNotificationReceivedHandler`, so a single implementation handles every case
— remote/local × foreground/background/killed. It is registered via app-manifest meta-data and
instantiated by **reflection**, so it runs even in a freshly spawned process when the app is killed:
```xml
<meta-data android:name="com.beamable.push.notification_received_handler"
           android:value="your.Handler" />
```
(Or add one at runtime via `PushManager.addNotificationReceivedHandler(...)` — app-alive only.)

```kotlin
interface PushNotificationReceivedHandler {
    fun onNotificationReceived(context: Context, event: PushReceivedEvent)
}
// PushReceivedEvent(messageId, dataJson, sentTimeMillis, receivedTimeMillis, wasForeground, deepLink)
```

### Remote (FCM)
`PushFirebaseService.onMessageReceived` fires for every FCM message and invokes the handler
**before** any display/branching — so it runs in **foreground, background, and killed** states.
Background/killed delivery requires **data-only, high-priority** messages (a `notification`-block
message is auto-displayed by the OS and only reaches the app when tapped). When the app is
foreground it additionally forwards the raw message to the engine via `onMessageReceivedForeground`
(Unity `OnMessageForeground`).

### Local (AlarmManager)
`scheduleLocal(...)` sets an `AlarmManager` alarm; when it fires, the broadcast wakes the app
process and is delivered to `NotificationActionReceiver`, which posts the notification **and**
invokes the same handler — so local notifications reach it too, in **foreground, background, and
killed** states. Since `BroadcastReceiver.onReceive` runs on the main thread, the handler is called
on a background thread via `goAsync()` (~10s budget), matching the FCM path and allowing a short
blocking call (e.g. an HTTP request). Note: local notifications never go through FCM, so they fire
the handler **without** any `google-services.json` / Firebase setup.

### Foreground vs background — `event.wasForeground`
`wasForeground` is read from `PushManager.isForeground`, which the engine keeps current by calling
`setForeground(bool)` across its lifecycle (the Unity adapter does this at init and on
`OnApplicationFocus`/`OnApplicationPause`). In a killed-app process it defaults to `false`. So:
- `true`  → the app was open when the notification arrived.
- `false` → the app was backgrounded or killed.

### Engine-side callbacks (app-alive only)
Besides the native handler, results route to the engine via `PushListener`
(`onMessageReceivedForeground`, `onNotificationOpened` on tap, `onLocalNotificationScheduled`, …).
These need the engine running, so they cover the **app-open** case only; the native handler above
is the single hook that also runs when the app is **killed**.

> Constraints: ~10s background-thread budget (enqueue WorkManager for longer work); a
> force-stopped/OEM-killed app receives nothing until reopened. Working example: the
> `DiscordWebhookPushHandler.java` in the package's **Native Demo** sample
> (`nativeLibraries/EnginePlugins/Unity/Samples~/NativeDemo/`).
