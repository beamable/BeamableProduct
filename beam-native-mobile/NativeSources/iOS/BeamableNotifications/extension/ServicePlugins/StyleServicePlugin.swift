import UserNotifications
import Foundation

/// Applies the shared "style" wire contract to a remote notification inside the NSE — the iOS
/// counterpart of Android's `NotificationBuilder.applyStyle`. `PushRailService` sends every styling
/// field as a plain string in the top-level `userInfo` (see `WriteIntentData`), so this plugin reads
/// them back out and maps them onto the native `UNMutableNotificationContent`:
///
///   • `badge`    → `content.badge`   (parsed from the string; drives the app-icon badge)
///   • `sound`    → `content.sound`   (a bundled sound filename; absent/blank keeps the default)
///   • `category` → `content.categoryIdentifier` (action buttons + which content extension renders)
///   • `buttons`  → a `UNNotificationCategory` synthesized on the spot from the payload-authored
///                  `[{id,title,role}]`, so the fallback notification shows the SAME labels the author
///                  typed in the console instead of the built-in pair.
///
/// Category precedence is explicit: `category` (author override, and the only thing a Notification
/// Content Extension can match on) → `buttons` (synthesized) → the built-in `beam_actions` pair the
/// SDK registers at init (see `NotificationManager`).
///
/// The `buttons` tier exists because the console's `actions` style is dual-delivery: iOS devices with a
/// Live Activity push-to-start token get an ActivityKit card (which never reaches this plugin — it is a
/// separate APNs delivery), and everyone else gets this notification. Both surfaces must read the same.
///
/// The built-in `bigPicture` style is handled by `RichMediaServicePlugin` (image attachment) and
/// `default` / `bigText` need no work — iOS shows the full body natively on expand. This plugin is
/// transform-only and always forwards the content unchanged when there is nothing to apply.
public final class StyleServicePlugin: NotificationServicePlugin {

    /// Category id the SDK registers for the built-in `actions` style (mirrors Android's actions preset).
    /// Defined in core so `BeamActionButtons` can name it without seeing this NSE-only type.
    public static let builtInActionsCategory = BeamActionButtons.builtInActionsCategory

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

        // Category, tier 1 — an explicit `category` wins. It names a set the app registered on-device
        // (the Android contract, and an iOS override), and it is the only value a Notification Content
        // Extension can match on via `UNNotificationExtensionCategory`.
        if let category = info["category"] as? String, !category.isEmpty {
            content.categoryIdentifier = category
            completion(content)
            return
        }

        guard style == "actions" else {
            completion(content)
            return
        }

        // Tier 2 — payload-authored buttons. Synthesize a category so the notification carries the
        // author's own labels. The id is a deterministic hash of the buttons, so identical payloads
        // reuse one registration; registration is confirmed by read-back before we hand the content
        // back, because the OS resolves `categoryIdentifier` as soon as this plugin returns.
        let buttons = BeamActionButtons.parse(userInfo: info)
        guard !buttons.isEmpty else {
            // Tier 3 — no usable buttons (absent, empty, or malformed JSON): the built-in Open /
            // Dismiss pair. A bad `buttons` value degrades the buttons, never the notification.
            content.categoryIdentifier = Self.builtInActionsCategory
            completion(content)
            return
        }

        content.categoryIdentifier = BeamActionButtons.categoryId(for: buttons)
        CategoryStore.shared.registerSynthesized(buttons: buttons) {
            completion(content)
        }
    }
}
