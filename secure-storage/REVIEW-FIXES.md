# SecureStorage review follow-up

This revision applies the supplied review to the current BeamableProduct checkout. No backend analytics traffic is sent by the regression checks.

## Changes

| Finding | Applied change | Regression coverage |
| --- | --- | --- |
| Hidden tracker methods still used plaintext; two live managers | Remove the subclass integration. Inject `IAnalyticsBatchStorage` into the existing tracker/manager; interface and concrete registrations alias one tracker. | Real SDK registration, dispatch through both types, settings updates, one live coroutine, no PlayerPrefs writes on secure path. |
| DI tried to resolve constructor integers | Explicit tracker factory supplies batching values; add-on registration replaces only the storage factory. | Actual dependency builder/provider resolve both tracker access paths. |
| Failed backups cleared retry state | Respect both boolean results; preserve dirty state on false/exception; retain one storage instance. Log operation/error type without payloads. | Set failure, commit failure, write exception, retry on next heartbeat, factory initialization retry, disposal ordering. |
| Startup overwrote saved events before restoration | Restore before starting the coroutine; remove the tracker's startup flush; restore once despite heartbeat restarts. Pre-start explicit flush restores before expiring. | PlayerPrefs and injected storage, with and without Flush-before-Start; actual first-tick scheduling modeled. Real Unity PlayMode tests added separately. |
| Legacy plaintext remained | Migrate only after secure initialization, commit before deletion, retry failed cleanup, prefer existing secure entry. | Successful migration, failed Set/Flush, stale legacy entry, failed PlayerPrefs cleanup Save, unavailable key. |
| Apple identity depended on container path | Add application-qualified logical store identity; require it for iOS; safely promote an accessible legacy protected key to the logical account without replacing another key. | Native macOS relocation and legacy-key promotion checks; iOS installed-app upgrade remains unverified. |
| Integration compiled on unsupported targets | Guard integration source consistently with enabled DLL platforms. | Compile guarded sources without SDK/secure-library references for Android, Linux, WebGL, and unsupported Editor configurations. |
| Cache recovery/threat boundary unclear | Document explicit quarantine/discard policy and same-user DPAPI access; no plaintext fallback or automatic reset. | Corrupted JSON blocks overwrite until explicit recovery; core tamper and missing-key tests. |

## Validation boundary

The .NET integration harness compiles the current SDK batch/tracker/registration/DI sources and current add-on. Unity scheduling, PlayerPrefs, logging, pool, and JSON are controlled adapters; storage faults are injected. This verifies the reported dispatch, retry, and startup ordering defects without claiming an end-to-end Unity or BeamContext session.

Fresh commands for this revision:

- The solution and both managed DLL variants build with zero warnings/errors.
- Storage suite: **20/20 pass** on the macOS .NET host, including real Keychain round trip, directory relocation, and protected-key migration.
- Integration suite: **20/20 pass** with the actual SDK sources and controlled adapters described above.
- The Apple C bridge compiles for universal macOS (arm64/x86_64) and arm64 iOS.
- Unsupported-target guard builds pass without SDK/secure DLL references for Android, Linux, WebGL, and an unsupported Editor configuration.
- A fresh archive extraction builds cleanly and passes the 17 portable storage checks plus all 20 integration checks. These repeat existing cases; they are not additional distinct tests. The extracted build needed execution outside this sandbox because .NET restore stalled inside it.

```sh
bash secure-storage/scripts/build.sh
dotnet build secure-storage/Beamable.SecureStorage.sln -c Release
dotnet run --project secure-storage/tests/Beamable.SecureStorage.Tests -c Release --no-build -- --native
dotnet run --project secure-storage/tests/Beamable.SecureStorage.IntegrationTests -c Release --no-build
dotnet build secure-storage/tests/PlatformGuardCheck -c Release -p:GuardPlatform=UNITY_ANDROID
dotnet build secure-storage/tests/PlatformGuardCheck -c Release -p:GuardPlatform=UNITY_STANDALONE_LINUX
dotnet build secure-storage/tests/PlatformGuardCheck -c Release -p:GuardPlatform=UNITY_WEBGL
dotnet build secure-storage/tests/PlatformGuardCheck -c Release -p:GuardPlatform=UNITY_EDITOR
```

The native macOS suite creates and deletes only its own temporary Keychain items. The SDK PlayMode fixture is `client/Packages/com.beamable/Tests/Runtime/Beamable/AnalyticsBatchPersistenceTests.cs`. It uses real Unity coroutines/serialization but has not been run here.

The prior Windows .NET/Unity Editor/Mono player evidence in the supplied review applies to the previous library. This revision still needs Windows runtime revalidation, Unity Mono/IL2CPP player execution, signed macOS player/upgrade and locked/denied Keychain cases, an installed-app upgrade on iOS, and real-load performance/durability checks. The customer's exact SDK/Unity versions remain unknown. The supplied SDK source patch must be adapted and validated for that version rather than copied blindly across releases.
