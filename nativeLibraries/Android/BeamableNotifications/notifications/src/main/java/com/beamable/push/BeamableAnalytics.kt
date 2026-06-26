package com.beamable.push

import android.content.Context
import android.content.SharedPreferences
import android.util.Log
import org.json.JSONArray
import org.json.JSONObject
import java.io.OutputStream
import java.net.HttpURLConnection
import java.net.URL
import java.util.concurrent.Executors

/**
 * Native funnel-analytics POSTer (spec §4).
 *
 * Fires Beamable CoreEvent funnel events directly from native code so they work even when the
 * JS/C# VM is dead (closed-app FCM path). Events are POSTed fire-and-forget with a short timeout
 * to `/report/custom_batch/{cid}/{pid}/{gamerTag}` (§4.3).
 *
 * Auth (Decision Q5): the SDK persists the player's access + refresh token into shared prefs
 * (readable here because the FCM handler runs in the app process). Native attaches
 * `Authorization: Bearer {accessToken}` + `X-BEAM-SCOPE: {cid}.{pid}`. If the access token is
 * stale it refreshes first using the refresh token (refresh needs no bearer) then POSTs.
 *
 * NO realm secret is embedded in the library; everything comes from the persisted creds.
 */
object BeamableAnalytics {

    private const val TAG = "BeamableAnalytics"

    /** Shared-prefs namespace the SDK writes player auth into. */
    const val PREFS_NAME = "beamable_notifications_auth"
    const val KEY_ACCESS_TOKEN = "access_token"
    const val KEY_REFRESH_TOKEN = "refresh_token"
    const val KEY_ACCESS_TOKEN_EXPIRES_AT = "access_token_expires_at" // epoch millis
    const val KEY_CID = "cid"
    const val KEY_PID = "pid"
    const val KEY_HOST = "host" // e.g. https://api.beamable.com

    /** CoreEvent constants (§4.6). */
    private const val CORE_OP = "g.core"
    private const val FUNNEL_CATEGORY = "notification_funnel"

    /**
     * Microservice that exposes the `ForwardFunnelToSlack` [Callable] used as a debug mirror of the
     * funnel. After a successful analytics POST we also forward the same funnel params to it so the
     * device-side stages (Received/Opened/Clicked/Converted) show up in Slack alongside the
     * server-emitted "Sent" event. Invoked via the standard microservice route
     * `/basic/<cid>.<pid>.micro_<service>/<endpoint>` with the player's bearer + realm scope.
     */
    private const val SLACK_MICROSERVICE = "PushNotificationService"
    private const val SLACK_ENDPOINT = "ForwardFunnelToSlack"

    /** Short fire-and-forget timeouts (ms) — must stay well within the FCM ~10s budget. */
    private const val CONNECT_TIMEOUT_MS = 4_000
    private const val READ_TIMEOUT_MS = 4_000

    /** Refresh if the token expires within this skew window. */
    private const val EXPIRY_SKEW_MS = 60_000L

    /** SharedPreferences key holding the JSON array of persisted-for-replay funnel events (§4.3). */
    const val KEY_PENDING_FUNNEL = "pending_funnel"

    /** Cap on persisted-for-replay funnel events so a long offline streak can't grow unbounded. */
    private const val MAX_PENDING_FUNNEL = 200

    private val executor = Executors.newSingleThreadExecutor { r ->
        Thread(r, "beamable-analytics").apply { isDaemon = true }
    }

    enum class FunnelType { Sent, Received, Opened, Clicked, Converted }

    // ---- Public entry points -------------------------------------------------

    /**
     * Fires a funnel event for [intent] of [type], fire-and-forget. No-op unless the intent is a
     * tracked campaign (campaignId + nodeId) AND carries a gamerTag (§4.2). The realm scope comes
     * from the intent's cidPid or, failing that, the stored auth cid/pid (mirroring iOS, which
     * fills cidPid from persisted auth); if neither is known yet the event is persisted for replay
     * once the SDK calls configureAuth. [offer] is the single offer this event concerns
     * (Clicked/Converted), omitted otherwise.
     */
    fun trackFunnel(
        context: Context,
        intent: NotificationIntentData,
        type: FunnelType,
        offer: NotificationOffer? = null
    ) {
        // gamerTag is intent-only (no stored fallback); cidPid is resolved later from the intent
        // OR stored auth (see resolveScope), so don't hard-require it here — that would drop
        // events that iOS sends via its auth-config scope fallback.
        if (!intent.isTrackedCampaign() || intent.gamerTag.isNullOrEmpty()) {
            Log.i(
                TAG,
                "funnel ${type.name} skipped: not a tracked campaign or missing gamerTag " +
                    "(campaignId=${intent.campaignId}, nodeId=${intent.nodeId}, gamerTag=${intent.gamerTag})"
            )
            PushManager.dispatchFunnelResult(
                type.name, false, 0, "skipped: not a tracked campaign or missing gamerTag"
            )
            return
        }
        Log.i(TAG, "funnel ${type.name}: queuing POST (campaign=${intent.campaignId}/${intent.nodeId})")
        val appContext = context.applicationContext
        val event = PendingFunnel.from(intent, type, offer)
        executor.execute {
            try {
                postFunnel(appContext, event, persistOnFailure = true)
            } catch (t: Throwable) {
                Log.w(TAG, "funnel post failed: ${t.message}")
                PushManager.dispatchError("analytics_funnel", t.message ?: t.toString())
                appendPendingFunnel(appContext, event)
            }
        }
    }

    /**
     * Drains all persisted funnel events and re-POSTs each on the background executor (the
     * "connected to Beamable" replay trigger). Replay failures are NOT re-persisted
     * ([persistOnFailure]=false) to avoid an unbounded retry loop. Best-effort.
     */
    fun flushPendingFunnel(context: Context) {
        val appContext = context.applicationContext
        executor.execute {
            val pending = drainPendingFunnel(appContext)
            for (event in pending) {
                try {
                    postFunnel(appContext, event, persistOnFailure = false)
                } catch (t: Throwable) {
                    Log.w(TAG, "funnel replay failed: ${t.message}")
                }
            }
        }
    }

    // ---- Core POST -----------------------------------------------------------

    private fun postFunnel(
        context: Context,
        event: PendingFunnel,
        persistOnFailure: Boolean
    ) {
        val intent = event.toIntentData()
        Log.i(TAG, "postFunnel ${event.funnelType.name}: start (gamerTag=${intent.gamerTag}, cidPid=${intent.cidPid})")
        val prefs = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)

        // Scope comes from the intent's cidPid ("<cid>.<pid>"); fall back to stored cid/pid.
        val (cid, pid) = resolveScope(intent, prefs) ?: run {
            // Scope not known yet (no cidPid on the intent and no stored cid/pid). It may become
            // resolvable once the SDK calls configureAuth, so persist for replay rather than drop.
            Log.i(TAG, "no cid/pid scope yet; persisting funnel for replay")
            if (persistOnFailure) appendPendingFunnel(context, event)
            PushManager.dispatchFunnelResult(event.funnelType.name, false, 0, "no scope; queued for replay")
            return
        }
        val gamerTag = intent.gamerTag ?: return
        val host = prefs.getString(KEY_HOST, null)?.trimEnd('/') ?: run {
            // Unrecoverable for now (not connected to Beamable yet): persist for replay.
            Log.i(TAG, "no host in prefs; persisting funnel for replay")
            if (persistOnFailure) appendPendingFunnel(context, event)
            PushManager.dispatchFunnelResult(event.funnelType.name, false, 0, "no host; queued for replay")
            return
        }

        // Mirrors iOS's single-retry semantics: AT MOST ONE refresh round-trip per send. If
        // currentAccessToken already refreshed proactively (token was clock-stale), the 401/403
        // path below must NOT refresh again — a second round-trip is redundant and risks blowing
        // the ~10s FCM budget.
        val auth = currentAccessToken(prefs, host, cid, pid) ?: run {
            // No usable token (and refresh failed/absent): persist for replay once connected.
            Log.i(TAG, "no access token; persisting funnel for replay")
            if (persistOnFailure) appendPendingFunnel(context, event)
            PushManager.dispatchFunnelResult(event.funnelType.name, false, 0, "no token; queued for replay")
            return
        }
        var accessToken = auth.token
        val alreadyRefreshed = auth.refreshed

        val body = buildBatch(intent, event.funnelType).toString()
        val url = "$host/report/custom_batch/$cid/$pid/$gamerTag"
        val scope = "$cid.$pid"

        var code = doPost(url, body, accessToken, scope)
        if (code == HttpURLConnection.HTTP_UNAUTHORIZED || code == HttpURLConnection.HTTP_FORBIDDEN) {
            if (alreadyRefreshed) {
                // A proactive refresh already happened this send; a freshly-refreshed token still
                // got rejected, so refreshing again would not help. Persist for replay.
                Log.w(TAG, "funnel POST rejected after proactive refresh; persisting for replay")
                if (persistOnFailure) appendPendingFunnel(context, event)
                PushManager.dispatchFunnelResult(event.funnelType.name, false, code, "auth rejected; queued for replay")
                return
            }
            // Token looked valid but the server rejected it; refresh ONCE and retry the POST ONCE.
            val refreshed = refreshAccessToken(prefs, host, cid, pid) ?: run {
                if (persistOnFailure) appendPendingFunnel(context, event)
                PushManager.dispatchFunnelResult(event.funnelType.name, false, code, "token refresh failed; queued for replay")
                return
            }
            accessToken = refreshed
            code = doPost(url, body, accessToken, scope)
        }
        // Transport failure (-1) or any non-2xx after the single retry: persist for replay.
        if (code !in 200..299) {
            Log.w(TAG, "funnel POST unrecoverable (HTTP $code); persisting for replay")
            if (persistOnFailure) appendPendingFunnel(context, event)
            PushManager.dispatchFunnelResult(event.funnelType.name, false, code, "HTTP $code; queued for replay")
        } else {
            PushManager.dispatchFunnelResult(event.funnelType.name, true, code, "ok")
            // Best-effort debug mirror: forward the same funnel params to the microservice's
            // ForwardFunnelToSlack endpoint so device-side stages appear in Slack too. Never affects
            // the analytics result — failures are logged and swallowed.
            forwardFunnelToSlack(host, cid, pid, accessToken, buildParams(intent, event.funnelType).toString())
        }
    }

    /**
     * Forwards [funnelData] (the CoreEvent params JSON) to the microservice's `ForwardFunnelToSlack`
     * [Callable] via the standard microservice route `/basic/<cid>.<pid>.micro_<service>/<endpoint>`,
     * authenticated with the player bearer + realm scope. Fire-and-forget, best-effort: any failure
     * is logged and never propagates (the microservice may be undeployed, etc.).
     */
    private fun forwardFunnelToSlack(
        host: String,
        cid: String,
        pid: String,
        accessToken: String,
        funnelData: String
    ) {
        try {
            val url = "$host/basic/$cid.$pid.micro_$SLACK_MICROSERVICE/$SLACK_ENDPOINT"
            val scope = "$cid.$pid"
            // Callable args bind by parameter name: ForwardFunnelToSlack(string funnelData).
            val body = JSONObject().put("funnelData", funnelData).toString()
            val conn = (URL(url).openConnection() as HttpURLConnection).apply {
                requestMethod = "POST"
                connectTimeout = CONNECT_TIMEOUT_MS
                readTimeout = READ_TIMEOUT_MS
                doOutput = true
                setRequestProperty("Content-Type", "application/json")
                setRequestProperty("Authorization", "Bearer $accessToken")
                setRequestProperty("X-BEAM-SCOPE", scope)
            }
            conn.outputStream.use { it.writeBytesUtf8(body) }
            val code = conn.responseCode
            try { conn.inputStream.use { it.readBytes() } } catch (_: Throwable) {
                try { conn.errorStream?.use { it.readBytes() } } catch (_: Throwable) {}
            }
            Log.i(TAG, "funnel→slack microservice POST -> HTTP $code")
        } catch (t: Throwable) {
            Log.w(TAG, "funnel→slack microservice error: ${t.message}")
        }
    }

    /** Result of [currentAccessToken]: the usable token plus whether a refresh was performed. */
    private data class AccessTokenResult(val token: String, val refreshed: Boolean)

    /** Splits the intent cidPid, or falls back to stored cid/pid. Returns null when unknown. */
    private fun resolveScope(
        intent: NotificationIntentData,
        prefs: SharedPreferences
    ): Pair<String, String>? {
        intent.cidPid?.let { cp ->
            val parts = cp.split(".")
            if (parts.size == 2 && parts[0].isNotEmpty() && parts[1].isNotEmpty()) {
                return parts[0] to parts[1]
            }
        }
        val cid = prefs.getString(KEY_CID, null)
        val pid = prefs.getString(KEY_PID, null)
        if (!cid.isNullOrEmpty() && !pid.isNullOrEmpty()) return cid to pid
        return null
    }

    /**
     * Returns a usable access token (and whether a proactive refresh was performed), refreshing
     * first if the stored one is stale (or about to be). When a proactive refresh succeeds the
     * result is flagged [AccessTokenResult.refreshed]=true so the caller can skip the redundant
     * 401/403 refresh (single-refresh-per-send guard). A FAILED proactive refresh that falls back
     * to the existing token is NOT flagged, so the caller may still refresh-and-retry once.
     */
    private fun currentAccessToken(
        prefs: SharedPreferences,
        host: String,
        cid: String,
        pid: String
    ): AccessTokenResult? {
        val token = prefs.getString(KEY_ACCESS_TOKEN, null)
        val expiresAt = prefs.getLong(KEY_ACCESS_TOKEN_EXPIRES_AT, 0L)
        val stale = expiresAt in 1 until (System.currentTimeMillis() + EXPIRY_SKEW_MS)
        if (token.isNullOrEmpty() || stale) {
            val refreshed = refreshAccessToken(prefs, host, cid, pid)
            if (refreshed != null) return AccessTokenResult(refreshed, refreshed = true)
            // Refresh failed; fall back to the existing token (if any). Not flagged as refreshed
            // so the caller can still attempt a single 401/403 refresh-and-retry.
            return token?.let { AccessTokenResult(it, refreshed = false) }
        }
        return AccessTokenResult(token, refreshed = false)
    }

    /**
     * Refreshes the access token using the persisted refresh token (no bearer needed), persists
     * the new token + expiry back into prefs, and returns it. Best-effort: null on failure.
     */
    private fun refreshAccessToken(
        prefs: SharedPreferences,
        host: String,
        cid: String,
        pid: String
    ): String? {
        val refreshToken = prefs.getString(KEY_REFRESH_TOKEN, null) ?: return null
        return try {
            val url = URL("$host/basic/auth/token")
            val conn = (url.openConnection() as HttpURLConnection).apply {
                requestMethod = "POST"
                connectTimeout = CONNECT_TIMEOUT_MS
                readTimeout = READ_TIMEOUT_MS
                doOutput = true
                setRequestProperty("Content-Type", "application/json")
                setRequestProperty("X-BEAM-SCOPE", "$cid.$pid")
            }
            val reqBody = JSONObject()
                .put("grant_type", "refresh_token")
                .put("refresh_token", refreshToken)
                .toString()
            conn.outputStream.use { it.writeBytesUtf8(reqBody) }
            val code = conn.responseCode
            if (code in 200..299) {
                val text = conn.inputStream.bufferedReader().use { it.readText() }
                val obj = JSONObject(text)
                val newAccess = obj.optString("access_token").ifEmpty { null }
                val newRefresh = obj.optString("refresh_token").ifEmpty { null }
                val expiresInMs = obj.optLong("expires_in", 0L)
                if (newAccess != null) {
                    prefs.edit().apply {
                        putString(KEY_ACCESS_TOKEN, newAccess)
                        if (newRefresh != null) putString(KEY_REFRESH_TOKEN, newRefresh)
                        if (expiresInMs > 0) {
                            putLong(KEY_ACCESS_TOKEN_EXPIRES_AT, System.currentTimeMillis() + expiresInMs)
                        }
                    }.apply()
                }
                newAccess
            } else {
                Log.w(TAG, "token refresh failed: HTTP $code")
                null
            }
        } catch (t: Throwable) {
            Log.w(TAG, "token refresh error: ${t.message}")
            null
        }
    }

    /** Executes the POST and returns the HTTP status code (or -1 on transport failure). */
    private fun doPost(url: String, body: String, accessToken: String, scope: String): Int {
        return try {
            val conn = (URL(url).openConnection() as HttpURLConnection).apply {
                requestMethod = "POST"
                connectTimeout = CONNECT_TIMEOUT_MS
                readTimeout = READ_TIMEOUT_MS
                doOutput = true
                setRequestProperty("Content-Type", "application/json")
                setRequestProperty("Authorization", "Bearer $accessToken")
                setRequestProperty("X-BEAM-SCOPE", scope)
            }
            Log.i(TAG, "funnel POST $url scope=$scope body=$body")
            conn.outputStream.use { it.writeBytesUtf8(body) }
            val code = conn.responseCode
            // Drain so the connection can be reused/closed cleanly.
            try { conn.inputStream.use { it.readBytes() } } catch (_: Throwable) {
                try { conn.errorStream?.use { it.readBytes() } } catch (_: Throwable) {}
            }
            Log.i(TAG, "funnel POST $url -> HTTP $code")
            code
        } catch (t: Throwable) {
            Log.w(TAG, "funnel POST error: ${t.message}")
            -1
        }
    }

    // ---- CoreEvent JSON builder (§4.6) --------------------------------------

    /** Builds the POST body: a JSON array of one CoreEvent (`/report/custom_batch` accepts a batch). */
    internal fun buildBatch(
        intent: NotificationIntentData,
        type: FunnelType
    ): JSONArray = JSONArray().put(buildCoreEvent(intent, type))

    /** Builds one CoreEvent: {"op":"g.core","e":<funnelType>,"c":"notification_funnel","p":{...}}. */
    internal fun buildCoreEvent(
        intent: NotificationIntentData,
        type: FunnelType
    ): JSONObject = JSONObject()
        .put("op", CORE_OP)
        .put("e", type.name)
        .put("c", FUNNEL_CATEGORY)
        .put("p", buildParams(intent, type))

    /** Builds the funnel event params bag (the CoreEvent `p`). Shared by the analytics POST and the
     *  `ForwardFunnelToSlack` debug mirror, so both carry an identical payload. Keys are emitted in
     *  alphabetical (ordinal) order to match the microservice and iOS (which encodes with
     *  `.sortedKeys`), so every platform's funnel JSON has the same field order. */
    internal fun buildParams(
        intent: NotificationIntentData,
        type: FunnelType
    ): JSONObject {
        // Collect into a sorted map first; JSONObject preserves insertion order, so inserting in
        // sorted-key order yields alphabetically-ordered JSON.
        val sorted = sortedMapOf<String, Any>()
        intent.campaignId?.let { sorted["campaignId"] = it }
        intent.nodeId?.let { sorted["nodeId"] = it }
        intent.gamerTag?.let { sorted["gamerTag"] = it }
        // accountId is auto-set to the user's gamerTag (the SDK-known player id); callers need
        // not send it. Falls back to an explicitly-provided accountId if one is present.
        (intent.accountId ?: intent.gamerTag)?.let { sorted["accountId"] = it }
        intent.cidPid?.let { sorted["cidPid"] = it }
        // Offers relevant to this event (§4.6) as a SINGLE flat column holding a stringified JSON
        // array of offer objects (`[{customData,itemId,value}, ...]`). Athena has no nested-object
        // column type, so the whole array is carried as one string the reader JSON-parses — this
        // lets any offer shape (incl. free-form customData) survive intact. Stage events carry every
        // offer the push held; Clicked/Converted carry a one-element array (set in PendingFunnel.from).
        intent.offersJson?.let { sorted["offerData"] = it }
        // Free-form campaign metadata, carried verbatim as a stringified JSON object (same flat-
        // column rule as offerData). Present on every stage when the push carried it.
        intent.campaignDataJson?.let { sorted["campaignData"] = it }
        intent.deeplink?.let { sorted["deeplink"] = it }
        sorted["funnelType"] = type.name

        val params = JSONObject()
        for ((k, v) in sorted) params.put(k, v)
        return params
    }

    private fun OutputStream.writeBytesUtf8(s: String) = write(s.toByteArray(Charsets.UTF_8))

    // ---- Persist-and-replay (§4.3, mirrors iOS SharedConfig/FunnelEvent) ----

    /**
     * Serializable snapshot of a funnel event persisted for later replay (mirrors iOS's
     * `FunnelEvent`). Captures the campaign coordinates, the offers it concerns (as the wire JSON
     * array string), the free-form campaignData, and a timestamp. [dedupKey] keys on
     * `funnelType|campaignId|nodeId|gamerTag|offersJson` (excludes the timestamp) so the same
     * stage is never enqueued/replayed twice, while distinct Clicked/Converted events for
     * different offers stay separate. `gamerTag` is included so an offline account-switch on a
     * shared device doesn't collapse two players' events.
     */
    internal data class PendingFunnel(
        val funnelType: FunnelType,
        val campaignId: String?,
        val nodeId: String?,
        val gamerTag: String?,
        val accountId: String?,
        val cidPid: String?,
        val deeplink: String?,
        /** Stringified JSON array of the offer(s) this event concerns (null when none). */
        val offersJson: String?,
        /** Stringified JSON object of the free-form campaign metadata (null when absent). */
        val campaignDataJson: String?,
        val timestamp: Long
    ) {
        /**
         * Stable identity for replay dedup — campaign coordinates + stage + gamerTag + offers (no
         * timestamp). `gamerTag` is included so an offline account-switch on a shared device
         * doesn't collapse two players' otherwise-identical events; `offersJson` keeps distinct
         * Clicked/Converted events for different offers from collapsing.
         */
        val dedupKey: String
            get() = listOf(
                funnelType.name, campaignId ?: "", nodeId ?: "", gamerTag ?: "", offersJson ?: ""
            ).joinToString("|")

        /** Rebuilds a [NotificationIntentData] from the persisted fields (for the POST body). */
        fun toIntentData(): NotificationIntentData = NotificationIntentData(
            campaignId = campaignId,
            nodeId = nodeId,
            gamerTag = gamerTag,
            accountId = accountId,
            cidPid = cidPid,
            offersJson = offersJson,
            campaignDataJson = campaignDataJson,
            deeplink = deeplink
        )

        fun toJson(): JSONObject {
            val obj = JSONObject()
            obj.put("funnelType", funnelType.name)
            campaignId?.let { obj.put("campaignId", it) }
            nodeId?.let { obj.put("nodeId", it) }
            gamerTag?.let { obj.put("gamerTag", it) }
            accountId?.let { obj.put("accountId", it) }
            cidPid?.let { obj.put("cidPid", it) }
            deeplink?.let { obj.put("deeplink", it) }
            offersJson?.let { obj.put("offersJson", it) }
            campaignDataJson?.let { obj.put("campaignDataJson", it) }
            obj.put("timestamp", timestamp)
            return obj
        }

        companion object {
            fun from(
                intent: NotificationIntentData,
                type: FunnelType,
                offer: NotificationOffer?
            ): PendingFunnel = PendingFunnel(
                funnelType = type,
                campaignId = intent.campaignId,
                nodeId = intent.nodeId,
                gamerTag = intent.gamerTag,
                accountId = intent.accountId,
                cidPid = intent.cidPid,
                deeplink = intent.deeplink,
                // Stage events (offer == null) carry every offer the push held; Clicked/Converted
                // carry just the single concerned offer. Either way the offers are re-serialized
                // through [toAnalyticsArray] so customData is guaranteed to be a stringified JSON
                // string (Athena-safe), regardless of how the sender shaped the wire payload.
                offersJson = if (offer != null) toAnalyticsArray(listOf(offer))
                             else intent.offers().takeIf { it.isNotEmpty() }?.let { toAnalyticsArray(it) },
                campaignDataJson = intent.campaignDataJson,
                timestamp = System.currentTimeMillis()
            )

            /**
             * Serializes [offers] for the analytics `offerData` column: a JSON array of
             * `{itemId, value, customData}` where customData stays a STRINGIFIED JSON string.
             * Athena has no nested-object column type, so customData must never be an embedded
             * object — `customDataJson` is already the string form (see NotificationOffer.fromJson).
             */
            private fun toAnalyticsArray(offers: List<NotificationOffer>): String {
                val arr = JSONArray()
                for (o in offers) {
                    // Insert keys in alphabetical order (customData, itemId, value) — JSONObject keeps
                    // insertion order, so this matches the microservice + iOS (.sortedKeys) per-offer shape.
                    val obj = JSONObject()
                    o.customDataJson?.let { obj.put("customData", it) }
                    o.itemId?.let { obj.put("itemId", it) }
                    o.rawValue?.let { obj.put("value", it) }
                    arr.put(obj)
                }
                return arr.toString()
            }

            fun fromJson(obj: JSONObject): PendingFunnel {
                val typeName = obj.optString("funnelType")
                val type = FunnelType.values().firstOrNull { it.name == typeName }
                    ?: FunnelType.Received
                return PendingFunnel(
                    funnelType = type,
                    campaignId = obj.optStringOrNull("campaignId"),
                    nodeId = obj.optStringOrNull("nodeId"),
                    gamerTag = obj.optStringOrNull("gamerTag"),
                    accountId = obj.optStringOrNull("accountId"),
                    cidPid = obj.optStringOrNull("cidPid"),
                    deeplink = obj.optStringOrNull("deeplink"),
                    offersJson = obj.optStringOrNull("offersJson"),
                    campaignDataJson = obj.optStringOrNull("campaignDataJson"),
                    timestamp = obj.optLong("timestamp")
                )
            }

            private fun JSONObject.optStringOrNull(key: String): String? {
                if (!has(key) || isNull(key)) return null
                return optString(key).ifEmpty { null }
            }
        }
    }

    /**
     * Appends [event] to the persisted-funnel store for replay once connected. Deduped by
     * [PendingFunnel.dedupKey] and capped at [MAX_PENDING_FUNNEL] (oldest trimmed). Best-effort.
     */
    internal fun appendPendingFunnel(context: Context, event: PendingFunnel) {
        val prefs = context.applicationContext
            .getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
        synchronized(this) {
            val existing = loadPendingFunnel(prefs).toMutableList()
            if (existing.any { it.dedupKey == event.dedupKey }) return
            existing.add(event)
            while (existing.size > MAX_PENDING_FUNNEL) existing.removeAt(0)
            writePendingFunnel(prefs, existing)
        }
    }

    /** Returns (without clearing) the persisted funnel events. */
    internal fun loadPendingFunnel(context: Context): List<PendingFunnel> =
        loadPendingFunnel(
            context.applicationContext.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
        )

    /** Returns and CLEARS all persisted funnel events (for replay on launch / connect). */
    internal fun drainPendingFunnel(context: Context): List<PendingFunnel> {
        val prefs = context.applicationContext
            .getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
        synchronized(this) {
            val events = loadPendingFunnel(prefs)
            prefs.edit().remove(KEY_PENDING_FUNNEL).apply()
            return events
        }
    }

    private fun loadPendingFunnel(prefs: SharedPreferences): List<PendingFunnel> {
        val raw = prefs.getString(KEY_PENDING_FUNNEL, null) ?: return emptyList()
        return try {
            val arr = JSONArray(raw)
            (0 until arr.length()).mapNotNull { i ->
                arr.optJSONObject(i)?.let { PendingFunnel.fromJson(it) }
            }
        } catch (_: Throwable) {
            emptyList()
        }
    }

    private fun writePendingFunnel(prefs: SharedPreferences, events: List<PendingFunnel>) {
        val arr = JSONArray()
        for (e in events) arr.put(e.toJson())
        prefs.edit().putString(KEY_PENDING_FUNNEL, arr.toString()).apply()
    }
}
