package com.beamable.push.unreal

import android.app.Activity
import com.beamable.push.PushManager

/**
 * INBOUND Unreal bridge: the UE plugin's C++ calls these `@JvmStatic` methods via JNI
 * (`CallStaticVoidMethod` etc.). They take only Strings/primitives and resolve the current
 * Activity natively (from `com.epicgames.unreal.GameActivity`), so C++ never has to pass JNI
 * object references around.
 *
 * Callbacks flow back the other way through [UnrealPushBridge].
 */
object UnrealPush {

    private fun currentActivity(): Activity? {
        return try {
            val cls = Class.forName("com.epicgames.unreal.GameActivity")
            cls.getMethod("Get").invoke(null) as? Activity
        } catch (t: Throwable) {
            null
        }
    }

    @JvmStatic
    fun initialize(enableRemote: Boolean) {
        val act = currentActivity() ?: return
        PushManager.isForeground = true
        PushManager.initialize(act.applicationContext, UnrealPushBridge, enableRemote)
    }

    /** Register a notification channel. [importance] uses NotificationManager.IMPORTANCE_* (4 = HIGH). */
    @JvmStatic
    fun registerChannel(id: String, name: String, description: String, importance: Int) =
        PushManager.registerChannel(id, name, description, importance)

    @JvmStatic
    fun requestPermission() {
        currentActivity()?.let { PushManager.requestPermission(it) }
    }

    @JvmStatic
    fun hasPermission(): Boolean {
        val act = currentActivity() ?: return false
        return PushManager.hasPermission(act)
    }

    @JvmStatic
    fun scheduleLocal(json: String, delayMillis: Long): Int =
        PushManager.scheduleLocal(json, delayMillis)

    /** Exact variant of [scheduleLocal] (needs SCHEDULE_EXACT_ALARM; falls back to inexact). */
    @JvmStatic
    fun scheduleLocalExact(json: String, delayMillis: Long): Int =
        PushManager.scheduleLocalExact(json, delayMillis)

    /** Schedule at an absolute wall-clock time, interpreted as UTC ([useUtc]) or device-local. month is 1-12. */
    @JvmStatic
    fun scheduleLocalAt(
        json: String, year: Int, month: Int, dayOfMonth: Int,
        hourOfDay: Int, minute: Int, second: Int, useUtc: Boolean, exact: Boolean
    ): Int = PushManager.scheduleLocalAt(json, year, month, dayOfMonth, hourOfDay, minute, second, useUtc, exact)

    @JvmStatic fun canScheduleExactAlarms(): Boolean = PushManager.canScheduleExactAlarms()

    @JvmStatic
    fun requestExactAlarmPermission() {
        currentActivity()?.let { PushManager.requestExactAlarmPermission(it) }
    }

    @JvmStatic fun fetchToken() = PushManager.fetchToken()
    @JvmStatic fun subscribeTopic(topic: String) = PushManager.subscribeTopic(topic)
    @JvmStatic fun unsubscribeTopic(topic: String) = PushManager.unsubscribeTopic(topic)

    @JvmStatic
    fun consumeLaunchIntent(): String? {
        val act = currentActivity() ?: return null
        return PushManager.consumeLaunchIntent(act)
    }

    @JvmStatic fun cancel(id: Int) = PushManager.cancel(id)
    @JvmStatic fun cancelAll() = PushManager.cancelAll()

    /**
     * Persists the player's auth credentials so the native funnel can POST (see
     * [PushManager.configureAuth]). [authJson] is the canonical credential object.
     */
    @JvmStatic
    fun configureAuth(authJson: String) {
        val act = currentActivity() ?: return
        PushManager.configureAuth(act.applicationContext, authJson)
    }

    /** Clears persisted auth credentials (see [PushManager.clearAuth]). */
    @JvmStatic
    fun clearAuth() {
        val act = currentActivity() ?: return
        PushManager.clearAuth(act.applicationContext)
    }

    /**
     * Emits a **Clicked** funnel event for an offer the player acted on (§4.7).
     *
     * The Unreal C++ side passes ONE canonical `OfferTrackRequest` JSON (matching iOS
     * `bmn_trackOfferClicked`), a FLAT object:
     *
     *   {"campaignId","nodeId","gamerTag","accountId","cidPid","deeplink","offer":{...}}
     *
     * [PushManager.trackOfferClicked] instead expects TWO strings: the intent-data JSON
     * (the campaign scalars) and the offer JSON. We therefore split the request: the
     * `offer` sub-object becomes `offerJson`, and the remaining top-level scalars
     * (campaignId/nodeId/gamerTag/accountId/cidPid/deeplink) are the intent-data JSON.
     * [com.beamable.push.NotificationIntentData.fromJson] reads only those scalar keys, so
     * passing the request object minus `offer` is safe.
     */
    @JvmStatic
    fun trackOfferClicked(requestJson: String) {
        val (intentDataJson, offerJson) = splitOfferTrackRequest(requestJson)
        PushManager.trackOfferClicked(intentDataJson, offerJson)
    }

    /** Emits a **Converted** funnel event for an offer conversion (§4.7). See [trackOfferClicked]. */
    @JvmStatic
    fun trackOfferConverted(requestJson: String) {
        val (intentDataJson, offerJson) = splitOfferTrackRequest(requestJson)
        PushManager.trackOfferConverted(intentDataJson, offerJson)
    }

    /**
     * Splits a canonical [OfferTrackRequest] JSON into the (intentDataJson, offerJson) pair
     * [PushManager.trackOfferClicked] / [PushManager.trackOfferConverted] expect. Returns the
     * request object (sans `offer`) as the intent data and the `offer` sub-object (or null)
     * as the offer JSON. On a parse failure, falls back to the raw request as intent data.
     */
    private fun splitOfferTrackRequest(requestJson: String): Pair<String, String?> {
        return try {
            val root = org.json.JSONObject(requestJson)
            val offer = root.optJSONObject("offer")
            val offerJson = offer?.toString()
            root.remove("offer")
            Pair(root.toString(), offerJson)
        } catch (_: Throwable) {
            Pair(requestJson, null)
        }
    }

    @JvmStatic
    fun setForeground(foreground: Boolean) {
        PushManager.isForeground = foreground
    }
}
