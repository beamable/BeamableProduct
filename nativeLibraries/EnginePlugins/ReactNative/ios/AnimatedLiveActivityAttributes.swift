import Foundation
#if canImport(ActivityKit)
import ActivityKit

/// Shared `ActivityAttributes` for the "animated" **Live Activity** — the iOS parity for the Android
/// `animated` style (a custom notification whose colored panels auto-cycle via a `ViewFlipper`). iOS
/// has no equivalent for a notification banner, so the always-visible cycling card is delivered as a
/// Live Activity instead. Mirrors the Android wire keys `colors` (hex list) and `flipIntervalMs`.
///
/// IMPORTANT constraint: SwiftUI inside a Live Activity cannot free-run a timer, and the OS throttles
/// Lock-Screen redraws to a small update budget — so a literal 900ms flip is NOT achievable on the
/// Lock Screen. The widget cycles best-effort via `TimelineView` (smooth only in the on-screen
/// Dynamic Island); `activeIndex` lets the server drive true stepping via update pushes for realms
/// that need it. See `SampleAnimatedLiveActivity`.
///
/// Same type compiled into BOTH the app (podspec glob) and the Widget extension (Expo plugin copy).
/// The unqualified name `BeamAnimatedActivityAttributes` and the `ContentState` field names are the
/// APNs push-to-start `attributes-type` / `content-state` wire contract shared with the rail/portal.
@available(iOS 16.1, *)
public struct BeamAnimatedActivityAttributes: ActivityAttributes {
    public struct ContentState: Codable, Hashable {
        /// Secondary descriptive line.
        public var body: String
        /// Panel colors as hex strings (e.g. "#FF8800") — mirrors the Android `colors` CSV.
        public var colors: [String]
        /// Best-effort flip cadence in ms (Android default 900); throttled on the Lock Screen.
        public var flipIntervalMs: Int
        /// Server-driven current panel index (used when a realm pushes explicit steps instead of
        /// relying on the local `TimelineView` cycle).
        public var activeIndex: Int
        public init(body: String,
                    colors: [String],
                    flipIntervalMs: Int = 900,
                    activeIndex: Int = 0) {
            self.body = body
            self.colors = colors
            self.flipIntervalMs = flipIntervalMs
            self.activeIndex = activeIndex
        }
    }

    /// Static title shown for the life of the activity.
    public var title: String
    public init(title: String) { self.title = title }
}
#endif
