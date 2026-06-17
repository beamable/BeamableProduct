package com.beamable.push.unity

import com.beamable.push.PushListener

/**
 * Unity adapter: forwards [PushListener] callbacks to a Unity GameObject via
 * UnityPlayer.UnitySendMessage.
 *
 * The Unity dependency is resolved reflectively so the core module still compiles
 * and links for Unreal / React Native consumers (where UnityPlayer is absent).
 * UnitySendMessage is thread-safe, so callbacks from FCM's background threads are
 * forwarded directly without any main-thread marshaling.
 *
 * @param gameObject name of the Unity GameObject whose script receives the messages.
 */
class UnityPushBridge(private val gameObject: String) : PushListener {

    private fun send(method: String, payload: String) {
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

    override fun onTokenReceived(token: String) = send("OnTokenReceived", token)

    override fun onTokenRefreshError(error: String) = send("OnTokenError", error)

    override fun onMessageReceivedForeground(messageJson: String) =
        send("OnMessageForeground", messageJson)

    override fun onNotificationOpened(dataJson: String) =
        send("OnNotificationOpened", dataJson)

    override fun onPermissionResult(granted: Boolean) =
        send("OnPermissionResult", granted.toString())

    override fun onLocalNotificationScheduled(id: Int) =
        send("OnLocalScheduled", id.toString())

    override fun onError(stage: String, message: String) =
        send("OnNativeError", "$stage|$message")
}
