# Unreal setup

## Install

UE's `PublicAdditionalFrameworks` does **not** consume an `.xcframework`; it expects a
single **dynamic** `.framework` zipped as `<Name>.embeddedframework/<Name>.framework`.

1. Build the dynamic framework: `./scripts/build-xcframework-dynamic.sh`
   (produces `build/BeamableNotifications.embeddedframework.zip`, device arm64).
2. Copy it where `Build.cs` expects it:
   ```
   cp build/BeamableNotifications.embeddedframework.zip unreal/ThirdParty/
   ```
3. Copy the `unreal/` folder into your project's `Plugins/BeamableNotifications/`, enable
   the plugin, and add `BeamableNotifications` to your game module's `Build.cs` deps.
4. Copy the NSE sources into `<plugin>/Extension/` (`extension/*.swift` +
   `extension/ServicePlugins/*.swift`); add them to a Notification Service Extension
   target in Xcode (see step 3 of the manual steps below).

> The embedded framework is device-only. For an iOS Simulator build, repackage the
> simulator slice from `build/BeamableNotifications.xcframework` in the same layout.

## One-time Xcode / Project Settings steps (UPL can't do these)

1. **Project Settings → iOS → Online → "Enable Remote Notifications Support"** — writes
   the `aps-environment` entitlement.
2. Add the **App Group** capability (`group.com.beamable.notifications`) to **both** the
   app and the extension in your provisioning profiles.
3. In the generated Xcode project, add a **Notification Service Extension** target using
   the staged `BeamableNotificationServiceExtension/` sources, and set its `BMNAppGroup`
   Info.plist key + App Group entitlement.

The UPL automatically adds the `remote-notification` background mode and the
`BMNAppGroup` Info.plist key, and links `UserNotifications`.

## Usage (Blueprint or C++)

Get the subsystem via **Get Game Instance Subsystem → BeamableNotificationsSubsystem**,
bind the event dispatchers, then call the functions. In C++:

```cpp
auto* Notif = GetGameInstance()->GetSubsystem<UBeamableNotificationsSubsystem>();

Notif->OnTokenReceived.AddDynamic(this, &AMyActor::HandleToken);
Notif->OnNotificationTapped.AddDynamic(this, &AMyActor::HandleTap);

Notif->RequestPermission(/*alert*/true, /*badge*/true, /*sound*/true);

Notif->ScheduleLocalNotification(
    TEXT("daily"), TEXT("Come back!"), TEXT("Your energy is full"),
    /*DelaySeconds*/ 3600.f, /*DeepLink*/ TEXT("game://home"));

Notif->RegisterForRemote();

FBMNNotificationData Launch;
if (Notif->GetLaunchNotification(Launch)) { /* route Launch.DeepLink */ }
```

All delegates are broadcast on the game thread. For payloads beyond the simple helper,
use `ScheduleLocalJson` / `RegisterTemplateJson` / `RegisterCategoryJson` with the JSON
schemas in [payloads.md](payloads.md).
