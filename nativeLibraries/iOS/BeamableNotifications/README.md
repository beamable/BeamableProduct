# Beamable Notifications

A small, reusable **iOS notifications SDK** with one shared native core consumed from
**Unity**, **Unreal**, and **React Native**.

## Features

| # | Feature | Notes |
|---|---------|-------|
| 1 | Local push notifications | time-interval / calendar / immediate triggers |
| 2 | Remote push notifications | raw APNs — exposes the Apple device token; your backend sends |
| 3 | All callbacks | permission, token, foreground-present, received, tap/action |
| 4 | Notification templates | `{placeholder}` substitution, reusable defaults |
| 5 | Request permission | alert/badge/sound/provisional/critical |
| 6 | Get notification data ("get intent") | the notification that launched the app |
| 7 | Deep links, action buttons, rich media | deep link delivered as JSON; media via NSE |
| 8 | Received-while-closed analytics | NSE fires analytics + logs a delivery receipt |

Plus a **native plugin system** to extend the SDK without editing its core (see
the *plugin system* section of the per-engine notification guides in the Beamable documentation).

## Architecture

```
core/  (Swift)  ──►  C ABI (@_cdecl) + include/BeamableNotifications.h
   │                      ├──►  EnginePlugins/Unity     C# P/Invoke + events
   │                      └──►  EnginePlugins/Unreal    C++ GameInstanceSubsystem + Blueprints
   └────────── Swift interop ──────────►  EnginePlugins/ReactNative  RCTEventEmitter + TS
extension/          Notification Service Extension (rich media + closed-app analytics)
content-extension/  Notification Content Extension host (custom expanded UI)
```

- **Unity & Unreal** link the static `BeamableNotifications.xcframework` and call the
  flat C ABI. Callbacks are C function pointers delivering a JSON string.
- **React Native** calls the Swift core directly (no C ABI).
- Deep link + custom data always reach engine code as a single JSON payload.

## Build the native core

```bash
./scripts/build-xcframework.sh
# -> build/BeamableNotifications.xcframework  (static; device + simulator slices)
```

Normally you don't run this by hand — the repo's `../../dev-native.sh` builds **and stages** the binary
into each engine plugin. Per-engine variants:

- **Unity** — the static xcframework, staged to `EnginePlugins/Unity/Plugins/iOS/`.
- **Unreal** — needs the **dynamic** variant; `dev-native.sh` builds it via
  `scripts/build-xcframework-dynamic.sh` and stages
  `EnginePlugins/Unreal/ThirdParty/BeamableNotifications.embeddedframework.zip`.
- **React Native** — the pod compiles the core **from source** (see *Engineering notes* below), with the
  sources mirrored by `scripts/sync-rn-core.sh`.

## Per-engine setup

Install and usage live with each plugin: [Unity](../../EnginePlugins/Unity/README.md) ·
[Unreal](../../EnginePlugins/Unreal/README.md) ·
[React Native](../../EnginePlugins/ReactNative/README.md).

The payload JSON schemas, the native plugin protocols, and APNs provisioning are documented in the
per-engine push notification guides in the Beamable documentation.

## Required iOS capabilities (every host app)

- **Push Notifications** (`aps-environment` entitlement)
- **Background Modes → Remote notifications**
- **App Group** shared by the app + the Notification Service Extension (used by the
  analytics config and delivery receipts; the App Group id is published to both via the
  `BMNAppGroup` Info.plist key)

## iOS limits worth knowing

- When the app is **force-quit**, iOS runs no app/extension code on delivery — the
  closed-app analytics path needs `mutable-content:1` and a non-force-quit state.
- **Local** notifications fired while the app is closed run no code at fire time; you
  only learn about them via "get intent" on next launch or on tap.

## Repo layout

```
core/               Swift package — the native core, C ABI, plugin system, reference plugins
extension/          Notification Service Extension host + service plugins
content-extension/  Notification Content Extension host + content renderers
scripts/            build-xcframework.sh, build-xcframework-dynamic.sh (UE dynamic framework),
                    sync-rn-core.sh (mirror core sources into the RN pod)
```

The engine wrappers live under `../../EnginePlugins/` (Unity, Unity.Web, Unreal, ReactNative), not in
this folder.

---

# Engineering notes

## Locked decisions (don't re-litigate without asking)

- Native core in **Swift**; exposes a flat **C ABI** (`@_cdecl`) for Unity/Unreal.
- **React Native calls the Swift core directly** (no C ABI).
- **Raw APNs** only — the SDK surfaces the Apple device token (hex); sending is the backend's job. FCM is
  intentionally *not* built, but reachable as a single plugin.
- Deep link + custom data cross to engine code as **one JSON string**.
- Distributed as a **prebuilt static XCFramework** (static is required for Unity's
  `[DllImport("__Internal")]`).
- Min iOS **14**.
- All engine wrappers ship together.
- Extensibility via a **native plugin system**, no core edits — an explicit requirement.

## Key conventions

- **C ABI**: functions prefixed `bmn_`; structured args/results are JSON strings; callbacks are
  `void(*)(const char* json)`, registered once via `bmn_setOn*`.
- **Callback closures** live on `NotificationManager` (`onTokenReceived`, `onNotificationTapped`, …).
  `CABI.swift` wires C function pointers into them; the RN module sets them to emit JS events;
  Unreal/Unity register C trampolines that hop to the game/main thread.
- **Cold-start taps** delivered before the engine registers a callback are queued in `LaunchTracker` and
  flushed when `onNotificationTapped` is set (see the `didSet` in `NotificationManager` and
  `bmn_setOnNotificationTapped`).
- **Token source is pluggable** via `NotificationPlugin.provideRemoteToken`. The default returns false →
  the core does standard APNs. FCM would be a plugin returning true.
- **App Group** (`BMNAppGroup` Info.plist key) is required for closed-app analytics + delivery receipts,
  and is shared by the app and the NSE. Default id `group.com.beamable.notifications` — change it per
  project (it is set in the Unity post-processor and the Unreal UPL).
- **Swizzling** of the app delegate (`RemotePush.installSwizzlingIfNeeded`) uses `class_replaceMethod`
  with stored original IMPs to forward. Opt out with `BMNDisableSwizzling=YES`.
- **Plugins** register via the Info.plist arrays `BMNPlugins` (app) / `BMNServicePlugins` (NSE) /
  `BMNContentRenderers` (content extension), or `PluginRegistry.shared.register(...)`. Auto-discovered
  classes must be `NSObject` subclasses with `init()`.

## Feature → code map

| Feature | Where |
|---|---|
| Local push | `core/.../NotificationManager.scheduleLocal` + `buildContent`/`buildTrigger` |
| Remote push (raw APNs) | `core/.../RemotePush.swift` (registration + delegate swizzling) |
| All callbacks | `NotificationManager` closures; C ABI in `CABI.swift`; per-engine events |
| Templates | `core/.../TemplateStore.swift` |
| Permission | `NotificationManager.requestPermission` / `getPermissionStatus` |
| Get intent | `core/.../LaunchTracker.swift` + `bmn_getLaunchNotification` |
| Deep link / action buttons / rich media | userInfo `deepLink`; `CategoryStore`; NSE `RichMediaServicePlugin` |
| Received + closed-app analytics | `onNotificationReceived`; `SharedConfig` (App Group); NSE `AnalyticsServicePlugin`; `getDeliveryReceipts` |
| Plugin system | `core/.../PluginRegistry.swift` + `core/.../Plugins/*` + `extension/ServicePlugins/*` |

## Open items & caveats

- **Unreal needs a DYNAMIC framework, not the xcframework.** UE's `PublicAdditionalFrameworks` can't
  consume an `.xcframework`; it expects a zip that unzips to
  `<Name>.embeddedframework/<Name>.framework` (a single dynamic framework, device arm64). Use
  `scripts/build-xcframework-dynamic.sh` and link with `bCopyFramework: true`. The static
  `build-xcframework.sh` remains for Unity (`__Internal`).
- **RN builds the core FROM SOURCE**, not from the xcframework. The SPM-built xcframework exposes no
  importable Swift module (no `Modules/*.swiftmodule`), so `import BeamableNotifications` failed with
  "Unable to find module dependency". The RN pod (`BeamableNotificationsRN`) therefore compiles the core
  sources directly; they are mirrored into the pod by `scripts/sync-rn-core.sh` (CocoaPods sandboxes
  `source_files` to the pod root, so `../core` globs are silently dropped). The bridge shares that module
  — no `import BeamableNotifications` — and needs `import React` for `RCTEventEmitter`. **Re-run
  `sync-rn-core.sh` after editing the core.**
- **The RN NSE is unsolved for the from-source path**: the core uses `UIApplication`
  (extension-unsafe), so it can't compile into an app extension. The Expo config plugin gates the NSE
  behind `enableServiceExtension`. Enabling rich media / closed-app analytics on RN needs an
  extension-API-safe core slice (or a proper Swift-module framework) for the NSE to link — open work.
- **Unreal NSE**: the UPL stages the Info.plist edits, but the Xcode *extension target* and the
  push/App-Group **entitlements** are a documented manual one-time step (see the Unreal plugin README).
  Automating it further (a build graph / xcodeproj patch) is open.
- **Remote push, rich media, and closed-app analytics need a physical device** plus a real App Group and
  provisioning profiles; they are not exercised by the unit tests.
- **Local notifications fired while the app is fully closed produce no receipt** — an iOS limit. Don't
  try to "fix" it.

## Verify the core

```bash
# Compile + unit-test the core on a simulator
cd core
UDID=$(xcrun simctl list devices available | grep -m1 -oE "[0-9A-F-]{36}")
xcodebuild -scheme BeamableNotifications -destination "platform=iOS Simulator,id=$UDID" \
  -derivedDataPath /tmp/bmn-dd test
```

## Changing the SDK

1. A native behavior change almost always belongs in `core/` — then it's free for every engine. Add a C
   ABI function in `CABI.swift` + the header, then expose it in each wrapper.
2. A new event → add a closure on `NotificationManager`, wire it in `CABI.swift`, then add it to the Unity
   events / Unreal delegates / RN `supportedEvents` + TS types.
3. New *optional* native behavior → prefer a **plugin** over touching the core.
4. Keep payload schemas in sync across the published guides, Unity `Payloads.cs`, RN `src/index.ts`, and the
   Swift `Models.swift`.
