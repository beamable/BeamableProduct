# Beamable Secure Storage

Standalone C# library for Unity 2021.3+, targeting .NET Standard 2.1 with no NuGet or Unity dependencies. Windows uses CurrentUser DPAPI to protect a random encryption key. macOS and iOS store that key in Apple Keychain through the included small C bridge. No default password, embedded secret, or plaintext fallback is used.

## API

```csharp
using Beamable.Security;

ISecureStorage storage = new SecureStorage(
    Application.persistentDataPath,
    Application.identifier + ".analytics.v1");

bool accepted = storage.SetString("analytics-batch", batchJson);
bool persisted = accepted && storage.Flush();

if (storage.TryGetString("analytics-batch", out string restoredJson))
{
    // Deserialize restoredJson.
}

bool deletionAccepted = storage.DeleteKey("analytics-batch");
bool deletionPersisted = deletionAccepted && storage.Flush();

storage.Dispose(); // Releases the directory; does not flush automatically.
```

- `SetString` and `DeleteKey` return whether an in-memory update was accepted. **Only `Flush()` confirms persistence.** Reads immediately reflect pending updates/deletions.
- A missing key returns `false` from `TryGetString` and assigns `null`. An empty string is a valid stored value. Deleting an absent key succeeds.
- `Flush()` returns `true` when nothing is pending or all changes were committed. Failed changes remain pending for retry. The concrete class exposes `LastError` and `HasPendingChanges` for diagnostics.
- Invalid arguments and calls after disposal throw. Keys are case-sensitive, nonempty, at most 1,024 UTF-8 bytes. Null values and invalid Unicode are rejected. The total serialized snapshot is limited to 16 MiB; an oversized update returns `false` without replacing the previous value.
- Construction loads and authenticates the existing snapshot. Initialization failures, missing native libraries, unavailable keys, and corrupted storage throw; they never silently reset saved data.
- Calls on one instance are synchronized. A held file lock rejects additional owners of the same directory on runtimes that enforce file sharing. Share one instance per directory, and dispose it when finished. Independent processes/instances do not merge updates.

## Build and distribute

Prerequisites: .NET 8 SDK or newer. Apple native builds also need Xcode command line tools.

From the repository root on macOS:

```sh
bash secure-storage/scripts/build.sh
```

On Windows:

```powershell
pwsh -File secure-storage/scripts/build.ps1
```

The scripts produce `artifacts/unity/` and `artifacts/sdk-patch/` inside this project. For the storage library alone, copy `unity/Plugins` and `unity/Editor` into the Unity project's `Assets` folder, preserving the included `.meta` files. For analytics integration, also apply the SDK changes described below and copy `unity/Analytics`. The macOS script builds a universal arm64/x86_64 native library with a macOS 11 minimum. The Windows script does not build that library; obtain it from a Mac build if shipping macOS too. Rebuild within this repository checkout: the SDK patch is copied from the adjacent `client/Packages/com.beamable` source.

The output contains:

| Asset | Enabled platforms |
| --- | --- |
| `Plugins/Desktop/Beamable.SecureStorage.dll` | Unity Editor, Windows, macOS |
| `Plugins/macOS/libBeamableSecureStorage.dylib` | macOS Editor and player |
| `Plugins/iOS/Beamable.SecureStorage.dll` | iOS player only |
| `Plugins/iOS/BeamableSecureStorage.c` | iOS native build |
| `Editor/SecureStorageIOSBuild.cs` | Links Security and CoreFoundation into UnityFramework for iOS |
| `Analytics/SecureAnalyticsBatchStorage.cs` and `SecureAnalyticsRegistration.cs` | Windows/macOS Editor, Windows/macOS/iOS players; opt-in SDK integration |

The two managed DLLs have the same assembly identity and public API. Their import settings must remain mutually exclusive. iOS uses `DllImport("__Internal")` for statically linked native calls; desktop uses a dynamic library. This distinction is compiled into the DLL using `-p:SecureStorageIOS=true`, rather than Unity preprocessor symbols that cannot change precompiled code. Construction selects the appropriate backend automatically in each build. Update both the managed DLL and Apple native bridge together.

If your Unity assembly definition disables automatic references, explicitly reference `Beamable.SecureStorage.dll`. The iOS postprocessor requires Unity's iOS Build Support/Xcode assembly. Apply your application's normal signing/notarization process to the macOS native plug-in.

Android, Linux players, WebGL, and consoles are unsupported and have no insecure fallback. Verify actual Unity Mono/IL2CPP player builds before releasing; a .NET host test is not a Unity player test.

## Files and security

The constructor owns only `<directoryPath>/SecureStorage/`:

- `values.bss`: encrypted snapshot containing both keys and values.
- `key.dpapi`: Windows-only DPAPI-protected key material; no raw encryption key is written to disk.
- `owner.lock`: empty ownership lock, intentionally retained after disposal.

Each snapshot uses AES-256-CBC with a fresh random IV and encrypt-then-HMAC-SHA256, with independent 256-bit encryption and authentication keys. Authentication is checked before decryption. These primitives are available on older Unity profiles without depending on `AesGcm` support. Transient byte buffers are cleared where practical; immutable strings and runtime/native copies cannot be guaranteed erased from memory.

Flush writes only ciphertext to a unique temporary file in the same directory, flushes it, then atomically replaces the snapshot. If replacement fails or is unsupported, the old snapshot is retained and `Flush` fails. Removing the last entry deletes the snapshot on the next flush. The protected master key remains for future writes. A flush covers the entire dictionary but is not a transaction with analytics delivery. File/directory durability after sudden power loss ultimately depends on the OS/filesystem; parent directory metadata is not explicitly fsynced.

Apple Keychain items use service `com.beamable.secure-storage.v1`. The overload taking `storeId` derives the account from that stable logical identity, independent of the directory. Use an application-qualified identity (for example `com.example.game.analytics.v1`) and retain it across app upgrades; give different logical stores distinct identities. This overload is required on iOS, where an update may move the app container. The original directory-only constructor remains available for desktop compatibility and keeps its original path-derived account on macOS.

When adopting a logical identity for an existing path-derived Apple store, the bridge can copy the existing protected key into the new Keychain account while the original directory/account is still reachable. It never regenerates a key over existing ciphertext or replaces another account's key. The old alias is retained for rollback. An already-relocated legacy store whose old account cannot be located needs explicit migration/recovery; the original path cannot be inferred from encrypted data. A newly created stable-identity store survives moving its directory on the same device. This is tested on macOS and still needs an actual installed-app upgrade test on iOS.

iOS keys use `AfterFirstUnlockThisDeviceOnly`, do not sync through iCloud, and are unavailable before the first unlock. Restoring encrypted files onto another device cannot restore this device-bound key. macOS Keychain may prompt when a different signed application first accesses a key created by the Editor.

This removes casual plaintext inspection; it does not hide match information from the owner of the Windows session. DPAPI is scoped to that Windows user: another process under the same user can open the store through this public API without game injection or an embedded secret. Keep information that must be hidden from the player on the server. Immediate sending avoids the local batch but does not make client-produced data secret from the client. Neither native backend guarantees secrecy against someone who can inspect the running process. Encryption does not prevent deletion or replay of an older valid snapshot.

## Integrating analytics

This revision changes the actual SDK batch/tracker path. It replaces the former `SecureAnalyticsTracker` subclass and separate `SecurePersistentBatchManager`; remove both old adapter files from consuming projects. The build scripts also remove those obsolete files from generated output.

1. Apply `artifacts/sdk-patch/Packages/com.beamable/` to the corresponding SDK sources. It adds `IAnalyticsBatchStorage`, the default `PlayerPrefsBatchStorage`, and the tracker's shared dependency registration; it updates `Beam.cs`, `AnalyticsTracker`, `BatchManager`, and `PersistentBatchManager`.
2. Install the plug-ins and the two guarded files in `artifacts/unity/Analytics/`. The registration replaces only `IAnalyticsBatchStorage` with `SecureAnalyticsBatchStorage`. Both `IAnalyticsTracker` and concrete `AnalyticsTracker` resolve the same normal tracker, with one queue/coroutine. Factories supply paths and batching integers explicitly rather than asking DI to resolve primitive services.
3. Validate the change against the consuming game's SDK/Unity versions. The supplied SDK files are from this checkout, not a universal drop-in patch for every historical SDK. The customer's exact versions were not supplied in the review.

Without the opt-in analytics registration the SDK retains its default PlayerPrefs backend. Registration is compiled only for supported Apple/Windows targets, so importing it does not reference an unavailable DLL in Android/Linux/WebGL/console builds. It does not secure those targets: register a suitable policy/backend separately if those platforms must also avoid plaintext persistence.

The shared adapter lazily opens a store once, retries initialization failures, and retains pending writes across backups. `SetString=false`, `Flush=false`, and exceptions leave the manager's dirty flag set and emit non-sensitive diagnostics. The next heartbeat retries an unchanged queue. `AnalyticsTracker` stops and saves its queue during dependency disposal before the adapter flushes/releases the store. One storage owner is required per directory; use separate stores/identities if creating independent contexts rather than sharing a dependency scope.

`BatchManager.Start` invokes `OnStart` before `StartCoroutine`, whose first heartbeat can execute immediately. `PersistentBatchManager` restores only once per lifetime, including settings/heartbeat restarts. The tracker no longer expires an empty queue before startup. An explicit batch `Flush()` called before `Start()` also restores before expiring, then persists the rotated queue including events added by expiration callbacks. `SavePendingBatch()` persists without sending. Batch flush is still `void`; storage flush is `bool`. These changes add local backup retries, not delivery acknowledgement or server retry guarantees.

Legacy PlayerPrefs migration is implemented in the adapter. It reads the legacy value only after secure storage opens successfully and the secure entry is absent. It commits the encrypted entry before removing the plaintext key and saving PlayerPrefs. If the secure entry already exists, it wins, and stale plaintext is removed after a confirmed secure commit. Failed commits and failed legacy cleanup remain retryable. Authentication/key-access failures never trigger plaintext fallback.

For unreadable encrypted files or malformed batch JSON, persistence remains blocked and retries; the code does not silently replace data. An application can adopt an explicit cache-discard/quarantine policy, but must preserve evidence or deliberately discard the cache before retrying initialization. This library does not automatically discard an unreadable store that might contain other important keys.

## Checks

The tests are a dependency-free .NET 8 executable (install the .NET 8 SDK/runtime to run them), including disk persistence, buffered deletion, failed-write retries, failed atomic replacement, authentication/tampering, missing-key recovery behavior, limits, and lock ownership:

```sh
dotnet run --project secure-storage/tests/Beamable.SecureStorage.Tests -c Release
dotnet run --project secure-storage/tests/Beamable.SecureStorage.IntegrationTests -c Release
```

After running the build script, add `-- --native` for the platform integration check. It creates and removes its own temporary Keychain item on macOS, or exercises real DPAPI on Windows:

```sh
dotnet run --project secure-storage/tests/Beamable.SecureStorage.Tests -c Release -- --native
```

Keychain integration checks require access to the logged-in user's Keychain and cannot run in a sandbox that blocks it. iOS device/IL2CPP validation and Windows DPAPI execution require those target environments.

The integration executable compiles the actual SDK tracker, batch managers, registration helper, dependency builder/provider, and secure adapter. It uses controlled Unity scheduling (including the immediate first tick), PlayerPrefs, logging, pool, and JSON adapters, and a fault-injectable storage backend. It sends no backend traffic. This is source-level regression coverage, not a full BeamContext or real Unity player test.

The previous review reported the older library passing 18 checks in real Windows .NET, Unity Editor, and a Windows Mono player. Those results do not validate this revised integration or the customer's exact game. Fresh local validation for this revision is recorded in `REVIEW-FIXES.md`; Windows runtime, PowerShell packaging, signed macOS player upgrades, iOS device upgrades, IL2CPP, and real analytics performance remain target-environment checks.

After rebuilding, `python3 secure-storage/scripts/package.py` refreshes `secure-storage/SecureStorage.zip`. Python 3 is needed only for this optional packaging step. The archive includes current Unity assets, the required SDK source patch, library source/tests, and the SDK snapshots used by the .NET harness. It excludes obsolete adapters, build intermediates, and backups; the prior archive is retained under `artifacts/review-backup/`.
