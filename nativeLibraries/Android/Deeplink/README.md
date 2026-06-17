# Deeplink (`com.beamable.deeplink`)

Engine-agnostic Android deeplink library: natively captures the `ACTION_VIEW` intent's data URI
for both **cold-start** (pull) and **warm-start**, without replacing the host activity.

The engine adapters (Unity / Unreal / React Native) ship **inside the same `.aar`** under
`unity/`, `unreal/`, `react/` — thin routing layers over the shared core (`DeepLinkManager` +
`DeepLinkListener`).

> The `VIEW` intent-filter and URL scheme stay in the **consuming app's** manifest; this library
> only observes intents.

---

## Build

```bash
gradle wrapper --gradle-version 8.2     # once, if gradle/wrapper/gradle-wrapper.jar is absent
./gradlew :deeplink:assembleRelease
# → deeplink/build/outputs/aar/deeplink-release.aar
```
Toolchain: AGP 8.1.4, Gradle 8.2, Kotlin 1.9.22, `compileSdk 34`, `minSdk 24`, Java 11.

---

## API (shared core)

`com.beamable.deeplink.DeepLinkManager`:
- `initialize(application, listener)` — register the cold-start observer.
- `getInitialLink(activity): String?` — pull the launch URL (cold start).
- `handleNewIntent(intent)` — warm-start hook (forward the activity's `onNewIntent`).
- `clearConsumed()` — reset dedupe.

`DeepLinkListener`: `onDeepLink(url, isColdStart)`.

Pattern: read `getInitialLink` once at startup (cold start); for warm start, route `onNewIntent`
into `handleNewIntent` and receive results via the listener.

---

## Adapter packages — what the `unity/`, `unreal/`, `react/` `.kt` are

**Thin routing layers** over the shared core (`DeepLinkManager` + `DeepLinkListener`) — no
deeplink logic, only how each engine calls *in* and how results route *back*:

| Package | Inbound (engine → core) | Outbound (core → engine) | Engine dependency |
|---|---|---|---|
| `unity/` | `UnityDeepLink` — `@JvmStatic` facade C# calls via `AndroidJavaClass` | `UnityDeepLinkBridge` → `UnityPlayer.UnitySendMessage` (reflection) | none |
| `react/` | `ReactDeepLinkModule` — `@ReactMethod` + `ActivityEventListener` | `ReactDeepLinkModule` → `RCTDeviceEventEmitter` | React (compileOnly) |
| `unreal/` | `UnrealDeepLink` — `@JvmStatic` facade UE C++ calls via JNI | `UnrealDeepLinkBridge` → JNI `external` func (impl'd in the UE plugin's C++) | none |

All three target the **identical core**; the other engines' classes are never loaded in a given
engine's app.

## ProGuard / the `com.facebook.react` dependency

`build.gradle` declares:
```gradle
compileOnly 'com.facebook.react:react-android:0.73.4'
```
The `react/` adapter needs React classes to **compile**, but `compileOnly` keeps React **out of
the `.aar`** and out of the transitive graph — Unity/Unreal apps never pull it. Since the kept
`react/` classes reference React types that are absent in a non-RN app, a minifying (R8) consumer
would log "missing class" warnings, so `proguard-rules.pro` adds:
```
-dontwarn com.facebook.react.**
```
Safe because the React module is only instantiated by React Native's package system — never
loaded (and never references those classes) in a Unity/Unreal app. The `unreal/` adapter needs no
such rule (reflection + JNI `external` functions only, no compile-time UE references).

---

## Use in Unity

1. Drop `deeplink-release.aar` into `Assets/Plugins/Android/` (the consuming Unity project's
   editor build processor injects `kotlin-stdlib` + `androidx.core`).
2. Keep your scheme's `VIEW` intent-filter in `Assets/Plugins/Android/AndroidManifest.xml`.
3. Call the `@JvmStatic` facade `com.beamable.deeplink.unity.UnityDeepLink` from C#:
   ```csharp
   using (var dl = new AndroidJavaClass("com.beamable.deeplink.unity.UnityDeepLink"))
   {
       dl.CallStatic("initialize", "DeepLinkManager");          // routes warm-start to GameObject "DeepLinkManager"
       string cold = dl.CallStatic<string>("getInitialLink");   // cold start (may be null)
       if (!string.IsNullOrEmpty(cold)) Process(cold);
   }
   ```
4. Warm-start callbacks arrive via `UnitySendMessage` on the **`DeepLinkManager`** GameObject:
   `OnNativeDeepLink(string url)`. (Unity's own `onNewIntent` already drives the native observer,
   so no extra wiring is needed in Unity.)

## Use in React Native

The native module + `ReactPackage` ship in the `.aar` (`com.beamable.deeplink.react`); the module
auto-forwards `onNewIntent` via an `ActivityEventListener`.
1. Register with autolinking — `react-native.config.js`:
   ```js
   module.exports = { dependency: { platforms: { android: {
     packageImportPath: 'import com.beamable.deeplink.react.ReactDeepLinkPackage;',
     packageInstance: 'new ReactDeepLinkPackage()' } } } };
   ```
2. Use from JS (native module name **`BeamableDeeplink`**):
   ```ts
   import { NativeModules, NativeEventEmitter } from 'react-native';
   const { BeamableDeeplink } = NativeModules;
   const emitter = new NativeEventEmitter(BeamableDeeplink);

   BeamableDeeplink.initialize();
   BeamableDeeplink.getInitialLink().then((url) => url && route(url));   // cold start
   emitter.addListener('onDeepLink', ({ url, isColdStart }) => route(url)); // warm start
   ```

## Use in Unreal

The Kotlin facade (`com.beamable.deeplink.unreal.UnrealDeepLink`) + JNI bridge
(`UnrealDeepLinkBridge`) ship in the `.aar`. The UE plugin supplies the C++/UPL glue:
1. UPL imports `deeplink-release.aar` and forwards `GameActivity.onNewIntent(intent)` →
   `UnrealDeepLink.handleNewIntent(intent)`.
2. C++ calls `UnrealDeepLink.initialize()` and `UnrealDeepLink.getInitialLink()` via JNI.
3. Implement the JNI callback the bridge declares — marshal to the Game thread, then broadcast a
   UE delegate: `Java_com_beamable_deeplink_unreal_UnrealDeepLinkBridge_nativeOnDeepLink`.
