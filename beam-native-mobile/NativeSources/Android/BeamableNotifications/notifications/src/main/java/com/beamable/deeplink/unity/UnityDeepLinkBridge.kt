package com.beamable.deeplink.unity

import com.beamable.deeplink.DeepLinkListener

/**
 * Unity adapter. Forwards deeplinks to a Unity GameObject via reflection so the
 * library compiles WITHOUT Unity (UnityPlayer) on the classpath.
 *
 * The target GameObject must have a MonoBehaviour with a public method
 * `OnNativeDeepLink(string url)`.
 *
 * @param gameObject name of the GameObject that receives the message.
 */
class UnityDeepLinkBridge(private val gameObject: String) : DeepLinkListener {

    override fun onDeepLink(url: String, isColdStart: Boolean) {
        try {
            val cls = Class.forName("com.unity3d.player.UnityPlayer")
            val m = cls.getMethod(
                "UnitySendMessage",
                String::class.java,
                String::class.java,
                String::class.java
            )
            m.invoke(null, gameObject, "OnNativeDeepLink", url)
        } catch (t: Throwable) {
            // Unity not present / not yet initialized; nothing we can do here.
        }
    }
}
