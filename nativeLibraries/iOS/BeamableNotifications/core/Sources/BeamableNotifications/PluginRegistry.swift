import Foundation
import UserNotifications

/// Services a plugin may use to interact with the SDK without reaching into internals.
public final class PluginContext {
    public unowned let manager: NotificationManager
    public var sharedConfig: SharedConfig { SharedConfig.shared }

    init(manager: NotificationManager) { self.manager = manager }

    public func schedule(_ request: LocalRequest) { manager.scheduleLocal(request) }
    public func setBadge(_ count: Int) { manager.setBadge(count) }
    public func log(_ message: String) { NSLog("[BeamableNotifications] %@", message) }
}

/// The native extension point (see plan §Extensibility). Implement this protocol in a
/// new file to add behavior — custom token providers, analytics, payload transforms,
/// logging — without touching the SDK core. All methods have default no-op
/// implementations, so a plugin only overrides what it needs.
///
/// Register either by listing the class name under the `BMNPlugins` array in the app's
/// Info.plist (zero code), or by calling `PluginRegistry.shared.register(_:)`.
/// Classes discovered via Info.plist must be `NSObject` subclasses with an `init()`.
public protocol NotificationPlugin: AnyObject {
    var id: String { get }

    // Observe — called in registration order.
    func onInitialize(_ ctx: PluginContext)
    func onPermissionResult(_ status: UNAuthorizationStatus)
    func onTokenReceived(_ token: String)
    func onNotificationReceived(_ note: NotificationData)
    func onNotificationTapped(_ note: NotificationData, actionId: String?)

    // Transform — chained. Return the (possibly mutated) value, or nil to drop/decline.
    func willSchedule(_ request: LocalRequest) -> LocalRequest?
    func willPresent(_ note: NotificationData) -> UNNotificationPresentationOptions?

    /// Return true to take over remote-token acquisition (e.g. an FCM plugin). The
    /// default APNs flow runs only if no plugin claims it. Call `register` with the token.
    func provideRemoteToken(register: @escaping (Result<String, Error>) -> Void) -> Bool
}

public extension NotificationPlugin {
    func onInitialize(_ ctx: PluginContext) {}
    func onPermissionResult(_ status: UNAuthorizationStatus) {}
    func onTokenReceived(_ token: String) {}
    func onNotificationReceived(_ note: NotificationData) {}
    func onNotificationTapped(_ note: NotificationData, actionId: String?) {}
    func willSchedule(_ request: LocalRequest) -> LocalRequest? { request }
    func willPresent(_ note: NotificationData) -> UNNotificationPresentationOptions? { nil }
    func provideRemoteToken(register: @escaping (Result<String, Error>) -> Void) -> Bool { false }
}

/// Holds and dispatches to registered plugins. The core calls into the `dispatch*` /
/// `transform*` methods at each event; the registry never knows the concrete plugins.
public final class PluginRegistry {

    public static let shared = PluginRegistry()

    private var plugins: [NotificationPlugin] = []
    private let lock = NSLock()

    public func register(_ plugin: NotificationPlugin) {
        lock.lock(); defer { lock.unlock() }
        guard !plugins.contains(where: { $0.id == plugin.id }) else { return }
        plugins.append(plugin)
    }

    private var snapshot: [NotificationPlugin] {
        lock.lock(); defer { lock.unlock() }
        return plugins
    }

    /// Instantiate plugins named in the `BMNPlugins` Info.plist array. Each entry is a
    /// class name (optionally module-qualified). Failures are logged and skipped so a
    /// bad entry can't crash launch.
    public func discoverAndRegisterFromInfoPlist(infoDictionary: [String: Any]? = Bundle.main.infoDictionary) {
        guard let names = infoDictionary?["BMNPlugins"] as? [String] else { return }
        for name in names {
            guard let plugin = Self.instantiatePlugin(named: name) else {
                NSLog("[BeamableNotifications] could not instantiate plugin '%@'", name)
                continue
            }
            register(plugin)
        }
    }

    static func instantiatePlugin(named name: String) -> NotificationPlugin? {
        // Try the bare name, then a module-qualified form for the host app/product.
        var cls: AnyClass? = NSClassFromString(name)
        if cls == nil, let module = Bundle.main.infoDictionary?["CFBundleExecutable"] as? String {
            let sanitized = module.replacingOccurrences(of: " ", with: "_")
            cls = NSClassFromString("\(sanitized).\(name)")
        }
        guard let objectClass = cls as? NSObject.Type else { return nil }
        return objectClass.init() as? NotificationPlugin
    }

    // MARK: Observe dispatch

    func dispatchInitialize(_ ctx: PluginContext) { snapshot.forEach { $0.onInitialize(ctx) } }
    func dispatchPermissionResult(_ status: UNAuthorizationStatus) { snapshot.forEach { $0.onPermissionResult(status) } }
    func dispatchTokenReceived(_ token: String) { snapshot.forEach { $0.onTokenReceived(token) } }
    func dispatchNotificationReceived(_ note: NotificationData) { snapshot.forEach { $0.onNotificationReceived(note) } }
    func dispatchNotificationTapped(_ note: NotificationData, actionId: String?) {
        snapshot.forEach { $0.onNotificationTapped(note, actionId: actionId) }
    }

    // MARK: Transform chains

    func transformWillSchedule(_ request: LocalRequest) -> LocalRequest? {
        var current = request
        for plugin in snapshot {
            guard let next = plugin.willSchedule(current) else { return nil }
            current = next
        }
        return current
    }

    func transformWillPresent(_ note: NotificationData) -> UNNotificationPresentationOptions? {
        for plugin in snapshot {
            if let options = plugin.willPresent(note) { return options }
        }
        return nil
    }

    /// Returns true if a plugin claimed remote-token acquisition.
    func provideRemoteToken(register: @escaping (Result<String, Error>) -> Void) -> Bool {
        for plugin in snapshot {
            if plugin.provideRemoteToken(register: register) { return true }
        }
        return false
    }
}
