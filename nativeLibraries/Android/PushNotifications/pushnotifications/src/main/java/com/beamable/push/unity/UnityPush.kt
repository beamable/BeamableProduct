package com.beamable.push.unity

import android.app.Activity
import com.beamable.push.PushManager

/**
 * INBOUND Unity bridge: the C# layer calls these `@JvmStatic` methods through
 * `AndroidJavaClass("com.beamable.push.unity.UnityPush").CallStatic(...)`.
 *
 * Why a dedicated facade instead of calling [PushManager] directly from C#:
 *  - [PushManager] is a Kotlin `object`, so its members are instance methods on
 *    `INSTANCE` (not JNI-static) — awkward to call from Unity.
 *  - Passing the Unity activity across JNI is fragile: Unity builds the JNI method
 *    signature from the argument's *runtime* class (`UnityPlayerActivity`), which
 *    does not match a formal `Context`/`Activity` parameter, so `GetMethodID` fails.
 *
 * This facade sidesteps both: every method takes only primitives/Strings (which map
 * cleanly across JNI) and resolves the current Activity natively via
 * `UnityPlayer.currentActivity` by reflection.
 *
 * The OUTBOUND direction (native -> C#) is handled by [UnityPushBridge].
 */
object UnityPush {

    private fun currentActivity(): Activity? {
        return try {
            val up = Class.forName("com.unity3d.player.UnityPlayer")
            up.getField("currentActivity").get(null) as? Activity
        } catch (t: Throwable) {
            null
        }
    }

    /**
     * Initialize push and route callbacks to the Unity GameObject [gameObject].
     *
     * @param enableRemote opt into FCM; remote is only activated if a Firebase config is
     *   present (auto-detected). Pass false to force local-only.
     */
    @JvmStatic
    fun initialize(gameObject: String, enableRemote: Boolean) {
        val act = currentActivity() ?: return
        PushManager.isForeground = true
        PushManager.initialize(act.applicationContext, gameObject, enableRemote)
    }

    /** Register a notification channel from a JSON spec. */
    @JvmStatic
    fun registerChannel(json: String) = PushManager.registerChannel(json)

    /** Request POST_NOTIFICATIONS (API 33+). */
    @JvmStatic
    fun requestPermission() {
        currentActivity()?.let { PushManager.requestPermission(it) }
    }

    /** Whether notifications are currently permitted. */
    @JvmStatic
    fun hasPermission(): Boolean {
        val act = currentActivity() ?: return false
        return PushManager.hasPermission(act)
    }

    /** Schedule a local notification from a [NotificationTemplate] JSON; returns its id. */
    @JvmStatic
    fun scheduleLocal(json: String, delayMillis: Long): Int =
        PushManager.scheduleLocal(json, delayMillis)

    /** Fetch the current FCM token (delivered via OnTokenReceived). */
    @JvmStatic
    fun fetchToken() = PushManager.fetchToken()

    @JvmStatic
    fun subscribeTopic(topic: String) = PushManager.subscribeTopic(topic)

    @JvmStatic
    fun unsubscribeTopic(topic: String) = PushManager.unsubscribeTopic(topic)

    /** Read & clear the launch intent's notification payload JSON (or null). */
    @JvmStatic
    fun consumeLaunchIntent(): String? {
        val act = currentActivity() ?: return null
        return PushManager.consumeLaunchIntent(act)
    }

    @JvmStatic
    fun cancel(id: Int) = PushManager.cancel(id)

    @JvmStatic
    fun cancelAll() = PushManager.cancelAll()

    /** Lets the engine report foreground/background so FCM routing is correct. */
    @JvmStatic
    fun setForeground(foreground: Boolean) {
        PushManager.isForeground = foreground
    }
}
