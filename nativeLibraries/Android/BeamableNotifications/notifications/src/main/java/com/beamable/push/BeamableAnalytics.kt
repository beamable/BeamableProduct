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

    /** Short fire-and-forget timeouts (ms) — must stay well within the FCM ~10s budget. */
    private const val CONNECT_TIMEOUT_MS = 4_000
    private const val READ_TIMEOUT_MS = 4_000

    /** Refresh if the token expires within this skew window. */
    private const val EXPIRY_SKEW_MS = 60_000L

    private val executor = Executors.newSingleThreadExecutor { r ->
        Thread(r, "beamable-analytics").apply { isDaemon = true }
    }

    enum class FunnelType { Sent, Received, Opened, Clicked, Converted }

    // ---- Public entry points -------------------------------------------------

    /**
     * Fires a funnel event for [intent] of [type], fire-and-forget. No-op unless the intent is a
     * tracked campaign (campaignId + nodeId) AND carries scope + gamerTag (§4.2). [offer] is the
     * single offer this event concerns (Clicked/Converted), omitted otherwise.
     */
    fun trackFunnel(
        context: Context,
        intent: NotificationIntentData,
        type: FunnelType,
        offer: NotificationOffer? = null
    ) {
        if (!intent.isTrackedCampaign() || !intent.hasFunnelCredentials()) return
        val appContext = context.applicationContext
        executor.execute {
            try {
                postFunnel(appContext, intent, type, offer)
            } catch (t: Throwable) {
                Log.w(TAG, "funnel post failed: ${t.message}")
                PushManager.dispatchError("analytics_funnel", t.message ?: t.toString())
            }
        }
    }

    // ---- Core POST -----------------------------------------------------------

    private fun postFunnel(
        context: Context,
        intent: NotificationIntentData,
        type: FunnelType,
        offer: NotificationOffer?
    ) {
        val prefs = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)

        // Scope comes from the intent's cidPid ("<cid>.<pid>"); fall back to stored cid/pid.
        val (cid, pid) = resolveScope(intent, prefs) ?: run {
            Log.i(TAG, "skip funnel: no cid/pid scope")
            return
        }
        val gamerTag = intent.gamerTag ?: return
        val host = prefs.getString(KEY_HOST, null)?.trimEnd('/') ?: run {
            Log.i(TAG, "skip funnel: no host in prefs")
            return
        }

        // Mirrors iOS's single-retry semantics: AT MOST ONE refresh round-trip per send. If
        // currentAccessToken already refreshed proactively (token was clock-stale), the 401/403
        // path below must NOT refresh again — a second round-trip is redundant and risks blowing
        // the ~10s FCM budget.
        val auth = currentAccessToken(prefs, host, cid, pid) ?: run {
            Log.i(TAG, "skip funnel: no access token")
            return
        }
        var accessToken = auth.token
        val alreadyRefreshed = auth.refreshed

        val body = buildBatch(intent, type, offer).toString()
        val url = "$host/report/custom_batch/$cid/$pid/$gamerTag"
        val scope = "$cid.$pid"

        val code = doPost(url, body, accessToken, scope)
        if (code == HttpURLConnection.HTTP_UNAUTHORIZED || code == HttpURLConnection.HTTP_FORBIDDEN) {
            if (alreadyRefreshed) {
                // A proactive refresh already happened this send; a freshly-refreshed token still
                // got rejected, so refreshing again would not help. Fail/persist (the new token is
                // already persisted by refreshAccessToken) — drop the event.
                Log.w(TAG, "funnel POST rejected after proactive refresh; dropping event")
                return
            }
            // Token looked valid but the server rejected it; refresh ONCE and retry the POST ONCE.
            accessToken = refreshAccessToken(prefs, host, cid, pid) ?: return
            doPost(url, body, accessToken, scope)
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
            conn.outputStream.use { it.writeBytesUtf8(body) }
            val code = conn.responseCode
            // Drain so the connection can be reused/closed cleanly.
            try { conn.inputStream.use { it.readBytes() } } catch (_: Throwable) {
                try { conn.errorStream?.use { it.readBytes() } } catch (_: Throwable) {}
            }
            if (code !in 200..299) Log.w(TAG, "funnel POST $url -> HTTP $code")
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
        type: FunnelType,
        offer: NotificationOffer?
    ): JSONArray = JSONArray().put(buildCoreEvent(intent, type, offer))

    /** Builds one CoreEvent: {"op":"g.core","e":<funnelType>,"c":"notification_funnel","p":{...}}. */
    internal fun buildCoreEvent(
        intent: NotificationIntentData,
        type: FunnelType,
        offer: NotificationOffer?
    ): JSONObject {
        val params = JSONObject()
        intent.campaignId?.let { params.put("campaignId", it) }
        intent.nodeId?.let { params.put("nodeId", it) }
        intent.gamerTag?.let { params.put("gamerTag", it) }
        intent.accountId?.let { params.put("accountId", it) }
        intent.cidPid?.let { params.put("cidPid", it) }
        // Single offer relevant to this event (§4.6): attached ONLY when explicitly passed
        // (Clicked/Converted). Received/Opened/Sent (offer=null) emit NO offerData to avoid
        // mis-attributing the first carried offer.
        offer?.let { params.put("offerData", it.toJson()) }
        intent.deeplink?.let { params.put("deeplink", it) }
        params.put("funnelType", type.name)

        return JSONObject()
            .put("op", CORE_OP)
            .put("e", type.name)
            .put("c", FUNNEL_CATEGORY)
            .put("p", params)
    }

    private fun OutputStream.writeBytesUtf8(s: String) = write(s.toByteArray(Charsets.UTF_8))
}
