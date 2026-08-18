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

    // MARK: Campaign intent-data schema

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

    // MARK: Beamable funnel CoreEvent

    func testCoreEventShapeMatchesContract() {
        let event = FunnelEvent(funnelType: FunnelType.received.rawValue,
                                campaignId: "camp-1", nodeId: "node-1",
                                gamerTag: "123", accountId: "acct-1", cidPid: "CID.PID",
                                deeplink: "game://x",
                                offers: [NotificationOffer(itemId: "gold", value: .number(5))],
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
        // offerData is a single column: a stringified JSON array of offer objects.
        let offers = p?["offerData"]?.stringValue.flatMap { JSON.decode([NotificationOffer].self, from: $0) }
        XCTAssertEqual(offers?.first?.itemId, "gold")
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

    func testMakeEventCarriesOffersForStagesAndSingleOfferForActions() {
        // Stage events (Received/Opened) carry every offer the push held, so the funnel rows are
        // consistent with the microservice "Sent" event.
        let intent = CampaignIntentData(campaignId: "c", nodeId: "n", gamerTag: "1",
                                        cidPid: "CID.PID",
                                        offers: [NotificationOffer(itemId: "gold"),
                                                 NotificationOffer(itemId: "gem")])
        let received = BeamableAnalytics.makeEvent(.received, intent: intent)
        XCTAssertNotNil(received)
        XCTAssertEqual(received?.offers?.count, 2, "Received carries all carried offers")
        XCTAssertEqual(received?.offers?.first?.itemId, "gold")

        // Clicked/Converted attach ONLY the explicitly-passed offer, not the carried list.
        let clicked = BeamableAnalytics.makeEvent(.clicked, intent: intent,
                                                  offer: NotificationOffer(itemId: "silver"))
        XCTAssertEqual(clicked?.offers?.count, 1)
        XCTAssertEqual(clicked?.offers?.first?.itemId, "silver", "Explicit offer wins over carried")
    }

    func testMakeParamsEmitsOfferArrayAndCampaignData() {
        let event = FunnelEvent(funnelType: "Received", campaignId: "c", nodeId: "n",
                                gamerTag: "g", cidPid: "CID.PID",
                                offers: [NotificationOffer(itemId: "gold", value: .number(5),
                                                           customData: ["k": .string("v")]),
                                         NotificationOffer(itemId: "gem")],
                                campaignData: ["season": .string("summer")],
                                timestamp: 0)
        let p = BeamableAnalytics.makeParams(for: event)
        // offerData: single stringified JSON array. Decode generically because customData is itself
        // a stringified JSON string (Athena-safe), not a nested object.
        guard case .array(let arr)? = p["offerData"]?.stringValue.flatMap({ JSON.decode(JSONValue.self, from: $0) }) else {
            return XCTFail("offerData should be a stringified JSON array")
        }
        XCTAssertEqual(arr.count, 2)
        XCTAssertEqual(arr.first?["itemId"]?.stringValue, "gold")
        // customData is a stringified JSON string — parse the inner string to read its fields.
        let cdInner = arr.first?["customData"]?.stringValue.flatMap { JSON.decode(JSONValue.self, from: $0) }
        XCTAssertEqual(cdInner?["k"]?.stringValue, "v")
        // campaignData: single stringified JSON object.
        let cd = p["campaignData"]?.stringValue.flatMap { JSON.decode(JSONValue.self, from: $0) }
        XCTAssertEqual(cd?["season"]?.stringValue, "summer")
    }

    // MARK: Campaign attribution stamp (beam_outreach / trackId)

    func testCampaignIntentParsesTheAttributionStamp() {
        // The push spells the join key `beam_outreach`; engine code handing an intent object back
        // uses the field name. Both must resolve, or the echoed funnel loses its attribution.
        let fromPush: [String: JSONValue] = [
            "campaignId": .string("c"), "nodeId": .string("n"),
            "beam_outreach": .string("outreach-1"), "trackId": .string("campaign:c:1:send")
        ]
        XCTAssertEqual(fromPush.bmnCampaignIntent.outreachId, "outreach-1")
        XCTAssertEqual(fromPush.bmnCampaignIntent.trackId, "campaign:c:1:send")

        let fromEngine: [String: JSONValue] = [
            "campaignId": .string("c"), "nodeId": .string("n"),
            "outreachId": .string("outreach-2")
        ]
        XCTAssertEqual(fromEngine.bmnCampaignIntent.outreachId, "outreach-2")
    }

    func testMakeParamsEchoesTheAttributionStamp() {
        // CampaignEventProcessor.ProcessAttributedStage reads exactly these two param names; if
        // either is missing or renamed the stage is silently not counted in the campaign funnel.
        let intent = CampaignIntentData(campaignId: "c", nodeId: "n", gamerTag: "1",
                                        cidPid: "CID.PID",
                                        outreachId: "outreach-1", trackId: "campaign:c:1:send")
        guard let event = BeamableAnalytics.makeEvent(.clicked, intent: intent) else {
            return XCTFail("clicked event should be built for a tracked campaign")
        }
        let p = BeamableAnalytics.makeParams(for: event)
        XCTAssertEqual(p["outreachId"]?.stringValue, "outreach-1")
        XCTAssertEqual(p["trackId"]?.stringValue, "campaign:c:1:send")
    }

    func testMakeParamsOmitsTheStampWhenAbsent() {
        // A hand-built funnel (no originating push) must not emit empty attribution keys — an empty
        // trackId would fail CampaignSendAttribution.TryParse anyway, but the columns stay clean.
        let event = FunnelEvent(funnelType: "Clicked", campaignId: "c", nodeId: "n",
                                gamerTag: "1", cidPid: "CID.PID",
                                outreachId: "", trackId: nil, timestamp: 0)
        let p = BeamableAnalytics.makeParams(for: event)
        XCTAssertNil(p["outreachId"])
        XCTAssertNil(p["trackId"])
    }

    func testFunnelEventRoundTripsTheStampForReplay() {
        // The killed-app path persists the event and replays it on next open; dropping the stamp
        // there would make every replayed Clicked invisible to the campaign funnel.
        let event = FunnelEvent(funnelType: "Clicked", campaignId: "c", nodeId: "n",
                                outreachId: "outreach-1", trackId: "campaign:c:1:send",
                                timestamp: 0)
        let decoded = JSON.decode(FunnelEvent.self, from: JSON.encode(event))
        XCTAssertEqual(decoded?.outreachId, "outreach-1")
        XCTAssertEqual(decoded?.trackId, "campaign:c:1:send")
    }

    func testOfferTrackRequestCarriesTheStampIntoTheIntent() {
        let request = OfferTrackRequest(campaignId: "c", nodeId: "n", gamerTag: "1",
                                        cidPid: "CID.PID",
                                        outreachId: "outreach-1", trackId: "campaign:c:1:send",
                                        offer: NotificationOffer(itemId: "gold"))
        let intent = request.intent(fallbackAuth: nil)
        XCTAssertEqual(intent.outreachId, "outreach-1")
        XCTAssertEqual(intent.trackId, "campaign:c:1:send")
    }

    func testFunnelEventCodableRoundTripsOffersAndCampaignData() {
        let event = FunnelEvent(funnelType: "Received", campaignId: "c", nodeId: "n",
                                offers: [NotificationOffer(itemId: "gold")],
                                campaignData: ["season": .string("summer")],
                                timestamp: 0)
        let decoded = JSON.decode(FunnelEvent.self, from: JSON.encode(event))
        XCTAssertEqual(decoded?.offers?.first?.itemId, "gold")
        XCTAssertEqual(decoded?.campaignData?["season"]?.stringValue, "summer")
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
                                    offers: [NotificationOffer(itemId: "gold")], timestamp: 0)
        let clickGem = FunnelEvent(funnelType: "Clicked", campaignId: "c", nodeId: "n",
                                   offers: [NotificationOffer(itemId: "gem")], timestamp: 0)
        XCTAssertNotEqual(clickGold.dedupKey, clickGem.dedupKey, "different offer must differ")

        // Two players' events on a shared device (offline account-switch) differ ONLY by
        // gamerTag — they must NOT collapse into one dedup key.
        let playerA = FunnelEvent(funnelType: "Received", campaignId: "c", nodeId: "n",
                                  gamerTag: "11111", timestamp: 0)
        let playerB = FunnelEvent(funnelType: "Received", campaignId: "c", nodeId: "n",
                                  gamerTag: "22222", timestamp: 0)
        XCTAssertNotEqual(playerA.dedupKey, playerB.dedupKey, "different gamerTag must differ")
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

    // MARK: Action buttons (the `buttons` wire key)

    /// The rail stringifies every non-scalar, so this is the shape that actually arrives on device.
    func testButtonsParseFromJSONString() {
        let raw = "[{\"id\":\"claim\",\"title\":\"Claim\",\"role\":\"default\"}," +
                  "{\"id\":\"dismiss\",\"title\":\"No thanks\",\"role\":\"destructive\"}]"
        let buttons = BeamActionButtons.parse(userInfo: ["buttons": raw])
        XCTAssertEqual(buttons.count, 2)
        XCTAssertEqual(buttons[0], BeamActionButton(id: "claim", title: "Claim", role: "default"))
        XCTAssertTrue(buttons[1].isDestructive)
    }

    /// A locally-scheduled notification or a hand-written `simctl push` can carry a real array.
    func testButtonsParseFromArray() {
        let raw: [[String: Any]] = [["id": "claim", "title": "Claim", "role": "default"]]
        let buttons = BeamActionButtons.parse(userInfo: ["buttons": raw])
        XCTAssertEqual(buttons.map(\.id), ["claim"])
    }

    /// A malformed value must degrade the buttons, never the notification — the caller falls back to
    /// the built-in pair on an empty result.
    func testButtonsParseTolerantOfGarbage() {
        XCTAssertTrue(BeamActionButtons.parse(userInfo: ["buttons": "not json"]).isEmpty)
        XCTAssertTrue(BeamActionButtons.parse(userInfo: ["buttons": ""]).isEmpty)
        XCTAssertTrue(BeamActionButtons.parse(userInfo: ["buttons": 42]).isEmpty)
        XCTAssertTrue(BeamActionButtons.parse(userInfo: [:]).isEmpty)
    }

    /// Missing keys default rather than throwing away the whole set.
    func testButtonsPartialEntryDecodes() {
        let buttons = BeamActionButtons.parse(userInfo: ["buttons": "[{\"id\":\"a\",\"title\":\"A\"}]"])
        XCTAssertEqual(buttons.first?.role, "default")
        XCTAssertFalse(buttons.first?.isDestructive ?? true)
    }

    func testButtonsSanitizeDropsUnusableDedupesAndCaps() {
        let input = [
            BeamActionButton(id: "", title: "blank id", role: "default"),
            BeamActionButton(id: "no-title", title: "  ", role: "default"),
            // Would be indistinguishable from "player tapped the body" in the tap path.
            BeamActionButton(id: "com.apple.UNNotificationDefaultActionIdentifier", title: "X"),
            BeamActionButton(id: "a", title: "A"),
            BeamActionButton(id: "a", title: "A duplicate"),
            BeamActionButton(id: "b", title: "B"),
            BeamActionButton(id: "c", title: "C"),
        ]
        let out = BeamActionButtons.sanitize(input)
        XCTAssertEqual(out.map(\.id), ["a", "b"], "keeps first-wins order, capped at maxButtons")
        XCTAssertEqual(BeamActionButtons.maxButtons, 2)
    }

    /// The id must be stable across processes and launches: the NSE and the app both compute it, and
    /// repeated pushes of one campaign must reuse a single registration. A literal expectation here is
    /// deliberate — it fails loudly if the hash is ever refactored.
    func testCategoryIdIsDeterministic() {
        let buttons = [BeamActionButton(id: "claim", title: "Claim"),
                       BeamActionButton(id: "dismiss", title: "No thanks", role: "destructive")]
        let id = BeamActionButtons.categoryId(for: buttons)
        XCTAssertEqual(id, BeamActionButtons.categoryId(for: buttons))
        XCTAssertTrue(BeamActionButtons.isSynthesized(id))
        XCTAssertEqual(id, "beam_actions_" + BeamActionButtons.fnv1a64Hex(
            "claim\u{01}Claim\u{01}default\u{02}dismiss\u{01}No thanks\u{01}destructive"))
    }

    func testCategoryIdVariesWithTitleAndRole() {
        let a = BeamActionButtons.categoryId(for: [BeamActionButton(id: "x", title: "Claim")])
        let b = BeamActionButtons.categoryId(for: [BeamActionButton(id: "x", title: "Claimed")])
        let c = BeamActionButtons.categoryId(for: [BeamActionButton(id: "x", title: "Claim",
                                                                   role: "destructive")])
        XCTAssertNotEqual(a, b)
        XCTAssertNotEqual(a, c)
    }

    /// No buttons ⇒ the built-in category, so callers can use the result unconditionally.
    func testCategoryIdEmptyFallsBackToBuiltIn() {
        XCTAssertEqual(BeamActionButtons.categoryId(for: []),
                       BeamActionButtons.builtInActionsCategory)
        XCTAssertFalse(BeamActionButtons.isSynthesized(BeamActionButtons.builtInActionsCategory))
    }

    /// Option mapping must mirror the built-in pair: destructive is not foreground, everything else is
    /// (its tap has to route the deep link, which needs the app frontmost).
    func testSynthesizedCategorySpecMapsRoles() {
        let spec = CategorySpec(synthesizedFrom: [
            BeamActionButton(id: "claim", title: "Claim"),
            BeamActionButton(id: "dismiss", title: "No thanks", role: "destructive"),
        ])
        XCTAssertEqual(spec.id, BeamActionButtons.categoryId(for: [
            BeamActionButton(id: "claim", title: "Claim"),
            BeamActionButton(id: "dismiss", title: "No thanks", role: "destructive"),
        ]))
        XCTAssertEqual(spec.actions.count, 2)
        XCTAssertEqual(spec.actions[0].foreground, true)
        XCTAssertNil(spec.actions[0].destructive)
        XCTAssertEqual(spec.actions[1].destructive, true)
        XCTAssertNil(spec.actions[1].foreground)
    }

    // MARK: Live Activity capability serialization

    /// `available` and `reason` are computed properties; the synthesized Codable dropped them, so the
    /// engine layer saw `available === undefined` and treated every type as unavailable. They MUST be
    /// present in the encoded wire shape.
    func testCapabilityEncodesComputedAvailableAndReason() throws {
        let available = LiveActivityCapability(
            attributesType: "BeamActionsActivityAttributes", activityType: "actions",
            supported: true, enabled: true, declared: true, widgetPresent: true)
        let data = try JSONEncoder().encode(available)
        let dict = try XCTUnwrap(
            JSONSerialization.jsonObject(with: data) as? [String: Any])
        XCTAssertEqual(dict["available"] as? Bool, true)
        XCTAssertEqual(dict["reason"] as? String, "")

        let unavailable = LiveActivityCapability(
            attributesType: "BeamActionsActivityAttributes", activityType: "actions",
            supported: true, enabled: true, declared: false, widgetPresent: true)
        let dict2 = try XCTUnwrap(
            JSONSerialization.jsonObject(
                with: try JSONEncoder().encode(unavailable)) as? [String: Any])
        XCTAssertEqual(dict2["available"] as? Bool, false)
        XCTAssertEqual(dict2["reason"] as? String,
                       "BeamActionsActivityAttributes is not listed in Info.plist BMNLiveActivityTypes")
    }

    /// Round-trip still works: the custom decoder ignores the derived `available`/`reason` and
    /// recomputes them from the stored gate fields.
    func testCapabilityCodableRoundTrips() throws {
        let original = LiveActivityCapability(
            attributesType: "BeamCountdownActivityAttributes", activityType: "countdown",
            supported: true, enabled: false, declared: true, widgetPresent: true)
        let decoded = try JSONDecoder().decode(
            LiveActivityCapability.self, from: try JSONEncoder().encode(original))
        XCTAssertEqual(decoded, original)
        XCTAssertFalse(decoded.available)
        XCTAssertEqual(decoded.reason, "player has Live Activities turned off in Settings")
    }

    // MARK: Live Activity push-to-start token replay

    /// A push-to-start token minted before the app attached its rail-forwarding listener must be
    /// re-delivered when `start()` runs again (the sample calls `startLiveActivityPushRegistration()`
    /// right before attaching that listener, after the player connects). Without replay the one-shot
    /// token is lost and the rail falls back to a plain notification.
    func testStartReplaysCachedPushToStartToken() {
        let coordinator = LiveActivityCoordinator()
        var received: [LiveActivityTokenEvent] = []
        coordinator.onToken = { received.append($0) }

        // ActivityKit hands us the token before any forwarding listener exists.
        let token = LiveActivityTokenEvent(
            kind: "pushToStart",
            activityType: "actions",
            attributesType: "BeamActionsActivityAttributes",
            activityId: nil,
            token: "deadbeef")
        coordinator.record(token)
        XCTAssertEqual(received.count, 1, "record forwards the token immediately")

        // A later start() (post-connect) must replay the cached token to the now-attached listener.
        coordinator.start()
        XCTAssertEqual(received.count, 2, "start() replays the cached push-to-start token")
        XCTAssertEqual(received.last, token)
    }

    /// A fresh coordinator with no token seen yet must not emit anything on `start()`.
    func testStartWithoutTokensReplaysNothing() {
        let coordinator = LiveActivityCoordinator()
        var received: [LiveActivityTokenEvent] = []
        coordinator.onToken = { received.append($0) }

        coordinator.start()
        XCTAssertTrue(received.isEmpty, "no cached token means no replay")
    }

    /// The cache keeps push-to-start (keyed by attributes type) and update (keyed by activity id)
    /// tokens in separate namespaces, so both survive a replay without clobbering each other.
    func testStartReplaysBothPushToStartAndUpdateTokens() {
        let coordinator = LiveActivityCoordinator()
        var received: [LiveActivityTokenEvent] = []
        coordinator.onToken = { received.append($0) }

        coordinator.record(LiveActivityTokenEvent(
            kind: "pushToStart", activityType: "actions",
            attributesType: "BeamActionsActivityAttributes", activityId: nil, token: "aaaa"))
        coordinator.record(LiveActivityTokenEvent(
            kind: "update", activityType: "actions",
            attributesType: "BeamActionsActivityAttributes", activityId: "act-1", token: "bbbb"))
        received.removeAll()

        coordinator.start()
        XCTAssertEqual(received.count, 2, "both cached tokens replay")
        XCTAssertTrue(received.contains { $0.kind == "pushToStart" && $0.token == "aaaa" })
        XCTAssertTrue(received.contains { $0.kind == "update" && $0.token == "bbbb" })
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
