import Foundation
import UIKit

/// Reference plugin: emits Beamable funnel analytics for notifications observed *while the
/// app is alive* (§4). The closed-app case is handled by the NSE
/// (`AnalyticsServicePlugin`); together they give full funnel coverage.
///
/// - **Received** — fired when a notification is presented in the foreground.
/// - **Opened** — fired when the user taps the notification (or an action button).
///
/// Events are only emitted when the notification carries a tracked campaign
/// (`campaignId` + `nodeId`) plus the scope/gamerTag needed to authenticate (§4.2/§4.3);
/// otherwise the notification isn't part of a tracked campaign and is ignored. The POST is
/// authenticated with the player bearer token persisted in the App Group via `SharedConfig`.
///
/// Opt in by listing `AnalyticsPlugin` in the app's `BMNPlugins` Info.plist array, or by
/// `PluginRegistry.shared.register(AnalyticsPlugin())`. Teams can replace it with their own
/// analytics plugin without changing the SDK core.
public final class AnalyticsPlugin: NSObject, NotificationPlugin {

    public var id: String { "com.beamable.notifications.analytics" }

    private let session: URLSession
    private let shared: SharedConfig

    public override convenience init() {
        self.init(session: .shared, shared: .shared)
    }

    public init(session: URLSession = .shared, shared: SharedConfig = .shared) {
        self.session = session
        self.shared = shared
        super.init()
    }

    public func onNotificationReceived(_ note: NotificationData) {
        // Foreground receipt while the app is alive.
        emit(.received, note: note)
    }

    public func onNotificationTapped(_ note: NotificationData, actionId: String?) {
        // A tap on the notification itself is the "Opened" funnel stage (§4.5). In-app offer
        // clicks ("Clicked"/"Converted") are emitted separately by the offer-tracking helpers.
        emit(.opened, note: note)
    }

    private func emit(_ type: FunnelType, note: NotificationData) {
        let intent = note.campaignIntent
        guard let event = BeamableAnalytics.makeEvent(type, intent: intent) else { return }
        // App is alive: fire-and-forget, no persist-on-failure (no NSE deadline here).
        BeamableAnalytics.emit(event, shared: shared, session: session, persistOnFailure: false)
    }
}
