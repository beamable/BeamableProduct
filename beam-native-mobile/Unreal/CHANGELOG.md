# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0]

Initial release of `BeamPlatformNotifications`, the Beamable notifications plugin for Unreal Engine.

### Added

- `UBeamPlatformNotificationsSubsystem` — permissions, local and scheduled notifications, remote push
  (APNs/FCM), deep links, and closed-app delivery analytics, exposed through Blueprint-assignable
  delegates that broadcast on the game thread. A no-op on editor and desktop targets, so it always
  compiles.
- Campaign funnel analytics: `ConfigureAuth` / `ClearAuth` and `TrackOfferClicked` /
  `TrackOfferConverted`. Closed-app receipts are emitted natively, so no game-side receive handler is
  needed.
- An **iOS + NSE → Device** editor toolbar button that packages iOS, grafts and signs the closed-app
  Notification Service Extension, and installs to a chosen device.
- `install-beamplatformnotifications.sh` — generates a self-contained plugin copy, installs and
  enables it, and writes the App Group, deep-link scheme, and FCM settings into `DefaultEngine.ini`.
  Nothing project-specific is baked into the plugin.
- APL/UPL build wiring for the Android manifest and the iOS background mode, entitlements, and
  frameworks, plus `NSE-SETUP.md`.
- Prebuilt Android AAR and iOS framework binaries ship under `ThirdParty/`.

### Known issues

- The bundled iOS framework is dynamic and **device-only (arm64)**, because Unreal's
  `PublicAdditionalFrameworks` cannot consume an `.xcframework`. iOS simulator builds are not
  supported.
