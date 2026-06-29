# AGENTS.md — Beamable Native Libraries

Guidance for AI agents working in `nativeLibraries/` (the cross-platform push + deep-link
native libraries for Android, iOS, and their Unity / Unreal / React Native engine adapters).

## Repository shape

- `Android/BeamableNotifications/` — one Gradle module → one `.aar` (push core + deep links +
  all engine adapters). Build: `./gradlew :notifications:assembleRelease`.
- `iOS/BeamableNotifications/` — Swift package; `core/` is the shared module, `extension/` the NSE.
- `EnginePlugins/` — Unity / Unreal / React Native packages. **They ship prebuilt binaries**
  (the `.aar` under `ReactNative/android/libs/`, the `.xcframework` under `ReactNative/ios/`),
  staged by `../dev-native.sh`. Editing native source does NOT change the engine packages until
  the binaries are rebuilt and restaged.
- `Samples/` — runnable samples (React Native app + the C# microservice it talks to).

## Constraint: keep the file count low — consolidate aggressively

**Prefer folding closely-related declarations into one file over creating many small files.**
Kotlin and Swift both allow multiple top-level types per file — use that.

- A small helper, callback interface, or companion type belongs **in the file of the thing it
  serves**, not in its own file. Example: `DeepLinkManager.kt` folds in `IntentDeepLinkExtractor`,
  `ActivityIntentObserver`, and `DeepLinkNormalizer` rather than splitting them out. iOS does the
  same — deep-link normalization lives inside `Models.swift`, not a separate file.
- When you add a new type, **first look for an existing file it naturally belongs in**. Only create
  a new file when the type is large or genuinely standalone.
- When a file shrinks to a thin wrapper (a one-line `ReactPackage`, a tiny listener interface),
  fold it back into its primary file.
- Mark folded sections with a banner comment, e.g.
  `// --- DeepLinkNormalizer (folded in from DeepLinkNormalizer.kt) ---`.

Apply the same rule on every platform (Kotlin, Swift, the TS bridge, the C# microservice) and to
tests. When in doubt, fewer files.

## Other conventions

- The funnel analytics contract (CoreEvent params: `offerData` as a stringified JSON array,
  `customData`/`campaignData` as stringified JSON strings — Athena takes no nested objects) is the
  cross-platform source of truth; keep Android, iOS, and the microservice emitting identical shapes.
- Slack forwarding of funnel data is **microservice-only** by policy; native libraries POST
  analytics and may mirror to the portal CampaignService's `ForwardFunnelToSlack` endpoint, but
  never call a Slack webhook directly.
- After changing native source, rebuild + restage binaries (`../dev-native.sh`, or for Android the
  `:notifications:assembleRelease` AAR copied into `EnginePlugins/*/`) before testing in a sample.
