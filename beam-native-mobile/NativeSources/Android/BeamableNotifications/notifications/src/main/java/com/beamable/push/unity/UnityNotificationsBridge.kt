package com.beamable.push.unity

import com.beamable.push.PushListener
import org.json.JSONObject

/**
 * Unity adapter: forwards [PushListener] callbacks to a Unity GameObject via
 * UnityPlayer.UnitySendMessage, reshaped to match the iOS `BeamableNotifications` callback names
 * and JSON payloads (`NotificationData`, `PermissionResult`, `{token}`, `{error}`).
 *
 * The Unity dependency is resolved reflectively so the core module still compiles for
 * Unreal / React Native consumers. UnitySendMessage is thread-safe and delivers on Unity's main
 * thread, so callbacks are forwarded directly.
 *
 * @param gameObject name of the Unity GameObject (the AndroidNotificationsRelay) that receives them.
 */
class UnityNotificationsBridge(private val gameObject: String) : PushListener {

    /** Sends [payload] to [gameObject].[method] via UnityPlayer.UnitySendMessage. */
    fun emit(method: String, payload: String) {
        try {
            val cls = Class.forName("com.unity3d.player.UnityPlayer")
            val m = cls.getMethod(
                "UnitySendMessage",
                String::class.java,
                String::class.java,
                String::class.java
            )
            m.invoke(null, gameObject, method, payload)
        } catch (t: Throwable) {
            // Not running under Unity (or method unavailable): silently ignore.
        }
    }

    override fun onTokenReceived(token: String) =
        emit("OnTokenReceived", JSONObject().put("token", token).toString())

    override fun onTokenRefreshError(error: String) =
        emit("OnTokenError", JSONObject().put("error", error).toString())

    override fun onMessageReceivedForeground(messageJson: String) =
        emit("OnNotificationReceived", toNotificationData(messageJson, false))

    override fun onNotificationOpened(dataJson: String) =
        emit("OnNotificationTapped", toNotificationData(dataJson, true))

    override fun onPermissionResult(granted: Boolean) =
        emit(
            "OnPermissionResult",
            JSONObject()
                .put("status", if (granted) "authorized" else "denied")
                .put("granted", granted)
                .toString()
        )

    // No iOS-side equivalents — kept internal/ignored.
    override fun onLocalNotificationScheduled(id: Int) {}
    override fun onError(stage: String, message: String) {}

    companion object {
        /**
         * Converts the Android flat data JSON (FCM data map + optional title/body, or a launch
         * payload) into the shared iOS `NotificationData` shape.
         */
        fun toNotificationData(flatJson: String, wasLaunch: Boolean): String {
            val src = try {
                JSONObject(flatJson)
            } catch (t: Throwable) {
                JSONObject()
            }
            val out = JSONObject()
            out.put("id", src.optString("id", src.optString("messageId", "")))
            if (src.has("title")) out.put("title", src.optString("title"))
            if (src.has("body")) out.put("body", src.optString("body"))
            val deep = src.optString("deepLink", src.optString("deeplink", ""))
            if (deep.isNotEmpty()) out.put("deepLink", deep)
            out.put("wasLaunch", wasLaunch)
            out.put("userInfo", src)
            return out.toString()
        }
    }
}
