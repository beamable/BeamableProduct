import XCTest
@testable import BeamableNotifications

final class CoreTests: XCTestCase {

    // MARK: JSONValue

    func testJSONValueRoundTrip() {
        let original: JSONValue = .object([
            "deepLink": .string("game://store"),
            "count": .number(3),
            "flag": .bool(true),
            "nested": .array([.string("a"), .null])
        ])
        let encoded = JSON.encode(original)
        let decoded = JSON.decode(JSONValue.self, from: encoded)
        XCTAssertEqual(decoded, original)
    }

    func testJSONValueFromFoundation() {
        let dict: [AnyHashable: Any] = ["deepLink": "x://y", "n": 2, "b": true]
        let value = JSONValue(any: dict)
        XCTAssertEqual(value["deepLink"]?.stringValue, "x://y")
        XCTAssertEqual(value["n"], .number(2))
        XCTAssertEqual(value["b"], .bool(true))
    }

    // MARK: Templates (feature 4)

    func testTemplatePlaceholderSubstitution() {
        let result = TemplateStore.apply("Hi {name}, you have {count} gifts",
                                         values: ["name": "Ada", "count": "5"])
        XCTAssertEqual(result, "Hi Ada, you have 5 gifts")
    }

    func testTemplateResolveFillsUnsetFields() {
        let store = TemplateStore()
        store.register(TemplateSpec(id: "welcome",
                                    titleFormat: "Welcome {name}",
                                    bodyFormat: "Tap to start",
                                    subtitleFormat: nil,
                                    sound: "default",
                                    categoryId: "GENERIC",
                                    badge: 1,
                                    defaultAttachments: nil))
        var request = LocalRequest(id: "1")
        request.templateId = "welcome"
        request.templateValues = ["name": "Ada"]
        let resolved = store.resolve(request)
        XCTAssertEqual(resolved.title, "Welcome Ada")
        XCTAssertEqual(resolved.body, "Tap to start")
        XCTAssertEqual(resolved.categoryId, "GENERIC")
        XCTAssertEqual(resolved.badge, 1)
    }

    func testTemplateResolveDoesNotOverrideExplicit() {
        let store = TemplateStore()
        store.register(TemplateSpec(id: "t", titleFormat: "Default", bodyFormat: nil,
                                    subtitleFormat: nil, sound: nil, categoryId: nil,
                                    badge: nil, defaultAttachments: nil))
        var request = LocalRequest(id: "1")
        request.title = "Explicit"
        request.templateId = "t"
        XCTAssertEqual(store.resolve(request).title, "Explicit")
    }

    // MARK: Deep link extraction

    func testDeepLinkLiftedFromUserInfo() {
        let request = LocalRequest(id: "1")
        let data = NotificationData(id: request.id,
                                    deepLink: nil,
                                    userInfo: ["deepLink": .string("game://reward/42")])
        // Re-derive the way the manager does it on inbound notifications.
        XCTAssertEqual(data.userInfo["deepLink"]?.stringValue, "game://reward/42")
    }

    /// Remote pushes from the backend (and Android) send `deeplink` (lowercase); local
    /// notifications use `deepLink`. The lift must accept either spelling — this is the
    /// fix for "remote notification deep link not working".
    func testDeepLinkExtractionToleratesKeyVariants() {
        let camel: [String: JSONValue] = ["deepLink": .string("game://a")]
        let lower: [String: JSONValue] = ["deeplink": .string("game://b")]
        let snake: [String: JSONValue] = ["deep_link": .string("game://c")]
        let none: [String: JSONValue] = ["other": .string("x")]
        XCTAssertEqual(camel.bmnDeepLink, "game://a")
        XCTAssertEqual(lower.bmnDeepLink, "game://b")
        XCTAssertEqual(snake.bmnDeepLink, "game://c")
        XCTAssertNil(none.bmnDeepLink)
        // Empty strings are ignored so callers fall through to the next source.
        XCTAssertNil((["deeplink": .string("")] as [String: JSONValue]).bmnDeepLink)
    }

    // MARK: Analytics payload (feature 8)

    func testAnalyticsPayloadHasRichContextAndComposedMessage() {
        let config = AnalyticsConfig(enabled: true,
                                     endpoint: "https://example.com",
                                     commonParams: ["message": .string("📬 label")])
        let payload = AnalyticsPayload.make(
            event: "received",
            source: "nse",
            notificationId: "promo-42",
            title: "Oi Gabriel",
            body: "Fala ai Gabriel",
            deepLink: "123",
            wasForeground: false,
            receivedAtMillis: 1782134720704,
            data: ["deeplink": .string("123")],
            config: config
        )
        XCTAssertEqual(payload["notificationId"]?.stringValue, "promo-42")
        XCTAssertEqual(payload["deepLink"]?.stringValue, "123")
        XCTAssertEqual(payload["wasForeground"], .bool(false))
        XCTAssertEqual(payload["title"]?.stringValue, "Oi Gabriel")
        // The composed `message` carries the label header plus the per-notification context.
        let message = payload["message"]?.stringValue ?? ""
        XCTAssertTrue(message.hasPrefix("📬 label"))
        XCTAssertTrue(message.contains("messageId: promo-42"))
        XCTAssertTrue(message.contains("deepLink: 123"))
        XCTAssertTrue(message.contains("wasForeground: false"))
        XCTAssertTrue(message.contains("receivedAt: 1782134720704"))
    }

    // MARK: LocalRequest decoding from engine JSON

    func testLocalRequestDecodesFromEngineJSON() {
        let json = """
        {"id":"abc","title":"Hi","body":"There",
         "trigger":{"type":"timeInterval","seconds":60,"repeats":false},
         "userInfo":{"deepLink":"app://home"}}
        """
        let request = JSON.decode(LocalRequest.self, from: json)
        XCTAssertEqual(request?.id, "abc")
        XCTAssertEqual(request?.trigger?.type, .timeInterval)
        XCTAssertEqual(request?.trigger?.seconds, 60)
        XCTAssertEqual(request?.userInfo?["deepLink"]?.stringValue, "app://home")
    }

    // MARK: Delivery receipts (feature 8) — without an App Group, ops are no-ops.

    func testSharedConfigWithoutAppGroupIsSafe() {
        let config = SharedConfig(infoDictionary: [:])
        XCTAssertFalse(config.isAvailable)
        config.appendReceipt(DeliveryReceipt(id: "x", timestamp: 0, source: "nse", userInfo: nil))
        XCTAssertEqual(config.drainReceipts(), [])
    }

    // MARK: Plugin transform chain

    func testWillScheduleTransformChainMutates() {
        let registry = PluginRegistry()
        registry.register(TagInjector())
        let out = registry.transformWillSchedule(LocalRequest(id: "1"))
        XCTAssertEqual(out?.userInfo?["injected"]?.stringValue, "yes")
    }

    func testWillScheduleCanDrop() {
        let registry = PluginRegistry()
        registry.register(Dropper())
        XCTAssertNil(registry.transformWillSchedule(LocalRequest(id: "1")))
    }
}

private final class TagInjector: NSObject, NotificationPlugin {
    var id: String { "test.tag" }
    func willSchedule(_ request: LocalRequest) -> LocalRequest? {
        var r = request
        var info = r.userInfo ?? [:]
        info["injected"] = .string("yes")
        r.userInfo = info
        return r
    }
}

private final class Dropper: NSObject, NotificationPlugin {
    var id: String { "test.drop" }
    func willSchedule(_ request: LocalRequest) -> LocalRequest? { nil }
}
