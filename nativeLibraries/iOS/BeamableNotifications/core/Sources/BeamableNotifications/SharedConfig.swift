import Foundation

/// Shared storage backed by an App Group, readable from both the main app and the
/// Notification Service Extension (separate processes). Holds the analytics config
/// written by `bmn_configureAnalytics` and the delivery receipts logged by the NSE
/// when the app is closed.
///
/// The App Group identifier is resolved at runtime from the `BMNAppGroup` key in the
/// host target's `Info.plist`. If absent, all operations degrade to no-ops (so the SDK
/// still works without closed-app analytics).
public final class SharedConfig {

    public static let shared = SharedConfig()

    private static let analyticsKey = "bmn.analyticsConfig"
    private static let receiptsKey = "bmn.deliveryReceipts"
    private static let authKey = "bmn.authConfig"
    private static let pendingFunnelKey = "bmn.pendingFunnelEvents"
    /// Cap so a long offline streak can't grow the store unbounded.
    private static let maxReceipts = 200
    /// Cap on persisted-for-replay funnel events (NSE fallback path, §4.3).
    private static let maxPendingFunnel = 200

    private let defaults: UserDefaults?
    public let appGroupId: String?

    public init(infoDictionary: [String: Any]? = Bundle.main.infoDictionary) {
        let groupId = infoDictionary?["BMNAppGroup"] as? String
        self.appGroupId = groupId
        if let groupId = groupId, !groupId.isEmpty {
            self.defaults = UserDefaults(suiteName: groupId)
        } else {
            self.defaults = nil
        }
    }

    public var isAvailable: Bool { defaults != nil }

    // MARK: Analytics config

    public func saveAnalyticsConfig(_ config: AnalyticsConfig) {
        guard let defaults = defaults,
              let data = try? JSON.encoder.encode(config) else { return }
        defaults.set(data, forKey: Self.analyticsKey)
    }

    public func loadAnalyticsConfig() -> AnalyticsConfig? {
        guard let defaults = defaults,
              let data = defaults.data(forKey: Self.analyticsKey),
              let config = try? JSON.decoder.decode(AnalyticsConfig.self, from: data) else {
            return nil
        }
        return config
    }

    // MARK: Auth config (§4.3) — player bearer token + realm routing for the funnel POST.

    public func saveAuthConfig(_ config: AuthConfig) {
        guard let defaults = defaults,
              let data = try? JSON.encoder.encode(config) else { return }
        defaults.set(data, forKey: Self.authKey)
    }

    public func loadAuthConfig() -> AuthConfig? {
        guard let defaults = defaults,
              let data = defaults.data(forKey: Self.authKey),
              let config = try? JSON.decoder.decode(AuthConfig.self, from: data) else {
            return nil
        }
        return config
    }

    /// Update the persisted tokens after a refresh (called by the native auth helper), so a
    /// freshly-minted access token survives back into shared storage. Other fields are kept.
    public func updateTokens(accessToken: String, refreshToken: String?, expiresAt: Double?) {
        var config = loadAuthConfig() ?? AuthConfig()
        config.accessToken = accessToken
        if let refreshToken = refreshToken { config.refreshToken = refreshToken }
        // Only overwrite the expiry when the refresh response carried a valid new one. A nil
        // (or non-positive) `expiresAt` means `expires_in` was absent/<=0 — keep the prior
        // stored expiry rather than wiping it (matches Android's `if (expiresInMs > 0)`).
        if let expiresAt = expiresAt, expiresAt > 0 {
            config.accessTokenExpiresAt = expiresAt
        }
        saveAuthConfig(config)
    }

    public func clearAuthConfig() {
        defaults?.removeObject(forKey: Self.authKey)
    }

    // MARK: Pending funnel events (§4.3 NSE fallback / persist-and-replay)

    /// Append a funnel event to be flushed (authenticated) on next app open. Used when a
    /// closed-app POST can't complete inside the NSE's ~30s budget.
    public func appendPendingFunnel(_ event: FunnelEvent) {
        guard let defaults = defaults else { return }
        var events = loadPendingFunnel()
        // Dedup by stable key so the NSE safety-timer persist and `emit`'s persist-on-failure
        // can't enqueue (and later replay) the SAME funnel stage twice (mirrors appendReceipt).
        guard !events.contains(where: { $0.dedupKey == event.dedupKey }) else { return }
        events.append(event)
        if events.count > Self.maxPendingFunnel {
            events.removeFirst(events.count - Self.maxPendingFunnel)
        }
        if let data = try? JSON.encoder.encode(events) {
            defaults.set(data, forKey: Self.pendingFunnelKey)
        }
    }

    public func loadPendingFunnel() -> [FunnelEvent] {
        guard let defaults = defaults,
              let data = defaults.data(forKey: Self.pendingFunnelKey),
              let events = try? JSON.decoder.decode([FunnelEvent].self, from: data) else {
            return []
        }
        return events
    }

    /// Return and clear all pending funnel events. Called by the app on launch to replay them.
    public func drainPendingFunnel() -> [FunnelEvent] {
        let events = loadPendingFunnel()
        defaults?.removeObject(forKey: Self.pendingFunnelKey)
        return events
    }

    // MARK: Delivery receipts

    /// Append a receipt, deduping by id and trimming to the cap. Safe to call from the NSE.
    public func appendReceipt(_ receipt: DeliveryReceipt) {
        guard let defaults = defaults else { return }
        var receipts = loadReceipts()
        guard !receipts.contains(where: { $0.id == receipt.id }) else { return }
        receipts.append(receipt)
        if receipts.count > Self.maxReceipts {
            receipts.removeFirst(receipts.count - Self.maxReceipts)
        }
        if let data = try? JSON.encoder.encode(receipts) {
            defaults.set(data, forKey: Self.receiptsKey)
        }
    }

    public func loadReceipts() -> [DeliveryReceipt] {
        guard let defaults = defaults,
              let data = defaults.data(forKey: Self.receiptsKey),
              let receipts = try? JSON.decoder.decode([DeliveryReceipt].self, from: data) else {
            return []
        }
        return receipts
    }

    /// Return and clear all pending receipts. Called by the app on launch to replay them.
    public func drainReceipts() -> [DeliveryReceipt] {
        let receipts = loadReceipts()
        defaults?.removeObject(forKey: Self.receiptsKey)
        return receipts
    }
}
