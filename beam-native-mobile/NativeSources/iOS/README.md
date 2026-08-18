# Beamable Native iOS Library

One reusable, **engine-agnostic** Swift library (one SPM package → one `BeamableNotifications.xcframework`)
holding **push notifications, deep links, rich media, and Live Activities** — mirroring the single
Android `notifications` `.aar`. Consumed by **Unity**, **Unreal**, and **React Native**.

```
beam-native-mobile/NativeSources/iOS/BeamableNotifications/   (Swift package + extension hosts)
  core/               → the Swift core, C ABI (bmn_*), plugin system, Live Activity widget templates
  extension/          → Notification Service Extension host + service plugins
  content-extension/  → Notification Content Extension host + content renderers
  scripts/            → build-xcframework.sh (static), build-xcframework-dynamic.sh (Unreal)
  → builds BeamableNotifications.xcframework
```

**Where iOS differs from Android:** on Android the thin per-engine adapters (`unity/`, `unreal/`,
`react/`) ship *inside* the `.aar`. On iOS they do not — the core exposes a flat **C ABI**
(`@_cdecl`, 33 `bmn_*` functions in `core/.../CABI.swift`) plus direct Swift interop, and each engine
package holds its own wrapper. So the iOS core has no engine-specific code at all, and adding an
engine means adding a wrapper in that engine's package, not in this folder.

## Building the xcframework

Swift package: swift-tools **5.7**, min iOS **14**, product type **static** (`core/Package.swift`).
**macOS only** — needs a full Xcode install with the iOS SDK; on Windows the entire iOS path is
skipped.

```bash
cd BeamableNotifications && ./scripts/build-xcframework.sh
# → build/BeamableNotifications.xcframework  (static; device + simulator slices)
```

Normally you don't run this directly — **`./dev-native.sh`** (repo root) builds both variants and
stages them into every engine package. Two variants exist because the engines can't share one:

| Variant | Script | Staged to | Why |
|---|---|---|---|
| **Static** xcframework | `build-xcframework.sh` | `Unity/Plugins/iOS/`, `Unity.Web/Plugins/iOS/`, `ReactNative/ios/` | Unity's `[DllImport("__Internal")]` requires static linkage |
| **Dynamic** framework zip | `build-xcframework-dynamic.sh` | `Unreal/ThirdParty/BeamableNotifications.embeddedframework.zip` | UE's `PublicAdditionalFrameworks` cannot consume an `.xcframework` |

The staged binaries are **not** committed for React Native (gitignored, produced on demand); the
Unity and Unreal copies are tracked.

## Consuming from Unity

The static xcframework **ships inside the shared `Beamable.Notifications` package**
(`Plugins/iOS/`), so a consuming Unity project gets it via the package reference — nothing to copy
into `Assets/`.

1. Reference the package in `Packages/manifest.json`:
   `"com.beamable.notifications": "file:../../beam-native-mobile/Unity"`.
2. **Editor tooling (shipped in the package) does the Xcode wiring automatically** —
   `Editor/NotificationsPostProcess.cs` runs on iOS post-process and:
   - creates the **Notification Service Extension** target (`BeamableNotificationServiceExtension`)
     with its `Info.plist` (`NSExtensionPrincipalClass = $(PRODUCT_MODULE_NAME).NotificationService`)
     and entitlements;
   - adds the **App Group** + **push** entitlements to *both* the app and the extension;
   - adds the **remote-notification** background mode;
   - writes the `BMNAppGroup` Info.plist key on both targets.

   The App Group id is the `AppGroupId` const (default `group.com.beamable.notifications`) — change
   it per project, and make sure it exists in **both** provisioning profiles.
3. Game code uses the shared `Beamable.Notifications` C# API (same as Android); on iOS it
   P/Invokes the `bmn_*` C ABI. Native → C# callbacks are C function pointers delivering a JSON
   string, hopped to the main thread.

## Required capabilities (every host app, any engine)

Unlike Android — where remote push is *optional* and needs no project capability — iOS push does
not work at all without these:

- **Push Notifications** (`aps-environment` entitlement)
- **Background Modes → Remote notifications**
- An **App Group** shared by the app and the Notification Service Extension (carries the analytics
  config and delivery receipts; the id is published to both via the `BMNAppGroup` Info.plist key)

## Local vs remote (push)

- **Local-only:** works with no backend and no APNs setup. Time-interval / calendar / immediate
  triggers via `NotificationManager.scheduleLocal`.
- **Remote:** **raw APNs only.** The SDK registers and surfaces the Apple **device token** (hex);
  *sending is your backend's job*. FCM is deliberately not built in — it is reachable as a single
  plugin (`NotificationPlugin.provideRemoteToken`), which is the iOS counterpart to Android's
  optional `google-services.json`.

## Receive-time hook (runs even when the app is closed)

Android runs a `PushNotificationReceivedHandler` in an FCM background process. iOS has no
equivalent — the closed-app path is a **Notification Service Extension**:

- The push must carry `mutable-content: 1`; iOS then wakes the NSE (`extension/NotificationService.swift`)
  before display, and its service plugins run — `RichMediaServicePlugin` (attachments) and
  `AnalyticsServicePlugin` (fires the funnel "Received" event and logs a delivery receipt into the
  App Group store).
- Read receipts back in-app via `getDeliveryReceipts`.

**Limits worth knowing (do not try to "fix" these):**

- When the app is **force-quit**, iOS runs no app *or* extension code on delivery — the closed-app
  analytics path needs a non-force-quit state.
- **Local** notifications that fire while the app is closed run no code at fire time. You only
  learn about them via "get intent" on next launch, or on tap.

## Engine integration — development guide (Unity / Unreal / React Native)

### The model: one shared core, two directions

The core is engine-agnostic: `NotificationManager` (facade) + a set of callback **closures**
(`onTokenReceived`, `onNotificationTapped`, …). An engine wrapper handles two things, and only the
second is engine-specific:

1. **Inbound (engine → core)** — calling functions. Unity and Unreal go through the flat C ABI
   (`bmn_*`, JSON strings for structured args/results); React Native calls the Swift core directly.
2. **Outbound (core → engine)** — delivering callbacks back. `CABI.swift` wires C function pointers
   into the core's closures; the transport and the thread hop differ per engine:
   - Unity → C trampoline → main thread → `UnitySendMessage`-style event
   - Unreal → C trampoline → game thread → delegate broadcast
   - React Native → the module sets the closures directly and emits via `RCTEventEmitter`

Cold-start taps that arrive before the engine has registered a callback are **queued** in
`LaunchTracker` and flushed when `onNotificationTapped` is set — so no engine needs its own
cold-start buffer.

### Unity — implemented (`Unity/`, `Unity.Web/`)
Links the **static** xcframework, P/Invokes `__Internal`. Xcode project wiring (NSE target, App
Group, entitlements, background mode) is automated by `Editor/NotificationsPostProcess.cs`.

### Unreal — implemented (`Unreal/`)
Links the **dynamic** framework via `PublicAdditionalFrameworks` with `bCopyFramework: true` (the
framework's install name is `@rpath/…`, so it must be copied). C++ `GameInstanceSubsystem` +
Blueprint delegates over the C ABI. The Xcode **extension target** and the push/App-Group
**entitlements** remain a documented manual one-time step (`Unreal/Scripts/add-nse.sh` helps) —
automating them further is open work.

### React Native — implemented (`ReactNative/`)
The pod **vendors the prebuilt xcframework** (`vendored_frameworks` in
`BeamableNotificationsRN.podspec`) — the same binary Unity links. The bridge
(`ReactNative/ios/*.swift`) is bridge-only; the core is not recompiled.

The NSE is **opt-in** via the Expo config plugin
(`{ "enableServiceExtension": true }` → `ReactNative/plugin/withBeamableNotifications.js`), which at
`expo prebuild` copies the extension host plus an explicitly **extension-safe subset** of the core
(`CORE_NSE_FILES`: `Models`, `SharedConfig`, `BeamableAnalytics`, `ActionButton`, `ActionButtons`,
`CategoryStore` — all Foundation/UserNotifications only, no `UIApplication`). The full core cannot
compile into an app extension because `NotificationManager`/`RemotePush` use `UIApplication`, which
is why the subset is curated by hand rather than globbed.

### Rules for any engine
- Pick the right binary: **static** for anything using `__Internal`, **dynamic embeddedframework**
  for Unreal. There is no one-size binary.
- **min iOS 14.** Structured data crosses the boundary as **one JSON string** — deep link and custom
  data included.
- The host app needs the three capabilities above; ship the wiring as engine-side build tooling
  (as Unity does) rather than asking users to click through Xcode.
- Prefer a **plugin** over touching the core for optional native behavior — plugins register via
  the `BMNPlugins` / `BMNServicePlugins` / `BMNContentRenderers` Info.plist arrays or
  `PluginRegistry.shared.register(...)`.
- Anything compiled into the NSE or a Widget extension must stay `UIApplication`-free — see the
  banner comment in `core/.../ActionButton.swift`.

## Going deeper

[`BeamableNotifications/README.md`](BeamableNotifications/README.md) carries the feature list, the
architecture diagram, the **feature → code map**, locked design decisions, key conventions, open
caveats, and how to compile + unit-test the core on a simulator.

Live Activities (`core/.../LiveActivity/`, `core/WidgetTemplates/`) and custom notification styles
are documented in [`ReactNative/docs/custom-notifications.md`](../../ReactNative/docs/custom-notifications.md).
Per-engine install and usage live with each plugin: [Unity](../../Unity/README.md) ·
[Unreal](../../Unreal/README.md) · [React Native](../../ReactNative/README.md).
