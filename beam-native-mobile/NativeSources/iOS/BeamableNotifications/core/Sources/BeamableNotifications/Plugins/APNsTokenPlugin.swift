import Foundation

/// Reference plugin demonstrating the token-provider seam. The SDK's *default* behavior
/// is plain APNs handled inside `RemotePush` — no plugin is required for that. This
/// plugin simply observes the resolved APNs token (e.g. to forward it somewhere), and
/// declines to take over acquisition (`provideRemoteToken` returns false).
///
/// A future `FCMTokenPlugin` would instead return `true` from `provideRemoteToken` and
/// call `register(.success(fcmToken))`, swapping the token source with zero core changes.
public final class APNsTokenPlugin: NSObject, NotificationPlugin {

    public var id: String { "com.beamable.notifications.apnsToken" }

    private let onToken: ((String) -> Void)?

    public override convenience init() {
        self.init(onToken: nil)
    }

    public init(onToken: ((String) -> Void)? = nil) {
        self.onToken = onToken
        super.init()
    }

    public func onTokenReceived(_ token: String) {
        onToken?(token)
    }

    public func provideRemoteToken(register: @escaping (Result<String, Error>) -> Void) -> Bool {
        // Decline: let RemotePush perform the standard APNs registration.
        return false
    }
}
