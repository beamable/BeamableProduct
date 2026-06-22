import Foundation

/// Builds the JSON body sent to the analytics endpoint by both the in-app `AnalyticsPlugin`
/// and the closed-app NSE `AnalyticsServicePlugin`.
///
/// Historically this only carried `event` / `notificationId` / `source`, so a webhook that
/// renders a single `message` field (the React Native sample's Slack trigger) showed no
/// real context. This builder mirrors the Android receive-time handler: it emits the full
/// set of structured fields *and* composes a human-readable, multi-line `message` so the
/// same webhook shows the title, body, deep link, foreground flag, receipt time, and the
/// raw data payload — matching what Android posts.
///
/// `config.commonParams` are still merged in. A `message` entry there is treated as the
/// header line of the composed message (a label like "📬 …"), not a literal override, so
/// callers keep their label while gaining the per-notification context below it.
public enum AnalyticsPayload {

    public static func make(event: String,
                            source: String,
                            notificationId: String,
                            title: String?,
                            body: String?,
                            deepLink: String?,
                            wasForeground: Bool,
                            receivedAtMillis: Int64,
                            data: [String: JSONValue],
                            config: AnalyticsConfig) -> [String: JSONValue] {

        var payload: [String: JSONValue] = [
            "event": .string(event),
            "source": .string(source),
            "notificationId": .string(notificationId),
            "messageId": .string(notificationId),
            "wasForeground": .bool(wasForeground),
            "receivedAt": .number(Double(receivedAtMillis))
        ]
        if let title = title { payload["title"] = .string(title) }
        if let body = body { payload["body"] = .string(body) }
        if let deepLink = deepLink { payload["deepLink"] = .string(deepLink) }
        if !data.isEmpty { payload["data"] = .object(data) }

        var header: String?
        if let common = config.commonParams {
            for (key, value) in common where key != "message" { payload[key] = value }
            header = common["message"]?.stringValue
        }

        payload["message"] = .string(composeMessage(
            header: header,
            event: event,
            source: source,
            notificationId: notificationId,
            title: title,
            body: body,
            deepLink: deepLink,
            wasForeground: wasForeground,
            receivedAtMillis: receivedAtMillis,
            data: data
        ))
        return payload
    }

    /// Mirrors `BeamablePushReceivedHandler.buildContent` on Android so both platforms post
    /// the same shape to the webhook.
    private static func composeMessage(header: String?,
                                       event: String,
                                       source: String,
                                       notificationId: String,
                                       title: String?,
                                       body: String?,
                                       deepLink: String?,
                                       wasForeground: Bool,
                                       receivedAtMillis: Int64,
                                       data: [String: JSONValue]) -> String {
        var lines: [String] = []
        lines.append(header ?? "📬 iOS: Beamable push \(event) (native handler)")
        lines.append("event: \(event)")
        lines.append("source: \(source)")
        lines.append("messageId: \(notificationId)")
        if let title = title { lines.append("title: \(title)") }
        if let body = body { lines.append("body: \(body)") }
        lines.append("deepLink: \(deepLink ?? "null")")
        lines.append("wasForeground: \(wasForeground)")
        lines.append("receivedAt: \(receivedAtMillis)")
        lines.append("data: \(JSON.encode(JSONValue.object(data)))")
        return lines.joined(separator: "\n")
    }
}
