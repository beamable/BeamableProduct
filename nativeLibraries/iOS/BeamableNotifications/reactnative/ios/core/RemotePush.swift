import Foundation
import UIKit
import UserNotifications

/// Handles remote (APNs) registration and the app-delegate push callbacks.
///
/// Engines own the `UIApplicationDelegate`, so rather than ask users to edit their
/// AppDelegate we graft the three push selectors onto the live delegate class,
/// forwarding to any existing implementation. The token-source itself is pluggable: if
/// a plugin claims `provideRemoteToken`, we use that (the FCM seam); otherwise we do
/// standard APNs and surface the raw device token as lowercase hex.
public final class RemotePush: NSObject {

    public static let shared = RemotePush()

    private var swizzled = false
    /// Original IMPs captured when the delegate already implemented a selector, so the
    /// grafted block can forward to them. Keyed by selector name (single delegate class).
    private var originalIMPs: [String: IMP] = [:]

    // MARK: Registration (feature 2)

    public func register() {
        // Give plugins first refusal on token acquisition (e.g. FCM).
        let claimed = PluginRegistry.shared.provideRemoteToken { [weak self] result in
            switch result {
            case .success(let token): self?.deliver(token: token)
            case .failure(let error): self?.deliverError(error.localizedDescription)
            }
        }
        guard !claimed else { return }

        DispatchQueue.main.async {
            UIApplication.shared.registerForRemoteNotifications()
        }
    }

    public func unregister() {
        DispatchQueue.main.async {
            UIApplication.shared.unregisterForRemoteNotifications()
        }
    }

    // MARK: Token plumbing

    func deviceTokenReceived(_ deviceToken: Data) {
        let hex = deviceToken.map { String(format: "%02x", $0) }.joined()
        deliver(token: hex)
    }

    func registrationFailed(_ error: Error) {
        deliverError(error.localizedDescription)
    }

    private func deliver(token: String) {
        PluginRegistry.shared.dispatchTokenReceived(token)
        let manager = NotificationManager.shared
        DispatchQueue.main.async { manager.onTokenReceived?(token) }
    }

    private func deliverError(_ message: String) {
        let manager = NotificationManager.shared
        DispatchQueue.main.async { manager.onTokenError?(message) }
    }

    /// Silent / background remote notification (`content-available:1`) while the app is
    /// alive. Surfaced as a "received" event.
    func handleSilentRemote(_ userInfo: [AnyHashable: Any]) {
        guard case .object(let obj) = JSONValue(any: userInfo) else { return }
        let id = obj["bmnId"]?.stringValue
            ?? obj["aps"]?["thread-id"]?.stringValue
            ?? UUID().uuidString
        let data = NotificationData(
            id: id,
            deepLink: obj.bmnDeepLink,
            userInfo: obj
        )
        PluginRegistry.shared.dispatchNotificationReceived(data)
        let manager = NotificationManager.shared
        DispatchQueue.main.async { manager.onNotificationReceived?(data) }
    }

    // MARK: Delegate grafting

    /// Set `BMNDisableSwizzling=YES` in Info.plist to opt out and forward manually instead.
    func installSwizzlingIfNeeded() {
        guard !swizzled else { return }
        if Bundle.main.infoDictionary?["BMNDisableSwizzling"] as? Bool == true { return }
        guard let delegate = UIApplication.shared.delegate else {
            // Delegate not set yet — retry on the next runloop tick.
            DispatchQueue.main.async { [weak self] in self?.installSwizzlingIfNeeded() }
            return
        }
        let cls: AnyClass = type(of: delegate)

        graftDidRegister(into: cls)
        graftDidFail(into: cls)
        graftDidReceive(into: cls)

        swizzled = true
    }

    private func graftDidRegister(into cls: AnyClass) {
        let sel = #selector(UIApplicationDelegate.application(_:didRegisterForRemoteNotificationsWithDeviceToken:))
        let block: @convention(block) (AnyObject, UIApplication, Data) -> Void = { [weak self] receiver, app, token in
            RemotePush.shared.deviceTokenReceived(token)
            if let original = self?.originalIMPs[sel.description] {
                typealias Fn = @convention(c) (AnyObject, Selector, UIApplication, Data) -> Void
                unsafeBitCast(original, to: Fn.self)(receiver, sel, app, token)
            }
        }
        replace(cls, sel, "v@:@@", imp_implementationWithBlock(block))
    }

    private func graftDidFail(into cls: AnyClass) {
        let sel = #selector(UIApplicationDelegate.application(_:didFailToRegisterForRemoteNotificationsWithError:))
        let block: @convention(block) (AnyObject, UIApplication, Error) -> Void = { [weak self] receiver, app, error in
            RemotePush.shared.registrationFailed(error)
            if let original = self?.originalIMPs[sel.description] {
                typealias Fn = @convention(c) (AnyObject, Selector, UIApplication, Error) -> Void
                unsafeBitCast(original, to: Fn.self)(receiver, sel, app, error)
            }
        }
        replace(cls, sel, "v@:@@", imp_implementationWithBlock(block))
    }

    private func graftDidReceive(into cls: AnyClass) {
        let sel = #selector(UIApplicationDelegate.application(_:didReceiveRemoteNotification:fetchCompletionHandler:))
        let block: @convention(block) (AnyObject, UIApplication, [AnyHashable: Any], @escaping (UIBackgroundFetchResult) -> Void) -> Void = { [weak self] receiver, app, userInfo, completion in
            RemotePush.shared.handleSilentRemote(userInfo)
            if let original = self?.originalIMPs[sel.description] {
                typealias Fn = @convention(c) (AnyObject, Selector, UIApplication, [AnyHashable: Any], @escaping (UIBackgroundFetchResult) -> Void) -> Void
                unsafeBitCast(original, to: Fn.self)(receiver, sel, app, userInfo, completion)
            } else {
                completion(.newData)
            }
        }
        replace(cls, sel, "v@:@@@?", imp_implementationWithBlock(block))
    }

    /// Install `imp` for `sel` on `cls`. `class_replaceMethod` returns the previous IMP
    /// if the method existed (which we keep to forward to), or nil if it was added fresh.
    private func replace(_ cls: AnyClass, _ sel: Selector, _ types: String, _ imp: IMP) {
        let previous = class_replaceMethod(cls, sel, imp, types)
        if let previous = previous {
            originalIMPs[sel.description] = previous
        }
    }
}
