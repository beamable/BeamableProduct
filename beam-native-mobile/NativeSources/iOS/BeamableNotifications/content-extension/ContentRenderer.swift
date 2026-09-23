import UserNotifications
import UIKit

/// A custom-UI renderer for the Notification **Content** Extension — the iOS analog of Android's
/// `PushNotificationStyleRenderer`. Where the NSE `NotificationServicePlugin` can only *mutate* the
/// notification content (attach media, set badge/sound/category), a `BeamContentRenderer` draws a
/// fully-custom UIKit view in the **expanded** notification (long-press / pull-down).
///
/// A renderer claims a notification by returning `true` from `render`, exactly like the Android hook:
///   • `true`  → "I drew this" — the host view controller stops and shows your view.
///   • `false` → "not mine" — the host offers it to the next renderer, else shows a default layout.
///
/// Renderers are discovered by class name from the **`BMNContentRenderers`** array in the Content
/// Extension target's Info.plist (via `NSClassFromString`), so a conforming type must be an
/// `NSObject` subclass with a no-arg `init()` and be compiled INTO the content-extension target.
///
/// The renderer reads the style id + custom fields from `notification.request.content.userInfo`
/// (the same flat keys `PushRailService` puts on the wire). Add subviews to `container`; it is laid
/// out to fill the extension's content area. A renderer may keep a `Timer` to update its view while
/// it is on-screen (e.g. a countdown) — but note the collapsed banner is the OS default and cannot
/// show live-updating custom UI.
@objc public protocol BeamContentRenderer {
    /// Draw into `container` for `notification`. Return `true` if this renderer handled it.
    @objc func render(in container: UIView, notification: UNNotification) -> Bool
}
