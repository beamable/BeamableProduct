import Foundation

// C ABI surface (see include/BeamableNotifications.h). These flat `@_cdecl` functions
// are what Unity (P/Invoke) and Unreal (C++) link against. Strings are UTF-8 C strings;
// structured arguments and results are JSON. Callbacks are C function pointers invoked
// with a single JSON string. React Native does NOT use this layer — it talks to
// NotificationManager directly in Swift.

public typealias BMNCallback = @convention(c) (UnsafePointer<CChar>?) -> Void

private enum CB {
    static var permission: BMNCallback?
    static var tokenReceived: BMNCallback?
    static var tokenError: BMNCallback?
    static var presented: BMNCallback?
    static var received: BMNCallback?
    static var tapped: BMNCallback?
    static var pending: BMNCallback?
    static var receipts: BMNCallback?
}

private func invoke(_ cb: BMNCallback?, _ json: String) {
    guard let cb = cb else { return }
    json.withCString { cb($0) }
}

private func cString(_ ptr: UnsafePointer<CChar>?) -> String {
    guard let ptr = ptr else { return "" }
    return String(cString: ptr)
}

// MARK: - Lifecycle

@_cdecl("bmn_initialize")
public func bmn_initialize() {
    NSLog("[BeamableNotifications] bmn_initialize() called")
    let m = NotificationManager.shared
    m.onPermissionResult = { invoke(CB.permission, JSON.encode($0)) }
    m.onTokenReceived = { invoke(CB.tokenReceived, "{\"token\":\(JSON.encode($0))}") }
    m.onTokenError = { invoke(CB.tokenError, "{\"error\":\(JSON.encode($0))}") }
    m.onNotificationPresented = { invoke(CB.presented, JSON.encode($0)) }
    m.onNotificationReceived = { invoke(CB.received, JSON.encode($0)) }
    // onNotificationTapped is installed in bmn_setOnNotificationTapped so the manager's
    // queued-tap flush fires only once the C callback actually exists (cold-start safety).
    m.onPendingNotifications = { invoke(CB.pending, $0) }
    m.onDeliveryReceipts = { invoke(CB.receipts, $0) }
    m.initialize()
}

// MARK: - Permission (feature 5)

@_cdecl("bmn_requestPermission")
public func bmn_requestPermission(_ optionsJson: UnsafePointer<CChar>?) {
    let options = JSON.decode(PermissionOptions.self, from: cString(optionsJson)) ?? PermissionOptions()
    NotificationManager.shared.requestPermission(options)
}

@_cdecl("bmn_getPermissionStatus")
public func bmn_getPermissionStatus() {
    NotificationManager.shared.getPermissionStatus()
}

// MARK: - Local notifications (feature 1)

@_cdecl("bmn_scheduleLocal")
public func bmn_scheduleLocal(_ requestJson: UnsafePointer<CChar>?) {
    guard let request = JSON.decode(LocalRequest.self, from: cString(requestJson)) else { return }
    NotificationManager.shared.scheduleLocal(request)
}

@_cdecl("bmn_cancelLocal")
public func bmn_cancelLocal(_ id: UnsafePointer<CChar>?) {
    NotificationManager.shared.cancelLocal(id: cString(id))
}

@_cdecl("bmn_cancelAllLocal")
public func bmn_cancelAllLocal() {
    NotificationManager.shared.cancelAllLocal()
}

@_cdecl("bmn_getPending")
public func bmn_getPending() {
    NotificationManager.shared.getPending()
}

// MARK: - Remote notifications (feature 2)

@_cdecl("bmn_registerForRemote")
public func bmn_registerForRemote() {
    RemotePush.shared.register()
}

@_cdecl("bmn_unregisterForRemote")
public func bmn_unregisterForRemote() {
    RemotePush.shared.unregister()
}

// MARK: - Analytics / delivery receipts (feature 8)

@_cdecl("bmn_configureAnalytics")
public func bmn_configureAnalytics(_ configJson: UnsafePointer<CChar>?) {
    guard let config = JSON.decode(AnalyticsConfig.self, from: cString(configJson)) else { return }
    SharedConfig.shared.saveAnalyticsConfig(config)
}

@_cdecl("bmn_getDeliveryReceipts")
public func bmn_getDeliveryReceipts() {
    NotificationManager.shared.emitDeliveryReceipts()
}

// MARK: - Templates & categories (feature 4, 7)

@_cdecl("bmn_registerTemplate")
public func bmn_registerTemplate(_ templateJson: UnsafePointer<CChar>?) {
    guard let template = JSON.decode(TemplateSpec.self, from: cString(templateJson)) else { return }
    TemplateStore.shared.register(template)
}

@_cdecl("bmn_registerCategory")
public func bmn_registerCategory(_ categoryJson: UnsafePointer<CChar>?) {
    guard let category = JSON.decode(CategorySpec.self, from: cString(categoryJson)) else { return }
    CategoryStore.shared.register(category)
}

// MARK: - Badge

@_cdecl("bmn_setBadge")
public func bmn_setBadge(_ count: Int32) {
    NotificationManager.shared.setBadge(Int(count))
}

@_cdecl("bmn_clearDelivered")
public func bmn_clearDelivered() {
    NotificationManager.shared.clearDelivered()
}

// MARK: - Get intent (feature 6)

/// Returns a malloc'd UTF-8 JSON string for the launching notification, or NULL.
/// The caller MUST release it with `bmn_free`.
@_cdecl("bmn_getLaunchNotification")
public func bmn_getLaunchNotification() -> UnsafePointer<CChar>? {
    guard let data = LaunchTracker.shared.launchNotification else { return nil }
    let json = JSON.encode(data)
    return UnsafePointer(strdup(json))
}

@_cdecl("bmn_free")
public func bmn_free(_ ptr: UnsafePointer<CChar>?) {
    guard let ptr = ptr else { return }
    free(UnsafeMutableRawPointer(mutating: ptr))
}

// MARK: - Callback registration (feature 3)

@_cdecl("bmn_setOnPermissionResult")
public func bmn_setOnPermissionResult(_ cb: BMNCallback?) { CB.permission = cb }

@_cdecl("bmn_setOnTokenReceived")
public func bmn_setOnTokenReceived(_ cb: BMNCallback?) { CB.tokenReceived = cb }

@_cdecl("bmn_setOnTokenError")
public func bmn_setOnTokenError(_ cb: BMNCallback?) { CB.tokenError = cb }

@_cdecl("bmn_setOnNotificationPresented")
public func bmn_setOnNotificationPresented(_ cb: BMNCallback?) { CB.presented = cb }

@_cdecl("bmn_setOnNotificationReceived")
public func bmn_setOnNotificationReceived(_ cb: BMNCallback?) { CB.received = cb }

@_cdecl("bmn_setOnNotificationTapped")
public func bmn_setOnNotificationTapped(_ cb: BMNCallback?) {
    CB.tapped = cb
    // Installing the closure here triggers the manager's didSet flush of any taps that
    // were queued before the engine registered this callback (cold-start taps).
    NotificationManager.shared.onNotificationTapped = (cb == nil)
        ? nil
        : { invoke(CB.tapped, JSON.encode($0)) }
}

@_cdecl("bmn_setOnPendingNotifications")
public func bmn_setOnPendingNotifications(_ cb: BMNCallback?) { CB.pending = cb }

@_cdecl("bmn_setOnDeliveryReceipts")
public func bmn_setOnDeliveryReceipts(_ cb: BMNCallback?) { CB.receipts = cb }
