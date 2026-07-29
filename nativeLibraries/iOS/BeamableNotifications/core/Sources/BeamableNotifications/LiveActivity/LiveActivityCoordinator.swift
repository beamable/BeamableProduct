import Foundation
#if canImport(ActivityKit)
import ActivityKit
#endif

/// A push token ActivityKit handed us, on its way to the message rail.
public struct LiveActivityTokenEvent: Codable, Equatable {
    /// `"pushToStart"` — starts an activity for this attributes type — or `"update"`, which targets one
    /// already-running activity.
    public var kind: String
    public var activityType: String
    public var attributesType: String
    /// Set for `update` only: ActivityKit assigns it when the activity starts.
    public var activityId: String?
    /// Raw APNs token as lowercase hex, the same shape as a device token.
    public var token: String
}

public struct LiveActivityStartedEvent: Codable, Equatable {
    public var activityType: String
    public var attributesType: String
    public var activityId: String
}

/// Observes ActivityKit and hands what it learns to the engine layer. **Registration with the message
/// rail is deliberately NOT done here** — the app owns that, because it owns the authenticated player
/// (the ReactNative sample does it in `src/beam/liveActivity.ts` via `beam.messageRail`). This type's
/// whole job is to produce the three facts the app needs: a push-to-start token, an update token, and
/// whether a Live Activity is possible at all.
///
/// Everything is `#if canImport(ActivityKit)` / `@available`-guarded so the core package keeps building
/// and running at its iOS 14 floor.
public final class LiveActivityCoordinator {

    public static let shared = LiveActivityCoordinator()

    /// A push-to-start or per-activity update token arrived. Forward it to the rail.
    public var onToken: ((LiveActivityTokenEvent) -> Void)?
    /// An activity started (locally or from a push-to-start).
    public var onStarted: ((LiveActivityStartedEvent) -> Void)?
    /// Emitted once on `start()` and again whenever the player's Live Activity setting changes.
    ///
    /// Emitted on EVERY launch, not only on change, on purpose: a token published by a previous app
    /// version that shipped a widget stays valid server-side after an update removes the widget, and no
    /// ActivityKit callback reports that. Re-stating capability at launch is what lets the app withdraw
    /// a token that has quietly gone stale — otherwise the rail keeps sending Live Activities that
    /// render nothing, and suppresses the notification that would have worked.
    public var onCapability: (([LiveActivityCapability]) -> Void)?

    private var started = false
    private let lock = NSLock()

    /// Current capability for every attributes type the SDK knows.
    public func capabilities() -> [LiveActivityCapability] {
        LiveActivitySupport.capabilities()
    }

    /// Begin observing. Idempotent — safe to call from `NotificationManager.initialize()` and again
    /// from an engine bridge.
    ///
    /// Only types whose capability is fully `available` are observed, so an app with no widget for a
    /// type never publishes its token and the rail correctly falls back to a notification.
    public func start() {
        lock.lock()
        let alreadyStarted = started
        started = true
        lock.unlock()

        let caps = capabilities()
        onCapability?(caps)
        guard !alreadyStarted else { return }

        #if canImport(ActivityKit)
        if #available(iOS 16.1, *) { observeEnablement() }
        if #available(iOS 17.2, *) {
            for cap in caps where cap.available {
                switch cap.attributesType {
                case "BeamActionsActivityAttributes":
                    observeTokens(BeamActionsActivityAttributes.self, capability: cap)
                case "BeamAnimatedActivityAttributes":
                    observeTokens(BeamAnimatedActivityAttributes.self, capability: cap)
                case "BeamCountdownActivityAttributes":
                    observeTokens(BeamCountdownActivityAttributes.self, capability: cap)
                default:
                    continue
                }
            }
        }
        #endif
    }

    #if canImport(ActivityKit)
    /// Observes the push-to-start token (one per attributes type) and, for every activity that starts,
    /// its per-activity update token. Long-lived: these `for await` loops live for the app's lifetime.
    @available(iOS 17.2, *)
    private func observeTokens<T: ActivityAttributes>(_ type: T.Type,
                                                     capability: LiveActivityCapability) {
        Task {
            for await tokenData in Activity<T>.pushToStartTokenUpdates {
                self.onToken?(LiveActivityTokenEvent(
                    kind: "pushToStart",
                    activityType: capability.activityType,
                    attributesType: capability.attributesType,
                    activityId: nil,
                    token: Self.hexString(tokenData)))
            }
        }
        Task {
            for await activity in Activity<T>.activityUpdates {
                self.onStarted?(LiveActivityStartedEvent(
                    activityType: capability.activityType,
                    attributesType: capability.attributesType,
                    activityId: activity.id))
                Task {
                    for await tokenData in activity.pushTokenUpdates {
                        self.onToken?(LiveActivityTokenEvent(
                            kind: "update",
                            activityType: capability.activityType,
                            attributesType: capability.attributesType,
                            activityId: activity.id,
                            token: Self.hexString(tokenData)))
                    }
                }
            }
        }
    }

    /// Re-publish capability when the player toggles Live Activities in Settings, so the app can
    /// register or withdraw its token without waiting for the next launch.
    @available(iOS 16.1, *)
    private func observeEnablement() {
        Task {
            for await _ in ActivityAuthorizationInfo().activityEnablementUpdates {
                self.onCapability?(self.capabilities())
            }
        }
    }
    #endif

    /// Lowercase hex, the shape APNs tokens are registered in.
    static func hexString(_ data: Data) -> String {
        data.map { String(format: "%02x", $0) }.joined()
    }
}
