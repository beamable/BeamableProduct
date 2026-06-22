import Foundation

// MARK: - JSONValue
//
// A dependency-free, Codable representation of arbitrary JSON. Notification
// `userInfo` payloads are free-form, so we round-trip them through this type
// rather than forcing a fixed schema. It also bridges to/from the
// `[AnyHashable: Any]` dictionaries that UNNotification exposes.

public indirect enum JSONValue: Codable, Equatable {
    case string(String)
    case number(Double)
    case bool(Bool)
    case object([String: JSONValue])
    case array([JSONValue])
    case null

    public init(from decoder: Decoder) throws {
        let container = try decoder.singleValueContainer()
        if container.decodeNil() {
            self = .null
        } else if let b = try? container.decode(Bool.self) {
            self = .bool(b)
        } else if let n = try? container.decode(Double.self) {
            self = .number(n)
        } else if let s = try? container.decode(String.self) {
            self = .string(s)
        } else if let o = try? container.decode([String: JSONValue].self) {
            self = .object(o)
        } else if let a = try? container.decode([JSONValue].self) {
            self = .array(a)
        } else {
            self = .null
        }
    }

    public func encode(to encoder: Encoder) throws {
        var container = encoder.singleValueContainer()
        switch self {
        case .string(let s): try container.encode(s)
        case .number(let n): try container.encode(n)
        case .bool(let b): try container.encode(b)
        case .object(let o): try container.encode(o)
        case .array(let a): try container.encode(a)
        case .null: try container.encodeNil()
        }
    }

    /// Bridge an arbitrary Foundation/`userInfo` value into a `JSONValue`.
    public init(any value: Any) {
        switch value {
        case let v as String: self = .string(v)
        case let v as Bool where type(of: value) == type(of: true): self = .bool(v)
        case let v as Int: self = .number(Double(v))
        case let v as Double: self = .number(v)
        case let v as NSNumber:
            // NSNumber tagged as bool vs numeric.
            if CFGetTypeID(v) == CFBooleanGetTypeID() {
                self = .bool(v.boolValue)
            } else {
                self = .number(v.doubleValue)
            }
        case let v as [Any]: self = .array(v.map { JSONValue(any: $0) })
        case let v as [String: Any]:
            var out: [String: JSONValue] = [:]
            for (k, val) in v { out[k] = JSONValue(any: val) }
            self = .object(out)
        case let v as [AnyHashable: Any]:
            var out: [String: JSONValue] = [:]
            for (k, val) in v { out["\(k)"] = JSONValue(any: val) }
            self = .object(out)
        default:
            self = .null
        }
    }

    /// Convert back to a Foundation value suitable for `UNNotificationContent.userInfo`.
    public var foundationValue: Any {
        switch self {
        case .string(let s): return s
        case .number(let n): return n
        case .bool(let b): return b
        case .null: return NSNull()
        case .array(let a): return a.map { $0.foundationValue }
        case .object(let o):
            var out: [String: Any] = [:]
            for (k, v) in o { out[k] = v.foundationValue }
            return out
        }
    }

    public var stringValue: String? {
        if case .string(let s) = self { return s }
        return nil
    }

    public subscript(_ key: String) -> JSONValue? {
        if case .object(let o) = self { return o[key] }
        return nil
    }
}

// MARK: - Deep link extraction

public extension Dictionary where Key == String, Value == JSONValue {
    /// Pull the deep link out of a payload, tolerant of key spelling. Remote pushes from
    /// the backend (and the Android SDK) send `deeplink` (lowercase); locally scheduled
    /// notifications use `deepLink` (camelCase). We accept either, plus `deep_link`, so a
    /// server-sent deep link is surfaced the same as a local one.
    var bmnDeepLink: String? {
        for variant in ["deepLink", "deeplink", "deep_link"] {
            if let value = self[variant]?.stringValue, !value.isEmpty { return value }
        }
        return nil
    }
}

// MARK: - Inbound payload (callbacks / get-intent)

/// A normalized view of a notification, delivered to engine code as JSON.
public struct NotificationData: Codable, Equatable {
    public var id: String
    public var title: String?
    public var body: String?
    public var subtitle: String?
    /// Convenience field lifted out of `userInfo` when present (see `bmnDeepLink` — accepts
    /// `deepLink` / `deeplink` / `deep_link`).
    public var deepLink: String?
    /// The action button identifier, when the event originated from a tap on an action.
    public var actionId: String?
    /// Set when this notification launched the app (cold start).
    public var wasLaunch: Bool?
    /// Free-form payload (remote `aps` siblings or local `userInfo`).
    public var userInfo: [String: JSONValue]

    public init(id: String,
                title: String? = nil,
                body: String? = nil,
                subtitle: String? = nil,
                deepLink: String? = nil,
                actionId: String? = nil,
                wasLaunch: Bool? = nil,
                userInfo: [String: JSONValue] = [:]) {
        self.id = id
        self.title = title
        self.body = body
        self.subtitle = subtitle
        self.deepLink = deepLink
        self.actionId = actionId
        self.wasLaunch = wasLaunch
        self.userInfo = userInfo
    }
}

// MARK: - Local scheduling request

public struct TriggerSpec: Codable, Equatable {
    public enum Kind: String, Codable {
        case immediate
        case timeInterval
        case calendar
    }
    public var type: Kind
    /// For `.timeInterval`: seconds from now (>= 1 when repeating).
    public var seconds: Double?
    public var repeats: Bool?
    /// For `.calendar`: explicit components (any subset).
    public var year: Int?
    public var month: Int?
    public var day: Int?
    public var hour: Int?
    public var minute: Int?
    public var second: Int?
    public var weekday: Int?

    public init(type: Kind = .immediate) { self.type = type }
}

public struct AttachmentSpec: Codable, Equatable {
    public var identifier: String?
    /// Local file path or file URL string. Remote URLs are handled by the NSE, not here.
    public var url: String
    public var typeHint: String?
}

public struct LocalRequest: Codable, Equatable {
    public var id: String
    public var title: String?
    public var body: String?
    public var subtitle: String?
    public var badge: Int?
    public var sound: String?            // "default", "none", or a bundled filename
    public var categoryId: String?
    public var threadId: String?
    public var interruptionLevel: String? // passive | active | timeSensitive | critical
    public var trigger: TriggerSpec?
    public var attachments: [AttachmentSpec]?
    public var userInfo: [String: JSONValue]?
    /// When set, the named template supplies defaults; `templateValues` fills placeholders.
    public var templateId: String?
    public var templateValues: [String: String]?
}

// MARK: - Templates & categories

public struct TemplateSpec: Codable, Equatable {
    public var id: String
    public var titleFormat: String?
    public var bodyFormat: String?
    public var subtitleFormat: String?
    public var sound: String?
    public var categoryId: String?
    public var badge: Int?
    public var defaultAttachments: [AttachmentSpec]?
}

public struct ActionSpec: Codable, Equatable {
    public var id: String
    public var title: String
    public var foreground: Bool?
    public var destructive: Bool?
    public var authenticationRequired: Bool?
}

public struct CategorySpec: Codable, Equatable {
    public var id: String
    public var actions: [ActionSpec]
    /// Placeholder shown when notifications of this category are hidden on the lock screen.
    public var hiddenPreviewsBodyPlaceholder: String?
}

// MARK: - Permission

public struct PermissionOptions: Codable, Equatable {
    public var alert: Bool?
    public var badge: Bool?
    public var sound: Bool?
    public var provisional: Bool?
    public var criticalAlert: Bool?
    public var carPlay: Bool?
}

public struct PermissionResult: Codable, Equatable {
    /// notDetermined | denied | authorized | provisional | ephemeral
    public var status: String
    public var granted: Bool
    public var alert: Bool?
    public var badge: Bool?
    public var sound: Bool?
}

// MARK: - Analytics / delivery receipts (feature 8)

public struct AnalyticsConfig: Codable, Equatable {
    public var enabled: Bool
    public var endpoint: String
    public var headers: [String: String]?
    public var commonParams: [String: JSONValue]?

    public init(enabled: Bool = false,
                endpoint: String = "",
                headers: [String: String]? = nil,
                commonParams: [String: JSONValue]? = nil) {
        self.enabled = enabled
        self.endpoint = endpoint
        self.headers = headers
        self.commonParams = commonParams
    }
}

public struct DeliveryReceipt: Codable, Equatable {
    public var id: String
    /// Seconds since 1970, recorded by whichever process observed delivery.
    public var timestamp: Double
    public var source: String   // "nse" | "app"
    public var userInfo: [String: JSONValue]?
}

// MARK: - JSON helpers

public enum JSON {
    public static let encoder: JSONEncoder = {
        let e = JSONEncoder()
        e.outputFormatting = [.sortedKeys]
        return e
    }()

    public static let decoder = JSONDecoder()

    public static func encode<T: Encodable>(_ value: T) -> String {
        guard let data = try? encoder.encode(value),
              let str = String(data: data, encoding: .utf8) else {
            return "{}"
        }
        return str
    }

    public static func decode<T: Decodable>(_ type: T.Type, from string: String) -> T? {
        guard let data = string.data(using: .utf8) else { return nil }
        return try? decoder.decode(type, from: data)
    }
}
