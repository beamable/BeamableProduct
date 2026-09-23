import Foundation
import UserNotifications

/// Registers interactive notification categories (action buttons, feature 7) with
/// `UNUserNotificationCenter`. Categories accumulate: each `register` re-sets the full
/// set on the center, since the API has no incremental add.
///
/// Two processes write here — the app (built-in + app-registered categories, at init) and the NSE
/// (categories synthesized from a payload's `buttons`, at delivery). They have separate memory, and
/// `setNotificationCategories` REPLACES the OS-held set, so every write merges the OS set in first
/// (`mergeThenSet`); otherwise whichever process wrote last would silently delete the other's
/// categories. Synthesized categories are additionally persisted to the App Group so the app can
/// re-seed them at launch instead of wiping notifications that are still sitting in Notification
/// Center (`hydrateFromSharedStore`).
public final class CategoryStore {

    public static let shared = CategoryStore()

    /// Cap on SYNTHESIZED categories only. Built-in and app-registered categories are never evicted;
    /// without a cap, every distinct authored button pair would add one more registration forever.
    private static let maxSynthesized = 32

    private var categories: [String: UNNotificationCategory] = [:]
    /// Synthesized ids in insertion order, oldest first (LRU eviction order).
    private var synthesizedOrder: [String] = []
    private let lock = NSLock()

    public func register(_ spec: CategorySpec, center: UNUserNotificationCenter = .current()) {
        register(spec, center: center, completion: nil)
    }

    /// `register` with a completion, for callers that must not proceed until the OS has the category —
    /// i.e. the NSE, which is about to hand back content naming it.
    public func register(_ spec: CategorySpec,
                         center: UNUserNotificationCenter,
                         completion: (() -> Void)?) {
        store(spec)
        mergeThenSet(center: center, confirming: spec.identifierIfSynthesized, completion: completion)
    }

    /// Register a category built from payload-authored buttons and return the id to stamp on the
    /// notification. Idempotent for an identical button set, because the id is a deterministic hash of
    /// the buttons — repeated pushes of the same campaign reuse one registration.
    ///
    /// Returns the built-in `beam_actions` id unchanged when there are no usable buttons, so the caller
    /// can use the result unconditionally.
    @discardableResult
    public func registerSynthesized(buttons: [BeamActionButton],
                                    center: UNUserNotificationCenter = .current(),
                                    persist: Bool = true,
                                    completion: (() -> Void)? = nil) -> String {
        let usable = BeamActionButtons.sanitize(buttons)
        guard !usable.isEmpty else {
            completion?()
            return BeamActionButtons.builtInActionsCategory
        }

        let spec = CategorySpec(synthesizedFrom: usable)
        if persist { SharedConfig.shared.appendSynthesizedCategory(spec) }
        register(spec, center: center, completion: completion)
        return spec.id
    }

    /// Re-seed the synthesized categories the NSE registered while the app was dead.
    ///
    /// Must run BEFORE the app writes its own set at init: `setNotificationCategories` replaces the
    /// whole set, so without this, launching the app strips the buttons from any synthesized-category
    /// notification still in Notification Center. No-op without an App Group.
    public func hydrateFromSharedStore(_ shared: SharedConfig = .shared) {
        for spec in shared.loadSynthesizedCategories() {
            store(spec)
        }
    }

    // MARK: - Internals

    private func store(_ spec: CategorySpec) {
        let category: UNNotificationCategory
        if let placeholder = spec.hiddenPreviewsBodyPlaceholder {
            category = UNNotificationCategory(
                identifier: spec.id,
                actions: spec.actions.map(Self.action(from:)),
                intentIdentifiers: [],
                hiddenPreviewsBodyPlaceholder: placeholder,
                options: []
            )
        } else {
            category = UNNotificationCategory(
                identifier: spec.id,
                actions: spec.actions.map(Self.action(from:)),
                intentIdentifiers: [],
                options: []
            )
        }

        lock.lock()
        let isNew = categories.updateValue(category, forKey: spec.id) == nil
        if BeamActionButtons.isSynthesized(spec.id) {
            if isNew { synthesizedOrder.append(spec.id) }
            while synthesizedOrder.count > Self.maxSynthesized {
                let evicted = synthesizedOrder.removeFirst()
                categories.removeValue(forKey: evicted)
            }
        }
        lock.unlock()
    }

    private static func action(from action: ActionSpec) -> UNNotificationAction {
        var options: UNNotificationActionOptions = []
        if action.foreground == true { options.insert(.foreground) }
        if action.destructive == true { options.insert(.destructive) }
        if action.authenticationRequired == true { options.insert(.authenticationRequired) }
        return UNNotificationAction(identifier: action.id, title: action.title, options: options)
    }

    /// Union this process's categories with the OS-held set, then write. Ours win on id collision (an
    /// app re-registering `beam_actions` is deliberately overriding the built-in pair).
    private func mergeThenSet(center: UNUserNotificationCenter,
                              confirming id: String?,
                              completion: (() -> Void)?) {
        center.getNotificationCategories { [weak self] existing in
            guard let self = self else { completion?(); return }
            self.lock.lock()
            let mine = self.categories
            self.lock.unlock()

            var merged = mine
            for category in existing where merged[category.identifier] == nil {
                merged[category.identifier] = category
            }
            center.setNotificationCategories(Set(merged.values))

            guard let id = id, let completion = completion else { completion?(); return }
            Self.confirmRegistration(id: id, center: center, done: completion)
        }
    }

    /// `setNotificationCategories` is fire-and-forget, and the OS resolves a notification's
    /// `categoryIdentifier` when it presents it — which happens right after the NSE returns. Read back
    /// until the id shows up so we don't hand over content naming a category the OS hasn't ingested.
    ///
    /// Bounded and best-effort: ~0.24s worst case against the NSE's 27s budget, and if it never lands
    /// the notification is still delivered, just without buttons. Never fails the delivery.
    private static func confirmRegistration(id: String,
                                           center: UNUserNotificationCenter,
                                           attempts: Int = 8,
                                           interval: TimeInterval = 0.03,
                                           done: @escaping () -> Void) {
        center.getNotificationCategories { categories in
            if categories.contains(where: { $0.identifier == id }) || attempts <= 1 {
                done()
                return
            }
            DispatchQueue.global().asyncAfter(deadline: .now() + interval) {
                confirmRegistration(id: id, center: center,
                                    attempts: attempts - 1, interval: interval, done: done)
            }
        }
    }
}

private extension CategorySpec {
    /// Only a synthesized id is worth read-back confirming: the built-in and app-registered ones are
    /// written at init, long before any notification names them.
    var identifierIfSynthesized: String? {
        BeamActionButtons.isSynthesized(id) ? id : nil
    }
}
