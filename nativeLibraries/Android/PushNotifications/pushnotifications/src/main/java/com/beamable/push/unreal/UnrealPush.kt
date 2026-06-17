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

    @JvmStatic fun registerChannel(json: String) = PushManager.registerChannel(json)

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

    @JvmStatic
    fun setForeground(foreground: Boolean) {
        PushManager.isForeground = foreground
    }
}
