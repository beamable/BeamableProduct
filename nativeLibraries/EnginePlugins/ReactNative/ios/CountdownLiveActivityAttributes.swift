import Foundation
#if canImport(ActivityKit)
import ActivityKit

/// Shared `ActivityAttributes` for the "countdown" **Live Activity** (the no-tap, always-visible
/// countdown shown on the Lock Screen / Dynamic Island — the iFood/Duolingo-style live card).
///
/// This SAME type must be compiled into BOTH:
///   • the app (this file lives in the RN package's `ios/*.swift`, which the `BeamableNotificationsRN`
///     podspec compiles into the app — that's what `BeamableNotificationsModule.startCountdownLiveActivity`
///     uses to `Activity.request(...)`), and
///   • the Widget extension (the Expo config plugin copies this file into the widget target so its
///     `ActivityConfiguration(for:)` can reference it).
/// ActivityKit matches a running Activity to its widget by the attributes type's UNQUALIFIED name,
/// so both copies must keep the same name + `ContentState` shape. Gated `@available(iOS 16.1, *)`
/// because the app's deployment target can be lower than ActivityKit's minimum.
@available(iOS 16.1, *)
public struct BeamCountdownActivityAttributes: ActivityAttributes {
    public struct ContentState: Codable, Hashable {
        /// Absolute expiry instant. The widget renders it with `Text(timerInterval:)`, so iOS ticks
        /// the countdown down on its own — no per-second push updates needed.
        public var expiresAt: Date
        /// Secondary line (e.g. the offer body).
        public var body: String
        /// Flipped to `true` by a content update at the deadline so the widget shows an "expired"
        /// state reliably (the OS `context.isStale` re-render is unreliable on the Simulator).
        public var isExpired: Bool
        public init(expiresAt: Date, body: String, isExpired: Bool = false) {
            self.expiresAt = expiresAt
            self.body = body
            self.isExpired = isExpired
        }
    }

    /// Static title shown for the life of the activity.
    public var title: String
    public init(title: String) { self.title = title }
}
#endif
