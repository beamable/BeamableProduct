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
[docs/plugins.md](docs/plugins.md)).

## Architecture

```
core/  (Swift)  ──►  C ABI (@_cdecl) + include/BeamableNotifications.h
   │                      ├──►  unity/    C# P/Invoke + events
   │                      └──►  unreal/   C++ GameInstanceSubsystem + Blueprints
   └────────── Swift interop ──────────►  reactnative/  RCTEventEmitter + TS
extension/ (Swift)  Notification Service Extension (rich media + closed-app analytics)
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

Then place it per engine:
- **Unity** — copy into `unity/Plugins/iOS/`
- **Unreal** — the plugin lives at `EnginePlugins/Unreal/` and needs the **dynamic** variant; the
  repo's `dev-native.sh` builds it via `scripts/build-xcframework-dynamic.sh` and stages it into
  `EnginePlugins/Unreal/ThirdParty/BeamableNotifications.embeddedframework.zip`.
- **React Native** — copy into `reactnative/ios/Frameworks/`

## Per-engine setup

- [docs/unity.md](docs/unity.md)
- [docs/unreal.md](docs/unreal.md)
- [docs/reactnative.md](docs/reactnative.md)
- [docs/payloads.md](docs/payloads.md) — JSON schemas for every call/callback
- [docs/plugins.md](docs/plugins.md) — extend the SDK natively

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
core/         Swift package — the native core, C ABI, plugin system, reference plugins
extension/    Notification Service Extension host + service plugins
unity/        UPM package: C# bindings, events, editor post-build
reactnative/  npm package: Swift module, ObjC bridge, TS API, podspec
scripts/      build-xcframework.sh, build-xcframework-dynamic.sh (UE dynamic framework)
docs/         per-engine setup + payload schemas + plugin authoring

(The UE plugin BeamPlatformNotifications now lives alongside the other engine plugins at
 EnginePlugins/Unreal/, no longer under this iOS folder.)
```
