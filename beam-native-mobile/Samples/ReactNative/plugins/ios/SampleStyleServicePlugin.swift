import UserNotifications

/// Sample **NSE service plugin** (Tier 2 custom-style seam) for the RN sample app.
///
/// It maps the app's custom `countdown` style onto a notification category so the sample's
/// Notification **Content** Extension (`SampleCountdownContentRenderer`) is triggered when the user
/// expands the notification. The shared `StyleServicePlugin` already maps an explicit `category`
/// field and the built-in `actions` style; this shows how a consuming app wires its OWN style →
/// category without asking the sender to set a `category` field.
///
/// Discovery: listed by class name in the NSE target's `BMNServicePlugins` Info.plist array (the
/// Expo plugin's `iosServicePlugins` prop writes it). `@objc(SampleStyleServicePlugin)` gives the
/// class a stable Objective-C runtime name so `NSClassFromString("SampleStyleServicePlugin")`
/// resolves regardless of the generated extension module name. Must be an `NSObject` subclass with
/// a no-arg `init()`.
@objc(SampleStyleServicePlugin)
public final class SampleStyleServicePlugin: NSObject, NotificationServicePlugin {

    public override init() { super.init() }

    public func process(_ content: UNMutableNotificationContent,
                        completion: @escaping (UNMutableNotificationContent) -> Void) {
        if (content.userInfo["style"] as? String) == "countdown" {
            // Route to the Content Extension registered for the `countdown` category.
            content.categoryIdentifier = "countdown"
        }
        completion(content)
    }
}
