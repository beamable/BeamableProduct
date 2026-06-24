import Foundation

/// Shared Beamable funnel analytics for iOS (§4). Builds the Beamable `CoreEvent` body and
/// POSTs it directly to `/report/custom_batch/{cid}/{pid}/{gamerTag}` (§4.3), authenticated
/// with the persisted player bearer token + realm scope (Decision Q5). This replaces the
/// demo Slack webhook.
///
/// Both the in-app `AnalyticsPlugin` (app alive) and the closed-app NSE
/// `AnalyticsServicePlugin` use this type via `import BeamableNotifications`, so the wire
/// shape is identical across platforms and across the alive/closed paths.
///
/// All public so the extension target (which links the core module) can call it.
public enum BeamableAnalytics {

    // MARK: CoreEvent JSON (§4.6)

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
        if let v = event.accountId { p["accountId"] = .string(v) }
        if let v = event.cidPid { p["cidPid"] = .string(v) }
        if let v = event.deeplink { p["deeplink"] = .string(v) }
        if let offer = event.offerData {
            p["offerData"] = offerJSON(offer)
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

    private static func offerJSON(_ offer: NotificationOffer) -> JSONValue {
        var o: [String: JSONValue] = [:]
        if let itemId = offer.itemId { o["itemId"] = .string(itemId) }
        if let value = offer.value { o["value"] = value }
        if let customData = offer.customData { o["customData"] = .object(customData) }
        return .object(o)
    }

    // MARK: Build a FunnelEvent from campaign intent + a chosen offer

    /// Compose a `FunnelEvent` from the campaign intent data of a notification. `offer` is
    /// the single offer this event concerns and is attached as `offerData` **only when
    /// explicitly passed** by the caller (Clicked/Converted via `trackOffer*`). Stage events
    /// with no specific offer (Sent/Received/Opened) must NOT attribute a carried campaign
    /// offer, so there is no `offers.first` fallback here. Returns nil if the intent isn't a
    /// tracked campaign (§4.2) — caller can rely on that to gate emission.
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
                           offerData: offer)
    }

    // MARK: Emit (auth + POST + fallback)

    /// Emit a funnel event. Authenticates with the persisted bearer token (refreshing first
    /// if stale), then POSTs fire-and-forget with a short timeout. If credentials/scope are
    /// missing, or the refresh+POST can't complete within `budget` (the NSE ~30s window),
    /// the event is persisted to the App Group for authenticated replay on next app open
    /// (§4.3 fallback).
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
        guard let auth = shared.loadAuthConfig(),
              let host = auth.host, !host.isEmpty,
              let cidPid = event.cidPid, cidPid.contains("."),
              let gamerTag = event.gamerTag, !gamerTag.isEmpty else {
            // No way to authenticate/route — persist for replay if asked, else drop.
            if persistOnFailure { shared.appendPendingFunnel(event) }
            completion?()
            return
        }

        let deadline = Date().addingTimeInterval(budget)

        func fallbackAndFinish() {
            if persistOnFailure { shared.appendPendingFunnel(event) }
            completion?()
        }

        // POST with `token`. On 401/403 — a revoked (not merely clock-stale) token — force a
        // single refresh (ignoring staleness) and re-POST once with the new token, mirroring
        // Android's single-retry semantics. The persist-on-failure fallback only runs after the
        // retry (or refresh) also fails, so a recoverable auth error isn't dropped prematurely.
        func doPost(token: String, allowRetry: Bool) {
            postFunnelStatus(events: [event], host: host, cidPid: cidPid, gamerTag: gamerTag,
                             accessToken: token, session: session) { status in
                if (200..<300).contains(status) {
                    completion?()
                    return
                }
                let isAuthError = (status == 401 || status == 403)
                guard isAuthError, allowRetry,
                      Date() < deadline,
                      let refreshToken = auth.refreshToken, !refreshToken.isEmpty else {
                    fallbackAndFinish()
                    return
                }
                refresh(refreshToken: refreshToken, host: host, cidPid: cidPid,
                        session: session) { result in
                    guard Date() < deadline else { fallbackAndFinish(); return }
                    switch result {
                    case .success(let refreshed):
                        shared.updateTokens(accessToken: refreshed.accessToken,
                                            refreshToken: refreshed.refreshToken,
                                            expiresAt: refreshed.expiresAt)
                        doPost(token: refreshed.accessToken, allowRetry: false)
                    case .failure:
                        fallbackAndFinish()
                    }
                }
            }
        }

        if auth.isAccessTokenStale(), let refreshToken = auth.refreshToken, !refreshToken.isEmpty {
            // Clock-stale: refresh first, then POST — but bail to the fallback if we blow the budget.
            refresh(refreshToken: refreshToken, host: host, cidPid: cidPid,
                    session: session) { result in
                guard Date() < deadline else { fallbackAndFinish(); return }
                switch result {
                case .success(let refreshed):
                    shared.updateTokens(accessToken: refreshed.accessToken,
                                        refreshToken: refreshed.refreshToken,
                                        expiresAt: refreshed.expiresAt)
                    // Already refreshed up-front; don't burn the single retry on another refresh.
                    doPost(token: refreshed.accessToken, allowRetry: false)
                case .failure:
                    fallbackAndFinish()
                }
            }
        } else if let token = auth.accessToken, !token.isEmpty {
            // Token looks fresh by the clock: POST once, with a 401/403 refresh+retry available.
            doPost(token: token, allowRetry: true)
        } else {
            fallbackAndFinish()
        }
    }

    // MARK: Native POST (§4.3)

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

    /// Fire the funnel POST, surfacing the HTTP status code (0 when the request couldn't be
    /// built/sent). Used by `emit` to drive the 401/403 refresh+retry. Completion is always
    /// called exactly once.
    public static func postFunnelStatus(events: [FunnelEvent],
                                        host: String,
                                        cidPid: String,
                                        gamerTag: String,
                                        accessToken: String,
                                        session: URLSession = .shared,
                                        completion: @escaping (Int) -> Void) {
        let parts = cidPid.split(separator: ".", maxSplits: 1).map(String.init)
        guard parts.count == 2, !parts[0].isEmpty, !parts[1].isEmpty,
              let body = makeBody(for: events) else {
            completion(0)
            return
        }
        let cid = parts[0], pid = parts[1]
        let base = host.hasSuffix("/") ? String(host.dropLast()) : host
        guard let url = URL(string: "\(base)/report/custom_batch/\(cid)/\(pid)/\(gamerTag)") else {
            completion(0)
            return
        }

        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.timeoutInterval = 15
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.setValue("Bearer \(accessToken)", forHTTPHeaderField: "Authorization")
        request.setValue("\(cid).\(pid)", forHTTPHeaderField: "X-BEAM-SCOPE")
        request.httpBody = body

        let task = session.dataTask(with: request) { _, response, _ in
            let code = (response as? HTTPURLResponse)?.statusCode ?? 0
            completion(code)
        }
        task.resume()
    }

    // MARK: Token refresh (§4.3) — POST {host}/basic/auth/token, no bearer, scope header.

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
