# Extending the SDK with native plugins

The core is **closed for modification, open for extension**. Add behavior — custom token
providers (e.g. FCM), analytics backends, payload transforms, logging — by writing a new
Swift/Obj-C file. You never edit the SDK core.

There are two plugin protocols: one for the **main app**, one for the **Notification
Service Extension** process.

## App plugins — `NotificationPlugin`

Every method has a default no-op, so override only what you need.

```swift
import BeamableNotifications

final class LoggingPlugin: NSObject, NotificationPlugin {
    var id: String { "com.acme.logging" }

    func onTokenReceived(_ token: String) {
        NSLog("APNs token: \(token)")
    }

    // Transform hook — mutate or drop a scheduling request (return nil to drop).
    func willSchedule(_ request: LocalRequest) -> LocalRequest? {
        var r = request
        var info = r.userInfo ?? [:]
        info["loggedAt"] = .number(Date().timeIntervalSince1970)
        r.userInfo = info
        return r
    }
}
```

- **Observe** hooks run in registration order: `onInitialize`, `onPermissionResult`,
  `onTokenReceived`, `onNotificationReceived`, `onNotificationTapped`.
- **Transform** hooks are chained: `willSchedule` (mutate/drop a request) and
  `willPresent` (decide foreground presentation options).
- `provideRemoteToken` is the **FCM/APNs seam** — return `true` to take over token
  acquisition; the default APNs flow runs only if no plugin claims it.

### The FCM example (not built in v1)

```swift
final class FCMTokenPlugin: NSObject, NotificationPlugin {
    var id: String { "com.acme.fcm" }
    func provideRemoteToken(register: @escaping (Result<String, Error>) -> Void) -> Bool {
        Messaging.messaging().token { token, error in
            if let token = token { register(.success(token)) }
            else if let error = error { register(.failure(error)) }
        }
        return true   // we own token acquisition now
    }
}
```

No change to the C ABI, the engine wrappers, or `NotificationManager`.

## NSE plugins — `NotificationServicePlugin`

Run inside the extension on remote-push receipt (even when the app is killed). This is
where closed-app work lives.

```swift
final class TaggingServicePlugin: NotificationServicePlugin {
    func process(_ content: UNMutableNotificationContent,
                 completion: @escaping (UNMutableNotificationContent) -> Void) {
        content.title = "🔔 " + content.title
        completion(content)   // always call exactly once
    }
}
```

The built-ins `AnalyticsServicePlugin` and `RichMediaServicePlugin` ship by default.

## Registering plugins

Either way requires **no core edits**:

1. **Zero-code (Info.plist auto-discovery).** Add the class name to an array key:
   - App target Info.plist → `BMNPlugins` (array of class names)
   - NSE target Info.plist → `BMNServicePlugins`
   Classes discovered this way must be `NSObject` subclasses with an `init()`.

   ```xml
   <key>BMNPlugins</key>
   <array><string>LoggingPlugin</string><string>AnalyticsPlugin</string></array>
   ```

2. **Explicit (when you need constructor arguments).**
   ```swift
   PluginRegistry.shared.register(AnalyticsPlugin(session: mySession, configProvider: { … }))
   ```
   Call before / during `bmn_initialize`.

## Reference plugins shipped with the SDK

| Plugin | Where | Purpose |
|--------|-------|---------|
| `APNsTokenPlugin` | app | example token observer; declines the token seam |
| `AnalyticsPlugin` | app | reports received/opened while the app is alive |
| `RichMediaServicePlugin` | NSE | downloads & attaches `media-url` |
| `AnalyticsServicePlugin` | NSE | closed-app analytics POST + delivery receipt |
