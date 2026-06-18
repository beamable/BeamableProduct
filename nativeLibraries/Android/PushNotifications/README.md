# PushNotifications (`com.beamable.push`)

Engine-agnostic Android push library: **local** notifications (AlarmManager), **optional remote**
push (FCM), configurable notification **templates** + **channels**, **permission** request,
**launch-intent** reading, a **receive-time handler** that runs even while the app is killed, and
all callbacks via `PushListener`.

The engine adapters (Unity / Unreal / React Native) all ship **inside the same `.aar`** under
`unity/`, `unreal/`, `react/`. They are thin routing layers over the shared core
(`PushManager` + `PushListener`); per-engine code is simply unused in the other engines' apps.

---

## Build

```bash
gradle wrapper --gradle-version 8.2     # once, if gradle/wrapper/gradle-wrapper.jar is absent
./gradlew :pushnotifications:assembleRelease
# → pushnotifications/build/outputs/aar/pushnotifications-release.aar
```
Toolchain: AGP 8.1.4, Gradle 8.2, Kotlin 1.9.22, `compileSdk 34`, `minSdk 24`, Java 11.
The React adapter compiles against a `compileOnly` `com.facebook.react` dependency (not bundled).

---

## API (shared core)

JSON shapes used by every engine:

```jsonc
// channel
{ "id": "default", "name": "General", "description": "", "importance": 4 } // 4 = IMPORTANCE_HIGH

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
`registerChannel(json)`, `requestPermission()`, `hasPermission()`,
`scheduleLocal(templateJson, delayMillis) → id`, `fetchToken()`, `subscribeTopic(t)`,
`unsubscribeTopic(t)`, `consumeLaunchIntent()` (was app opened from a notification?),
`cancel(id)`, `cancelAll()`, `setForeground(bool)`.

Callbacks (`PushListener`): `onTokenReceived`, `onTokenRefreshError`,
`onMessageReceivedForeground`, `onNotificationOpened`, `onPermissionResult`,
`onLocalNotificationScheduled`, `onError`.

**Remote (FCM) is optional.** With a `google-services.json` in the consuming app it's enabled;
without it the library auto-detects no Firebase and runs **local-only** (token/topic calls no-op).

---

## Adapter packages — what the `unity/`, `unreal/`, `react/` `.kt` are

These are **thin routing layers** over the shared core (`PushManager` + `PushListener`). They
contain **no push logic** — only how each engine calls *in* and how results are routed *back*:

| Package | Inbound (engine → core) | Outbound (core → engine) | Engine dependency |
|---|---|---|---|
| `unity/` | `UnityPush` — `@JvmStatic` facade C# calls via `AndroidJavaClass` | `UnityPushBridge` → `UnityPlayer.UnitySendMessage` (reflection) | none |
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

1. Build the `.aar` (above) and drop it into `Assets/Plugins/Android/`. (In the consuming Unity
   project, the editor tool `Tools/Beamable/Android/Setup & Validation` injects the transitive
   deps — `kotlin-stdlib`, `androidx.core`, `firebase-messaging` — automatically.)
2. Call the `@JvmStatic` facade `com.beamable.push.unity.UnityPush` from C#:
   ```csharp
   using (var push = new AndroidJavaClass("com.beamable.push.unity.UnityPush"))
   {
       push.CallStatic("initialize", "BeamablePush", /*enableRemote*/ true); // routes callbacks to GameObject "BeamablePush"
       push.CallStatic("registerChannel", "{\"id\":\"default\",\"name\":\"General\",\"importance\":4}");
       push.CallStatic("requestPermission");
       int id = push.CallStatic<int>("scheduleLocal",
           "{\"title\":\"Hi\",\"body\":\"Yo\",\"channelId\":\"default\",\"deepLinkUrl\":\"myapp://x\"}", (long)5000);
   }
   ```
3. Receive callbacks: put a `DontDestroyOnLoad` GameObject named **`BeamablePush`** with a
   MonoBehaviour exposing methods that match the bridge (`UnitySendMessage` delivers on the main
   thread): `OnTokenReceived(string)`, `OnTokenError(string)`, `OnMessageForeground(string)`,
   `OnNotificationOpened(string)`, `OnPermissionResult(string)`, `OnLocalScheduled(string)`,
   `OnNativeError(string)`.

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
   BeamablePush.registerChannel(JSON.stringify({ id: 'default', name: 'General', importance: 4 }));
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
1. UPL imports `pushnotifications-release.aar` + its gradle deps into the Android build.
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
(Or set it at runtime via `PushManager.setNotificationReceivedHandler(...)` — app-alive only.)

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
> force-stopped/OEM-killed app receives nothing until reopened. Reference impl:
> `sample/DemoPushNotificationReceivedHandler.kt`.
