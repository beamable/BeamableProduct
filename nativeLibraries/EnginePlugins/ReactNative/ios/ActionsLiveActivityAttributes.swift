import Foundation
#if canImport(ActivityKit)
import ActivityKit

/// A single action button rendered inside the "actions" **Live Activity** (the always-visible,
/// Lock-Screen answer to "action buttons without tap-and-hold"). Lives in `ContentState` — NOT the
/// static attributes — so the server can change the buttons with an update push (e.g. swap "Claim"
/// for "Claimed" after the player taps). `role` mirrors the native `actions` category semantics
/// (`CategoryStore` foreground/destructive): `"destructive"` tints the button red and, by
/// convention, ends the activity; anything else is a normal action.
public struct BeamLiveActivityButton: Codable, Hashable {
    public var id: String
    public var title: String
    public var role: String   // "default" | "destructive"
    public init(id: String, title: String, role: String = "default") {
        self.id = id
        self.title = title
        self.role = role
    }
}

/// Shared `ActivityAttributes` for the "actions" **Live Activity**. The always-visible Lock-Screen /
/// Dynamic-Island card with persistent, interactive buttons (via `BeamLiveActivityActionIntent`) —
/// the iOS equivalent of the always-on action buttons apps like iFood/Duolingo show. A native
/// `actions` push notification can only reveal its buttons on expand (OS rule); this is the no-tap path.
///
/// This SAME type is compiled into BOTH the app (the `BeamableNotificationsRN` podspec globs
/// `ios/*.swift`) and the Widget extension (the Expo config plugin copies this file into the widget
/// target). ActivityKit matches a running Activity to its widget by the attributes type's UNQUALIFIED
/// name, and APNs push-to-start matches on the `attributes-type` string — so this name
/// (`BeamActionsActivityAttributes`) and the `ContentState` field names are a wire contract shared
/// with `PushRailService`/the portal. Gated `@available(iOS 16.1, *)`.
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
    }

    /// Static title shown for the life of the activity.
    public var title: String
    public init(title: String) { self.title = title }
}
#endif
