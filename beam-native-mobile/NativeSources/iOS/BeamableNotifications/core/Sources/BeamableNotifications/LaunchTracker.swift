import Foundation

/// Captures the notification that launched the app (cold start) so engine code can
/// retrieve it on demand via `bmn_getLaunchNotification` (feature 6, "get intent").
///
/// Also holds a small queue of tap events that arrive before the engine has registered
/// its tap callback — common on cold start, where the OS delivers the tap before the
/// game's scripting layer is alive. The queue is flushed once the callback is set.
public final class LaunchTracker {

    public static let shared = LaunchTracker()

    private let lock = NSLock()
    private var _launchNotification: NotificationData?
    private var pendingTaps: [NotificationData] = []

    /// Record the launching notification. First non-nil wins for a given launch.
    public func setLaunchNotification(_ data: NotificationData) {
        lock.lock(); defer { lock.unlock() }
        if _launchNotification == nil {
            var d = data
            d.wasLaunch = true
            _launchNotification = d
        }
    }

    public var launchNotification: NotificationData? {
        lock.lock(); defer { lock.unlock() }
        return _launchNotification
    }

    // MARK: Pre-registration tap queue

    public func enqueueTap(_ data: NotificationData) {
        lock.lock(); defer { lock.unlock() }
        pendingTaps.append(data)
    }

    /// Return and clear the queued taps so they can be flushed to a newly-set callback.
    public func drainPendingTaps() -> [NotificationData] {
        lock.lock(); defer { lock.unlock() }
        let taps = pendingTaps
        pendingTaps.removeAll()
        return taps
    }
}
