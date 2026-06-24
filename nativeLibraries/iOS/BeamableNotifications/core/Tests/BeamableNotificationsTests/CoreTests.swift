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

    /// FIX 4: a refresh that omits `expires_in` (nil/<=0 expiresAt) must NOT wipe the prior
    /// stored expiry — otherwise `isAccessTokenStale` would permanently return false. Only a
    /// valid positive new expiry overwrites it (matches Android's `if (expiresInMs > 0)`).
    func testUpdateTokensPreservesPriorExpiryWhenNewIsNil() {
        let suite = "bmn.test.\(UUID().uuidString)"
        let config = SharedConfig(infoDictionary: ["BMNAppGroup": suite])
        // Tests that target a real app-group suite are skipped when the OS denies it.
        guard config.isAvailable else { return }
        defer { config.clearAuthConfig() }

        config.saveAuthConfig(AuthConfig(accessToken: "old", refreshToken: "r",
                                         accessTokenExpiresAt: 5_000_000))
        // Refresh response without expires_in → expiresAt nil: keep prior expiry.
        config.updateTokens(accessToken: "new", refreshToken: "r2", expiresAt: nil)
        XCTAssertEqual(config.loadAuthConfig()?.accessToken, "new")
        XCTAssertEqual(config.loadAuthConfig()?.accessTokenExpiresAt, 5_000_000,
                       "nil expiresAt must not wipe the prior expiry")

        // <= 0 is also treated as 'unknown' and preserves prior.
        config.updateTokens(accessToken: "new2", refreshToken: nil, expiresAt: 0)
        XCTAssertEqual(config.loadAuthConfig()?.accessTokenExpiresAt, 5_000_000)

        // A valid positive expiry DOES overwrite.
        config.updateTokens(accessToken: "new3", refreshToken: nil, expiresAt: 9_000_000)
        XCTAssertEqual(config.loadAuthConfig()?.accessTokenExpiresAt, 9_000_000)
    }

    // MARK: Campaign intent-data schema (§3.3)

    func testCampaignIntentParsesStringifiedOffersAndCampaignData() {
        let offersJSON = #"[{"itemId":"gold_pack","value":"5","customData":{"tier":"gold"}}]"#
        let info: [String: JSONValue] = [
            "campaignId": .string("camp-1"),
            "nodeId": .string("node-1"),
            "gamerTag": .string("123"),
            "accountId": .string("acct-1"),
            "cidPid": .string("CID.PID"),
            "deeplink": .string("game://store"),
            "offers": .string(offersJSON),
            "campaignData": .string(#"{"theme":"summer"}"#)
        ]
        let intent = info.bmnCampaignIntent
        XCTAssertEqual(intent.campaignId, "camp-1")
        XCTAssertEqual(intent.nodeId, "node-1")
        XCTAssertEqual(intent.cidPid, "CID.PID")
        XCTAssertEqual(intent.deeplink, "game://store")
        XCTAssertTrue(intent.isTrackedCampaign)
        XCTAssertTrue(intent.canEmitFunnel)
        XCTAssertEqual(intent.offers?.count, 1)
        XCTAssertEqual(intent.offers?.first?.itemId, "gold_pack")
        XCTAssertEqual(intent.offers?.first?.value, .string("5"))
        XCTAssertEqual(intent.offers?.first?.customData?["tier"]?.stringValue, "gold")
        XCTAssertEqual(intent.campaignData?["theme"]?.stringValue, "summer")
        XCTAssertEqual(intent.cidAndPid?.cid, "CID")
        XCTAssertEqual(intent.cidAndPid?.pid, "PID")
    }

    func testCampaignIntentNotTrackedWhenFieldsMissing() {
        let info: [String: JSONValue] = ["campaignId": .string("c")] // no nodeId
        XCTAssertFalse(info.bmnCampaignIntent.isTrackedCampaign)
        XCTAssertFalse(info.bmnCampaignIntent.canEmitFunnel)
    }

    func testNotificationDataCodableStaysAdditive() {
        // A non-campaign notification must not gain campaign keys in its JSON.
        let note = NotificationData(id: "n1", title: "T", userInfo: ["deepLink": .string("a://b")])
        let json = JSON.encode(note)
        XCTAssertFalse(json.contains("campaignId"))
        XCTAssertFalse(json.contains("offers"))
        // And it round-trips.
        XCTAssertEqual(JSON.decode(NotificationData.self, from: json), note)
    }

    // MARK: Beamable funnel CoreEvent (§4.6)

    func testCoreEventShapeMatchesContract() {
        let event = FunnelEvent(funnelType: FunnelType.received.rawValue,
                                campaignId: "camp-1", nodeId: "node-1",
                                gamerTag: "123", accountId: "acct-1", cidPid: "CID.PID",
                                deeplink: "game://x",
                                offerData: NotificationOffer(itemId: "gold", value: .number(5)),
                                timestamp: 0)
        let core = BeamableAnalytics.makeCoreEvent(for: event)
        XCTAssertEqual(core["op"]?.stringValue, "g.core")
        XCTAssertEqual(core["e"]?.stringValue, "Received")
        XCTAssertEqual(core["c"]?.stringValue, "notification_funnel")
        let p = core["p"]
        XCTAssertEqual(p?["campaignId"]?.stringValue, "camp-1")
        XCTAssertEqual(p?["nodeId"]?.stringValue, "node-1")
        XCTAssertEqual(p?["gamerTag"]?.stringValue, "123")
        XCTAssertEqual(p?["cidPid"]?.stringValue, "CID.PID")
        XCTAssertEqual(p?["funnelType"]?.stringValue, "Received")
        XCTAssertEqual(p?["offerData"]?["itemId"]?.stringValue, "gold")
        // Body is a JSON array of one CoreEvent.
        let body = BeamableAnalytics.makeBody(for: [event])
        XCTAssertNotNil(body)
        if let body = body, let decoded = try? JSON.decoder.decode(JSONValue.self, from: body) {
            if case .array(let arr) = decoded { XCTAssertEqual(arr.count, 1) } else { XCTFail("not array") }
        }
    }

    func testMakeEventReturnsNilForUntrackedCampaign() {
        let intent = CampaignIntentData(campaignId: "c") // missing nodeId
        XCTAssertNil(BeamableAnalytics.makeEvent(.received, intent: intent))
    }

    func testMakeEventDoesNotAttributeCarriedOfferForStageEvents() {
        // A campaign carries an offer, but Received/Opened must NOT attach it as offerData —
        // only an explicitly-passed offer (Clicked/Converted) is attributed.
        let intent = CampaignIntentData(campaignId: "c", nodeId: "n", gamerTag: "1",
                                        cidPid: "CID.PID",
                                        offers: [NotificationOffer(itemId: "gold")])
        let received = BeamableAnalytics.makeEvent(.received, intent: intent)
        XCTAssertNotNil(received)
        XCTAssertNil(received?.offerData, "Received must not attribute a carried campaign offer")

        let clicked = BeamableAnalytics.makeEvent(.clicked, intent: intent,
                                                  offer: NotificationOffer(itemId: "gold"))
        XCTAssertEqual(clicked?.offerData?.itemId, "gold", "Explicit offer must be attached")
    }

    func testAccessTokenStaleness() {
        // `now` is epoch SECONDS; `accessTokenExpiresAt` is absolute epoch MILLISECONDS.
        // Default skew is 60s. Stale when nowMs >= expMs - 60_000.
        let now: Double = 1000 // 1_000_000 ms
        XCTAssertTrue(AuthConfig(accessToken: nil).isAccessTokenStale(now: now))
        XCTAssertTrue(AuthConfig(accessToken: "t", accessTokenExpiresAt: 1_030_000).isAccessTokenStale(now: now)) // within skew
        XCTAssertFalse(AuthConfig(accessToken: "t", accessTokenExpiresAt: 5_000_000).isAccessTokenStale(now: now))
        XCTAssertFalse(AuthConfig(accessToken: "t").isAccessTokenStale(now: now)) // no expiry known
    }

    /// FIX 5: a nil OR <= 0 expiry is "unknown" and must NOT be treated as stale (Android
    /// canonical behavior — rely on the 401/403 retry path, don't proactively refresh).
    func testAccessTokenUnknownExpiryIsNotStale() {
        let now: Double = 1000
        XCTAssertFalse(AuthConfig(accessToken: "t", accessTokenExpiresAt: nil).isAccessTokenStale(now: now))
        XCTAssertFalse(AuthConfig(accessToken: "t", accessTokenExpiresAt: 0).isAccessTokenStale(now: now))
        XCTAssertFalse(AuthConfig(accessToken: "t", accessTokenExpiresAt: -5).isAccessTokenStale(now: now))
        // A missing/empty token is still stale regardless of expiry.
        XCTAssertTrue(AuthConfig(accessToken: "", accessTokenExpiresAt: 0).isAccessTokenStale(now: now))
    }

    /// FIX 3: pending-funnel dedup key is stable across the two enqueue paths (the NSE safety
    /// persist and emit's persist-on-failure) so the same Received stage isn't replayed twice,
    /// yet distinct stages / offers stay distinct.
    func testFunnelEventDedupKeyIsStable() {
        let a = FunnelEvent(funnelType: "Received", campaignId: "c", nodeId: "n",
                            gamerTag: "g", cidPid: "CID.PID", timestamp: 100)
        let b = FunnelEvent(funnelType: "Received", campaignId: "c", nodeId: "n",
                            gamerTag: "g", cidPid: "CID.PID", timestamp: 999) // different timestamp
        XCTAssertEqual(a.dedupKey, b.dedupKey, "timestamp must not affect the dedup key")

        let opened = FunnelEvent(funnelType: "Opened", campaignId: "c", nodeId: "n", timestamp: 0)
        XCTAssertNotEqual(a.dedupKey, opened.dedupKey, "different funnel stage must differ")

        let clickGold = FunnelEvent(funnelType: "Clicked", campaignId: "c", nodeId: "n",
                                    offerData: NotificationOffer(itemId: "gold"), timestamp: 0)
        let clickGem = FunnelEvent(funnelType: "Clicked", campaignId: "c", nodeId: "n",
                                   offerData: NotificationOffer(itemId: "gem"), timestamp: 0)
        XCTAssertNotEqual(clickGold.dedupKey, clickGem.dedupKey, "different offer must differ")
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
