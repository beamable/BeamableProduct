import Foundation
import UserNotifications

/// Registers interactive notification categories (action buttons, feature 7) with
/// `UNUserNotificationCenter`. Categories accumulate: each `register` re-sets the full
/// set on the center, since the API has no incremental add.
public final class CategoryStore {

    public static let shared = CategoryStore()

    private var categories: [String: UNNotificationCategory] = [:]
    private let lock = NSLock()

    public func register(_ spec: CategorySpec, center: UNUserNotificationCenter = .current()) {
        let actions: [UNNotificationAction] = spec.actions.map { action in
            var options: UNNotificationActionOptions = []
            if action.foreground == true { options.insert(.foreground) }
            if action.destructive == true { options.insert(.destructive) }
            if action.authenticationRequired == true { options.insert(.authenticationRequired) }
            return UNNotificationAction(identifier: action.id, title: action.title, options: options)
        }

        let category: UNNotificationCategory
        if let placeholder = spec.hiddenPreviewsBodyPlaceholder {
            category = UNNotificationCategory(
                identifier: spec.id,
                actions: actions,
                intentIdentifiers: [],
                hiddenPreviewsBodyPlaceholder: placeholder,
                options: []
            )
        } else {
            category = UNNotificationCategory(
                identifier: spec.id,
                actions: actions,
                intentIdentifiers: [],
                options: []
            )
        }

        lock.lock()
        categories[spec.id] = category
        let all = Set(categories.values)
        lock.unlock()

        center.setNotificationCategories(all)
    }
}
