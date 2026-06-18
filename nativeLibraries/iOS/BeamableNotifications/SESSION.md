# Session summary — Beamable Notifications SDK

Reference for continuing work (engine integrations, changes). Reflects state as of the
initial build. Plan file: `~/.claude/plans/swirling-pondering-hamming.md`.

## What this is

A cross-engine **iOS notifications SDK**: one Swift core consumed by **Unity**,
**Unreal**, and **React Native**. Status: fully scaffolded; core compiles for iOS and
passes unit tests; xcframework build verified. Remote-push end-to-end needs a physical
device (not yet exercised).

## Locked decisions (don't re-litigate without asking)

- Native core in **Swift**; exposes a flat **C ABI** (`@_cdecl`) for Unity/Unreal.
- **React Native calls the Swift core directly** (no C ABI).
- **Raw APNs** only — SDK surfaces the Apple device token (hex); sending is the
  backend's job. FCM is intentionally *not* built, but reachable as a single plugin.
- Deep link + custom data cross to engine code as **one JSON string**.
- Distributed as a **prebuilt static XCFramework** (static is required for Unity's
  `[DllImport("__Internal")]`).
- Min iOS **14**.
- All three engine wrappers ship together.
- Extensibility via a **native plugin system** (no core edits) — this was an explicit
  requirement.

## Feature → code map

| Feature | Where |
|---|---|
| 1 Local push | `core/.../NotificationManager.scheduleLocal` + `buildContent/buildTrigger` |
| 2 Remote push (raw APNs) | `core/.../RemotePush.swift` (registration + delegate swizzling) |
| 3 All callbacks | `NotificationManager` closures; C ABI in `CABI.swift`; per-engine events |
| 4 Templates | `core/.../TemplateStore.swift` |
| 5 Permission | `NotificationManager.requestPermission/getPermissionStatus` |
| 6 Get intent | `core/.../LaunchTracker.swift` + `bmn_getLaunchNotification` |
| 7 Deep link / action buttons / rich media | userInfo `deepLink`; `CategoryStore`; NSE `RichMediaServicePlugin` |
| 8 Received + closed-app analytics | `onNotificationReceived`; `SharedConfig` (App Group); NSE `AnalyticsServicePlugin`; `getDeliveryReceipts` |
| Plugin system | `core/.../PluginRegistry.swift` + `core/.../Plugins/*` + `extension/ServicePlugins/*` |

## Repo layout

```
core/        Swift package: NotificationManager, Models (JSONValue), TemplateStore,
             CategoryStore, RemotePush, LaunchTracker, SharedConfig, PluginRegistry,
             CABI.swift, include/BeamableNotifications.h, Plugins/, Tests/
extension/   NotificationService.swift (NSE host) + ServicePlugins/ (protocol, RichMedia, Analytics)
unity/       Runtime/{Native,BeamableNotifications,Payloads,Dispatcher}.cs, Editor/NotificationsPostProcess.cs,
             package.json, asmdefs, Plugins/iOS/
unreal/      BeamableNotifications.uplugin, Source/BeamableNotifications/{Public,Private}/,
             Build.cs, IOS/BeamableNotifications_UPL.xml
reactnative/ ios/BeamableNotificationsModule.{swift,m}, BeamableNotificationsRN.podspec,
             src/index.ts, package.json, tsconfig.json
scripts/     build-xcframework.sh
docs/        payloads.md, plugins.md, unity.md, unreal.md, reactnative.md
```

## Key conventions / architecture facts

- **C ABI**: functions prefixed `bmn_`; structured args/results are JSON strings;
  callbacks are `void(*)(const char* json)`. Registered once via `bmn_setOn*`.
- **Callback closures** live on `NotificationManager` (`onTokenReceived`, `onNotificationTapped`, …).
  `CABI.swift` wires C function pointers into them; RN module sets them to emit JS events;
  Unreal/Unity register C trampolines that hop to the game/main thread.
- **Cold-start taps** delivered before the engine registers a callback are queued in
  `LaunchTracker` and flushed when `onNotificationTapped` is set (see the didSet in
  `NotificationManager` and `bmn_setOnNotificationTapped`).
- **Token source is pluggable** via `NotificationPlugin.provideRemoteToken`. Default
  returns false → core does standard APNs. FCM = a plugin returning true.
- **App Group** (`BMNAppGroup` Info.plist key) is required for closed-app analytics +
  delivery receipts; shared by app + NSE. Default id `group.com.beamable.notifications`.
- **Swizzling** of the app delegate (`RemotePush.installSwizzlingIfNeeded`) uses
  `class_replaceMethod` + stored original IMPs to forward. Opt out with
  `BMNDisableSwizzling=YES`.
- **Plugins**: register via Info.plist arrays `BMNPlugins` (app) / `BMNServicePlugins`
  (NSE), or `PluginRegistry.shared.register(...)`. Auto-discovered classes must be
  `NSObject` subclasses with `init()`.

## Build & verify

```bash
# Build the static xcframework (device + simulator), output build/BeamableNotifications.xcframework
./scripts/build-xcframework.sh

# Compile + unit-test the core on a simulator
cd core
UDID=$(xcrun simctl list devices available | grep -m1 -oE "[0-9A-F-]{36}")
xcodebuild -scheme BeamableNotifications -destination "platform=iOS Simulator,id=$UDID" \
  -derivedDataPath /tmp/bmn-dd test
```

After building the xcframework, place it per engine:
- Unity → `unity/Plugins/iOS/`
- Unreal → zip to `unreal/ThirdParty/BeamableNotifications.embeddedframework.zip`
- RN → `reactnative/ios/Frameworks/`

## Open items / caveats (likely next-session topics)

- **Remote push, rich media, closed-app analytics** (verification steps 4–9 in the plan)
  are untested — require a physical device + an APNs sender (curl/backend) and a real
  App Group + provisioning profiles.
- **Unreal needs a DYNAMIC framework, not the xcframework.** UE's
  `PublicAdditionalFrameworks` can't consume an `.xcframework`; it expects a zip that
  unzips to `<Name>.embeddedframework/<Name>.framework` (single dynamic framework, device
  arm64). Use `scripts/build-xcframework-dynamic.sh` (produces
  `build/BeamableNotifications.embeddedframework.zip`) and link with `bCopyFramework: true`.
  The static `build-xcframework.sh` remains for Unity (`__Internal`) / RN. Validated: the
  `mobiletest` iOS target compiles+links with this. (Integrated at
  `~/Documents/Unreal Projects/mobiletest`; see its `Plugins/BeamableNotifications/INTEGRATION.md`.)
- **Unreal NSE**: UPL stages the NSE Info.plist edits, but the Xcode *extension target*
  and push/App-Group **entitlements** are a documented manual one-time step
  (`docs/unreal.md`). Automating this further (a build graph / xcodeproj patch) is open.
- **App Group id** is hardcoded to `group.com.beamable.notifications` in
  `unity/Editor/NotificationsPostProcess.cs` and the UPL — change per project.
- **Unity NSE sources** must be copied to `unity/Plugins/iOS~/Extension/` (the `~` dir is
  not auto-populated; the post-processor reads from there).
- No CI yet. RN now has an example app: `~/Documents/Work/ReactiveNative` (Expo) wires the
  package as a `file:` dep + an Expo config plugin for the iOS capabilities/NSE.
- **RN now builds the core FROM SOURCE** (not the xcframework). The SPM-built xcframework
  exposes no importable Swift module (no `Modules/*.swiftmodule`), so `import
  BeamableNotifications` failed with "Unable to find module dependency". Fix: the RN pod
  (`BeamableNotificationsRN`) compiles the core sources directly. They're mirrored into
  `reactnative/ios/core/` by `scripts/sync-rn-core.sh` (CocoaPods sandboxes source_files to
  the pod root, so `../core` globs are silently dropped). The bridge shares that module —
  no `import BeamableNotifications` — and needs `import React` for `RCTEventEmitter`.
  Re-run `sync-rn-core.sh` after editing core. The pod name stays distinct from the core
  module name. (Unity/Unreal still use the prebuilt xcframework.)
- **RN NSE is unsolved for the from-source path**: the core uses `UIApplication`
  (extension-unsafe), so it can't compile into an app extension. The Expo config plugin
  gates the NSE behind `enableServiceExtension` (default off). To enable rich media /
  closed-app analytics on RN, build an extension-API-safe core slice (or a proper
  Swift-module framework) the NSE can link — open work.
- Local notifications fired while the app is fully closed produce no receipt — iOS limit,
  documented; don't try to "fix" it.

## When asked to integrate/change an engine

1. Native behavior change → almost always belongs in `core/` (then it's free for all
   three engines). Add a C ABI function in `CABI.swift` + header, then expose in each
   wrapper.
2. New event → add closure on `NotificationManager`, wire in `CABI.swift`, then add to
   Unity events / Unreal delegates / RN `supportedEvents` + TS types.
3. New optional native behavior → prefer a **plugin** over touching the core.
4. Keep payload schemas in sync across `docs/payloads.md`, Unity `Payloads.cs`, RN
   `src/index.ts`, and the Swift `Models.swift`.
```
