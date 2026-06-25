import Foundation
import UserNotifications
import UIKit

/// The heart of the SDK. A singleton that owns the `UNUserNotificationCenter` delegate,
/// schedules local notifications, requests permission, and routes every event through
/// the plugin registry (observe + transform) before forwarding to the engine callbacks.
///
/// Engine wrappers interact with this either via the C ABI (`CABI.swift`, used by
/// Unity/Unreal) or directly in Swift (React Native module).
public final class NotificationManager: NSObject {

    public static let shared = NotificationManager()

    private let center = UNUserNotificationCenter.current()
    private var initialized = false

    // MARK: Event callbacks (closures; CABI wires C function pointers into these)

    public var onPermissionResult: ((PermissionResult) -> Void)?
    public var onTokenReceived: ((String) -> Void)?
    public var onTokenError: ((String) -> Void)?
    public var onNotificationPresented: ((NotificationData) -> Void)?
    public var onNotificationReceived: ((NotificationData) -> Void)?
    /// JSON array string of pending NotificationData.
    public var onPendingNotifications: ((String) -> Void)?
    /// JSON array string of DeliveryReceipt.
    public var onDeliveryReceipts: ((String) -> Void)?

    /// Setting this flushes any taps that were queued before a callback existed
    /// (e.g. a cold-start tap delivered before the engine booted).
    public var onNotificationTapped: ((NotificationData) -> Void)? {
        didSet {
            guard onNotificationTapped != nil else { return }
            let queued = LaunchTracker.shared.drainPendingTaps()
            for note in queued { dispatch { self.onNotificationTapped?(note) } }
        }
    }

    // MARK: Lifecycle

    public func initialize() {
        guard !initialized else { return }
        initialized = true
        reassertDelegate()
        PluginRegistry.shared.discoverAndRegisterFromInfoPlist()
        PluginRegistry.shared.dispatchInitialize(PluginContext(manager: self))
        RemotePush.shared.installSwizzlingIfNeeded()
        flushPendingFunnel()
    }

    /// Replay funnel events the NSE persisted because it couldn't authenticate/send them in
    /// its budget (§4.3 fallback). Drained and POSTed (authenticated) now that the app is alive.
    private func flushPendingFunnel() {
        let pending = SharedConfig.shared.drainPendingFunnel()
        guard !pending.isEmpty else { return }
        for event in pending {
            // No persist-on-failure: a second failure just drops it (already a best-effort replay).
            BeamableAnalytics.emit(event, persistOnFailure: false)
        }
    }

    /// Make sure we are the notification-center delegate. Host engines (notably Unreal's
    /// IOSAppDelegate) may set themselves as the `UNUserNotificationCenter` delegate during
    /// launch, which would suppress our foreground banners and tap routing. We claim it
    /// here and re-claim before scheduling / requesting permission.
    private func reassertDelegate() {
        if center.delegate !== self {
            center.delegate = self
        }
    }

    // MARK: Permission (feature 5)

    public func requestPermission(_ options: PermissionOptions) {
        NSLog("[BeamableNotifications] requestPermission() called")
        var authOptions: UNAuthorizationOptions = []
        if options.alert ?? true { authOptions.insert(.alert) }
        if options.badge ?? true { authOptions.insert(.badge) }
        if options.sound ?? true { authOptions.insert(.sound) }
        if options.provisional == true { authOptions.insert(.provisional) }
        if options.criticalAlert == true { authOptions.insert(.criticalAlert) }
        if options.carPlay == true { authOptions.insert(.carPlay) }

        reassertDelegate()
        center.requestAuthorization(options: authOptions) { [weak self] granted, error in
            if let error = error {
                NSLog("[BeamableNotifications] requestAuthorization error: %@", error.localizedDescription)
            }
            self?.emitPermissionStatus(grantedHint: granted)
        }
    }

    public func getPermissionStatus() {
        emitPermissionStatus(grantedHint: nil)
    }

    private func emitPermissionStatus(grantedHint: Bool?) {
        center.getNotificationSettings { [weak self] settings in
            guard let self = self else { return }
            let result = PermissionResult(
                status: Self.statusString(settings.authorizationStatus),
                granted: grantedHint ?? (settings.authorizationStatus == .authorized
                                         || settings.authorizationStatus == .provisional
                                         || settings.authorizationStatus == .ephemeral),
                alert: settings.alertSetting == .enabled,
                badge: settings.badgeSetting == .enabled,
                sound: settings.soundSetting == .enabled
            )
            NSLog("[BeamableNotifications] permission status=%@ granted=%@ alert=%@",
                  result.status, result.granted ? "true" : "false",
                  (result.alert ?? false) ? "true" : "false")
            PluginRegistry.shared.dispatchPermissionResult(settings.authorizationStatus)
            self.dispatch { self.onPermissionResult?(result) }
        }
    }

    private static func statusString(_ status: UNAuthorizationStatus) -> String {
        switch status {
        case .notDetermined: return "notDetermined"
        case .denied: return "denied"
        case .authorized: return "authorized"
        case .provisional: return "provisional"
        case .ephemeral: return "ephemeral"
        @unknown default: return "unknown"
        }
    }

    // MARK: Local notifications (feature 1)

    public func scheduleLocal(_ rawRequest: LocalRequest) {
        reassertDelegate()
        // 1) Apply template defaults, 2) let plugins transform (or drop).
        var request = TemplateStore.shared.resolve(rawRequest)
        guard let transformed = PluginRegistry.shared.transformWillSchedule(request) else {
            NSLog("[BeamableNotifications] schedule '%@' dropped by a plugin", rawRequest.id)
            return // a plugin dropped the request
        }
        request = transformed

        let content = buildContent(from: request)
        let trigger = buildTrigger(from: request.trigger)
        let unRequest = UNNotificationRequest(identifier: request.id, content: content, trigger: trigger)
        center.add(unRequest) { error in
            if let error = error {
                NSLog("[BeamableNotifications] failed to schedule '%@': %@", request.id, error.localizedDescription)
            }
        }
    }

    public func cancelLocal(id: String) {
        center.removePendingNotificationRequests(withIdentifiers: [id])
    }

    public func cancelAllLocal() {
        center.removeAllPendingNotificationRequests()
    }

    public func getPending() {
        center.getPendingNotificationRequests { [weak self] requests in
            guard let self = self else { return }
            let items = requests.map { NotificationData(fromRequest: $0) }
            let json = JSON.encode(items)
            self.dispatch { self.onPendingNotifications?(json) }
        }
    }

    public func setBadge(_ count: Int) {
        DispatchQueue.main.async {
            if #available(iOS 16.0, *) {
                self.center.setBadgeCount(count, withCompletionHandler: nil)
            } else {
                UIApplication.shared.applicationIconBadgeNumber = count
            }
        }
    }

    public func clearDelivered() {
        center.removeAllDeliveredNotifications()
    }

    // MARK: Beamable analytics auth + offer funnel (§4.7)

    /// Persist the player bearer token + realm routing for native funnel POSTs. The engine
    /// SDK calls this on login/refresh; `clearAuth()` on logout.
    public func configureAuth(_ config: AuthConfig) {
        SharedConfig.shared.saveAuthConfig(config)
        // Creds just arrived — replay any funnel events the NSE (or a prior app-path failure)
        // persisted because they couldn't authenticate, now that we can.
        flushPendingFunnel()
    }

    /// Convenience overload taking the canonical `AuthConfig` JSON string. Used by the React
    /// Native bridge (which talks to this manager directly in Swift) and mirrors the C ABI
    /// `bmn_configureAuth` decode path. A malformed/undecodable string is logged and ignored
    /// (no-op, never crashes), matching Android's `configureAuth(_, json)` lenient behavior.
    public func configureAuth(_ json: String) {
        guard let config = JSON.decode(AuthConfig.self, from: json) else {
            NSLog("[BeamableNotifications] configureAuth: ignoring malformed AuthConfig JSON")
            return
        }
        configureAuth(config)
    }

    public func clearAuth() {
        SharedConfig.shared.clearAuthConfig()
    }

    /// Emit a **Clicked** funnel event for an in-app offer click (§4.7), attributed to the
    /// originating campaign.
    public func trackOfferClicked(_ request: OfferTrackRequest) {
        emitOfferFunnel(.clicked, request: request)
    }

    /// Emit a **Converted** funnel event when an offer click results in a conversion (§4.7).
    public func trackOfferConverted(_ request: OfferTrackRequest) {
        emitOfferFunnel(.converted, request: request)
    }

    private func emitOfferFunnel(_ type: FunnelType, request: OfferTrackRequest) {
        let auth = SharedConfig.shared.loadAuthConfig()
        let intent = request.intent(fallbackAuth: auth)
        guard let event = BeamableAnalytics.makeEvent(type, intent: intent, offer: request.offer) else {
            NSLog("[BeamableNotifications] trackOffer %@ skipped: missing campaign/scope", type.rawValue)
            return
        }
        BeamableAnalytics.emit(event, persistOnFailure: false)
    }

    // MARK: Delivery receipts (feature 8)

    public func emitDeliveryReceipts() {
        let receipts = SharedConfig.shared.drainReceipts()
        guard !receipts.isEmpty else { return }
        let json = JSON.encode(receipts)
        dispatch { self.onDeliveryReceipts?(json) }
    }

    // MARK: Content & trigger building

    private func buildContent(from request: LocalRequest) -> UNMutableNotificationContent {
        let content = UNMutableNotificationContent()
        if let title = request.title { content.title = title }
        if let body = request.body { content.body = body }
        if let subtitle = request.subtitle { content.subtitle = subtitle }
        if let badge = request.badge { content.badge = NSNumber(value: badge) }
        if let categoryId = request.categoryId { content.categoryIdentifier = categoryId }
        if let threadId = request.threadId { content.threadIdentifier = threadId }

        switch request.sound {
        case .none, "default": content.sound = .default
        case "none": content.sound = nil
        case let name?: content.sound = UNNotificationSound(named: UNNotificationSoundName(name))
        }

        if #available(iOS 15.0, *), let level = request.interruptionLevel {
            switch level {
            case "passive": content.interruptionLevel = .passive
            case "active": content.interruptionLevel = .active
            case "timeSensitive": content.interruptionLevel = .timeSensitive
            case "critical": content.interruptionLevel = .critical
            default: break
            }
        }

        var userInfo: [String: Any] = [:]
        if let ui = request.userInfo {
            for (k, v) in ui { userInfo[k] = v.foundationValue }
        }
        content.userInfo = userInfo

        if let attachments = request.attachments {
            content.attachments = attachments.compactMap { spec in
                guard let url = Self.fileURL(from: spec.url) else { return nil }
                return try? UNNotificationAttachment(identifier: spec.identifier ?? UUID().uuidString,
                                                     url: url, options: nil)
            }
        }
        return content
    }

    private static func fileURL(from path: String) -> URL? {
        if path.hasPrefix("file://") { return URL(string: path) }
        return URL(fileURLWithPath: path)
    }

    private func buildTrigger(from spec: TriggerSpec?) -> UNNotificationTrigger? {
        guard let spec = spec else { return nil }
        switch spec.type {
        case .immediate:
            return nil
        case .timeInterval:
            let seconds = max(spec.seconds ?? 1, (spec.repeats == true) ? 60 : 0.1)
            return UNTimeIntervalNotificationTrigger(timeInterval: seconds, repeats: spec.repeats ?? false)
        case .calendar:
            var components = DateComponents()
            components.year = spec.year
            components.month = spec.month
            components.day = spec.day
            components.hour = spec.hour
            components.minute = spec.minute
            components.second = spec.second
            components.weekday = spec.weekday
            return UNCalendarNotificationTrigger(dateMatching: components, repeats: spec.repeats ?? false)
        }
    }

    // MARK: Helpers

    private func dispatch(_ block: @escaping () -> Void) {
        if Thread.isMainThread { block() } else { DispatchQueue.main.async(execute: block) }
    }
}

// MARK: - UNUserNotificationCenterDelegate

extension NotificationManager: UNUserNotificationCenterDelegate {

    /// Foreground delivery. We let plugins decide presentation options and surface both
    /// "presented" and "received" events.
    public func userNotificationCenter(_ center: UNUserNotificationCenter,
                                       willPresent notification: UNNotification,
                                       withCompletionHandler completionHandler: @escaping (UNNotificationPresentationOptions) -> Void) {
        let data = NotificationData(fromNotification: notification)
        PluginRegistry.shared.dispatchNotificationReceived(data)
        dispatch {
            self.onNotificationPresented?(data)
            self.onNotificationReceived?(data)
        }

        let options = PluginRegistry.shared.transformWillPresent(data) ?? defaultPresentationOptions()
        completionHandler(options)
    }

    /// User tapped the notification or an action button.
    public func userNotificationCenter(_ center: UNUserNotificationCenter,
                                       didReceive response: UNNotificationResponse,
                                       withCompletionHandler completionHandler: @escaping () -> Void) {
        var data = NotificationData(fromNotification: response.notification)
        if response.actionIdentifier != UNNotificationDefaultActionIdentifier,
           response.actionIdentifier != UNNotificationDismissActionIdentifier {
            data.actionId = response.actionIdentifier
        }

        // Capture as the launch notification for "get intent" (first one wins).
        LaunchTracker.shared.setLaunchNotification(data)
        PluginRegistry.shared.dispatchNotificationTapped(data, actionId: data.actionId)

        if let handler = onNotificationTapped {
            dispatch { handler(data) }
        } else {
            // No callback yet (cold start) — queue and flush when one is registered.
            LaunchTracker.shared.enqueueTap(data)
        }
        completionHandler()
    }

    private func defaultPresentationOptions() -> UNNotificationPresentationOptions {
        if #available(iOS 14.0, *) {
            return [.banner, .list, .sound, .badge]
        } else {
            return [.alert, .sound, .badge]
        }
    }
}

// MARK: - NotificationData bridging

extension NotificationData {
    init(fromNotification notification: UNNotification) {
        self.init(fromContent: notification.request.content, id: notification.request.identifier)
    }

    init(fromRequest request: UNNotificationRequest) {
        self.init(fromContent: request.content, id: request.identifier)
    }

    init(fromContent content: UNNotificationContent, id: String) {
        var info: [String: JSONValue] = [:]
        for (k, v) in content.userInfo { info["\(k)"] = JSONValue(any: v) }
        let deepLink = info.bmnDeepLink
        // Lift the campaign intent-data schema (§3.3) so engines get the full context, not
        // just the deep link. Parsing here means cold-start launch payloads carry it too.
        let intent = info.bmnCampaignIntent
        self.init(
            id: id,
            title: content.title.isEmpty ? nil : content.title,
            body: content.body.isEmpty ? nil : content.body,
            subtitle: content.subtitle.isEmpty ? nil : content.subtitle,
            deepLink: deepLink,
            campaignId: intent.campaignId,
            nodeId: intent.nodeId,
            gamerTag: intent.gamerTag,
            accountId: intent.accountId,
            cidPid: intent.cidPid,
            offers: intent.offers,
            campaignData: intent.campaignData,
            userInfo: info
        )
    }
}
