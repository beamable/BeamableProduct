import Foundation

/// One payload-authored action button.
///
/// The same `{id, title, role}` shape serves both iOS surfaces of the console's `actions` style: the
/// Live Activity's `ContentState.buttons`, and — when the device has no Live Activity and the rail
/// falls back to a plain notification — the actions of a `UNNotificationCategory` the NSE synthesizes
/// from the payload's `buttons` key. One authored pair, both surfaces, identical labels.
///
/// **Foundation only, on purpose.** This file is compiled into three places: the app (via the core
/// module), the Notification Service Extension (which compiles a small subset of core from source and
/// must stay `UIApplication`-free), and the Widget extension (which gets it as a copied source
/// alongside the attributes types). Anything needing `UserNotifications` belongs in
/// `ActionButtons.swift` instead, which the widget does NOT get.
public struct BeamActionButton: Codable, Equatable, Hashable {

    /// Echoed back as `NotificationData.actionId` when the button is tapped, so the app routes on it.
    /// `"dismiss"` is reserved on the Live Activity path — `BeamLiveActivityActionIntent` ends the
    /// activity on that id and resolves it on any other.
    public var id: String
    /// The label the player sees.
    public var title: String
    /// `"default"` | `"destructive"`. Only `destructive` changes anything: it tints the Live Activity
    /// button red, and makes the notification action `.destructive` rather than `.foreground`.
    public var role: String

    public var isDestructive: Bool { role.lowercased() == "destructive" }

    public init(id: String, title: String, role: String = "default") {
        self.id = id
        self.title = title
        self.role = role
    }

    /// Tolerant decode: a missing key must degrade one button, never throw.
    ///
    /// On the notification path a throw would cost the buttons; on the Live Activity path it is worse —
    /// the OS decodes `content-state` with a strict decoder, and one failure silently drops the whole
    /// push-to-start, so the player sees nothing at all.
    public init(from decoder: Decoder) throws {
        let c = try decoder.container(keyedBy: CodingKeys.self)
        id = (try? c.decode(String.self, forKey: .id)) ?? ""
        title = (try? c.decode(String.self, forKey: .title)) ?? ""
        role = (try? c.decode(String.self, forKey: .role)) ?? "default"
    }
}

/// Original name of this type, from when it existed only on the Live Activity path. Kept so app-side
/// widget UI written against it (e.g. the ReactNative sample's `SampleActionsLiveActivity`) keeps
/// compiling.
public typealias BeamLiveActivityButton = BeamActionButton
