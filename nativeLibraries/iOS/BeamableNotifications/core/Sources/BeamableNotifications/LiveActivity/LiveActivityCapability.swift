import Foundation
#if canImport(ActivityKit)
import ActivityKit
#endif

/// Whether this device can actually put a Live Activity on screen for one attributes type.
///
/// This is not cosmetic. The rail decides between a Live Activity and a plain notification by whether
/// the device published a push-to-start token: a token means "send ActivityKit", no token means
/// `FallBackToAlert` and the player gets a notification with the authored buttons instead. So an
/// over-optimistic capability check doesn't degrade the UI — it delivers a Live Activity that renders
/// nothing, with no notification to fall back to. Every part of the gate must hold before a token is
/// published, and the token must be withdrawn when any of them stops holding.
public struct LiveActivityCapability: Codable, Equatable {

    /// Unqualified Swift attributes type name — the wire identity shared with `PushRailService`.
    public var attributesType: String
    /// Short slug the engine bridges emit (`actions` | `animated` | `countdown`).
    public var activityType: String
    /// iOS 17.2+, the floor for push-to-start tokens. Below it, a Live Activity can only be started
    /// by the app in the foreground, which a push can never do.
    public var supported: Bool
    /// `ActivityAuthorizationInfo().areActivitiesEnabled` — the player's Settings toggle.
    public var enabled: Bool
    /// Declared in the app's `Info.plist` under `BMNLiveActivityTypes`.
    public var declared: Bool
    /// A WidgetKit extension is actually embedded in this build.
    public var widgetPresent: Bool

    /// All four must hold. When false the caller must NOT publish this type's push-to-start token, and
    /// must withdraw one it published earlier.
    public var available: Bool { supported && enabled && declared && widgetPresent }

    /// Why it is unavailable, for logs and for the engine-side event. Empty when available.
    public var reason: String {
        if available { return "" }
        if !supported { return "requires iOS 17.2 or later" }
        if !enabled { return "player has Live Activities turned off in Settings" }
        if !declared { return "\(attributesType) is not listed in Info.plist BMNLiveActivityTypes" }
        return "no WidgetKit extension is embedded in this app"
    }
}

/// Reads the device + app facts behind `LiveActivityCapability`.
public enum LiveActivitySupport {

    /// Info.plist key listing the attributes types this app embeds widget UI for.
    ///
    /// Follows the existing `BMNServicePlugins` / `BMNContentRenderers` discovery convention: an array
    /// of strings the host target declares. There is no runtime registry of
    /// `ActivityConfiguration(for:)` types to query, so a declaration is the only way to know WHICH
    /// type has UI — the Expo config plugin derives this from the widget files it actually stages, so
    /// it cannot drift from the build.
    public static let declarationKey = "BMNLiveActivityTypes"

    /// The attributes types the SDK ships and observes, with the slug each engine bridge emits.
    /// This pairing is the wire contract with the portal / `PushRailService`; the ReactNative sample's
    /// slug→type map in `src/beam/liveActivity.ts` mirrors it.
    public static let knownTypes: [(activityType: String, attributesType: String)] = [
        ("actions", "BeamActionsActivityAttributes"),
        ("animated", "BeamAnimatedActivityAttributes"),
        ("countdown", "BeamCountdownActivityAttributes"),
    ]

    public static func declaredTypes(infoDictionary: [String: Any]? = Bundle.main.infoDictionary)
        -> [String] {
        infoDictionary?[declarationKey] as? [String] ?? []
    }

    /// True when this app bundle embeds a WidgetKit extension.
    ///
    /// A structural cross-check on the declaration above, which can lie (an `app.json` copied between
    /// projects, or a widget target that failed to embed). Reading your own bundle is sandbox-legal and
    /// costs one directory scan, done once. It answers "is there ANY widget", not "does a widget render
    /// THAT type" — only the declaration can speak to that, so both are required.
    public static let hasWidgetExtension: Bool = {
        guard let plugins = Bundle.main.builtInPlugInsURL,
              let contents = try? FileManager.default.contentsOfDirectory(
                  at: plugins, includingPropertiesForKeys: nil) else { return false }
        for url in contents where url.pathExtension == "appex" {
            guard let bundle = Bundle(url: url),
                  let ext = bundle.infoDictionary?["NSExtension"] as? [String: Any],
                  let point = ext["NSExtensionPointIdentifier"] as? String else { continue }
            if point == "com.apple.widgetkit-extension" { return true }
        }
        return false
    }()

    /// Whether the OS can start a Live Activity from a push at all (push-to-start floor).
    public static var isPushToStartSupported: Bool {
        if #available(iOS 17.2, *) { return true }
        return false
    }

    /// Whether the player has Live Activities enabled. `true` on a build without ActivityKit only in
    /// the sense that the other gates will fail first.
    public static var areActivitiesEnabled: Bool {
        #if canImport(ActivityKit)
        if #available(iOS 16.1, *) { return ActivityAuthorizationInfo().areActivitiesEnabled }
        #endif
        return false
    }

    public static func capability(for attributesType: String,
                                  activityType: String? = nil,
                                  infoDictionary: [String: Any]? = Bundle.main.infoDictionary)
        -> LiveActivityCapability {
        let slug = activityType
            ?? knownTypes.first { $0.attributesType == attributesType }?.activityType
            ?? attributesType
        return LiveActivityCapability(
            attributesType: attributesType,
            activityType: slug,
            supported: isPushToStartSupported,
            enabled: areActivitiesEnabled,
            declared: declaredTypes(infoDictionary: infoDictionary).contains(attributesType),
            widgetPresent: hasWidgetExtension
        )
    }

    /// Capability for every type the SDK knows about, in a stable order.
    public static func capabilities(
        infoDictionary: [String: Any]? = Bundle.main.infoDictionary
    ) -> [LiveActivityCapability] {
        knownTypes.map {
            capability(for: $0.attributesType,
                       activityType: $0.activityType,
                       infoDictionary: infoDictionary)
        }
    }
}
