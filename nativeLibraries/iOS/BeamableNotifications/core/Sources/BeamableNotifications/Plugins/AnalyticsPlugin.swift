import Foundation
import UIKit

/// Reference plugin: reports notifications received *while the app is alive* to the
/// analytics endpoint configured via `bmn_configureAnalytics`. The closed-app case is
/// handled separately by the NSE (see extension/). Together they give full receipt
/// coverage. Opt in by listing `AnalyticsPlugin` in the app's `BMNPlugins` Info.plist
/// array, or by `PluginRegistry.shared.register(AnalyticsPlugin())`.
///
/// This ships as a plugin on purpose: teams can replace it with their own analytics
/// backend by writing a different plugin, with no change to the SDK core.
public final class AnalyticsPlugin: NSObject, NotificationPlugin {

    public var id: String { "com.beamable.notifications.analytics" }

    private let session: URLSession
    private let configProvider: () -> AnalyticsConfig?

    public override convenience init() {
        self.init(session: .shared, configProvider: { SharedConfig.shared.loadAnalyticsConfig() })
    }

    public init(session: URLSession = .shared,
                configProvider: @escaping () -> AnalyticsConfig?) {
        self.session = session
        self.configProvider = configProvider
        super.init()
    }

    public func onNotificationReceived(_ note: NotificationData) {
        send(event: "received", note: note, wasForeground: isAppForeground)
    }

    public func onNotificationTapped(_ note: NotificationData, actionId: String?) {
        // A tap means the app was backgrounded/closed when the notification arrived.
        send(event: "opened", note: note, wasForeground: false)
    }

    /// Best-effort foreground check. Reads `applicationState` on the main thread (where
    /// these callbacks fire); falls back to `false` if ever invoked off-main.
    private var isAppForeground: Bool {
        guard Thread.isMainThread else { return false }
        return UIApplication.shared.applicationState == .active
    }

    private func send(event: String, note: NotificationData, wasForeground: Bool) {
        guard let config = configProvider(), config.enabled,
              let url = URL(string: config.endpoint) else { return }

        // Raw payload, minus the APNs envelope, to mirror Android's `data` field.
        var data = note.userInfo
        data.removeValue(forKey: "aps")

        var payload = AnalyticsPayload.make(
            event: event,
            source: "app",
            notificationId: note.id,
            title: note.title,
            body: note.body,
            deepLink: note.deepLink ?? note.userInfo.bmnDeepLink,
            wasForeground: wasForeground,
            receivedAtMillis: Int64(Date().timeIntervalSince1970 * 1000),
            data: data,
            config: config
        )
        if let actionId = note.actionId { payload["actionId"] = .string(actionId) }

        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        config.headers?.forEach { request.setValue($0.value, forHTTPHeaderField: $0.key) }
        request.httpBody = try? JSON.encoder.encode(payload)

        session.dataTask(with: request).resume()
    }
}
