package com.beamable.push.unity

import android.app.Activity
import com.beamable.push.PushManager
import org.json.JSONObject

/**
 * INBOUND Unity facade matching the iOS `BeamableNotifications` C ABI naming. The shared C# layer
 * (Beamable.Notifications.BeamableNotifications) calls these `@JvmStatic` methods via
 * `AndroidJavaClass` with the SAME serialized DTO JSON used on iOS, so one C# call site drives both
 * platforms. Outbound callbacks go through [UnityNotificationsBridge].
 *
 * Methods/features with no Android backing (templates, categories, analytics, delivery receipts,
 * badge, pending list) are best-effort / no-op here.
 */
object UnityNotifications {

    private const val DEFAULT_CHANNEL = "beamable_default"
    private var bridge: UnityNotificationsBridge? = null

    private fun currentActivity(): Activity? {
        return try {
            val up = Class.forName("com.unity3d.player.UnityPlayer")
            up.getField("currentActivity").get(null) as? Activity
        } catch (t: Throwable) {
            null
        }
    }

    @JvmStatic
    fun initialize(gameObject: String) {
        val act = currentActivity() ?: return
        val b = UnityNotificationsBridge(gameObject)
        bridge = b
        PushManager.isForeground = true
        PushManager.initialize(act.applicationContext, b, /*enableRemote*/ true)
        PushManager.registerChannel(DEFAULT_CHANNEL, "Notifications", "", 4) // 4 = IMPORTANCE_HIGH
    }

    @JvmStatic
    fun requestPermission(optionsJson: String) {
        // iOS-only option fields (alert/badge/sound/provisional/…) don't apply on Android.
        currentActivity()?.let { PushManager.requestPermission(it) }
    }

    @JvmStatic
    fun getPermissionStatus() {
        val act = currentActivity() ?: return
        PushManager.dispatchPermissionResult(PushManager.hasPermission(act))
    }

    /** Schedules from a serialized iOS `LocalRequest` JSON, mapping its trigger onto AlarmManager. */
    @JvmStatic
    fun scheduleLocal(requestJson: String) {
        try {
            val req = JSONObject(requestJson)
            val id = req.optString("id", "")
            val userInfo = req.optJSONObject("userInfo")
            val deep = userInfo?.let {
                val d = it.optString("deepLink", it.optString("deeplink", ""))
                if (d.isEmpty()) null else d
            }

            val template = JSONObject()
            template.put("id", stableId(id))
            template.put("title", req.optString("title", ""))
            template.put("body", req.optString("body", ""))
            template.put("channelId", DEFAULT_CHANNEL)
            if (deep != null) template.put("deepLinkUrl", deep)
            val data = JSONObject()
            userInfo?.let { ui ->
                val keys = ui.keys()
                while (keys.hasNext()) {
                    val k = keys.next()
                    data.put(k, ui.opt(k)?.toString() ?: "")
                }
            }
            template.put("dataPayload", data)
            val templateJson = template.toString()

            val trigger = req.optJSONObject("trigger")
            when (trigger?.optString("type", "immediate") ?: "immediate") {
                "timeInterval" -> {
                    val seconds = trigger?.optDouble("seconds", 0.0) ?: 0.0
                    PushManager.scheduleLocal(templateJson, (seconds * 1000).toLong())
                }
                "calendar" -> {
                    PushManager.scheduleLocalAt(
                        templateJson,
                        trigger!!.optInt("year"), trigger.optInt("month"), trigger.optInt("day"),
                        trigger.optInt("hour"), trigger.optInt("minute"), trigger.optInt("second"),
                        /*useUtc*/ false, /*exact*/ false
                    )
                }
                else -> PushManager.scheduleLocal(templateJson, 0L) // immediate
            }
        } catch (t: Throwable) {
            PushManager.dispatchError("schedule_local", t.message ?: t.toString())
        }
    }

    @JvmStatic fun cancelLocal(id: String) = PushManager.cancel(stableId(id))
    @JvmStatic fun cancelAllLocal() = PushManager.cancelAll()
    @JvmStatic fun clearDelivered() = PushManager.cancelAll()

    // AlarmManager can't enumerate pending alarms / Android has no NSE delivery receipts → empty.
    @JvmStatic fun getPending() { bridge?.emit("OnPendingNotifications", "[]") }
    @JvmStatic fun getDeliveryReceipts() { bridge?.emit("OnDeliveryReceipts", "[]") }

    @JvmStatic fun registerForRemote() = PushManager.fetchToken()
    @JvmStatic fun unregisterForRemote() { /* FCM manages registration; no-op */ }

    // Best-effort / no-op on Android (no direct equivalent).
    @JvmStatic fun configureAnalytics(configJson: String) {}
    @JvmStatic fun registerTemplate(templateJson: String) {}
    @JvmStatic fun registerCategory(categoryJson: String) {}
    @JvmStatic fun setBadge(count: Int) {}

    @JvmStatic
    fun getLaunchNotification(): String? {
        val act = currentActivity() ?: return null
        val payload = PushManager.consumeLaunchIntent(act) ?: return null
        return UnityNotificationsBridge.toNotificationData(payload, /*wasLaunch*/ true)
    }

    /** Lets the engine report foreground/background (used for received-handler wasForeground). */
    @JvmStatic
    fun setForeground(foreground: Boolean) {
        PushManager.isForeground = foreground
    }

    /** Stable string-id → int-id mapping so cancelLocal targets the same AlarmManager id. */
    private fun stableId(id: String): Int =
        if (id.isEmpty()) (System.nanoTime() and Int.MAX_VALUE.toLong()).toInt()
        else id.hashCode() and Int.MAX_VALUE
}
