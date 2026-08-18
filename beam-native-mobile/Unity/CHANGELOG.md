# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0]

Initial release of `com.beamable.notifications`, the Beamable notifications package for Unity SDK games.

### Added

- `Beamable.Notifications.BeamableNotifications` — one cross-platform static C# API over the iOS and
  Android native cores, with main-thread events. A safe no-op in the Editor and on non-mobile targets.
- Local and remote push (APNs/FCM), notification templates, categories and action buttons, and rich
  media.
- Deep links, captured natively on both cold and warm start.
- Closed-app campaign funnel analytics (Sent, Received, Opened, Clicked, Converted). `ConfigureAuth`
  persists the player token into native shared storage so events still post while the app is killed.
- Editor tooling: a **Tools ▸ Beamable ▸ Notifications** setup window, plus iOS post-build and
  Android manifest/Gradle processing that wires up entitlements, the Notification Service Extension,
  and the native dependencies.
- Prebuilt Android `.aar` and iOS `.xcframework` binaries ship in the package, alongside a
  `Samples~/NativeDemo` test harness.

### Known issues

- The `com.beamable` dependency is temporarily hard-pinned to `5.1.0`; it should instead track the
  consumer's installed Beamable version.
