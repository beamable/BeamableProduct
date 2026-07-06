# iOS native sources — `@beamable/notifications-react-native`

This folder holds the React Native iOS **bridge** sources only:

- `BeamableNotificationsModule.swift` — the `RCTEventEmitter` module (`@objc(BeamableNotificationsModule)`).
- `BeamableNotificationsModule.m` — the `RCT_EXTERN_MODULE` / `RCT_EXTERN_METHOD` ABI shim.
- `BMNLaunchInstaller.m` — `+load` shim that claims the `UNUserNotificationCenter` delegate at launch.

## The Swift core is a prebuilt binary (Decision Q2)

The bridge talks to the Beamable Swift **core** via the prebuilt
`BeamableNotifications.xcframework` (the same binary Unity/Unreal link), which the podspec
(`../BeamableNotificationsRN.podspec`) **vendors** with `vendored_frameworks`. We no longer
compile a `ios/core/` mirror of the core Swift sources (that mirror — and its
`scripts/sync-rn-core.sh` — are gone).

`BeamableNotifications.xcframework` is **not committed**. It is dropped in here by the
native build tooling:

```bash
# from the repo root (D:/Repositories/BeamableProduct)
./dev-native.sh
```

`dev-native.sh` builds the xcframework and copies it to
`EnginePlugins/ReactNative/ios/BeamableNotifications.xcframework` (macOS only, mirroring the
Unity copy step). It is listed in the package `.gitignore`. If you open this package without
running `dev-native.sh`, `pod install` will fail to find the framework — build the natives
first.

## Config-plugin NSE sources (`../plugin/ios/`)

The Expo config plugin (`../app.plugin.js` → `../plugin/withBeamableNotifications.js`) copies
the Notification Service Extension + extension-safe core Swift into the app at `expo prebuild`
time. Those sources live canonically in `nativeLibraries/iOS/BeamableNotifications/`
(`extension/` + `core/Sources/BeamableNotifications/`).

- **In the monorepo** the plugin reads them directly from that sibling dir (always fresh) — no
  action needed.
- **For a published package** they must be self-contained: `dev-native.sh` should also copy
  `extension/` and `core/` into `EnginePlugins/ReactNative/plugin/ios/` (same subpath layout).
  The plugin prefers `plugin/ios/` when present and falls back to the sibling dir otherwise.
  `plugin/ios/` is gitignored (generated), like the xcframework.
