import UserNotifications
import Foundation

/// Applies the shared "style" wire contract to a remote notification inside the NSE — the iOS
/// counterpart of Android's `NotificationBuilder.applyStyle`. `PushRailService` sends every styling
/// field as a plain string in the top-level `userInfo` (see `WriteIntentData`), so this plugin reads
/// them back out and maps them onto the native `UNMutableNotificationContent`:
///
///   • `badge`    → `content.badge`   (parsed from the string; drives the app-icon badge)
///   • `sound`    → `content.sound`   (a bundled sound filename; absent/blank keeps the default)
///   • `category` → `content.categoryIdentifier` (action buttons + which content extension renders);
///                  the `actions` style with no explicit category falls back to the built-in
///                  `beam_actions` category the SDK registers at init (see `NotificationManager`).
///
/// The built-in `bigPicture` style is handled by `RichMediaServicePlugin` (image attachment) and
/// `default` / `bigText` need no work — iOS shows the full body natively on expand. This plugin is
/// transform-only and always forwards the content unchanged when there is nothing to apply.
public final class StyleServicePlugin: NotificationServicePlugin {

    /// Category id the SDK registers for the built-in `actions` style (mirrors Android's actions preset).
    public static let builtInActionsCategory = "beam_actions"

    public init() {}

    public func process(_ content: UNMutableNotificationContent,
                        completion: @escaping (UNMutableNotificationContent) -> Void) {
        let info = content.userInfo
        let style = (info["style"] as? String) ?? ""

        // Badge — the wire value is a string ("badge" in userInfo); set the native app-icon badge.
        if let badgeString = info["badge"] as? String, let badge = Int(badgeString) {
            content.badge = NSNumber(value: badge)
        }

        // Sound — a bundled filename; blank/absent leaves whatever the OS chose (aps.sound default).
        if let sound = info["sound"] as? String, !sound.isEmpty {
            content.sound = UNNotificationSound(named: UNNotificationSoundName(sound))
        }

        // Category — explicit `category` wins; the `actions` style with no category uses the built-in.
        if let category = info["category"] as? String, !category.isEmpty {
            content.categoryIdentifier = category
        } else if style == "actions" {
            content.categoryIdentifier = Self.builtInActionsCategory
        }

        completion(content)
    }
}
