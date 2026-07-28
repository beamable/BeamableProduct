import UIKit
import UserNotifications
import os.log

/// Sample **Content Extension renderer** (Tier 3) for the RN sample app — the iOS parity demo for the
/// Android `countdown` custom style. Draws a styled "expiring offer" card in the EXPANDED notification
/// with a live `mm:ss` countdown that ticks while the notification is on-screen. When it reaches zero
/// the card shows "Offer expired".
///
/// Discovery: listed by class name in the Content Extension target's `BMNContentRenderers` Info.plist
/// array (the Expo plugin's `iosContentRenderers` prop writes it), and the `countdown` category is
/// listed in `UNNotificationExtensionCategory` (the `contentCategories` prop). `@objc(...)` gives a
/// stable runtime name for `NSClassFromString`. Must be an `NSObject` subclass with a no-arg `init()`.
///
/// Reads the same wire fields the sender set in the console: `title`, `body`, and either
/// `expiresAtMs` (epoch ms) or `expiresInSeconds`.
@objc(SampleCountdownContentRenderer)
public final class SampleCountdownContentRenderer: NSObject, BeamContentRenderer {

    private let titleLabel = UILabel()
    private let countdownLabel = UILabel()
    private let bodyLabel = UILabel()
    private var expiresAt: Date?
    private var timer: Timer?

    public override init() { super.init() }

    deinit { timer?.invalidate() }

    public func render(in container: UIView, notification: UNNotification) -> Bool {
        let info = notification.request.content.userInfo
        guard (info["style"] as? String) == "countdown" else { return false }

        // Diagnostic: confirm the on-the-wire encoding of the expiry fields on device. The push rail
        // sends these as JSON numbers, `simctl push` files as strings — see resolveExpiry.
        let log = Logger(subsystem: "com.beamable.notifications", category: "countdown")
        log.info("countdown wire types — expiresInSeconds: \(String(describing: type(of: info["expiresInSeconds"])), privacy: .public), expiresAtMs: \(String(describing: type(of: info["expiresAtMs"])), privacy: .public)")

        expiresAt = Self.resolveExpiry(info, deliveredAt: notification.date)

        titleLabel.text = notification.request.content.title
        titleLabel.font = .preferredFont(forTextStyle: .headline)
        titleLabel.numberOfLines = 1

        countdownLabel.font = .monospacedDigitSystemFont(ofSize: 34, weight: .bold)
        countdownLabel.textColor = .systemOrange
        countdownLabel.textAlignment = .center

        bodyLabel.text = notification.request.content.body
        bodyLabel.font = .preferredFont(forTextStyle: .body)
        bodyLabel.textColor = .secondaryLabel
        bodyLabel.numberOfLines = 0

        let stack = UIStackView(arrangedSubviews: [titleLabel, countdownLabel, bodyLabel])
        stack.axis = .vertical
        stack.spacing = 6
        stack.alignment = .fill
        stack.translatesAutoresizingMaskIntoConstraints = false
        container.addSubview(stack)
        NSLayoutConstraint.activate([
            stack.leadingAnchor.constraint(equalTo: container.leadingAnchor, constant: 16),
            stack.trailingAnchor.constraint(equalTo: container.trailingAnchor, constant: -16),
            stack.topAnchor.constraint(equalTo: container.topAnchor, constant: 14),
            stack.bottomAnchor.constraint(lessThanOrEqualTo: container.bottomAnchor, constant: -14),
        ])

        updateCountdown()
        // Tick every second while the notification is expanded on-screen.
        let t = Timer(timeInterval: 1.0, repeats: true) { [weak self] _ in self?.updateCountdown() }
        RunLoop.main.add(t, forMode: .common)
        timer = t
        return true
    }

    private func updateCountdown() {
        guard let expiresAt = expiresAt else { countdownLabel.text = "--:--"; return }
        let remaining = expiresAt.timeIntervalSinceNow
        if remaining <= 0 {
            countdownLabel.text = "00:00"
            countdownLabel.textColor = .systemGray
            bodyLabel.text = "Offer expired"
            timer?.invalidate()
            timer = nil
            return
        }
        let total = Int(remaining.rounded())
        countdownLabel.text = String(format: "%02d:%02d", total / 60, total % 60)
    }

    /// Prefer an absolute `expiresAtMs` (epoch ms); else `expiresInSeconds` measured from the
    /// notification's DELIVERY time (`deliveredAt`), not `Date()` — otherwise the deadline is
    /// recomputed every time the notification is expanded, so the countdown appears to restart.
    /// Anchoring to delivery time keeps the relative `expiresInSeconds` wire field stable across
    /// re-expands (matches the Android `expiresInSeconds` semantics).
    ///
    /// The wire value may arrive as EITHER a JSON number or a string: a hand-authored
    /// `simctl push` `.apns` file typically uses string values, while the Beamable push rail
    /// (`ApnsProvider`) JSON-encodes them as numbers. A `String`-only cast silently dropped the
    /// rail's numeric values (countdown stuck at `--:--`), so accept both encodings.
    private static func resolveExpiry(_ info: [AnyHashable: Any], deliveredAt: Date) -> Date? {
        func number(_ key: String) -> Double? {
            (info[key] as? NSNumber)?.doubleValue ?? (info[key] as? String).flatMap(Double.init)
        }
        if let ms = number("expiresAtMs"), ms > 0 {
            return Date(timeIntervalSince1970: ms / 1000.0)
        }
        if let secs = number("expiresInSeconds"), secs > 0 {
            return deliveredAt.addingTimeInterval(secs)
        }
        return nil
    }
}
