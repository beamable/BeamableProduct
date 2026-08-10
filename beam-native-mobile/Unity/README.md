# Beamable Notifications — Unity (`com.beamable.notifications`)

One **cross-platform C# API** over both native cores: local + remote push (APNs/FCM), deep links,
templates, action buttons, rich media, and closed-app funnel analytics. Game code calls
`Beamable.Notifications.BeamableNotifications` and the package picks the right native at **compile
time** (`#if UNITY_IOS` / `#elif UNITY_ANDROID`) — you never touch the native facades.

In the Editor and on non-mobile targets every native call is a **safe no-op**, so a project that uses
this package still runs and builds everywhere.

## Layout

| Path | Holds |
|---|---|
| `Runtime/` | `BeamableNotifications` (the public API), `Payloads.cs` (DTOs), `Native.cs` (the P/Invoke + JNI seam), `AndroidNotificationsRelay`, `DeepLinkManager`, `Dispatcher` (main-thread marshalling) |
| `Editor/` | `NotificationsPostProcess.cs` (iOS post-build), `BeamableAndroidBuildProcessor.cs` + `BeamableAndroidSetup.cs` (Android manifest/Gradle), `BeamableNotificationsWindow.cs` (setup + validation window) |
| `Plugins/Android/` | `beamable-notifications-release.aar` — the prebuilt Android core |
| `Plugins/iOS/` | `BeamableNotifications.xcframework` — the prebuilt iOS core |
| `Plugins/iOS~/Extension/` | Notification Service Extension sources (the `~` stops Unity importing `.swift` as scripts) |
| `Samples~/NativeDemo/` | IMGUI test harness + a sample native receive-time handler |

> **The binaries in `Plugins/` are generated outputs**, staged by `../../dev-native.sh`. Editing native
> Kotlin/Swift changes nothing here until you rebuild and restage. Unity
> auto-links any `.xcframework` under `Plugins/iOS` into the generated Xcode project, so there is no
> manual linking step.

## Install

Add the package to your project — either a local path in `Packages/manifest.json` pointing at this
folder, or copy it under `Packages/com.beamable.notifications`. It depends on `com.beamable` (see
`package.json`) and targets Unity 2021.3+.

Then open **Tools ▸ Beamable ▸ Notifications** and run the setup + validation window. For Android it
scaffolds/patches `Assets/Plugins/Android/AndroidManifest.xml`, sets the min SDK, and injects the Gradle
dependencies; for iOS, set your **App Group** id (`AppGroupId` in `Editor/NotificationsPostProcess.cs`).

The iOS post-build step then runs automatically on build: it adds Push Notifications + Background Modes,
the App Group entitlement, and a **Notification Service Extension** target from `Plugins/iOS~/Extension/`,
embeds the Swift standard libraries, and links the xcframework.

Remote push additionally needs `google-services.json` (Android) and an APNs key configured for the realm
— see the *Push notifications in Unity* guide in the Beamable documentation.

## Usage

```csharp
using Beamable.Notifications;

void Start()
{
    BeamableNotifications.OnTokenReceived      += token => Debug.Log($"push token {token}");
    BeamableNotifications.OnNotificationTapped += n => Router.Open(n.DeepLink);
    BeamableNotifications.OnPermissionResult   += r => Debug.Log($"granted={r.Granted}");

    BeamableNotifications.Initialize();
    BeamableNotifications.RequestPermission();

    // Cold start: did a notification launch us?
    var launch = BeamableNotifications.GetLaunchNotification();
    if (launch != null) Router.Open(launch.DeepLink);
}

void ScheduleReminder()
{
    BeamableNotifications.ScheduleLocal(new LocalRequest {
        Id = "daily",
        Title = "Come back!",
        Body = "Your energy is full",
        Trigger = TriggerSpec.After(3600),
        UserInfo = new() { ["deepLink"] = "game://home" }
    });
}

// Remote + closed-app funnel analytics
void EnableRemote()
{
    // Persist the player's auth so the native funnel can POST even when the app is killed
    // (the funnel authenticates with this token even when the app is killed).
    BeamableNotifications.ConfigureAuth(accessToken, refreshToken, accessTokenExpiresAtMs,
        cid, pid, "https://api.beamable.com");
    // Or pull the token straight from BeamContext.Default:
    // BeamableNotifications.ConfigureAuthFromContext("https://api.beamable.com");
    BeamableNotifications.RegisterForRemote();
}
```

Events are raised on the **Unity main thread** (`Dispatcher`). The full method / event / DTO tables,
including which calls are iOS-only no-ops on Android, are in the *Push notifications in Unity* guide in
the Beamable documentation.

## Docs

| Topic | Where |
|---|---|
| Install, provisioning, custom styles, receive-time hook, walkthrough | *Push notifications in Unity* (Beamable documentation) |
| A web/React UI driving this binary from inside a WebView | *Push notifications in a Unity WebView* (Beamable documentation), and [`../Unity.Web/README.md`](../Unity.Web/README.md) |
| Sample scene | `Samples~/NativeDemo/README.md` |
