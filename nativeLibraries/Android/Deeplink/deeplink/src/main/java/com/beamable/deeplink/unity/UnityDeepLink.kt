package com.beamable.deeplink.unity

import android.app.Activity
import com.beamable.deeplink.DeepLinkManager

/**
 * INBOUND Unity bridge: the C# layer calls these `@JvmStatic` methods through
 * `AndroidJavaClass("com.beamable.deeplink.unity.UnityDeepLink").CallStatic(...)`.
 *
 * See [com.beamable.push.unity.UnityPush] for why a primitive-only `@JvmStatic`
 * facade is used instead of passing the Unity activity across JNI.
 *
 * The OUTBOUND direction (native -> C#) is handled by [UnityDeepLinkBridge], which
 * forwards cold/warm-start deeplinks to the Unity GameObject via
 * `UnitySendMessage(gameObject, "OnNativeDeepLink", url)`.
 */
object UnityDeepLink {

    private fun currentActivity(): Activity? {
        return try {
            val up = Class.forName("com.unity3d.player.UnityPlayer")
            up.getField("currentActivity").get(null) as? Activity
        } catch (t: Throwable) {
            null
        }
    }

    /** Initialize native deeplink handling and route results to [gameObject]. */
    @JvmStatic
    fun initialize(gameObject: String) {
        val act = currentActivity() ?: return
        DeepLinkManager.initialize(act.application, gameObject)
    }

    /** Pull the cold-start deeplink URL from the launch intent (or null). */
    @JvmStatic
    fun getInitialLink(): String? {
        val act = currentActivity() ?: return null
        return DeepLinkManager.getInitialLink(act)
    }

    @JvmStatic
    fun clearConsumed() = DeepLinkManager.clearConsumed()
}
