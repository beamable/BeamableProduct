# Unreal setup

The Unreal integration ships as the **`BeamPlatformNotifications`** plugin (master copy:
`iOS/BeamableNotifications/unreal/`). It supports **iOS and Android**, includes an editor
toolbar button that packages + deploys to a device, and is project-agnostic (all
project-specific values are read from the target project's `DefaultEngine.ini`).

> Note: the UE plugin/module is named `BeamPlatformNotifications`, but the iOS **framework**
> binary is still `BeamableNotifications.framework` (matches its `@rpath` install name), and the
> native C-ABI (`bmn_*`) / Android JNI exports (`Java_com_beamable_…`) are unchanged.

## Install (recommended)

From your UE project, run the installer with the path to this `nativeLibraries` checkout:
```
./install-beamplatformnotifications.sh --source <path/to/nativeLibraries>
```
It generates a self-contained plugin (sources + staged **dynamic** iOS framework
[`build/BeamableNotifications.embeddedframework.zip`, build it first with
`./scripts/build-xcframework-dynamic.sh`] + the Android `.aar` as a local maven repo + bundled
`Scripts/`), installs it into `Plugins/BeamPlatformNotifications/`, enables it, and **prompts** for
the project values (App Group, deep-link scheme, analytics endpoint, FCM on/off). Use
`--generate-only <dir>` to just emit the plugin for sharing.

> The embedded framework is device-only (arm64). For an iOS Simulator build, repackage the
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

Get the subsystem via **Get Game Instance Subsystem → BeamPlatformNotificationsSubsystem**,
bind the event dispatchers, then call the functions. In C++:

```cpp
auto* Notif = GetGameInstance()->GetSubsystem<UBeamPlatformNotificationsSubsystem>();

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
