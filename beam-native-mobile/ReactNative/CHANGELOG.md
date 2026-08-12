# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0]

Initial release of `@beamable/notifications-react-native`. One package: autolinking selects the
Android AAR or the iOS xcframework per platform.

### Added

- `BeamNotifications` — the single blessed façade, with every method platform-gated to a safe no-op.
  Solicited calls return Promises; the matching event still fires for unsolicited pushes.
- React hooks and typed listeners: `BeamPushNotifications()`, `BeamNotificationEvent()`,
  `BeamLaunchNotification()`, plus `addListener` / `addDeepLinkListener` / `addAllListeners`.
- Local and remote push (APNs/FCM), notification templates, categories and action buttons, and rich
  media.
- iOS Live Activities — countdown, actions, and animated variants, with push-to-start registration
  and a clean fallback on unsupported devices.
- Deep links and campaign coordinate helpers for notification payloads.
- Closed-app campaign funnel analytics: `configureAuth` / `clearAuth` write the player auth into
  native shared storage so the funnel keeps reporting while the JS runtime is dead, plus
  `trackOfferClicked` / `trackOfferConverted`.
- Device-token helpers that adapt to any generated microservice client exposing the device-token
  endpoints.
- A built-in web build resolved by Metro, so the same import works on iOS, Android, and web — routed
  over a pluggable transport that defaults to the Unity WebView bridge.
- Packaging: an Expo config plugin, autolinking config, podspec, prebuilt Android AAR, and the iOS
  module. `docs/custom-notifications.md` covers custom notification styles and Live Activities end to
  end.

### Deprecated

- `BeamableNotifications`, the default export, and the flat helpers (`requestBeamablePermission`,
  `addBeamableListener`, and friends) are back-compat aliases for `BeamNotifications`.
