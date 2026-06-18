# React Native setup

## Install

1. Build the core: `./scripts/build-xcframework.sh`.
2. Copy it into the package: `cp -R build/BeamableNotifications.xcframework reactnative/ios/Frameworks/`.
3. Add the dependency (local path or published):
   ```json
   "dependencies": { "beamable-notifications": "file:../reactnative" }
   ```
4. `cd ios && pod install`.

## Xcode capabilities (the RN app owns its project)

- Enable **Push Notifications** and **Background Modes → Remote notifications**.
- Add an **App Group** to the app and the NSE; set `BMNAppGroup` in both Info.plists.
- Add a **Notification Service Extension** target and include the SDK's NSE sources
  (`extension/NotificationService.swift` + `extension/ServicePlugins/*.swift`).

## Usage

```ts
import BeamableNotifications from 'beamable-notifications';

BeamableNotifications.addListener('tokenReceived', ({ token }) => sendToBackend(token));
BeamableNotifications.addListener('notificationTapped', n => router.open(n.deepLink));
BeamableNotifications.addListener('permissionResult', r => console.log(r.granted));

BeamableNotifications.initialize();
BeamableNotifications.requestPermission();

// Cold start ("get intent")
const launch = await BeamableNotifications.getLaunchNotification();
if (launch?.deepLink) router.open(launch.deepLink);

// Local notification
BeamableNotifications.scheduleLocal({
  id: 'daily',
  title: 'Come back!',
  body: 'Your energy is full',
  trigger: { type: 'timeInterval', seconds: 3600 },
  userInfo: { deepLink: 'app://home' },
});

// Remote + closed-app analytics
BeamableNotifications.configureAnalytics({
  enabled: true,
  endpoint: 'https://api.example.com/notif-events',
  commonParams: { playerId },
});
BeamableNotifications.registerForRemote();
```

`addListener` returns a subscription — call `.remove()` on unmount. All payloads are
typed (see `src/index.ts`).
