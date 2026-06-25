# Beamable Native Libraries

Home for **all native libraries used by Beamable**, organized by platform. Each platform lives
in its own folder.

> **Feature reference:** [`docs/notifications-feature.md`](docs/notifications-feature.md) is the
> authoritative design doc for BeamableNotifications (multi-handler model, intent-data schema,
> analytics funnel). See also [`LIBRARY_GUIDE.md`](LIBRARY_GUIDE.md) for the per-platform walkthrough.

```
nativeLibraries/
  Android/   # Kotlin libraries built as .aar
  iOS/       # (planned) Swift / Objective-C libraries
```

## Android/

Engine-agnostic Kotlin libraries, each a standalone Gradle project that builds a `.aar`.

- **PushNotifications/** (`com.beamable.push`) — local + optional remote (FCM) push,
  configurable notification templates, channels, permission request, launch-intent reading, and
  a receive-time handler that fires natively even when the app is killed.
- **Deeplink/** (`com.beamable.deeplink`) — native deeplink (VIEW intent) capture for cold and
  warm start, without replacing the host activity.

Each `.aar` bundles thin routing adapters for **Unity** (`unity/`), **Unreal** (`unreal/`), and
**React Native** (`react/`) over the shared core. **Unity** is wired up end-to-end today;
Unreal still needs its C++/UPL plugin glue and React Native its JS package (the Kotlin side is
already in the `.aar`). See `Android/README.md` for build, API, per-engine usage, and the
adapter/ProGuard details.

## iOS/

Planned — not implemented yet. See `iOS/README.md`.
