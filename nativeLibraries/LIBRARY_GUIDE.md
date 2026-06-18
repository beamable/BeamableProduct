# Beamable Native Android Libraries — Reference

Two reusable, **engine-agnostic** Kotlin Android libraries, each a standalone Gradle project that
builds a single `.aar`:

| Library | Package | Purpose |
|---|---|---|
| `Android/PushNotifications` | `com.beamable.push` | Local notifications (AlarmManager) + optional remote push (FCM), channels/templates, permission, launch-intent reading, and a **receive-time handler** that runs even when the app is killed. |
| `Android/Deeplink` | `com.beamable.deeplink` | Native deep-link (`VIEW` intent) capture for cold-start and warm-start. |

Both target: **AGP 8.1.4, Gradle 8.2, Kotlin 1.9.22, `compileSdk 34`, `minSdk 24`, Java 11.**

---

## 1. Design model

Each library is structured in three layers, and **ships every engine adapter inside the one `.aar`**
(`unity/`, `unreal/`, `react/`). Adapters for engines you don't use are simply never loaded.

```
            engine (C# / C++ / JS)
   inbound  │   ▲  outbound
            ▼   │
   ┌─────────────────────────┐   ← per-engine adapter (thin)
   │  *Push / *DeepLink       │     inbound:  @JvmStatic facade the engine calls
   │  *PushBridge / *Bridge   │     outbound: implements the core listener, forwards events
   └─────────────────────────┘
            │   ▲
            ▼   │
   ┌─────────────────────────┐   ← engine-agnostic CORE (no engine references)
   │  PushManager / DeepLinkManager  (facade)
   │  PushListener / DeepLinkListener (callbacks)
   └─────────────────────────┘
```

- **Inbound (engine → core):** a per-engine `@JvmStatic` facade (`UnityPush`, `UnrealPush`,
  `ReactPushModule`, …) taking only strings/primitives, resolving the activity natively.
- **Outbound (core → engine):** a per-engine bridge that implements the core listener and delivers
  events back — the *only* part that is genuinely engine-specific (different transport per engine).
- **Core:** never references any engine; the same code serves all three.

---

## 2. Features & support

**PushNotifications**
- Local notifications via `AlarmManager`: **inexact** by default (doze-friendly, no permission), with
  an opt-in **exact** option (`scheduleLocalExact`) and **absolute-time** scheduling with a
  **local-vs-UTC** choice (`scheduleLocalAt`). Exact alarms need `SCHEDULE_EXACT_ALARM` (API 33+ is
  user-granted) — the consuming app declares it; the library checks `canScheduleExactAlarms()` and
  **falls back to inexact** (with a `schedule_exact_denied` warning) otherwise.
- Optional remote push via **FCM** — auto-enabled only when a `google-services.json` is present;
  otherwise runs **local-only** (token/topic calls become no-ops).
- Notification **channels** (API 26+) and JSON **templates** (title/body/icon/channel/deeplink/data).
- `POST_NOTIFICATIONS` runtime **permission** request (API 33+).
- **Launch-intent reading** — was the app opened from a notification? (carries the data payload).
- **Receive-time handler** that fires for **local + remote**, in **foreground / background / killed**
  states, even with no engine running (see §5).
- Deep-link payload carried through notifications (`deepLinkUrl` → `dataPayload["deeplink"]`).

**Deeplink**
- Captures `VIEW` intents for the app's URL scheme (the consuming app declares the scheme).
- **Cold-start** (pull via `getInitialLink`) and **warm-start** (`onNewIntent`) with de-duplication.

**Both:** `minSdk 24`; engine adapters for **Unity, Unreal, React Native** bundled in the `.aar`.

---

## 3. PushNotifications library (`com.beamable.push`)

### How it works
- **Local:** `PushManager.scheduleLocal(template, delay)` → `LocalNotificationScheduler` sets an
  `AlarmManager` alarm whose broadcast targets `NotificationActionReceiver`. On fire, the receiver
  posts the notification (`NotificationBuilder`) **and** invokes the receive-time handler on a
  background thread (`goAsync()`), so it works even when the app is killed.
- **Remote:** `PushFirebaseService.onMessageReceived` invokes the receive-time handler for every
  message, then forwards to the engine (foreground) or displays data-only messages (background).
- **Foreground state:** `PushReceivedEvent.wasForeground` is read from `PushManager.isForeground`,
  which the engine keeps current via `setForeground(bool)` on its lifecycle.

### Core classes — `…/com/beamable/push/`

| File | Type | Purpose / key API |
|---|---|---|
| `PushManager.kt` | `object` | Public facade. `initialize`, `registerChannel(spec)` / `registerChannel(id, name, description, importance)`, `requestPermission`, `hasPermission`, `scheduleLocal(template/json, delayMillis)→id`, `scheduleLocalExact(template/json, delayMillis)→id` (exact alarm), `scheduleLocalAt(template/json, year, month, dayOfMonth, hourOfDay, minute, second, useUtc, exact)→id` (absolute, local/UTC), `canScheduleExactAlarms()`, `requestExactAlarmPermission(activity)`, `cancel(id)`, `cancelAll`, `fetchToken`, `subscribeTopic`/`unsubscribeTopic`, `consumeLaunchIntent`, `setNotificationReceivedHandler`, `resolveNotificationReceivedHandler`; state: `listener`, `isForeground`, `remoteEnabled`. |
| `PushListener.kt` | `interface` | App-alive callbacks: `onTokenReceived`, `onTokenRefreshError`, `onMessageReceivedForeground`, `onNotificationOpened`, `onPermissionResult`, `onLocalNotificationScheduled`, `onError(stage,message)`. |
| `PushNotificationReceivedHandler.kt` | `interface` | **The receive-time hook.** `onNotificationReceived(context, event)` — runs for local+remote, all states (incl. killed). |
| `PushReceivedEvent.kt` | `data class` | Snapshot: `messageId?`, `dataJson`, `sentTimeMillis`, `receivedTimeMillis`, `wasForeground`, `deepLink?`. |
| `PushFirebaseService.kt` | `FirebaseMessagingService` | FCM entry point. `onNewToken`, `onMessageReceived` → invokes handler, then foreground-forward or data-message display. |
| `LocalNotificationScheduler.kt` | `object` | Schedules/cancels local notifications via `AlarmManager`. `schedule(ctx,template,delay)→id`, `cancel(ctx,id)`, `cancelAll(ctx)`. |
| `NotificationActionReceiver.kt` | `BroadcastReceiver` | Receives the alarm broadcast → posts the notification **and** fires the receive-time handler off the main thread. |
| `NotificationBuilder.kt` | `object` | Builds/show the `Notification` + tap `PendingIntent`. `ensureChannel`, `build`, `buildContentIntent`, `show`. |
| `NotificationTemplate.kt` | `data class` | Engine-agnostic notification description (icons by name). `effectivePayload()`, `toJson()`/`fromJson()`. |
| `NotificationChannelSpec.kt` | `data class` | Channel description (API 26+): `id`, `name`, `description`, `importance`. |
| `PermissionHelper.kt` | `object` | `POST_NOTIFICATIONS` (API 33+): `hasPermission`, `requestPermission`. |
| `IntentDataReader.kt` | `object` | Reads (once) the notification payload from the launch intent: `readLaunchIntent(activity)→json?`. |
| `EngineBridge.kt` | `interface` | Lowest-common-denominator outbound bridge: `emit(method, payload)`. |

### Adapter classes

| Folder | Inbound (engine→core) | Outbound (core→engine) |
|---|---|---|
| `unity/` | `UnityPush` (`@JvmStatic` facade; resolves `UnityPlayer.currentActivity`) | `UnityPushBridge : PushListener` → `UnityPlayer.UnitySendMessage(gameObject, method, json)` |
| `unreal/` | `UnrealPush` (`@JvmStatic`; resolves GameActivity) | `UnrealPushBridge : PushListener` → JNI `external` `nativeOn*` funcs (implemented in the UE C++ plugin) |
| `react/` | `ReactPushModule` (`@ReactMethod`) | same class → `RCTDeviceEventEmitter` events; `ReactPushPackage : ReactPackage` registers it |

### Manifest & deps (declared by the library, merged into the app)
- **Service** `PushFirebaseService` (not exported) with `com.google.firebase.MESSAGING_EVENT`.
- **Receiver** `NotificationActionReceiver` (not exported).
- **Meta-data** `com.google.firebase.messaging.default_notification_channel_id` → `@string/beamable_default_channel`.
- **Permission** `POST_NOTIFICATIONS`.
- **Deps:** `androidx.core:core-ktx:1.12.0`, `platform(firebase-bom:33.7.0)`, `firebase-messaging-ktx`,
  `compileOnly com.facebook.react:react-android:0.73.4`.

---

## 4. Deeplink library (`com.beamable.deeplink`)

### How it works
- **Cold start:** the engine pulls the launch URL via `DeepLinkManager.getInitialLink(activity)`
  (extracted by `IntentDeepLinkExtractor` from an `ACTION_VIEW` intent), de-duped by
  `ActivityIntentObserver`.
- **Warm start:** `onNewIntent` → `DeepLinkManager.handleNewIntent(intent)` → dispatched to the
  listener with `isColdStart=false`.
- The consuming app declares the `VIEW` intent-filter + URL scheme; the library carries no scheme.

### Core classes — `…/com/beamable/deeplink/`

| File | Type | Purpose / key API |
|---|---|---|
| `DeepLinkManager.kt` | `object` | Facade. `initialize(app, listener / gameObject)`, `getInitialLink(activity)→url?`, `handleNewIntent(intent)`, `clearConsumed()`; state: `listener`. |
| `DeepLinkListener.kt` | `interface` | `onDeepLink(url, isColdStart)`. |
| `ActivityIntentObserver.kt` | `ActivityLifecycleCallbacks` | Detects cold-start deep links on activity create/resume, with de-dupe. |
| `IntentDeepLinkExtractor.kt` | `object` | Pure helper: `extract(intent)→url?` (data URI of an `ACTION_VIEW` intent). |
| `EngineBridge.kt` | `interface` | Outbound bridge: `emit(method, payload)`. |

### Adapter classes

| Folder | Inbound | Outbound |
|---|---|---|
| `unity/` | `UnityDeepLink` (`@JvmStatic`: `initialize(gameObject)`, `getInitialLink()`, `clearConsumed()`) | `UnityDeepLinkBridge : DeepLinkListener` → `UnitySendMessage(gameObject, "OnNativeDeepLink", url)` |
| `unreal/` | `UnrealDeepLink` (`@JvmStatic`: `initialize`, `getInitialLink`, `handleNewIntent`, `clearConsumed`) | `UnrealDeepLinkBridge : DeepLinkListener` → JNI `external nativeOnDeepLink(url, isColdStart)` |
| `react/` | `ReactDeepLinkModule` (`@ReactMethod initializeDeepLinks()`, `getInitialLink(promise)`; `ActivityEventListener.onNewIntent`) | same class → `onDeepLink` event via `RCTDeviceEventEmitter`; `ReactDeepLinkPackage` registers it |

### Manifest & deps
- **No components declared** — the library only observes the host activity's intents.
- **Deps:** `androidx.core:core-ktx:1.12.0`, `compileOnly com.facebook.react:react-android:0.73.4`.

---

## 5. The receive-time handler (push, all states)

Implement `com.beamable.push.PushNotificationReceivedHandler` and register it via app-manifest
meta-data (instantiated by **reflection**, so it runs in a freshly spawned process when killed):

```xml
<meta-data android:name="com.beamable.push.notification_received_handler"
           android:value="your.fully.Qualified.Handler" />
```

```kotlin
interface PushNotificationReceivedHandler {
    fun onNotificationReceived(context: Context, event: PushReceivedEvent)
}
```

- Fires for **local** (no Firebase needed) and **remote** FCM; `event.wasForeground` distinguishes
  app-open from background/killed.
- Runs on a **background thread (~10s budget)** — a short blocking call (e.g. an HTTP webhook) is
  fine; otherwise enqueue WorkManager.
- For remote **background/killed**, send **data-only, high-priority** FCM messages (a
  `notification`-block message is auto-shown by the OS and only reaches the app on tap).
- Requires the no-arg public constructor. Working example:
  `client/Assets/Plugins/Android/DiscordWebhookPushHandler.java`.

> Engine-side `PushListener` / `DeepLinkListener` callbacks require the engine to be running, so they
> only cover the **app-open** case; the receive-time handler is the only hook that also runs killed.

---

## 6. Using it from each engine

The build steps are the same everywhere: consume the `.aar` **and declare its transitive Maven
deps** (a loose `.aar`'s POM is not resolved) — `kotlin-stdlib`, `androidx.core`, and for push
`firebase-messaging`. Remote push activates only with a `google-services.json`.

### Unity
- **Inbound:** call the `@JvmStatic` facades via `AndroidJavaClass`:
  ```csharp
  using (var push = new AndroidJavaClass("com.beamable.push.unity.UnityPush"))
  {
      push.CallStatic("initialize", "BeamablePush", /*enableRemote*/ true);
      push.CallStatic("registerChannel", "default", "General", "", 4); // importance 4 = HIGH
      push.CallStatic("requestPermission");
      int id = push.CallStatic<int>("scheduleLocal", templateJson, (long)5000);       // inexact, no permission
      push.CallStatic<int>("scheduleLocalExact", templateJson, (long)5000);            // exact alarm (SCHEDULE_EXACT_ALARM)
      // absolute time; useUtc=false → device-local, exact=true. month is 1-12.
      push.CallStatic<int>("scheduleLocalAt", templateJson, 2026, 6, 20, 9, 0, 0, false, true);
      bool canExact = push.CallStatic<bool>("canScheduleExactAlarms");                 // else push.CallStatic("requestExactAlarmPermission")
  }
  using (var dl = new AndroidJavaClass("com.beamable.deeplink.unity.UnityDeepLink"))
  {
      dl.CallStatic("initialize", "DeepLinkManager");          // warm-start → UnitySendMessage
      string cold = dl.CallStatic<string>("getInitialLink");   // cold-start pull
  }
  ```
- **Outbound:** put a `DontDestroyOnLoad` GameObject named exactly as passed above; the bridges call
  `UnitySendMessage(gameObject, method, payload)` (main thread) — methods: push
  `OnTokenReceived`/`OnTokenError`/`OnMessageForeground`/`OnNotificationOpened`/`OnPermissionResult`/`OnLocalScheduled`/`OnNativeError`,
  deeplink `OnNativeDeepLink`. Adapters must ship precompiled in the `.aar` (Unity doesn't compile Kotlin from `Assets/`).

### Unreal
- **Shipped in the `.aar`:** `UnrealPush`/`UnrealDeepLink` (`@JvmStatic` inbound facades C++ calls via
  JNI) + `UnrealPushBridge`/`UnrealDeepLinkBridge` (outbound: call JNI `external` functions).
- **Supply in a UE plugin (C++/UPL — can't live in an `.aar`):**
  1. UPL imports `pushnotifications-release.aar`/`deeplink-release.aar` + their transitive deps.
  2. C++ calls the facades via JNI (`FJavaWrapper`/`FAndroidApplication`), e.g.
     `UnrealPush.initialize(enableRemote)`, `UnrealPush.scheduleLocal(json, delay)`,
     `UnrealPush.scheduleLocalExact(json, delay)`,
     `UnrealPush.scheduleLocalAt(json, year, month, day, hour, minute, second, useUtc, exact)`,
     `UnrealPush.canScheduleExactAlarms()` / `requestExactAlarmPermission()`.
  3. Implement the bridge's `native` functions
     (`Java_com_beamable_push_unreal_UnrealPushBridge_nativeOnToken`, …,
     `…deeplink_unreal_UnrealDeepLinkBridge_nativeOnDeepLink`), marshal to the game thread
     (`AsyncTask(ENamedThreads::GameThread)`), and broadcast a delegate.
  4. For deeplink, forward `GameActivity.onNewIntent` → `UnrealDeepLink.handleNewIntent`.

### React Native
- **Shipped in the `.aar`:** `ReactPushModule`/`ReactDeepLinkModule` (`ReactContextBaseJavaModule`,
  inbound `@ReactMethod` + outbound `RCTDeviceEventEmitter`; deeplink also registers an
  `ActivityEventListener`) + their `ReactPackage`s. React is `compileOnly` (not bundled).
- **Supply in an RN package (JS — not in the `.aar`):** register the packages with autolinking and
  call from JS:
  ```js
  // react-native.config.js
  module.exports = { dependency: { platforms: { android: {
    packageImportPath: 'import com.beamable.push.react.ReactPushPackage;',
    packageInstance: 'new ReactPushPackage()' } } } };
  ```
  ```ts
  import { NativeModules, NativeEventEmitter } from 'react-native';
  const { BeamablePush, BeamableDeeplink } = NativeModules;
  new NativeEventEmitter(BeamablePush).addListener('onNotificationOpened', route);
  BeamablePush.initialize(true);
  BeamablePush.scheduleLocalExact(json, 5000);                          // exact alarm
  BeamablePush.scheduleLocalAt(json, 2026, 6, 20, 9, 0, 0, false, true); // absolute, local, exact
  const canExact = await BeamablePush.canScheduleExactAlarms();          // else requestExactAlarmPermission()
  BeamableDeeplink.initializeDeepLinks();           // note: not "initialize" (avoids base-class clash)
  const link = await BeamableDeeplink.getInitialLink();
  ```
  Push events: `onTokenReceived`, `onTokenRefreshError`, `onMessageForeground`,
  `onNotificationOpened`, `onPermissionResult`, `onLocalScheduled`, `onError`. Deeplink event:
  `onDeepLink` (`{ url, isColdStart }`).

---

## 7. Build

```bash
# from nativeLibraries/Android
cd PushNotifications && gradle wrapper --gradle-version 8.2 && ./gradlew :pushnotifications:assembleRelease
cd ../Deeplink       && gradle wrapper --gradle-version 8.2 && ./gradlew :deeplink:assembleRelease
# → pushnotifications/build/outputs/aar/pushnotifications-release.aar
# → deeplink/build/outputs/aar/deeplink-release.aar
```

See the per-library READMEs for more detail:
`Android/PushNotifications/README.md`, `Android/Deeplink/README.md`, `Android/README.md`.
