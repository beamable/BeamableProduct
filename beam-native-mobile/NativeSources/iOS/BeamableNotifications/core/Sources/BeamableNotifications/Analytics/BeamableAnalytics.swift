import Foundation

/// Shared Beamable funnel analytics for iOS. Builds the Beamable `CoreEvent` body and POSTs it
/// authenticated with the persisted player bearer token + realm scope (Decision Q5).
///
/// Two routes, primary then fallback (identical body — see `postAnalyticsEventsStatus`):
///  - `/analytics/events` — the gateway endpoint. Publishes onto `analytics.events`, the bus the
///    campaign event consumer subscribes to, so this is the only route whose Opened/Clicked/
///    Converted can be counted in a campaign's funnel.
///  - `/report/custom_batch/{cid}/{pid}/{gamerTag}` — the canonical report route shared with
///    Unity/CLI/Android. Feeds the warehouse only; used when the gateway endpoint is unavailable
///    (an older backend) so the event is still recorded rather than dropped.
///
/// Both the in-app `AnalyticsPlugin` (app alive) and the closed-app NSE
/// `AnalyticsServicePlugin` use this type via `import BeamableNotifications`, so the wire
/// shape is identical across platforms and across the alive/closed paths.
///
/// All public so the extension target (which links the core module) can call it.
public enum BeamableAnalytics {

    // MARK: CoreEvent JSON

    /// Build the params bag (`p`) for a single funnel event. Matches the cross-platform
    /// contract: campaignId, nodeId, gamerTag, accountId, cidPid, optional offerData,
    /// deeplink, funnelType.
    public static func makeParams(for event: FunnelEvent) -> [String: JSONValue] {
        var p: [String: JSONValue] = [
            "campaignId": .string(event.campaignId),
            "nodeId": .string(event.nodeId),
            "funnelType": .string(event.funnelType)
        ]
        if let v = event.gamerTag { p["gamerTag"] = .string(v) }
        // accountId is auto-set to the user's gamerTag (the SDK-known player id); callers need
        // not send it. Falls back to an explicitly-provided accountId if one is present.
        if let v = event.accountId ?? event.gamerTag { p["accountId"] = .string(v) }
        if let v = event.cidPid { p["cidPid"] = .string(v) }
        if let v = event.deeplink { p["deeplink"] = .string(v) }
        // The push's attribution stamp, echoed verbatim. `CampaignEventProcessor.ProcessAttributedStage`
        // needs BOTH — trackId to recover the send node, outreachId as the exactly-once dedup key — to
        // count this stage in the campaign funnel the portal reads. Omitted when the funnel wasn't
        // triggered by a campaign push; the event is still recorded, just unattributed.
        if let v = event.outreachId, !v.isEmpty { p["outreachId"] = .string(v) }
        if let v = event.trackId, !v.isEmpty { p["trackId"] = .string(v) }
        // Offers as a SINGLE flat column holding a stringified JSON array of offer objects
        // (`[{itemId,value,customData}, ...]`). Athena has no nested-object column type, so the
        // whole array travels as one string the reader JSON-parses. customData is itself free-form,
        // so it is nested as a STRINGIFIED JSON string (never an object) — keeping every level
        // Athena-safe and matching the wire / microservice shape.
        if let offers = event.offers, !offers.isEmpty {
            let arr = JSONValue.array(offers.map { offer in
                var o: [String: JSONValue] = [:]
                if let itemId = offer.itemId { o["itemId"] = .string(itemId) }
                if let value = offer.value { o["value"] = value }
                if let customData = offer.customData { o["customData"] = .string(jsonString(.object(customData))) }
                return .object(o)
            })
            p["offerData"] = .string(jsonString(arr))
        }
        // Free-form campaign metadata, carried verbatim as a stringified JSON object (same flat-
        // column rule as offerData). Present on every stage when the push carried it.
        if let cd = event.campaignData, !cd.isEmpty {
            p["campaignData"] = .string(jsonString(.object(cd)))
        }
        return p
    }

    /// One Beamable `CoreEvent`: `{"op":"g.core","e":<funnelType>,"c":"notification_funnel","p":{...}}`.
    public static func makeCoreEvent(for event: FunnelEvent) -> JSONValue {
        .object([
            "op": .string("g.core"),
            "e": .string(event.funnelType),
            "c": .string(funnelCategory),
            "p": .object(makeParams(for: event))
        ])
    }

    /// The POST body: a JSON array of CoreEvent objects.
    public static func makeBody(for events: [FunnelEvent]) -> Data? {
        let array = JSONValue.array(events.map { makeCoreEvent(for: $0) })
        return try? JSON.encoder.encode(array)
    }

    public static let funnelCategory = "notification_funnel"

    /// Microservice + endpoint exposing the `ForwardFunnelToSlack` [Callable] used as a debug mirror
    /// of the funnel (so device-side stages show up in Slack alongside the server-emitted "Sent").
    /// Hosted by the portal's CampaignService.
    public static let funnelSlackMicroservice = "CampaignService"
    public static let funnelSlackEndpoint = "ForwardFunnelToSlack"

    /// Forward the funnel params (the CoreEvent `p`) to the microservice's `ForwardFunnelToSlack`
    /// [Callable] via the standard microservice route `/basic/<cid>.<pid>.micro_<service>/<endpoint>`,
    /// authenticated with the player bearer + realm scope. Fire-and-forget, best-effort: failures are
    /// ignored (the microservice may be undeployed) and never affect the analytics POST.
    public static func forwardFunnelToSlack(_ event: FunnelEvent,
                                            host: String,
                                            cidPid: String,
                                            accessToken: String,
                                            session: URLSession = .shared) {
        let base = host.hasSuffix("/") ? String(host.dropLast()) : host
        guard cidPid.contains("."),
              let url = URL(string: "\(base)/basic/\(cidPid).micro_\(funnelSlackMicroservice)/\(funnelSlackEndpoint)")
        else { return }
        // Callable args bind by parameter name: ForwardFunnelToSlack(string funnelData). funnelData is
        // the funnel params JSON (matches the microservice's own Slack payload shape).
        let funnelData = jsonString(.object(makeParams(for: event)))
        let bodyValue: JSONValue = .object(["funnelData": .string(funnelData)])
        guard let body = try? JSON.encoder.encode(bodyValue) else { return }

        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.timeoutInterval = 15
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.setValue("Bearer \(accessToken)", forHTTPHeaderField: "Authorization")
        request.setValue(cidPid, forHTTPHeaderField: "X-BEAM-SCOPE")
        request.httpBody = body
        session.dataTask(with: request) { _, _, _ in }.resume()
    }

    /// Compact JSON string for a `JSONValue` — carries free-form customData as a flat string.
    private static func jsonString(_ value: JSONValue) -> String {
        guard let data = try? JSON.encoder.encode(value),
              let s = String(data: data, encoding: .utf8) else { return "{}" }
        return s
    }

    // MARK: Build a FunnelEvent from campaign intent + a chosen offer

    /// Compose a `FunnelEvent` from the campaign intent data of a notification. When `offer` is
    /// explicitly passed (Clicked/Converted via `trackOffer*`) only that single offer is attached;
    /// stage events (Sent/Received/Opened) carry every offer the push held (`intent.offers`). The
    /// free-form `campaignData` is carried on every stage. Returns nil if the intent isn't a
    /// tracked campaign — caller can rely on that to gate emission.
    public static func makeEvent(_ type: FunnelType,
                                 intent: CampaignIntentData,
                                 offer: NotificationOffer? = nil) -> FunnelEvent? {
        guard intent.isTrackedCampaign,
              let campaignId = intent.campaignId, let nodeId = intent.nodeId else { return nil }
        return FunnelEvent(funnelType: type.rawValue,
                           campaignId: campaignId,
                           nodeId: nodeId,
                           gamerTag: intent.gamerTag,
                           accountId: intent.accountId,
                           cidPid: intent.cidPid,
                           deeplink: intent.deeplink,
                           outreachId: intent.outreachId,
                           trackId: intent.trackId,
                           offers: offer.map { [$0] } ?? intent.offers,
                           campaignData: intent.campaignData)
    }

    // MARK: Result reporting

    /// Called once per `emit` with the terminal outcome of the funnel POST. `NotificationManager`
    /// wires this to its `onFunnelResult` callback, which the engine bridges surface as the
    /// `funnelResult` event — the iOS counterpart of Android's `dispatchFunnelResult`, and what
    /// lets a caller await the real per-press HTTP status of `trackOfferClicked`/`Converted`.
    ///
    /// Set only in the app process; in the NSE (a separate process) it stays nil and the
    /// closed-app funnel is silent, exactly as before.
    public static var onResult: ((FunnelResult) -> Void)?

    private static func report(_ event: FunnelEvent, _ ok: Bool, _ statusCode: Int, _ message: String) {
        onResult?(FunnelResult(funnelType: event.funnelType, ok: ok,
                               statusCode: statusCode, message: message))
    }

    // MARK: Emit (auth + POST + fallback)

    /// Emit a funnel event. Authenticates with the persisted bearer token (refreshing first
    /// if stale), then POSTs fire-and-forget with a short timeout. If credentials/scope are
    /// missing, or the refresh+POST can't complete within `budget` (the NSE ~30s window),
    /// the event is persisted to the App Group for authenticated replay on next app open
    /// (fallback).
    ///
    /// - Parameters:
    ///   - persistOnFailure: when true (the NSE path), an event that can't be sent in time is
    ///     appended to the pending-funnel store. The app path keeps it false (the app is alive
    ///     and can retry naturally / there is no hard deadline).
    ///   - budget: soft deadline for refresh+POST. Defaults to a short window suitable for the NSE.
    public static func emit(_ event: FunnelEvent,
                            shared: SharedConfig = .shared,
                            session: URLSession = .shared,
                            persistOnFailure: Bool = false,
                            budget: TimeInterval = 25,
                            completion: (() -> Void)? = nil) {
        // No way to authenticate/route — persist for replay if asked, else drop. The reason is
        // reported verbatim so a caller awaiting `funnelResult` sees WHY nothing was sent
        // (missing native auth is by far the most common cause), matching Android's messages.
        func abort(_ reason: String) {
            if persistOnFailure { shared.appendPendingFunnel(event) }
            report(event, false, 0, persistOnFailure ? "\(reason); queued for replay" : reason)
            completion?()
        }
        guard let auth = shared.loadAuthConfig() else { abort("no native auth configured"); return }
        guard let host = auth.host, !host.isEmpty else { abort("no host"); return }
        guard let cidPid = event.cidPid, cidPid.contains(".") else { abort("no scope"); return }
        guard let gamerTag = event.gamerTag, !gamerTag.isEmpty else { abort("no gamerTag"); return }

        let deadline = Date().addingTimeInterval(budget)

        func fallbackAndFinish(_ statusCode: Int, _ reason: String) {
            if persistOnFailure { shared.appendPendingFunnel(event) }
            report(event, false, statusCode, persistOnFailure ? "\(reason); queued for replay" : reason)
            completion?()
        }

        /// Terminal success handling, shared by both routes.
        func succeed(_ status: Int, token: String) {
            // Best-effort debug mirror: forward the same funnel params to the microservice's
            // ForwardFunnelToSlack endpoint so device-side stages appear in Slack too. Never
            // affects the analytics result.
            forwardFunnelToSlack(event, host: host, cidPid: cidPid, accessToken: token, session: session)
            report(event, true, status, "ok")
            completion?()
        }

        // The gateway route is missing/unroutable (e.g. an older backend): fall back to the canonical
        // report route rather than drop the event — it still reaches the warehouse, it just cannot
        // reach a campaign. Auth failures are NOT retried here; the token was already proven above.
        func doFallbackPost(token: String, primaryStatus: Int) {
            postFunnelStatus(events: [event], host: host, cidPid: cidPid, gamerTag: gamerTag,
                             accessToken: token, session: session) { status in
                if (200..<300).contains(status) {
                    succeed(status, token: token)
                    return
                }
                // Report the PRIMARY status when the fallback couldn't be sent at all — that is the
                // status that describes what actually went wrong.
                let reported = status == 0 ? primaryStatus : status
                fallbackAndFinish(reported,
                                  reported == 0 ? "request failed (no response)" : "HTTP \(reported)")
            }
        }

        // POST with `token`. On 401/403 — a revoked (not merely clock-stale) token — force a
        // single refresh (ignoring staleness) and re-POST once with the new token, mirroring
        // Android's single-retry semantics. The persist-on-failure fallback only runs after the
        // retry (or refresh) also fails, so a recoverable auth error isn't dropped prematurely.
        func doPost(token: String, allowRetry: Bool) {
            postAnalyticsEventsStatus(events: [event], host: host, cidPid: cidPid,
                                      accessToken: token, session: session) { status in
                if (200..<300).contains(status) {
                    succeed(status, token: token)
                    return
                }
                let isAuthError = (status == 401 || status == 403)
                guard isAuthError else {
                    // Not an auth problem — the endpoint itself is unavailable. Try the legacy route.
                    doFallbackPost(token: token, primaryStatus: status)
                    return
                }
                guard allowRetry,
                      Date() < deadline,
                      let refreshToken = auth.refreshToken, !refreshToken.isEmpty else {
                    fallbackAndFinish(status, "HTTP \(status)")
                    return
                }
                refresh(refreshToken: refreshToken, host: host, cidPid: cidPid,
                        session: session) { result in
                    guard Date() < deadline else { fallbackAndFinish(status, "timed out"); return }
                    switch result {
                    case .success(let refreshed):
                        shared.updateTokens(accessToken: refreshed.accessToken,
                                            refreshToken: refreshed.refreshToken,
                                            expiresAt: refreshed.expiresAt)
                        doPost(token: refreshed.accessToken, allowRetry: false)
                    case .failure:
                        fallbackAndFinish(status, "token refresh failed")
                    }
                }
            }
        }

        if auth.isAccessTokenStale(), let refreshToken = auth.refreshToken, !refreshToken.isEmpty {
            // Clock-stale: refresh first, then POST — but bail to the fallback if we blow the budget.
            refresh(refreshToken: refreshToken, host: host, cidPid: cidPid,
                    session: session) { result in
                guard Date() < deadline else { fallbackAndFinish(0, "timed out"); return }
                switch result {
                case .success(let refreshed):
                    shared.updateTokens(accessToken: refreshed.accessToken,
                                        refreshToken: refreshed.refreshToken,
                                        expiresAt: refreshed.expiresAt)
                    // Already refreshed up-front; don't burn the single retry on another refresh.
                    doPost(token: refreshed.accessToken, allowRetry: false)
                case .failure:
                    fallbackAndFinish(0, "token refresh failed")
                }
            }
        } else if let token = auth.accessToken, !token.isEmpty {
            // Token looks fresh by the clock: POST once, with a 401/403 refresh+retry available.
            doPost(token: token, allowRetry: true)
        } else {
            fallbackAndFinish(0, "no token")
        }
    }

    // MARK: Native POST

    /// Fire the funnel POST. `ok` is true on a 2xx response (best-effort; fire-and-forget
    /// otherwise). Completion is always called exactly once.
    public static func postFunnel(events: [FunnelEvent],
                                  host: String,
                                  cidPid: String,
                                  gamerTag: String,
                                  accessToken: String,
                                  session: URLSession = .shared,
                                  completion: @escaping (Bool) -> Void) {
        postFunnelStatus(events: events, host: host, cidPid: cidPid, gamerTag: gamerTag,
                         accessToken: accessToken, session: session) { status in
            completion((200..<300).contains(status))
        }
    }

    /// Fire the funnel POST at the LEGACY report route, surfacing the HTTP status code (0 when the
    /// request couldn't be built/sent). This is the fallback leg of `emit` — see
    /// `postAnalyticsEventsStatus` for the primary. Completion is always called exactly once.
    public static func postFunnelStatus(events: [FunnelEvent],
                                        host: String,
                                        cidPid: String,
                                        gamerTag: String,
                                        accessToken: String,
                                        session: URLSession = .shared,
                                        completion: @escaping (Int) -> Void) {
        let parts = cidPid.split(separator: ".", maxSplits: 1).map(String.init)
        guard parts.count == 2, !parts[0].isEmpty, !parts[1].isEmpty else {
            completion(0)
            return
        }
        let cid = parts[0], pid = parts[1]
        post(events: events, path: "/report/custom_batch/\(cid)/\(pid)/\(gamerTag)",
             host: host, cidPid: "\(cid).\(pid)", accessToken: accessToken,
             session: session, completion: completion)
    }

    /// Fire the funnel POST at the gateway's analytics endpoint — the PRIMARY route.
    ///
    /// It publishes onto the `analytics.events` bus the campaign event consumer subscribes to, which
    /// makes it the only device path that can reach a campaign: `/report/custom_batch` publishes to
    /// `platform_metric_reports.general`, which feeds the warehouse loader and is never republished
    /// onto `analytics.events`. Mirrors Android's `BeamableAnalytics.primaryUrl`.
    ///
    /// The body is identical to the legacy route's — the gateway's `ClientAnalyticsEvent` has the
    /// same `{op,e,c,p}` shape as the CoreEvent `makeBody` builds — so this is purely a URL swap.
    public static func postAnalyticsEventsStatus(events: [FunnelEvent],
                                                 host: String,
                                                 cidPid: String,
                                                 accessToken: String,
                                                 session: URLSession = .shared,
                                                 completion: @escaping (Int) -> Void) {
        // The player id comes from the token claim here, not the URL, so no gamerTag path segment.
        post(events: events, path: "/analytics/events", host: host, cidPid: cidPid,
             accessToken: accessToken, session: session, completion: completion)
    }

    /// Shared transport for both funnel routes: same body, same headers, different path.
    private static func post(events: [FunnelEvent],
                             path: String,
                             host: String,
                             cidPid: String,
                             accessToken: String,
                             session: URLSession,
                             completion: @escaping (Int) -> Void) {
        let base = host.hasSuffix("/") ? String(host.dropLast()) : host
        guard let body = makeBody(for: events), let url = URL(string: base + path) else {
            completion(0)
            return
        }

        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.timeoutInterval = 15
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.setValue("Bearer \(accessToken)", forHTTPHeaderField: "Authorization")
        request.setValue(cidPid, forHTTPHeaderField: "X-BEAM-SCOPE")
        request.httpBody = body

        let task = session.dataTask(with: request) { _, response, _ in
            let code = (response as? HTTPURLResponse)?.statusCode ?? 0
            completion(code)
        }
        task.resume()
    }

    // MARK: Token refresh — POST {host}/basic/auth/token, no bearer, scope header.

    public struct RefreshedTokens {
        public let accessToken: String
        public let refreshToken: String?
        /// Absolute epoch **milliseconds** (canonical contract — matches `AuthConfig.accessTokenExpiresAt`).
        public let expiresAt: Double?
    }

    public static func refresh(refreshToken: String,
                               host: String,
                               cidPid: String,
                               session: URLSession = .shared,
                               completion: @escaping (Result<RefreshedTokens, Error>) -> Void) {
        let base = host.hasSuffix("/") ? String(host.dropLast()) : host
        guard let url = URL(string: "\(base)/basic/auth/token") else {
            completion(.failure(AnalyticsError.badURL))
            return
        }
        let bodyValue: JSONValue = .object([
            "grant_type": .string("refresh_token"),
            "refresh_token": .string(refreshToken)
        ])
        guard let body = try? JSON.encoder.encode(bodyValue) else {
            completion(.failure(AnalyticsError.badURL))
            return
        }

        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.timeoutInterval = 15
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        // The refresh call carries no bearer, but still needs the realm scope.
        request.setValue(cidPid, forHTTPHeaderField: "X-BEAM-SCOPE")
        request.httpBody = body

        let task = session.dataTask(with: request) { data, response, error in
            if let error = error { completion(.failure(error)); return }
            let code = (response as? HTTPURLResponse)?.statusCode ?? 0
            guard (200..<300).contains(code), let data = data,
                  let value = try? JSON.decoder.decode(JSONValue.self, from: data),
                  let access = value["access_token"]?.stringValue, !access.isEmpty else {
                completion(.failure(AnalyticsError.refreshFailed))
                return
            }
            // `expires_in` is in MILLISECONDS from now (matches the Beamable TokenResponse).
            // Store the absolute expiry as epoch-ms (canonical contract).
            var expiresAt: Double?
            if case .number(let ms)? = value["expires_in"] {
                expiresAt = (Date().timeIntervalSince1970 * 1000.0) + ms
            }
            let newRefresh = value["refresh_token"]?.stringValue
            completion(.success(RefreshedTokens(accessToken: access,
                                                refreshToken: newRefresh,
                                                expiresAt: expiresAt)))
        }
        task.resume()
    }

    public enum AnalyticsError: Error {
        case badURL
        case refreshFailed
    }
}
