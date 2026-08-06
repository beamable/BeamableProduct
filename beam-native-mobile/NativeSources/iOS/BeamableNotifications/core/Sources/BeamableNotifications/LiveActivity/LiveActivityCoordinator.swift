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

    /// The most recent token seen for each key, so a listener that attaches AFTER the token arrived
    /// still receives it. The push-to-start token is a one-shot ActivityKit event minted at launch,
    /// but the app can only register it with the rail once the player has authenticated — which
    /// happens later, after the launch-time emission is gone. `start()` replays this cache on every
    /// call so `startLiveActivityPushRegistration()` (invoked right before the app attaches its
    /// forwarding listener) re-delivers the token. Keyed by `attributesType` for `pushToStart` and by
    /// `activityId` for `update`, in distinct namespaces so the two never collide.
    private var lastTokens: [String: LiveActivityTokenEvent] = [:]

    /// Attributes types whose push-to-start token stream we are already observing. Observation must be
    /// re-entrant: `start()` is first called extremely early (the `+load` launch shim), when
    /// `areActivitiesEnabled` can still be false and no type is `available` yet — so gating observation
    /// on the FIRST `start()` alone would skip it forever. Instead every `start()` (and every Settings
    /// enablement change) subscribes any now-available type not already in this set, exactly once.
    private var observing: Set<String> = []

    private static func tokenCacheKey(_ event: LiveActivityTokenEvent) -> String {
        event.kind == "update"
            ? "update:\(event.activityId ?? event.attributesType)"
            : "pushToStart:\(event.attributesType)"
    }

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
        NSLog("[BMN-LA] start() alreadyStarted=\(alreadyStarted) caps=[\(caps.map { "\($0.activityType):avail=\($0.available)" }.joined(separator: ", "))]")
        onCapability?(caps)
        replayTokens()

        #if canImport(ActivityKit)
        // Enablement observer is set up once; token observation is re-entrant (see `observing`), so it
        // catches types that only become available on a later call — NOT gated on the first `start()`.
        if #available(iOS 16.1, *), !alreadyStarted { observeEnablement() }
        if #available(iOS 17.2, *) { observeAvailableTypes(caps) }
        #endif
    }

    #if canImport(ActivityKit)
    /// Subscribe to the push-to-start token stream for every currently-available type not already
    /// observed. Idempotent per type via `observing`; safe to call on every `start()` and on each
    /// Settings enablement change.
    @available(iOS 17.2, *)
    private func observeAvailableTypes(_ caps: [LiveActivityCapability]) {
        for cap in caps where cap.available {
            lock.lock()
            let alreadyObserving = observing.contains(cap.attributesType)
            if !alreadyObserving { observing.insert(cap.attributesType) }
            lock.unlock()
            guard !alreadyObserving else { continue }
            NSLog("[BMN-LA] subscribing pushToStart stream for \(cap.attributesType)")
            switch cap.attributesType {
            case "BeamActionsActivityAttributes":
                observeTokens(BeamActionsActivityAttributes.self, capability: cap)
            case "BeamAnimatedActivityAttributes":
                observeTokens(BeamAnimatedActivityAttributes.self, capability: cap)
            case "BeamCountdownActivityAttributes":
                observeTokens(BeamCountdownActivityAttributes.self, capability: cap)
            default:
                lock.lock(); observing.remove(cap.attributesType); lock.unlock()
            }
        }
    }
    #endif

    /// Cache the token under `lastTokens`, then forward it. Caching is what lets `replayTokens()`
    /// re-deliver a token to a listener that attaches after ActivityKit first handed it to us.
    /// Internal (not private) so tests can simulate a token arriving without a live ActivityKit stream.
    func record(_ event: LiveActivityTokenEvent) {
        lock.lock()
        lastTokens[Self.tokenCacheKey(event)] = event
        lock.unlock()
        onToken?(event)
    }

    /// Re-emit every cached token. Called from `start()`, so a re-invocation (e.g. from
    /// `startLiveActivityPushRegistration()` after the player connects) re-delivers tokens that were
    /// minted earlier, before the app attached its rail-forwarding listener. A no-op until a token
    /// has actually arrived.
    private func replayTokens() {
        lock.lock()
        let events = Array(lastTokens.values)
        lock.unlock()
        for event in events { onToken?(event) }
    }

    #if canImport(ActivityKit)
    /// Observes the push-to-start token (one per attributes type) and, for every activity that starts,
    /// its per-activity update token. Long-lived: these `for await` loops live for the app's lifetime.
    @available(iOS 17.2, *)
    private func observeTokens<T: ActivityAttributes>(_ type: T.Type,
                                                     capability: LiveActivityCapability) {
        Task {
            NSLog("[BMN-LA] awaiting pushToStartTokenUpdates for \(capability.attributesType)")
            for await tokenData in Activity<T>.pushToStartTokenUpdates {
                NSLog("[BMN-LA] pushToStart token YIELDED for \(capability.attributesType)")
                self.record(LiveActivityTokenEvent(
                    kind: "pushToStart",
                    activityType: capability.activityType,
                    attributesType: capability.attributesType,
                    activityId: nil,
                    token: Self.hexString(tokenData)))
            }
            NSLog("[BMN-LA] pushToStartTokenUpdates stream ENDED for \(capability.attributesType)")
        }
        Task {
            for await activity in Activity<T>.activityUpdates {
                self.onStarted?(LiveActivityStartedEvent(
                    activityType: capability.activityType,
                    attributesType: capability.attributesType,
                    activityId: activity.id))
                Task {
                    for await tokenData in activity.pushTokenUpdates {
                        self.record(LiveActivityTokenEvent(
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
                let caps = self.capabilities()
                self.onCapability?(caps)
                // A type that just became enabled must start observing its token now, not on next launch.
                if #available(iOS 17.2, *) { self.observeAvailableTypes(caps) }
            }
        }
    }
    #endif

    /// Lowercase hex, the shape APNs tokens are registered in.
    static func hexString(_ data: Data) -> String {
        data.map { String(format: "%02x", $0) }.joined()
    }
}
