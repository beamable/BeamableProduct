# Beamable Google Sign-In plugin (Android)

The Android half of Beamable's Google Sign-In support. It builds a small `.aar` that is committed
into the Unity package at
[`client/Packages/com.beamable/Plugins/Android/googlesignin-release.aar`](../../client/Packages/com.beamable/Plugins/Android).

Two classes, no resources:

| Class | Role |
|---|---|
| `GoogleSignInActivity` | The only class name C# knows. A headless proxy Activity for the interactive account chooser, plus the static facade for everything else. |
| `GoogleSignInBridge` | Implementation of the paths that need no UI: `silentLogin`, `signOut`, `revokeAccess`. |

Results travel back to Unity as a flat string via `UnityPlayer.UnitySendMessage`. The C# counterpart
is `Beamable.Platform.SDK.Auth.GoogleSignIn` / `GoogleSignInService`.

## Prerequisites

- **JDK 11 or 17.** Both work; nothing outside 11–17 does — AGP 7.4.2 will not run on Java 8, and
  Gradle 7.6.4 will not run on Java 20+. The Unity-bundled JDK is a fine choice and needs no separate
  install: `/Applications/Unity/Hub/Editor/<version>/PlaybackEngines/AndroidPlayer/OpenJDK`
  `install-to-unity.cs` checks the major version of every candidate, so a `JAVA_HOME` outside the
  range is reported and skipped in favour of the Unity-bundled JDK rather than failing later inside
  Gradle.
- **Android SDK with platform 34 and build-tools 34.0.0.** The SDK bundled with Unity 2022.3 has
  exactly these, so pointing `ANDROID_HOME` at it requires no `sdkmanager` downloads:
  `/Applications/Unity/Hub/Editor/<version>/PlaybackEngines/AndroidPlayer/SDK`
  (Unity 6's bundled SDK ships build-tools 36.0.0 instead; Gradle will fetch 34.0.0 on first build,
  or point `ANDROID_HOME` at a 2022.3 install.)

```bash
export JAVA_HOME=/Applications/Unity/Hub/Editor/2022.3.67f2/PlaybackEngines/AndroidPlayer/OpenJDK
export ANDROID_HOME=/Applications/Unity/Hub/Editor/2022.3.67f2/PlaybackEngines/AndroidPlayer/SDK
```

## Toolchain, and why it is pinned where it is

Gradle 7.6.4, AGP 7.4.2, `compileSdk 34`, `minSdk 16`, Java 8 bytecode. None of these are arbitrary:

- **AGP 7.4.2, not 8.x** — 7.4.2 is the newest AGP that runs on *both* JDKs Unity ships (11 for
  2021.3–2022.3, 17 for Unity 6). AGP 8 requires JDK 17 and would exclude anyone whose only JDK
  comes from Unity 2022.3.
- **`compileOptions` pinned to Java 8** — the classes in this `.aar` are dexed by the *consuming*
  project's D8. Java 8 gives class file major version 52, readable by every D8 back to AGP 3.x.
  AGP 8 defaults to Java 17 (major 61), which fails in older consumer projects with
  `Unsupported class file major version 61` — invisible here, fatal there, and unfixable on the
  consumer's side. CI gates on major version 52.
- **`minSdk` stays 16** — AGP writes it into the `.aar` manifest and the manifest merger enforces
  `library.minSdk <= app.minSdk`, where the app's value is the *customer's* Unity Player Setting.
  Raising it breaks every customer below the new floor and gains nothing.
- **`compileSdk 34`** — has no consumer-visible effect at all; it is set to 34 purely because that is
  what the Unity 2022.3+ SDK bundles.

Also deliberate: **no lambdas or method references in the source** (at `-target 8` they compile to
`invokedynamic`, which depends on the consumer's desugaring), and **`catch (Throwable)`** rather than
`catch (Exception)` (a consuming project missing `play-services-auth` raises `NoClassDefFoundError`,
which is an `Error`, and uncaught it kills the player instead of reporting to C#).

## Build

```bash
./gradlew :googlesignin:assembleRelease
```

Output: `googlesignin/build/outputs/aar/googlesignin-release.aar`

## Install into the Unity package

The recommended way, which also finds the toolchain for you and verifies the artifact before it
touches the package:

```bash
dotnet run install-to-unity.cs
```

[`install-to-unity.cs`](install-to-unity.cs) is a .NET file-based app — a single `.cs` file, no
project file, no NuGet packages, requires the .NET 10 SDK. It locates a Unity-bundled JDK and Android
SDK (honouring `JAVA_HOME` / `ANDROID_HOME` when set), runs the Gradle build, checks the `.aar`, and
only then copies it in. On macOS/Linux `./install-to-unity.cs` works too, via its shebang.

```bash
dotnet run install-to-unity.cs --verify-only
```

```bash
dotnet run install-to-unity.cs -- --help
```

The `--` in the second command is needed because `dotnet run` claims `--help` for itself; options it
does not recognise, such as `--verify-only`, reach the script either way.

The checks it runs are the ones whose failures are invisible locally and fatal in a customer project:
`minSdkVersion` is still 16, every class is Java 8 bytecode, no lambdas, no Unity classes packaged,
the ProGuard consumer rules shipped, the manifest declares the Activity, and `GoogleSignInActivity`
still exposes the five static methods C# calls by name with no Google Play services type in their
signatures.

The equivalent Gradle task, without the verification, is:

```bash
./gradlew :googlesignin:installToUnity
```

Either way, **commit the `.aar` along with the source change** — nothing rebuilds it automatically,
so a Java change that is not accompanied by a rebuilt binary does nothing in Unity. The build is
reproducible as long as the JDK is the same: the script pairs the JDK with the Android SDK from one
editor install for that reason, since javac 11 and javac 17 both emit valid Java 8 bytecode but not
byte-identical class files.

The filename must stay exactly `googlesignin-release.aar`. Unity binds a `.meta` file to an asset by
path, and `googlesignin-release.aar.meta` carries both the asset GUID
(`71a1c54ea311a44fd913f7e43899b8b2`) and the Android-only import settings. Rename the file and Unity
generates a fresh `.meta` with a new random GUID and default settings — and since this ships inside
the published `com.beamable` UPM package, that GUID is part of its public surface. The plugin version
lives in `BuildConfig.PLUGIN_VERSION`, never in the filename.

## The consuming project must supply play-services-auth

The GMS dependency is declared `implementation`, so it is neither embedded in the `.aar` nor carried
as dependency metadata. The **game** provides it, in `Assets/Plugins/Android/mainTemplate.gradle`:

```gradle
implementation 'com.google.android.gms:play-services-auth:18.1.0'
```

Consequence when editing this plugin: any GMS API used here must exist in the oldest version a
customer might resolve. Without this line, calls fail with `NoClassDefFoundError`, which the plugin
reports to C# as `EXCEPTION - NoClassDefFoundError: ...`.

## Google Cloud setup

Two OAuth clients are needed, which is the most common source of confusion:

1. A **Web application** client. Its client ID is what gets passed as `clientId` here and set as
   `GoogleClientID` in Beamable's `AccountManagementConfiguration`. `requestIdToken` needs a *web*
   client ID even on Android, because the ID token's audience must be the backend that verifies it.
2. An **Android** client, registered against the app's package name and signing certificate SHA-1.
   Nothing references its ID directly, but sign-in fails without it.

A missing or mismatched SHA-1 surfaces as status code 10, reported as
`EXCEPTION - DEVELOPER_ERROR(10)` — from both `login` and `silentLogin`. Get the debug SHA-1 with:

```bash
keytool -list -v -keystore ~/.android/debug.keystore -alias androiddebugkey -storepass android -keypass android
```

You do not have to: when GMS rejects the configuration, the plugin logs everything needed to fix it,
including the SHA-1 of the certificate the installed APK is actually signed with:

```
adb logcat -s GoogleSignInActivity
```
```
E  Sign-in failed: DEVELOPER_ERROR(10)
E  Google Sign-In configuration check - compare these with the Google Cloud console:
E    packageName = com.example.game
E    signingSha1 = A1:B2:C3:...
E    clientId    = 000000000000-xxxxxxxx.apps.googleusercontent.com
```

Register an **Android** client against that package name and SHA-1, in the same Cloud project as the
web client, and confirm in the console that the `clientId` shown is the **Web application** client —
web and Android client IDs are indistinguishable from the string alone, so the log cannot check that
for you.

The same block is logged when sign-in succeeds but Google grants no ID token (reported as `UNKNOWN`),
which is the specific signature of a client ID that is not a web client.

### Why `login` used to report a misconfiguration as `CANCELED`

Worth knowing if you are debugging an older build. GMS returns `RESULT_CANCELED` both when the player
dismisses the chooser and when it refuses the request outright, and only the `Status` inside the
result intent separates the two. Before plugin 2.0.0, `onActivityResult` branched on `resultCode`
before ever unpacking that `Status`, so a `DEVELOPER_ERROR(10)` arrived in Unity as `CANCELED` — the
chooser would flash open, close by itself with no UI, and the game would report that the player had
dismissed it. `CANCELED` now means `SIGN_IN_CANCELLED` (12501), or a chooser that closed without
producing any status at all.

## C# usage

```csharp
var plugin = new AndroidJavaClass("com.beamable.googlesignin.GoogleSignInActivity");

// interactive: always shows the account chooser
plugin.CallStatic("login", gameObject.name, "GoogleAuthResponse", clientId);

// silent: no UI at all, refreshes the cached account's ID token
plugin.CallStatic("silentLogin", gameObject.name, "GoogleAuthResponse", clientId);

// clear the cached Google account ("log out")
plugin.CallStatic("signOut", gameObject.name, "GoogleSignOutResponse", clientId);

// withdraw the OAuth grant ("delete/unlink account" only - re-prompts for consent)
plugin.CallStatic("revokeAccess", gameObject.name, "GoogleSignOutResponse", clientId);

var version = plugin.CallStatic<string>("getPluginVersion");
```

In practice, prefer the managed wrapper `Beamable.Platform.SDK.Auth.GoogleSignInService`, which
handles the receiver GameObject, timeouts, and old-`.aar` detection for you.

### Response vocabulary

| Message | Meaning |
|---|---|
| *a JWT* (`eyJ…`) | success; the Google ID token |
| `NO_CREDENTIAL - 4` | silent attempt found no usable credential, or consent is required. A normal outcome, not an error |
| `CANCELED` | GMS reported `SIGN_IN_CANCELLED` (12501), or the chooser closed without producing any status. **Not** used for a rejected configuration — see above |
| `SIGNED_OUT` / `REVOKED` | `signOut` / `revokeAccess` completed |
| `UNKNOWN` | no token and no exception — usually a client ID that is not a *web* OAuth client |
| `EXCEPTION - <detail>` | anything else; includes the GMS status code name and number where available |

These strings are a wire contract with the C# parser. Do not reword them.

## Testing on a device

```bash
adb logcat -c && adb logcat -s GoogleSignInActivity
```

Acceptance sequence:

1. Fresh install → `silentLogin` reports `NO_CREDENTIAL - 4`.
2. `login` → chooser appears, sign in → an ID token comes back.
3. Force-stop the app, relaunch → `silentLogin` returns a token, **with no chooser and no UI**.
4. `signOut` → `SIGNED_OUT`, then `silentLogin` reports `NO_CREDENTIAL - 4` again.

To confirm the token is real rather than stale, decode its payload and check that `aud` matches the
web client ID and `exp` is in the future:

```bash
echo '<token>' | cut -d. -f2 | base64 -d 2>/dev/null | python3 -m json.tool
```

Note that the Unity sample project's `Assets/Plugins/Android/mainTemplate.gradle` still pins AGP
3.4.0 and lists `jcenter()`; it needs updating before it will build on a modern Unity at all.

## Why there are no instrumentation tests

Every code path here either calls into Google Play services or into `UnityPlayer`, neither of which
exists in a JVM unit test, and both of which need a real signed app plus a real Google account to
exercise meaningfully. The parts worth testing automatically — the response-string vocabulary — are
tested on the C# side, where the parser is a pure function
(`client/Packages/com.beamable/Tests/Runtime/Beamable/Platform/Auth/GoogleSignInTests/`). CI builds
this plugin and gates on the bytecode level; correctness is verified by the device sequence above.
