import Foundation
#if canImport(ActivityKit)
import ActivityKit

// The button model (`BeamActionButton`, aliased as `BeamLiveActivityButton`) lives one level up in
// `ActionButton.swift`, because the SAME authored `{id,title,role}` pair now drives both iOS surfaces:
// this Live Activity's `ContentState`, and the action buttons of the notification that an iOS device
// WITHOUT Live Activity support falls back to (the NSE synthesizes a `UNNotificationCategory` from the
// payload's `buttons` key). One model, so the two surfaces cannot drift.

/// Shared `ActivityAttributes` for the "actions" **Live Activity**. The always-visible Lock-Screen /
/// Dynamic-Island card with persistent, interactive buttons (via `BeamLiveActivityActionIntent`) —
/// the iOS equivalent of the always-on action buttons apps like iFood/Duolingo show. A native
/// `actions` push notification can only reveal its buttons on expand (OS rule); this is the no-tap path.
///
/// This SAME type is compiled into BOTH the app (via the `BeamableNotifications` core module) and the
/// Widget extension (the Expo config plugin copies this file, plus `ActionButton.swift`, into the
/// widget target). ActivityKit matches a running Activity to its widget by the attributes type's
/// UNQUALIFIED name, and APNs push-to-start matches on the `attributes-type` string — so this name
/// (`BeamActionsActivityAttributes`) and the `ContentState` field names are a wire contract shared
/// with `PushRailService`/the portal. It must therefore be defined EXACTLY once per target: that is why
/// the RN engine plugin's copy was deleted when this moved into core. Gated `@available(iOS 16.1, *)`.
@available(iOS 16.1, *)
public struct BeamActionsActivityAttributes: ActivityAttributes {
    public struct ContentState: Codable, Hashable {
        /// Prominent status line, updated by server update pushes (e.g. "Offer available" → "Claimed").
        public var headline: String
        /// Secondary descriptive line.
        public var body: String
        /// The buttons to show. Cleared (or replaced) via an update push once the offer is resolved.
        public var buttons: [BeamLiveActivityButton]
        /// Terminal state after the player taps Claim/Dismiss — the widget shows a resolved card.
        public var isResolved: Bool
        public init(headline: String,
                    body: String,
                    buttons: [BeamLiveActivityButton] = [],
                    isResolved: Bool = false) {
            self.headline = headline
            self.body = body
            self.buttons = buttons
            self.isResolved = isResolved
        }

        // Tolerant decode so an incomplete push-to-start `content-state` still starts the Activity
        // (a missing non-optional key would otherwise make the OS silently drop the start). `encode`
        // stays synthesized.
        public init(from decoder: Decoder) throws {
            let c = try decoder.container(keyedBy: CodingKeys.self)
            headline = (try? c.decode(String.self, forKey: .headline)) ?? ""
            body = (try? c.decode(String.self, forKey: .body)) ?? ""
            buttons = (try? c.decode([BeamLiveActivityButton].self, forKey: .buttons)) ?? []
            isResolved = (try? c.decode(Bool.self, forKey: .isResolved)) ?? false
        }
    }

    /// Static title shown for the life of the activity.
    public var title: String
    public init(title: String) { self.title = title }
}
#endif
