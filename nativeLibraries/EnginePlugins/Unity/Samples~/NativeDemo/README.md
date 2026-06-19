# Native Demo

A small IMGUI test harness for the `Beamable.Notifications` package, plus a sample native
receive-time handler.

Contents:
- `BeamableNativeSample.cs` — IMGUI panel: schedule local notifications (interval + calendar),
  register for remote (FCM/APNs) token, subscribe to the notification + deep-link events, and a
  button that posts a test webhook from C#.
- `NativeDemo.unity` — a ready boot scene with `BeamableNativeSample` on a GameObject.
- `Editor/BeamableNativeSampleSetup.cs` — adds **Tools/Beamable/Android/Set Up Native Sample**, which
  runs the Android setup (scaffolds/patches `AndroidManifest.xml` wired to this sample's handler,
  sets min SDK, disables stale gradle toggles) and adds `NativeDemo.unity` as the first scene in
  Build Settings. Run it once after importing this sample. Idempotent.
- `DiscordWebhookPushHandler.java` — a sample `com.beamable.push.PushNotificationReceivedHandler`
  (package `com.beamable.sample`) that POSTs a Discord webhook when a push arrives, including while
  the app is **killed**. The webhook URL is a throwaway test endpoint — replace or remove it.

## Quick start

After importing this sample, run **Tools/Beamable/Android/Set Up Native Sample** — it configures the
Android project and boots into `NativeDemo.unity`. Switch the platform to Android and build.

## Wiring the native handler (important)

The handler only fires if your app's `AndroidManifest.xml` names it:

```xml
<meta-data android:name="com.beamable.push.notification_received_handler"
           android:value="com.beamable.sample.DiscordWebhookPushHandler" />
```

The default manifest scaffolded by **Tools/Beamable/Android/Setup and Validation** uses a placeholder
class (`com.companyname.app.MyPushReceivedHandler`). Point it at this sample's class (above) after
importing, or at your own handler. `DiscordWebhookPushHandler.java` ships with an Android plugin
importer so Unity compiles it into the app once imported; if your Unity version doesn't pick it up
from the imported sample location, move it under `Assets/Plugins/Android/`.

Remote (killed-app) delivery additionally needs a `google-services.json` and **data-only,
high-priority** FCM messages — see `nativeLibraries/Android/BeamableNotifications/README.md`.
