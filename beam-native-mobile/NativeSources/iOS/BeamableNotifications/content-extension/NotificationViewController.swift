import UIKit
import UserNotifications
import UserNotificationsUI

/// The Notification **Content** Extension host — the principal class of the content-extension target
/// (declared via `NSExtensionPrincipalClass`, no storyboard). iOS instantiates it and calls
/// `didReceive(_:)` when the user expands a notification whose `categoryIdentifier` matches one of
/// the target's `UNNotificationExtensionCategory` values.
///
/// It mirrors the NSE host: it discovers `BeamContentRenderer`s by class name from the
/// **`BMNContentRenderers`** Info.plist array and offers the notification to each in order; the first
/// to return `true` owns the custom UI. If none claim it, a plain title/body fallback is shown.
///
/// This class ships in the SDK; consuming apps add their own `BeamContentRenderer` files to the
/// content-extension target and list them in `BMNContentRenderers` — no edit to this file.
final class NotificationViewController: UIViewController, UNNotificationContentExtension {

    private let container = UIView()
    private let fallbackTitle = UILabel()
    private let fallbackBody = UILabel()
    private var renderers: [BeamContentRenderer] = []

    override func viewDidLoad() {
        super.viewDidLoad()

        container.translatesAutoresizingMaskIntoConstraints = false
        view.addSubview(container)
        NSLayoutConstraint.activate([
            container.leadingAnchor.constraint(equalTo: view.leadingAnchor),
            container.trailingAnchor.constraint(equalTo: view.trailingAnchor),
            container.topAnchor.constraint(equalTo: view.topAnchor),
            container.bottomAnchor.constraint(equalTo: view.bottomAnchor),
        ])

        // Simple stacked fallback (used only when no renderer claims the notification).
        fallbackTitle.font = .preferredFont(forTextStyle: .headline)
        fallbackTitle.numberOfLines = 1
        fallbackBody.font = .preferredFont(forTextStyle: .body)
        fallbackBody.numberOfLines = 0
        let stack = UIStackView(arrangedSubviews: [fallbackTitle, fallbackBody])
        stack.axis = .vertical
        stack.spacing = 4
        stack.translatesAutoresizingMaskIntoConstraints = false
        stack.isHidden = true
        container.addSubview(stack)
        NSLayoutConstraint.activate([
            stack.leadingAnchor.constraint(equalTo: container.leadingAnchor, constant: 16),
            stack.trailingAnchor.constraint(equalTo: container.trailingAnchor, constant: -16),
            stack.topAnchor.constraint(equalTo: container.topAnchor, constant: 12),
            stack.bottomAnchor.constraint(lessThanOrEqualTo: container.bottomAnchor, constant: -12),
        ])
        self.fallbackStack = stack

        renderers = Self.discoverRenderers()
    }

    private weak var fallbackStack: UIStackView?

    func didReceive(_ notification: UNNotification) {
        let content = notification.request.content

        for renderer in renderers {
            if renderer.render(in: container, notification: notification) {
                return // a renderer claimed it and drew the custom UI
            }
        }

        // No renderer claimed it — show a plain title/body fallback.
        fallbackTitle.text = content.title
        fallbackBody.text = content.body
        fallbackStack?.isHidden = false
    }

    /// Resolve `BeamContentRenderer`s named in the content-extension Info.plist `BMNContentRenderers`
    /// array (mirrors the NSE's `BMNServicePlugins` discovery in `NotificationService`).
    private static func discoverRenderers() -> [BeamContentRenderer] {
        guard let names = Bundle.main.infoDictionary?["BMNContentRenderers"] as? [String] else { return [] }
        return names.compactMap { name in
            guard let cls = NSClassFromString(name) as? NSObject.Type else { return nil }
            return cls.init() as? BeamContentRenderer
        }
    }
}
