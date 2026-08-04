import Foundation
import React
#if canImport(ActivityKit)
import ActivityKit
#endif
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
            "pendingNotifications", "deliveryReceipts",
            // Live Activity push-to-start (iOS 17.2+): the app forwards these tokens to the push rail
            // (`message-rail/register`) so the backend can start/update/end Live Activities via APNs.
            "liveActivityPushToStartToken", "liveActivityUpdateToken", "liveActivityStarted",
            // Whether this device/build can actually DRAW a Live Activity per attributes type. Emitted
            // at init and whenever the player toggles the Settings switch — `available: false` means
            // withdraw the token so the rail falls back to a notification with action buttons.
            "liveActivityCapability"
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
        // Live Activity events, produced by core's LiveActivityCoordinator. Bridge them BEFORE
        // `initialize()`, which starts the observation — otherwise the first push-to-start token can
        // land before anything is listening. `capability.available == false` is the app's cue to
        // WITHDRAW a token it registered earlier, so the rail stops sending Live Activities this build
        // can't draw and falls back to a notification instead.
        let la = LiveActivityCoordinator.shared
        la.onToken = { [weak self] event in
            self?.emit(event.kind == "pushToStart" ? "liveActivityPushToStartToken"
                                                   : "liveActivityUpdateToken",
                       Self.object(event))
        }
        la.onStarted = { [weak self] event in self?.emit("liveActivityStarted", Self.object(event)) }
        la.onCapability = { [weak self] caps in
            self?.emit("liveActivityCapability", Self.object(["capabilities": caps]))
        }
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

    // MARK: Offer / conversion funnel
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

    // MARK: Live Activity (ActivityKit) — a live countdown shown WITHOUT tap-and-hold
    // Unlike the Notification Content Extension (custom UI only in the expanded/long-press view),
    // a Live Activity renders on the Lock Screen / Dynamic Island and updates on its own. A pure
    // countdown needs NO push updates: the widget uses `Text(timerInterval:)`, so we only START it
    // with an absolute expiry. `expiresInSeconds` (Android-style) is converted to an absolute Date
    // once, at start — it does not restart. `expiresAtMs` is honored if provided.

    @objc(startCountdownLiveActivity:)
    func startCountdownLiveActivity(_ options: NSDictionary) {
        #if canImport(ActivityKit)
        if #available(iOS 16.1, *) {
            guard ActivityAuthorizationInfo().areActivitiesEnabled else {
                NSLog("[Beam] Live Activities are not enabled (Settings → Face ID & Passcode / per-app).")
                return
            }
            let title = (options["title"] as? String) ?? "Offer"
            let body = (options["body"] as? String) ?? ""
            let expiresAt: Date
            if let ms = (options["expiresAtMs"] as? NSNumber)?.doubleValue, ms > 0 {
                expiresAt = Date(timeIntervalSince1970: ms / 1000.0)
            } else {
                let secs = (options["expiresInSeconds"] as? NSNumber)?.doubleValue ?? 300
                expiresAt = Date().addingTimeInterval(secs)
            }
            let attributes = BeamCountdownActivityAttributes(title: title)
            let state = BeamCountdownActivityAttributes.ContentState(expiresAt: expiresAt, body: body)
            do {
                let activity: Activity<BeamCountdownActivityAttributes>
                if #available(iOS 16.2, *) {
                    activity = try Activity.request(
                        attributes: attributes,
                        content: .init(state: state, staleDate: expiresAt),
                        pushType: nil
                    )
                } else {
                    activity = try Activity.request(attributes: attributes, contentState: state, pushType: nil)
                }
                // Flip to the "expired" state at the deadline. A pure countdown ticks via
                // Text(timerInterval:), but to change the copy at zero we push a real content update
                // from the app (reliable, unlike context.isStale on the Simulator). This runs while
                // the app is alive; in production an APNs Live Activity update would drive it.
                let expiredState = BeamCountdownActivityAttributes.ContentState(
                    expiresAt: expiresAt, body: "Offer expired", isExpired: true
                )
                Task {
                    let delay = max(0, expiresAt.timeIntervalSinceNow)
                    try? await Task.sleep(nanoseconds: UInt64(delay * 1_000_000_000))
                    if #available(iOS 16.2, *) {
                        await activity.update(ActivityContent(state: expiredState, staleDate: nil))
                    } else {
                        await activity.update(using: expiredState)
                    }
                }
            } catch {
                NSLog("[Beam] startCountdownLiveActivity failed: \(error)")
            }
        }
        #endif
    }

    @objc func endCountdownLiveActivity() {
        #if canImport(ActivityKit)
        if #available(iOS 16.1, *) {
            Task {
                for activity in Activity<BeamCountdownActivityAttributes>.activities {
                    if #available(iOS 16.2, *) {
                        await activity.end(nil, dismissalPolicy: .immediate)
                    } else {
                        await activity.end(dismissalPolicy: .immediate)
                    }
                }
            }
        }
        #endif
    }

    // MARK: Live Activity push-to-start (ActivityKit, iOS 17.2+)
    // The backend (PushRailService) can START a Live Activity for a player it isn't already running,
    // and later UPDATE/END it, via APNs — but only if the device first mints and registers the tokens:
    //   • a per-attributes-type PUSH-TO-START token (Activity<T>.pushToStartTokenUpdates), and
    //   • a per-activity UPDATE token for each running activity (activity.pushTokenUpdates).
    // We observe both and emit them to JS, which forwards them to `message-rail/register`. The token
    // is the raw APNs token as lowercase hex — the same shape the regular device token uses.

    /// Kick off (or re-state) Live Activity observation. Idempotent, and now a thin delegation: the
    /// observation itself lives in core's `LiveActivityCoordinator` so every engine shares one
    /// implementation and one capability gate. `initialize()` already calls it via
    /// `NotificationManager.initialize()`; JS can call this again safely (e.g. after connecting a
    /// player) to have the current capability + tokens re-emitted.
    @objc func startLiveActivityPushRegistration() {
        LiveActivityCoordinator.shared.start()
    }

    /// Current Live Activity capability per attributes type, for JS that wants to check before
    /// registering rather than waiting for the event.
    @objc(getLiveActivityCapabilities)
    func getLiveActivityCapabilities() {
        emit("liveActivityCapability",
             Self.object(["capabilities": LiveActivityCoordinator.shared.capabilities()]))
    }

    // MARK: Live Activity local start (Simulator UI/button testing only)
    // Push-to-start can't run on the Simulator, so these let you exercise the widget UIs and the
    // interactive App Intent buttons locally. Production starts come from the rail via APNs.

    @objc(startActionsLiveActivity:)
    func startActionsLiveActivity(_ options: NSDictionary) {
        #if canImport(ActivityKit)
        if #available(iOS 16.1, *) {
            guard ActivityAuthorizationInfo().areActivitiesEnabled else {
                NSLog("[Beam] Live Activities are not enabled."); return
            }
            let title = (options["title"] as? String) ?? "Offer"
            let headline = (options["headline"] as? String) ?? "Limited-time offer"
            let body = (options["body"] as? String) ?? ""
            var buttons: [BeamLiveActivityButton] = []
            if let arr = options["buttons"] as? [NSDictionary] {
                buttons = arr.compactMap { d in
                    guard let id = d["id"] as? String, let t = d["title"] as? String else { return nil }
                    return BeamLiveActivityButton(id: id, title: t, role: (d["role"] as? String) ?? "default")
                }
            }
            if buttons.isEmpty {
                buttons = [BeamLiveActivityButton(id: "claim", title: "Claim"),
                           BeamLiveActivityButton(id: "dismiss", title: "Dismiss", role: "destructive")]
            }
            let attributes = BeamActionsActivityAttributes(title: title)
            let state = BeamActionsActivityAttributes.ContentState(headline: headline, body: body, buttons: buttons)
            do {
                if #available(iOS 16.2, *) {
                    _ = try Activity.request(attributes: attributes, content: .init(state: state, staleDate: nil), pushType: nil)
                } else {
                    _ = try Activity.request(attributes: attributes, contentState: state, pushType: nil)
                }
            } catch { NSLog("[Beam] startActionsLiveActivity failed: \(error)") }
        }
        #endif
    }

    @objc(startAnimatedLiveActivity:)
    func startAnimatedLiveActivity(_ options: NSDictionary) {
        #if canImport(ActivityKit)
        if #available(iOS 16.1, *) {
            guard ActivityAuthorizationInfo().areActivitiesEnabled else {
                NSLog("[Beam] Live Activities are not enabled."); return
            }
            let title = (options["title"] as? String) ?? "Now live"
            let body = (options["body"] as? String) ?? ""
            let colors = (options["colors"] as? [String]) ?? ["#3366F2", "#F25A4D", "#27B373", "#F29E26"]
            let flip = (options["flipIntervalMs"] as? NSNumber)?.intValue ?? 900
            let attributes = BeamAnimatedActivityAttributes(title: title)
            let state = BeamAnimatedActivityAttributes.ContentState(body: body, colors: colors, flipIntervalMs: flip)
            do {
                if #available(iOS 16.2, *) {
                    _ = try Activity.request(attributes: attributes, content: .init(state: state, staleDate: nil), pushType: nil)
                } else {
                    _ = try Activity.request(attributes: attributes, contentState: state, pushType: nil)
                }
            } catch { NSLog("[Beam] startAnimatedLiveActivity failed: \(error)") }
        }
        #endif
    }

    @objc func endLiveActivities() {
        #if canImport(ActivityKit)
        if #available(iOS 16.1, *) {
            Task {
                for activity in Activity<BeamActionsActivityAttributes>.activities {
                    if #available(iOS 16.2, *) { await activity.end(nil, dismissalPolicy: .immediate) }
                    else { await activity.end(dismissalPolicy: .immediate) }
                }
                for activity in Activity<BeamAnimatedActivityAttributes>.activities {
                    if #available(iOS 16.2, *) { await activity.end(nil, dismissalPolicy: .immediate) }
                    else { await activity.end(dismissalPolicy: .immediate) }
                }
            }
        }
        #endif
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
