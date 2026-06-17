package com.beamable.deeplink.unreal

import android.app.Activity
import android.app.Application
import android.content.Intent
import com.beamable.deeplink.DeepLinkManager

/**
 * INBOUND Unreal bridge: the UE plugin's C++ calls these `@JvmStatic` methods via JNI.
 * Cold-start uses [getInitialLink]; warm-start is delivered through [UnrealDeepLinkBridge]
 * after the plugin's UPL forwards `GameActivity.onNewIntent` to [handleNewIntent].
 */
object UnrealDeepLink {

    private fun currentActivity(): Activity? {
        return try {
            val cls = Class.forName("com.epicgames.unreal.GameActivity")
            cls.getMethod("Get").invoke(null) as? Activity
        } catch (t: Throwable) {
            null
        }
    }

    @JvmStatic
    fun initialize() {
        val act = currentActivity() ?: return
        DeepLinkManager.initialize(act.application as Application, UnrealDeepLinkBridge)
    }

    @JvmStatic
    fun getInitialLink(): String? {
        val act = currentActivity() ?: return null
        return DeepLinkManager.getInitialLink(act)
    }

    /** Forward warm-start VIEW intents (called from the plugin's onNewIntent hook). */
    @JvmStatic
    fun handleNewIntent(intent: Intent) = DeepLinkManager.handleNewIntent(intent)

    @JvmStatic
    fun clearConsumed() = DeepLinkManager.clearConsumed()
}
