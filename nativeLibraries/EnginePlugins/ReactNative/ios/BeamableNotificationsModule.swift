import Foundation
import React
// The Swift core is now consumed as a PREBUILT xcframework (vendored by the podspec,
// Decision Q2) rather than compiled from a vendored source mirror. It is therefore a
// separate Swift module, so its public types (NotificationManager, LocalRequest, JSON,
// OfferTrackRequest, …) must be imported.
import BeamableNotifications

/// React Native bridge. Unlike Unity/Unreal this talks to the Swift core directly
/// (no C ABI). Methods accept plain JS objects (NSDictionary) which are re-encoded to
/// JSON and decoded into the core's Codable models. Events are emitted via
/// RCTEventEmitter; the JS side subscribes with NativeEventEmitter.
@objc(BeamableNotificationsModule)
final class BeamableNotificationsModule: RCTEventEmitter {

    private var hasListenersFlag = false
    // Events emitted before JS attaches its first listener are buffered here and
    // replayed in `startObserving`, rather than dropped. This is what lets a cold-start
    // notification tap reach JS: the OS delivers the tap during launch and the core
    // flushes it from `LaunchTracker` the instant `initialize()` wires the callback —
    // which is before the React `useEffect` finishes calling `addListener`. Without the
    // buffer that `notificationTapped` would be lost (the iOS-only gap vs Android).
    private var pendingEvents: [(String, Any?)] = []
    private let eventLock = NSLock()
    private static let maxBufferedEvents = 32   // safety cap if a host never subscribes

    override static func requiresMainQueueSetup() -> Bool { true }

    override func supportedEvents() -> [String]! {
        return [
            "permissionResult", "tokenReceived", "tokenError",
            "notificationPresented", "notificationReceived", "notificationTapped",
            "pendingNotifications", "deliveryReceipts"
        ]
    }

    override func startObserving() {
        eventLock.lock()
        hasListenersFlag = true
        let buffered = pendingEvents
        pendingEvents.removeAll()
        eventLock.unlock()
        // Replay events that arrived before JS attached its first listener. sendEvent
        // delivers asynchronously on the next JS tick, by which point every addListener
        // in the mounting effect (incl. notificationOpened) is registered.
        for (name, body) in buffered { sendEvent(withName: name, body: body) }
    }

    override func stopObserving() {
        eventLock.lock(); hasListenersFlag = false; eventLock.unlock()
    }

    private func emit(_ name: String, _ body: Any?) {
        eventLock.lock()
        if !hasListenersFlag {
            if pendingEvents.count < Self.maxBufferedEvents {
                pendingEvents.append((name, body))
            }
            eventLock.unlock()
            return
        }
        eventLock.unlock()
        sendEvent(withName: name, body: body)
    }

    // MARK: Lifecycle

    /// Claim the `UNUserNotificationCenter` delegate during app launch.
    ///
    /// iOS only guarantees that a *cold-start* notification tap (the app launched by
    /// tapping a push) is delivered to a delegate that was assigned while the app was
    /// launching. The JS `initialize()` below runs from a React `useEffect`, which is far
    /// too late — by then the launch tap is already gone. So `BMNLaunchInstaller` (an ObjC
    /// `+load` shim) calls this on `UIApplicationDidFinishLaunchingNotification`, before the
    /// run loop delivers that tap. `NotificationManager` then captures it into
    /// `LaunchTracker`, and the JS `initialize()` flushes it once the callbacks are wired.
    @objc static func bmnInstallAtLaunch() {
        // Register the default app-side funnel analytics plugin BEFORE initialize(), so a
        // tapped notification emits the "Opened" funnel stage. This mirrors the NSE, which
        // hardcodes AnalyticsServicePlugin for "Received": without an app-side counterpart
        // the funnel is only half-wired (Received reports, Opened never does). Registration
        // dedupes by plugin id, so calling it here and in `initialize()` is safe.
        PluginRegistry.shared.register(AnalyticsPlugin())
        NotificationManager.shared.initialize()
    }

    @objc func initialize() {
        let m = NotificationManager.shared
        // Belt-and-suspenders: also register on the warm/JS-driven path in case the launch
        // shim didn't run. Deduped by plugin id (no double-emit).
        PluginRegistry.shared.register(AnalyticsPlugin())
        m.onPermissionResult = { [weak self] in self?.emit("permissionResult", Self.object($0)) }
        m.onTokenReceived = { [weak self] in self?.emit("tokenReceived", ["token": $0]) }
        m.onTokenError = { [weak self] in self?.emit("tokenError", ["error": $0]) }
        m.onNotificationPresented = { [weak self] in self?.emit("notificationPresented", Self.object($0)) }
        m.onNotificationReceived = { [weak self] in self?.emit("notificationReceived", Self.object($0)) }
        m.onNotificationTapped = { [weak self] in self?.emit("notificationTapped", Self.object($0)) }
        m.onPendingNotifications = { [weak self] in self?.emit("pendingNotifications", Self.array($0)) }
        m.onDeliveryReceipts = { [weak self] in self?.emit("deliveryReceipts", Self.array($0)) }
        m.initialize()
    }

    // MARK: Permission (feature 5)

    @objc(requestPermission:)
    func requestPermission(_ options: NSDictionary) {
        let opts = decode(PermissionOptions.self, options) ?? PermissionOptions()
        NotificationManager.shared.requestPermission(opts)
    }

    @objc func getPermissionStatus() { NotificationManager.shared.getPermissionStatus() }

    // MARK: Local notifications (feature 1)

    @objc(scheduleLocal:)
    func scheduleLocal(_ request: NSDictionary) {
        guard let req = decode(LocalRequest.self, request) else { return }
        NotificationManager.shared.scheduleLocal(req)
    }

    @objc(cancelLocal:)
    func cancelLocal(_ id: NSString) { NotificationManager.shared.cancelLocal(id: id as String) }

    @objc func cancelAllLocal() { NotificationManager.shared.cancelAllLocal() }
    @objc func getPending() { NotificationManager.shared.getPending() }

    // MARK: Remote notifications (feature 2)

    @objc func registerForRemote() { RemotePush.shared.register() }
    @objc func unregisterForRemote() { RemotePush.shared.unregister() }

    // MARK: Templates / categories (4, 7)

    @objc(registerTemplate:)
    func registerTemplate(_ template: NSDictionary) {
        guard let spec = decode(TemplateSpec.self, template) else { return }
        TemplateStore.shared.register(spec)
    }

    @objc(registerCategory:)
    func registerCategory(_ category: NSDictionary) {
        guard let spec = decode(CategorySpec.self, category) else { return }
        CategoryStore.shared.register(spec)
    }

    @objc func getDeliveryReceipts() { NotificationManager.shared.emitDeliveryReceipts() }

    // MARK: Badge

    @objc(setBadge:)
    func setBadge(_ count: NSNumber) { NotificationManager.shared.setBadge(count.intValue) }

    @objc func clearDelivered() { NotificationManager.shared.clearDelivered() }

    // MARK: Get intent (feature 6)

    @objc(getLaunchNotification:rejecter:)
    func getLaunchNotification(_ resolve: RCTPromiseResolveBlock, rejecter reject: RCTPromiseRejectBlock) {
        if let data = LaunchTracker.shared.launchNotification {
            resolve(Self.object(data))
        } else {
            resolve(NSNull())
        }
    }

    // MARK: Offer / conversion funnel (feature §4.7)
    // New bridge methods (additive) — the core already exposes the API via the C ABI
    // (bmn_trackOfferClicked / bmn_trackOfferConverted) and NotificationManager; here we
    // surface it to React Native. The JS arg is an `OfferTrackRequest` JSON string
    // (campaign context + the single offer), matching the iOS core model.

    @objc(trackOfferClicked:)
    func trackOfferClicked(_ requestJson: NSString) {
        guard let req = decodeJson(OfferTrackRequest.self, requestJson as String) else { return }
        NotificationManager.shared.trackOfferClicked(req)
    }

    @objc(trackOfferConverted:)
    func trackOfferConverted(_ requestJson: NSString) {
        guard let req = decodeJson(OfferTrackRequest.self, requestJson as String) else { return }
        NotificationManager.shared.trackOfferConverted(req)
    }

    // MARK: Auth (closed-app analytics funnel)
    // Writes the player's Beamable tokens into native shared storage so the closed-app
    // analytics funnel can authenticate when the app is not running. The JS arg is a single
    // JSON string with the canonical camelCase contract:
    //   { accessToken, refreshToken, accessTokenExpiresAt (epoch ms), cid, pid, host }

    @objc(configureAuth:)
    func configureAuth(_ json: NSString) {
        NotificationManager.shared.configureAuth(json as String)
    }

    @objc func clearAuth() {
        NotificationManager.shared.clearAuth()
    }

    private func decodeJson<T: Decodable>(_ type: T.Type, _ json: String) -> T? {
        guard let data = json.data(using: .utf8) else { return nil }
        return try? JSONDecoder().decode(type, from: data)
    }

    // MARK: Encoding helpers

    private func decode<T: Decodable>(_ type: T.Type, _ dict: NSDictionary) -> T? {
        guard let data = try? JSONSerialization.data(withJSONObject: dict) else { return nil }
        return try? JSONDecoder().decode(type, from: data)
    }

    private static func object<T: Encodable>(_ value: T) -> Any? {
        guard let data = try? JSON.encoder.encode(value) else { return nil }
        return try? JSONSerialization.jsonObject(with: data)
    }

    /// Convert a JSON-array string (pending / receipts) to a JS array.
    private static func array(_ jsonString: String) -> Any? {
        guard let data = jsonString.data(using: .utf8) else { return nil }
        return try? JSONSerialization.jsonObject(with: data)
    }
}
