# BeamPlatformNotifications

A self-contained, reusable Unreal plugin wrapping the Beamable native notifications library for
**iOS and Android**: permissions, local + scheduled notifications, remote push (APNs/FCM),
notification callbacks/events, deep links, and closed-app delivery analytics.

## Modules
- **`BeamPlatformNotifications`** (Runtime) — `UBeamPlatformNotificationsSubsystem`
  (`UGameInstanceSubsystem`). iOS calls the Swift core's C ABI (`bmn_*`); Android calls the Kotlin
  core via JNI. No-op on editor/desktop so it always compiles. Blueprint-assignable delegates:
  `OnPermissionResult`, `OnTokenReceived/Error`, `OnNotificationPresented/Received/Tapped`,
  `OnPendingNotifications`, `OnDeliveryReceipts`, `OnDeepLink`. Funnel analytics:
  `ConfigureAuth`/`ClearAuth` (persist/clear the player bearer token for native funnel POSTs) and
  `TrackOfferClicked`/`TrackOfferConverted` (emit offer funnel events).
- **`BeamPlatformNotificationsEditor`** (Editor) — adds the **"iOS + NSE → Device"** toolbar
  button: pick a device, package iOS, graft + sign the closed-app Notification Service Extension,
  install. Runs as a child process streaming to the Output Log (`LogBeamNotif`); flips to Cancel
  while running.

## Install (into any UE project)
The installer ships in this folder (`install-beamplatformnotifications.sh`) — copy it into your UE
project (or run it in place) and point `--source` at your `nativeLibraries` checkout:
```
./install-beamplatformnotifications.sh --source <path/to/nativeLibraries>
```
Generates a self-contained copy (sources + the native binaries committed under `ThirdParty/` +
bundled `Scripts/`), installs it into the project, enables it, and **prompts** for the
project-specific values (App Group, deep-link scheme, FCM on/off) — writing them to
`DefaultEngine.ini`. Use `--generate-only <dir>` to just emit the plugin folder for sharing.
Nothing project-specific is baked into the plugin; everything is read from config at runtime/build
time.

> The binaries under `ThirdParty/` (iOS `BeamableNotifications.embeddedframework.zip`, Android
> `beamable-notifications-release.aar`) are staged by the repo's `dev-native.sh`. If `ThirdParty/`
> is empty, run `./dev-native.sh` from the repo root first (macOS + Xcode for the iOS framework).

> **Naming:** the UE plugin/module is `BeamPlatformNotifications`, but the iOS **framework** binary is
> still `BeamableNotifications.framework` (it matches its `@rpath` install name), and the native C ABI
> (`bmn_*`) / Android JNI exports (`Java_com_beamable_…`) are unchanged.

> The embedded iOS framework is **device-only (arm64)**. For an iOS Simulator build, repackage the
> simulator slice from `build/BeamableNotifications.xcframework` in the same layout.

## One-time Xcode / Project Settings steps (UPL can't do these)

The UPL automatically adds the `remote-notification` background mode and the `BMNAppGroup` Info.plist
key, and links `UserNotifications`. These three remain manual:

1. **Project Settings → iOS → Online → "Enable Remote Notifications Support"** — writes the
   `aps-environment` entitlement.
2. Add the **App Group** capability (e.g. `group.com.beamable.notifications`) to **both** the app and
   the extension in your provisioning profiles.
3. In the generated Xcode project, add a **Notification Service Extension** target using the staged
   `BeamableNotificationServiceExtension/` sources, and set its `BMNAppGroup` Info.plist key + App
   Group entitlement. (The editor's **"iOS + NSE → Device"** button does this for you on a packaged
   build — see `NSE-SETUP.md`.)

## Usage (Blueprint or C++)

Get the subsystem via **Get Game Instance Subsystem → BeamPlatformNotificationsSubsystem**, bind the
event dispatchers, then call the functions. In C++:

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

All delegates are broadcast on the game thread. For payloads beyond the simple helper, use
`ScheduleLocalJson` / `RegisterTemplateJson` / `RegisterCategoryJson` with the JSON schemas in
the *Push notifications for Unreal* guide in the Beamable documentation.

## Funnel analytics
Campaign funnel events (Sent/Received/Opened/Clicked/Converted) are POSTed natively to Beamable.
The native code (the iOS Swift core plus its NSE, and the Android Kotlin core's `PushFirebaseService`)
authenticates and POSTs even when the engine VM is asleep, using credentials the app supplies:
- **`ConfigureAuth(AuthJson)`** — persist the player bearer token + realm routing (`cid`/`pid`/
  `host`) so native funnel POSTs can authenticate. Call on login/refresh. **`ClearAuth()`** on logout.
- **`TrackOfferClicked(RequestJson)` / `TrackOfferConverted(RequestJson)`** — emit a Clicked /
  Converted funnel event for an in-app offer, attributed back to the originating campaign via the
  notification's intent data. `RequestJson` is the canonical `OfferTrackRequest`
  (`{campaignId,nodeId,gamerTag,accountId,cidPid,deeplink,offer:{...}}`).

Closed-app receipt funnel events are emitted natively: on **iOS** by the Notification Service Extension
(see `NSE-SETUP.md`), and on **Android** by `PushFirebaseService` itself, which fires the **Received**
event on delivery. No game-side receive handler is registered or required for the funnel to work — the
library declares none, and Unreal has no Blueprint/C++ hook to add one. To run your own code at receive
time, add a Kotlin `PushNotificationReceivedHandler` and register it through your own APL
`<androidManifestUpdates>` block.

## Settings (written to the project's `DefaultEngine.ini`)
- `[/Script/BeamPlatformNotifications.Settings] AppGroup` — iOS App Group id (UPL/Info.plist).
- `[BeamPlatformNotifications] DeepLinkScheme` — custom URL scheme (iOS `CFBundleURLSchemes` + Android VIEW intent).
- `[BeamPlatformNotifications] bUseFcm` — enable Android FCM remote push.

Funnel analytics needs no `DefaultEngine.ini` endpoint: the native layer authenticates with the
player bearer token supplied at runtime via `ConfigureAuth` and POSTs Beamable `CoreEvent`s
directly (see **Funnel analytics** above).

## Bundled scripts (`Scripts/`)
- `package-ios-deploy.sh` — package iOS → `add-nse.sh` → install to a device (driven by the button).
- `add-nse.sh` — build + graft + sign the closed-app Notification Service Extension.

See `NSE-SETUP.md` for the closed-app analytics extension details.
