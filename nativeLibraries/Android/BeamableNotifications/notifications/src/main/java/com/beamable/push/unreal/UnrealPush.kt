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

    @JvmStatic
    fun setForeground(foreground: Boolean) {
        PushManager.isForeground = foreground
    }
}
