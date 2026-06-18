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
    /// Cap so a long offline streak can't grow the store unbounded.
    private static let maxReceipts = 200

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
