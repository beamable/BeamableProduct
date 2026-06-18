# Beamable Native Android Libraries

Two reusable, **engine-agnostic** Kotlin Android libraries, each a standalone Gradle
project that builds a `.aar`. Currently consumed by **Unity** (React Native / Unreal later).

```
nativeLibraries/Android/
  PushNotifications/   → com.beamable.push     — local + optional remote (FCM) push
  Deeplink/            → com.beamable.deeplink  — native deeplink (VIEW intent) capture
```

Each project = **engine-agnostic core** + a thin **Unity adapter** (in `unity/`). The core
never references any engine; the Unity adapter forwards events via
`UnityPlayer.UnitySendMessage` using reflection. A future Unreal/RN adapter would call the same
core (see "Engine adapter" below).

## Building the AARs

These are plain Android library projects. Gradle **8.2**, AGP **8.1.4**, Kotlin **1.9.22**,
`compileSdk 34`, `minSdk 24`, Java 11.

The Gradle wrapper `.jar` is not committed. Generate the wrapper once (or open each project
in Android Studio, which does it automatically):

```bash
cd PushNotifications && gradle wrapper --gradle-version 8.2
cd ../Deeplink       && gradle wrapper --gradle-version 8.2
```

Then build:

```bash
cd PushNotifications && ./gradlew :pushnotifications:assembleRelease
cd ../Deeplink       && ./gradlew :deeplink:assembleRelease
```

Artifacts:
- `PushNotifications/pushnotifications/build/outputs/aar/pushnotifications-release.aar`
- `Deeplink/deeplink/build/outputs/aar/deeplink-release.aar`

## Consuming from Unity

1. Copy both `.aar` into the Unity project's `Assets/Plugins/Android/`.
2. **Editor tool does the gradle setup automatically** — there are no committed gradle template
   files. The consuming Unity project (`BeamableProduct/client`) ships
   `Assets/Scripts/Editor/BeamableAndroidBuildProcessor.cs`, which at build time injects the
   libraries' **transitive Maven dependencies** (Unity does NOT resolve a loose `.aar`'s POM),
   enables AndroidX, and wires Firebase only when a `google-services.json` is present:
   ```gradle
   implementation 'org.jetbrains.kotlin:kotlin-stdlib:1.9.22'
   implementation 'androidx.core:core-ktx:1.12.0'
   implementation platform('com.google.firebase:firebase-bom:33.7.0')
   implementation 'com.google.firebase:firebase-messaging-ktx'
   ```
   Run **Tools/Beamable/Android/Setup & Validation** in Unity to auto-apply settings and verify
   the AARs/manifest/SDK. A pre-build processor re-checks and auto-applies on every Android build.
3. Call the `@JvmStatic` Unity facades from C# via `AndroidJavaClass`:
   `com.beamable.push.unity.UnityPush` and `com.beamable.deeplink.unity.UnityDeepLink`.
   Native → C# events arrive via `UnitySendMessage` to a named GameObject.

## Local vs remote (push)

Remote (FCM) is **optional**:
- **Local-only:** no `google-services.json` needed. `PushManager.initialize(..., enableRemote)`
  auto-detects the absence of a Firebase config and disables remote; `fetchToken`/topics
  become no-ops. Local scheduled notifications work fully.
- **Remote:** add `google-services.json` to the app module and pass `enableRemote = true`.
  The Unity side applies the `google-services` plugin only when the json is present.

## Receive-time hook (runs even when the app is closed)

Implement `com.beamable.push.PushNotificationReceivedHandler` to run native code the moment a
push arrives — including while the app is killed (FCM background process; no engine running):

```kotlin
interface PushNotificationReceivedHandler {
    fun onNotificationReceived(context: Context, event: PushReceivedEvent)
}
```
Register via app-manifest meta-data (required for the closed-app case):
```xml
<meta-data android:name="com.beamable.push.notification_received_handler"
           android:value="your.fully.Qualified.HandlerClass" />
```
or programmatically with `PushManager.setNotificationReceivedHandler(...)`. Working example:
`client/Assets/Plugins/Android/DiscordWebhookPushHandler.java`.

**Requirements & caveats:** send **data-only, high-priority** FCM messages (a `notification`
block is auto-displayed and bypasses the hook while closed — only fires on tap). A
force-stopped/OEM-killed app receives nothing until reopened. The handler runs on a
background thread (~10s budget); a short blocking call is fine, otherwise enqueue WorkManager.

## Engine integration — development guide (Unity / Unreal / React Native)

### The model: one shared core, two directions

The libraries are an **engine-agnostic core** that any code calls:
`PushManager` / `DeepLinkManager` (facades) + `PushListener` / `DeepLinkListener` (callbacks).

An engine adapter only handles two things, and **only the second is engine-specific**:

1. **Inbound (engine → core)** — calling methods, e.g. `PushManager.scheduleLocal(json, delay)`.
   Every engine calls the *same* core; only the calling convention differs (C# JNI vs C++ JNI vs
   a Kotlin React module).
2. **Outbound (core → engine)** — delivering listener callbacks/results *back* to the engine.
   The transport is different per engine, so this is the part that can't be one shared file:
   - Unity → `UnityPlayer.UnitySendMessage`
   - React Native → `RCTDeviceEventEmitter.emit`
   - Unreal → a JNI callback into C++, marshalled to the game thread

> **Can Unreal and React Native share the same `.kt`?** They already share the **core**. The
> outbound delivery can't be a single class because the transport differs — but the
> `EngineBridge { emit(method, payload) }` interface lets you share the *listener→event mapping*
> and implement only `emit` per engine. So the engine-specific code shrinks to one small class.

**Recommended pattern (minimises per-engine code):**
```
listener callback ──> [shared] ListenerToBridge ──> EngineBridge.emit(method, json)
                                                          ▲
                          Unity: UnitySendMessage ────────┤
                          RN:    DeviceEventEmitter ───────┤
                          UE:    JNI → C++ (game thread) ──┘
```
Implement `EngineBridge` once per engine; the listener-to-event serialization stays shared.

### Unity — implemented (in this `.aar`, `unity/`)
- **Inbound:** `UnityPush` / `UnityDeepLink` — `@JvmStatic` facades. Needed because Unity's
  `AndroidJavaClass` can't cleanly call Kotlin `object` instance methods or pass the activity
  across JNI; the facades take only strings/primitives and resolve the activity natively.
- **Outbound:** `UnityPushBridge` / `UnityDeepLinkBridge` implement the core listeners and call
  `UnitySendMessage(gameObject, method, json)` via reflection (no Unity dependency at build).
- Must ship precompiled in the `.aar` — Unity does not compile Kotlin from `Assets/`.

### Unreal — Kotlin adapter shipped in the `.aar` (`unreal/`); C++/UPL glue still to do
- **Shipped (`unreal/`):** `UnrealPush`/`UnrealDeepLink` (`@JvmStatic` inbound facades UE C++
  calls via JNI) + `UnrealPushBridge`/`UnrealDeepLinkBridge` (outbound: implement the listener
  and call JNI `external` functions). No UE dependency.
- **Still to do (in a UE plugin):** the C++ side — call the facades via JNI
  (`FJavaWrapper`/`FAndroidApplication`), implement the bridge's `native` functions, marshal to
  the game thread (`AsyncTask(ENamedThreads::GameThread)`), and broadcast a delegate; plus a
  `.uplugin` + UPL that imports this `.aar` + its transitive deps and (deeplink) forwards
  `GameActivity.onNewIntent` → `UnrealDeepLink.handleNewIntent`. (C++/UPL can't live in an `.aar`.)

### React Native — Kotlin adapter shipped in the `.aar` (`react/`); JS package still to do
- **Shipped (`react/`):** `ReactPushModule`/`ReactDeepLinkModule`
  (`ReactContextBaseJavaModule`, inbound `@ReactMethod` + outbound `RCTDeviceEventEmitter`;
  deeplink also registers an `ActivityEventListener`) + their `ReactPackage`s. Compiled against a
  `compileOnly` `com.facebook.react` (not bundled — see ProGuard note in each library README).
- **Still to do (in an RN package):** the JS/TS API and `react-native.config.js` that registers
  the `ReactPackage` with autolinking; depend on this `.aar`.

### Rules for any engine
- Consume the `.aar` **and declare its transitive Maven deps** (`kotlin-stdlib`, `androidx.core`,
  and for push `firebase-messaging`) — a loose `.aar`'s POM is not resolved by consumers.
- `minSdk 24`. Remote push (FCM) is optional: it activates only when a `google-services.json` is
  present; otherwise the library is local-only.
- The notification-tap `PendingIntent` targets `com.unity3d.player.UnityPlayerActivity` by
  default (`NotificationBuilder.UNITY_ACTIVITY`) — change that constant for another engine, or
  rely on the package launch-intent fallback.
- The receive-time `PushNotificationReceivedHandler` (manifest meta-data) is already
  engine-agnostic and works for all engines unchanged.
