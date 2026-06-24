# Unity setup

## Install

1. Build the core: `./scripts/build-xcframework.sh`.
2. Copy `build/BeamableNotifications.xcframework` into `unity/Plugins/iOS/`.
3. Copy the NSE sources into `unity/Plugins/iOS~/Extension/` (the `~` keeps Unity from
   importing `.swift` as scripts):
   ```
   cp extension/NotificationService.swift          unity/Plugins/iOS~/Extension/
   cp extension/ServicePlugins/*.swift             unity/Plugins/iOS~/Extension/
   ```
4. Add the package to your project (`Packages/manifest.json` → local path to `unity/`,
   or copy under `Packages/com.beamable.notifications`). It depends on
   `com.unity.nuget.newtonsoft-json`.
5. Set your **App Group** id in `Editor/NotificationsPostProcess.cs` (`AppGroupId`).

The editor post-build step automatically adds Push Notifications + Background Modes, the
App Group entitlement, the Notification Service Extension target, embeds the Swift
standard libraries, and links the xcframework.

## Usage

```csharp
using Beamable.Notifications;

void Start()
{
    BeamableNotifications.OnTokenReceived += token => Debug.Log($"APNs token {token}");
    BeamableNotifications.OnNotificationTapped += n => Router.Open(n.DeepLink);
    BeamableNotifications.OnPermissionResult += r => Debug.Log($"granted={r.Granted}");

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
    // Persist the player's auth so the native funnel can POST even when the app is killed.
    // See docs/notifications-feature.md §4.3.
    BeamableNotifications.ConfigureAuth(accessToken, refreshToken, accessTokenExpiresAtMs,
        cid, pid, "https://api.beamable.com");
    // Or, to pull the token straight from BeamContext.Default:
    // BeamableNotifications.ConfigureAuthFromContext("https://api.beamable.com");
    BeamableNotifications.RegisterForRemote();
}
```

Events are raised on the Unity main thread. In the Editor / non-iOS, all native calls
are safe no-ops so your project still runs.
