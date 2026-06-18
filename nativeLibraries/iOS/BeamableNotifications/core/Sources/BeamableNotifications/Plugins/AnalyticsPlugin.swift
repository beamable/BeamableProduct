import Foundation

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
        send(event: "received", note: note)
    }

    public func onNotificationTapped(_ note: NotificationData, actionId: String?) {
        send(event: "opened", note: note)
    }

    private func send(event: String, note: NotificationData) {
        guard let config = configProvider(), config.enabled,
              let url = URL(string: config.endpoint) else { return }

        var payload: [String: JSONValue] = [
            "event": .string(event),
            "notificationId": .string(note.id),
            "source": .string("app")
        ]
        if let deepLink = note.deepLink { payload["deepLink"] = .string(deepLink) }
        if let actionId = note.actionId { payload["actionId"] = .string(actionId) }
        if let common = config.commonParams { for (k, v) in common { payload[k] = v } }

        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        config.headers?.forEach { request.setValue($0.value, forHTTPHeaderField: $0.key) }
        request.httpBody = try? JSON.encoder.encode(payload)

        session.dataTask(with: request).resume()
    }
}
