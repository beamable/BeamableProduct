# Beamable Native Libraries

Home for **all native libraries used by Beamable** — push notifications and deep links for Android and
iOS, plus the per-engine plugins that expose them to Unity, Unreal, and React Native.

> **Start here:** the per-engine push notification guides in the Beamable documentation —
> *Push notifications in Unity*, *…in a Unity WebView*, *…for Unreal*, and *…for React Native*. Each one
> covers install, Android and iOS provisioning, custom notification styles, Live Activities, the
> receive-time hook, and an end-to-end walkthrough for that engine.

```
beam-native-mobile/
  NativeSources/
    Android/BeamableNotifications/ # Kotlin — one Gradle module → one .aar
    iOS/BeamableNotifications/     # Swift — core/ (shared) + extension/ (NSE)
  Unity/                           # engine plugin — Beamable.Notifications
  Unity.Web/                       # engine plugin — WebView JSON relay
  Unreal/                          # engine plugin — BeamPlatformNotifications
  ReactNative/                     # engine plugin — @beamable/notifications-react-native
  Samples/                         # ReactNative, WebSDKUsageSample
```

## The two native cores

| Core | Location | Ships |
|---|---|---|
| **Android** | `NativeSources/Android/BeamableNotifications` | One module (`notifications`) → one `.aar`. **Push** (`com.beamable.push`): local notifications (AlarmManager), optional remote push (FCM), channels/templates, permission, launch-intent reading, and a receive-time handler that runs even when the app is killed. **Deep links** (`com.beamable.deeplink`): native `VIEW`-intent capture, cold and warm start. The thin engine adapters (`unity/`, `unreal/`, `react/`) ship inside the same `.aar`. |
| **iOS** | `NativeSources/iOS/BeamableNotifications` | Swift package → xcframework. Local + remote (APNs) notifications, permission, templates, action categories, rich media and closed-app analytics via a Notification Service Extension, and a plugin system. Exposes a C ABI (`bmn_*`). |

The two platforms are first-class equals — every capability exists on both, with the same
public/bridge-facing names. Where a platform has no native equivalent the method is kept as a
best-effort no-op, so it is never absent and never throws. The per-engine guides carry the full parity
tables.

## Engine plugins

Each plugin has its own README with install and setup steps:

- [`Unity/README.md`](Unity/README.md) — `Beamable.Notifications`, one
  cross-platform C# API over both natives.
- [`Unity.Web/README.md`](Unity.Web/README.md) — for a web/React UI running
  inside a Unity WebView; a thin JSON relay with no dependency on the Unity SDK.
- [`Unreal/README.md`](Unreal/README.md) — the `BeamPlatformNotifications`
  plugin (iOS C ABI + Android JNI).
- [`ReactNative/README.md`](ReactNative/README.md) —
  `@beamable/notifications-react-native`.

> **The plugins ship prebuilt binaries** (the `.aar`, the `.xcframework`), staged by `../dev-native.sh`.
> Editing native source does **not** change an engine package until the binaries are rebuilt and
> restaged — see [`AGENTS.md`](AGENTS.md).

## Docs

| Doc | Covers |
|---|---|
| Per-engine push notification guides (Beamable documentation) | Install, Android/iOS provisioning, custom styles, Live Activities, the receive-time hook, and a walkthrough — one page per engine. |
| [`AGENTS.md`](AGENTS.md) | Orientation for AI agents working in this folder. |
