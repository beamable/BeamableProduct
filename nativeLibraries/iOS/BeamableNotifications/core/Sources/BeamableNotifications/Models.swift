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

// MARK: - Campaign intent-data schema (§3.3)

/// A single offer carried by a campaign notification (§3.3). `customData` is free-form
/// (the spec's generic `T`), so it travels as an opaque JSON object and is typed only at
/// the engine/SDK layer. `value` is `string|number` on the wire, kept as a `JSONValue`.
public struct NotificationOffer: Codable, Equatable {
    public var itemId: String?
    public var value: JSONValue?
    public var customData: [String: JSONValue]?

    public init(itemId: String? = nil,
                value: JSONValue? = nil,
                customData: [String: JSONValue]? = nil) {
        self.itemId = itemId
        self.value = value
        self.customData = customData
    }
}

/// The canonical notification intent-data schema (§3.3), parsed out of a notification's
/// `userInfo`. Per Decision Q3 the nested objects (`offers`, `campaignData`) arrive as
/// JSON-encoded **strings** even on iOS, so this parser un-stringifies them. Scalar
/// fields are plain strings. All fields are optional — a notification is only treated as
/// part of a tracked campaign when both `campaignId` and `nodeId` are present (§4.2).
public struct CampaignIntentData: Codable, Equatable {
    public var campaignId: String?
    public var nodeId: String?
    public var gamerTag: String?
    public var accountId: String?
    /// "<cid>.<pid>" realm scope.
    public var cidPid: String?
    public var deeplink: String?
    public var offers: [NotificationOffer]?
    public var campaignData: [String: JSONValue]?

    public init(campaignId: String? = nil,
                nodeId: String? = nil,
                gamerTag: String? = nil,
                accountId: String? = nil,
                cidPid: String? = nil,
                deeplink: String? = nil,
                offers: [NotificationOffer]? = nil,
                campaignData: [String: JSONValue]? = nil) {
        self.campaignId = campaignId
        self.nodeId = nodeId
        self.gamerTag = gamerTag
        self.accountId = accountId
        self.cidPid = cidPid
        self.deeplink = deeplink
        self.offers = offers
        self.campaignData = campaignData
    }

    /// True when this notification belongs to a tracked campaign (§4.2): both
    /// `campaignId` and `nodeId` are present and non-empty.
    public var isTrackedCampaign: Bool {
        guard let c = campaignId, !c.isEmpty, let n = nodeId, !n.isEmpty else { return false }
        return true
    }

    /// Has enough context to authenticate + route a native funnel POST (§4.3): a realm
    /// scope (`cidPid`) and a `gamerTag`, in addition to being a tracked campaign.
    public var canEmitFunnel: Bool {
        guard isTrackedCampaign else { return false }
        guard let scope = cidPid, scope.contains("."),
              let tag = gamerTag, !tag.isEmpty else { return false }
        return true
    }

    /// Split `cidPid` ("<cid>.<pid>") into its parts, if well-formed.
    public var cidAndPid: (cid: String, pid: String)? {
        guard let scope = cidPid else { return nil }
        let parts = scope.split(separator: ".", maxSplits: 1).map(String.init)
        guard parts.count == 2, !parts[0].isEmpty, !parts[1].isEmpty else { return nil }
        return (parts[0], parts[1])
    }
}

public extension Dictionary where Key == String, Value == JSONValue {
    /// Parse the campaign intent-data schema (§3.3) out of a notification's `userInfo`.
    /// Nested `offers`/`campaignData` are accepted both as JSON-encoded strings (the
    /// canonical wire format per Decision Q3) and — defensively — as already-decoded
    /// objects/arrays, since locally-scheduled notifications may carry real nested values.
    var bmnCampaignIntent: CampaignIntentData {
        var intent = CampaignIntentData()
        intent.campaignId = self["campaignId"]?.stringValue
        intent.nodeId = self["nodeId"]?.stringValue
        intent.gamerTag = self["gamerTag"]?.stringValue
        intent.accountId = self["accountId"]?.stringValue
        intent.cidPid = self["cidPid"]?.stringValue
        intent.deeplink = bmnDeepLink

        if let offers = self["offers"] {
            intent.offers = JSONValue.bmnDecodeStringified([NotificationOffer].self, from: offers)
        }
        if let campaignData = self["campaignData"] {
            switch campaignData {
            case .object(let o): intent.campaignData = o
            case .string(let s):
                if let obj = JSON.decode([String: JSONValue].self, from: s) {
                    intent.campaignData = obj
                }
            default: break
            }
        }
        return intent
    }
}

extension JSONValue {
    /// Decode a `Codable` from a `JSONValue` that may be either a JSON-encoded string
    /// (the stringified wire format) or an already-structured value. Returns nil on
    /// mismatch so a malformed field is skipped rather than failing the whole parse.
    static func bmnDecodeStringified<T: Decodable>(_ type: T.Type, from value: JSONValue) -> T? {
        switch value {
        case .string(let s):
            return JSON.decode(type, from: s)
        case .array, .object:
            guard let data = try? JSON.encoder.encode(value) else { return nil }
            return try? JSON.decoder.decode(type, from: data)
        default:
            return nil
        }
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

    // MARK: Campaign intent-data (§3.3) — additive. Lifted out of `userInfo` and surfaced
    // to engines alongside the existing fields. All optional, so the synthesized Codable
    // uses `encodeIfPresent` and omits them when absent — keeping the existing JSON shape
    // (no new keys appear for non-campaign notifications) for backward compatibility.
    public var campaignId: String?
    public var nodeId: String?
    public var gamerTag: String?
    public var accountId: String?
    public var cidPid: String?
    /// Parsed offers (un-stringified from the §3.3 wire format).
    public var offers: [NotificationOffer]?
    /// Parsed free-form campaign data (un-stringified).
    public var campaignData: [String: JSONValue]?

    /// Free-form payload (remote `aps` siblings or local `userInfo`).
    public var userInfo: [String: JSONValue]

    public init(id: String,
                title: String? = nil,
                body: String? = nil,
                subtitle: String? = nil,
                deepLink: String? = nil,
                actionId: String? = nil,
                wasLaunch: Bool? = nil,
                campaignId: String? = nil,
                nodeId: String? = nil,
                gamerTag: String? = nil,
                accountId: String? = nil,
                cidPid: String? = nil,
                offers: [NotificationOffer]? = nil,
                campaignData: [String: JSONValue]? = nil,
                userInfo: [String: JSONValue] = [:]) {
        self.id = id
        self.title = title
        self.body = body
        self.subtitle = subtitle
        self.deepLink = deepLink
        self.actionId = actionId
        self.wasLaunch = wasLaunch
        self.campaignId = campaignId
        self.nodeId = nodeId
        self.gamerTag = gamerTag
        self.accountId = accountId
        self.cidPid = cidPid
        self.offers = offers
        self.campaignData = campaignData
        self.userInfo = userInfo
    }

    /// The campaign intent view of this notification.
    public var campaignIntent: CampaignIntentData {
        CampaignIntentData(campaignId: campaignId, nodeId: nodeId, gamerTag: gamerTag,
                           accountId: accountId, cidPid: cidPid,
                           deeplink: deepLink ?? userInfo.bmnDeepLink,
                           offers: offers, campaignData: campaignData)
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

// MARK: - Delivery receipts (feature 8)

public struct DeliveryReceipt: Codable, Equatable {
    public var id: String
    /// Seconds since 1970, recorded by whichever process observed delivery.
    public var timestamp: Double
    public var source: String   // "nse" | "app"
    public var userInfo: [String: JSONValue]?
}

// MARK: - Beamable analytics auth + funnel (§4)

/// Player session credentials + realm routing, persisted by the SDK into the App Group so
/// the native funnel POST (and the closed-app NSE) can authenticate without the engine VM
/// being alive (§4.3, Decision Q5). The SDK keeps this updated on login/refresh and clears
/// it on logout. No realm secret is ever stored here.
public struct AuthConfig: Codable, Equatable {
    public var accessToken: String?
    public var refreshToken: String?
    /// Absolute epoch **milliseconds** when `accessToken` expires. This is the canonical
    /// cross-platform contract: the engine SDKs (Unity C#, React Native TS) write this field
    /// as absolute epoch-ms via `bmn_configureAuth`, matching Android. The native refresh
    /// path (`BeamableAnalytics.refresh`) also stores the new expiry as epoch-ms.
    public var accessTokenExpiresAt: Double?
    public var cid: String?
    public var pid: String?
    /// API host, e.g. "https://api.beamable.com" (no trailing slash required).
    public var host: String?

    public init(accessToken: String? = nil,
                refreshToken: String? = nil,
                accessTokenExpiresAt: Double? = nil,
                cid: String? = nil,
                pid: String? = nil,
                host: String? = nil) {
        self.accessToken = accessToken
        self.refreshToken = refreshToken
        self.accessTokenExpiresAt = accessTokenExpiresAt
        self.cid = cid
        self.pid = pid
        self.host = host
    }

    /// True when the access token is missing or within `skew` seconds of expiry.
    ///
    /// `accessTokenExpiresAt` is absolute epoch **milliseconds** (canonical contract), so the
    /// `now` (epoch seconds, the default) and `skew` (seconds) are converted to milliseconds
    /// before the comparison. The default `skew` is 60s to match Android.
    public func isAccessTokenStale(now: Double = Date().timeIntervalSince1970,
                                   skew: Double = 60) -> Bool {
        guard let token = accessToken, !token.isEmpty else { return true }
        // A nil or <= 0 expiry means "unknown" — do NOT proactively refresh; rely on the
        // 401/403 retry path instead. Only compute staleness against a real positive expiry
        // (matches Android's `expiresAt in 1 until (now + skew)`).
        guard let expMs = accessTokenExpiresAt, expMs > 0 else { return false }
        let nowMs = now * 1000.0
        let skewMs = skew * 1000.0
        return nowMs >= (expMs - skewMs)
    }
}

/// One funnel event (§4.6) — the engine-facing / wire param bag. `op`/`e`/`c`/`p` are
/// assembled by `BeamableAnalytics` when POSTing; this struct is what we persist for replay
/// when a closed-app POST can't finish in the NSE budget (§4.3 fallback).
public struct FunnelEvent: Codable, Equatable {
    public var funnelType: String          // Sent | Received | Opened | Clicked | Converted
    public var campaignId: String
    public var nodeId: String
    public var gamerTag: String?
    public var accountId: String?
    public var cidPid: String?
    public var deeplink: String?
    /// The single offer this event concerns (omitted when none).
    public var offerData: NotificationOffer?
    /// Timestamp the event was observed (seconds since 1970).
    public var timestamp: Double

    public init(funnelType: String,
                campaignId: String,
                nodeId: String,
                gamerTag: String? = nil,
                accountId: String? = nil,
                cidPid: String? = nil,
                deeplink: String? = nil,
                offerData: NotificationOffer? = nil,
                timestamp: Double = Date().timeIntervalSince1970) {
        self.funnelType = funnelType
        self.campaignId = campaignId
        self.nodeId = nodeId
        self.gamerTag = gamerTag
        self.accountId = accountId
        self.cidPid = cidPid
        self.deeplink = deeplink
        self.offerData = offerData
        self.timestamp = timestamp
    }

    /// Stable identity for replay dedup (§4.3). The same Received event can be enqueued twice
    /// — once by the NSE safety-timer persist and once by `emit`'s own persist-on-failure —
    /// which would otherwise replay (and double-count) the same funnel stage. Keyed on the
    /// campaign coordinates + funnel stage + gamerTag + the specific offer it concerns (so
    /// distinct Clicked/Converted events for different offers of the same campaign are NOT
    /// collapsed). `gamerTag` is included so an offline account-switch on a shared device
    /// doesn't collapse two players' otherwise-identical events.
    /// Deliberately excludes `timestamp`, which differs between the two enqueue paths.
    public var dedupKey: String {
        [funnelType, campaignId, nodeId, gamerTag ?? "", offerData?.itemId ?? ""].joined(separator: "|")
    }
}

/// The five funnel stages (§4.5).
public enum FunnelType: String {
    case sent = "Sent"
    case received = "Received"
    case opened = "Opened"
    case clicked = "Clicked"
    case converted = "Converted"
}

/// Engine-facing request for the offer-tracking helpers (§4.7). Carries the campaign
/// context that arrived in the notification's intent data so an in-app offer click/convert
/// can be attributed back to the originating campaign. The `offer` is the single offer the
/// user acted on. `gamerTag`/`accountId`/`cidPid` are optional here because the helper
/// falls back to the persisted `AuthConfig` (cid/pid) when the caller omits the scope.
public struct OfferTrackRequest: Codable, Equatable {
    public var campaignId: String
    public var nodeId: String
    public var gamerTag: String?
    public var accountId: String?
    public var cidPid: String?
    public var deeplink: String?
    public var offer: NotificationOffer?

    public init(campaignId: String,
                nodeId: String,
                gamerTag: String? = nil,
                accountId: String? = nil,
                cidPid: String? = nil,
                deeplink: String? = nil,
                offer: NotificationOffer? = nil) {
        self.campaignId = campaignId
        self.nodeId = nodeId
        self.gamerTag = gamerTag
        self.accountId = accountId
        self.cidPid = cidPid
        self.deeplink = deeplink
        self.offer = offer
    }

    /// Convert to a `CampaignIntentData`, filling scope from the persisted auth config when
    /// the caller didn't supply it (cid/pid → cidPid).
    public func intent(fallbackAuth: AuthConfig?) -> CampaignIntentData {
        var resolvedCidPid = cidPid
        if resolvedCidPid == nil, let cid = fallbackAuth?.cid, let pid = fallbackAuth?.pid {
            resolvedCidPid = "\(cid).\(pid)"
        }
        return CampaignIntentData(campaignId: campaignId,
                                  nodeId: nodeId,
                                  gamerTag: gamerTag,
                                  accountId: accountId,
                                  cidPid: resolvedCidPid,
                                  deeplink: deeplink,
                                  offers: offer.map { [$0] },
                                  campaignData: nil)
    }
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
