import Foundation
import UserNotifications

/// Parsing, sanitizing and category-naming for the `buttons` wire key.
///
/// The button model itself lives in `ActionButton.swift` (Foundation only) because the Widget
/// extension needs it; everything here touches `UserNotifications` and stays app/NSE-side.
public enum BeamActionButtons {

    /// How many buttons survive into a synthesized category.
    ///
    /// iOS itself renders up to four actions on an expanded notification, but the console authors
    /// EXACTLY two (`MAX_LIVE_ACTIVITY_BUTTONS`) because the Dynamic Island expanded region only shows
    /// two. Both iOS surfaces must show the same buttons, so the smaller number wins here.
    public static let maxButtons = 2

    /// The `userInfo` key the rail writes (`PushRailService.WriteIntentData` → APNs payload root).
    public static let userInfoKey = "buttons"

    /// Prefix of every synthesized category id. `beam_actions` itself stays the built-in fallback.
    public static let synthesizedPrefix = "beam_actions_"

    /// Category id the SDK registers at init for the built-in `actions` style (Open / Dismiss), used
    /// as the last resort when a payload carries no usable `buttons`. Declared here rather than on
    /// `StyleServicePlugin` because the plugin lives in the NSE target, which core cannot see.
    public static let builtInActionsCategory = "beam_actions"

    /// True for a category id this SDK synthesized (used to scope LRU eviction to our own pool).
    public static func isSynthesized(_ categoryId: String) -> Bool {
        categoryId.hasPrefix(synthesizedPrefix)
    }

    /// Read `buttons` out of a notification's `userInfo`, sanitized and capped.
    public static func parse(userInfo: [AnyHashable: Any]) -> [BeamActionButton] {
        sanitize(parse(userInfo[userInfoKey]))
    }

    /// Tolerant parse of a single `buttons` value.
    ///
    /// The rail stringifies every non-scalar (`extra` is a `Dictionary<string,string>`, and FCM data
    /// values must be strings), so over the wire this is a JSON **string**. A locally-scheduled
    /// notification or a hand-written `simctl push` payload can carry a real array instead — accept
    /// both, exactly like `JSONValue.bmnDecodeStringified` does for `offers` / `campaignData`.
    /// Anything else, or malformed JSON, yields `[]` so the caller falls back rather than failing.
    public static func parse(_ raw: Any?) -> [BeamActionButton] {
        guard let raw = raw else { return [] }

        if let string = raw as? String {
            let trimmed = string.trimmingCharacters(in: .whitespacesAndNewlines)
            guard !trimmed.isEmpty, let data = trimmed.data(using: .utf8) else { return [] }
            return (try? JSON.decoder.decode([BeamActionButton].self, from: data)) ?? []
        }

        if let array = raw as? [Any] {
            guard JSONSerialization.isValidJSONObject(["b": array]),
                  let data = try? JSONSerialization.data(withJSONObject: array) else { return [] }
            return (try? JSON.decoder.decode([BeamActionButton].self, from: data)) ?? []
        }

        return []
    }

    /// Drop unusable entries, dedupe, and cap.
    ///
    /// A blank `id` can't be routed on tap and a blank `title` renders as an invisible button, so both
    /// are dropped. `com.apple.` ids are rejected because `UNNotificationDefaultActionIdentifier` /
    /// `UNNotificationDismissActionIdentifier` live in that namespace — a collision would make a real
    /// button indistinguishable from "the player tapped the body" in `NotificationManager`'s tap path.
    public static func sanitize(_ buttons: [BeamActionButton]) -> [BeamActionButton] {
        var seen = Set<String>()
        var result: [BeamActionButton] = []
        for button in buttons {
            let id = button.id.trimmingCharacters(in: .whitespacesAndNewlines)
            let title = button.title.trimmingCharacters(in: .whitespacesAndNewlines)
            guard !id.isEmpty, !title.isEmpty else { continue }
            guard !id.hasPrefix("com.apple.") else { continue }
            guard seen.insert(id).inserted else { continue }
            result.append(BeamActionButton(id: id, title: title, role: button.role))
            if result.count == maxButtons { break }
        }
        return result
    }

    /// Deterministic category id for a button set.
    ///
    /// Deterministic matters twice over: the NSE and the app are separate processes that must agree on
    /// the id, and two pushes carrying identical buttons must reuse one registration instead of
    /// leaking a new category per delivery. That rules out Swift's `hashValue`, whose seed is
    /// randomized per process — hence an explicit FNV-1a/64 over a canonical rendering.
    public static func categoryId(for buttons: [BeamActionButton]) -> String {
        guard !buttons.isEmpty else { return builtInActionsCategory }
        let canonical = buttons
            .map { "\($0.id)\u{01}\($0.title)\u{01}\($0.role.lowercased())" }
            .joined(separator: "\u{02}")
        return synthesizedPrefix + fnv1a64Hex(canonical)
    }

    /// FNV-1a, 64-bit, lower-case hex. Stable across processes, launches and OS versions.
    static func fnv1a64Hex(_ string: String) -> String {
        var hash: UInt64 = 0xcbf2_9ce4_8422_2325
        for byte in Array(string.utf8) {
            hash ^= UInt64(byte)
            hash = hash &* 0x0000_0100_0000_01B3
        }
        return String(format: "%016llx", hash)
    }
}

public extension CategorySpec {
    /// Build a category from payload-authored buttons.
    ///
    /// Option mapping mirrors the built-in `beam_actions` pair so a default payload behaves exactly as
    /// before: a `destructive` button is destructive and NOT foreground (like `dismiss`), every other
    /// button is `.foreground` (like `open`) because its tap has to route the deep link, which needs
    /// the app frontmost.
    init(synthesizedFrom buttons: [BeamActionButton]) {
        self.init(
            id: BeamActionButtons.categoryId(for: buttons),
            actions: buttons.map { button in
                ActionSpec(id: button.id,
                           title: button.title,
                           foreground: button.isDestructive ? nil : true,
                           destructive: button.isDestructive ? true : nil,
                           authenticationRequired: nil)
            },
            hiddenPreviewsBodyPlaceholder: nil
        )
    }
}
