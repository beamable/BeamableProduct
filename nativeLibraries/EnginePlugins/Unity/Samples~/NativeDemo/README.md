# Native Demo

A small IMGUI test harness for the `Beamable.Notifications` package, plus a sample native
receive-time handler.

Contents:
- `BeamableNativeSample.cs` — IMGUI panel: schedule local notifications (interval + calendar),
  register for remote (FCM/APNs) token, subscribe to the notification + deep-link events, and a
  button that posts a test webhook from C#.
- `Editor/BeamableNativeSampleSceneCreator.cs` — adds **Tools/Beamable/Android/Create Native Sample
  Scene**, which generates a boot scene wired to `BeamableNativeSample` and sets it first in Build
  Settings. Run it after importing this sample.
- `DiscordWebhookPushHandler.java` — a sample `com.beamable.push.PushNotificationReceivedHandler`
  (package `com.beamable.sample`) that POSTs a Discord webhook when a push arrives, including while
  the app is **killed**. The webhook URL is a throwaway test endpoint — replace or remove it.

## Wiring the native handler (important)

The handler only fires if your app's `AndroidManifest.xml` names it:

```xml
<meta-data android:name="com.beamable.push.notification_received_handler"
           android:value="com.beamable.sample.DiscordWebhookPushHandler" />
```

The default manifest scaffolded by **Tools/Beamable/Android/Setup & Validation** uses a placeholder
class (`com.companyname.app.MyPushReceivedHandler`). Point it at this sample's class (above) after
importing, or at your own handler. `DiscordWebhookPushHandler.java` ships with an Android plugin
importer so Unity compiles it into the app once imported; if your Unity version doesn't pick it up
from the imported sample location, move it under `Assets/Plugins/Android/`.

Remote (killed-app) delivery additionally needs a `google-services.json` and **data-only,
high-priority** FCM messages — see `nativeLibraries/Android/BeamableNotifications/README.md`.
