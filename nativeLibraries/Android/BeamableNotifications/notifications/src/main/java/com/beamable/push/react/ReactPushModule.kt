package com.beamable.push.react

import android.app.Activity
import android.content.Intent
import com.beamable.push.IntentDataReader
import com.beamable.push.PushListener
import com.beamable.push.PushManager
import com.facebook.react.bridge.ActivityEventListener
import com.facebook.react.bridge.Promise
import com.facebook.react.bridge.ReactApplicationContext
import com.facebook.react.bridge.ReactContextBaseJavaModule
import com.facebook.react.bridge.ReactMethod
import com.facebook.react.modules.core.DeviceEventManagerModule

/**
 * INBOUND + OUTBOUND React Native bridge. `@ReactMethod` functions are the inbound API the JS
 * layer calls; the implemented [PushListener] forwards results to JS via DeviceEventEmitter.
 *
 * Also an [ActivityEventListener] so RN forwards `onNewIntent` here: a notification tapped while
 * the app is alive (backgrounded) re-launches the activity with the payload, which we consume to
 * emit `onNotificationOpened`. Cold-start taps are recovered instead via [getLaunchNotification].
 *
 * Compiled against a `compileOnly` `com.facebook.react` dependency — react is provided by the
 * host RN app and is never present (or loaded) in a Unity/Unreal app.
 */
class ReactPushModule(
    private val reactContext: ReactApplicationContext
) : ReactContextBaseJavaModule(reactContext), PushListener, ActivityEventListener {

    init {
        // Receive `onNewIntent` for warm-start notification taps.
        reactContext.addActivityEventListener(this)
    }

    override fun getName() = "BeamablePush"

    @ReactMethod
    fun initialize(enableRemote: Boolean) {
        PushManager.initialize(reactContext.applicationContext, this, enableRemote)
    }

    /** Register a notification channel. importance uses NotificationManager.IMPORTANCE_* (4 = HIGH). */
    @ReactMethod
    fun registerChannel(id: String, name: String, description: String, importance: Double) =
        PushManager.registerChannel(id, name, description, importance.toInt())

    @ReactMethod
    fun requestPermission() {
        currentActivity?.let { PushManager.requestPermission(it) }
    }

    @ReactMethod
    fun scheduleLocal(templateJson: String, delayMillis: Double, promise: Promise) {
        promise.resolve(PushManager.scheduleLocal(templateJson, delayMillis.toLong()))
    }

    /** Exact variant of [scheduleLocal] (needs SCHEDULE_EXACT_ALARM; falls back to inexact). */
    @ReactMethod
    fun scheduleLocalExact(templateJson: String, delayMillis: Double, promise: Promise) {
        promise.resolve(PushManager.scheduleLocalExact(templateJson, delayMillis.toLong()))
    }

    /** Schedule at an absolute wall-clock time, interpreted as UTC ([useUtc]) or device-local. month is 1-12. */
    @ReactMethod
    fun scheduleLocalAt(
        templateJson: String, year: Double, month: Double, dayOfMonth: Double,
        hourOfDay: Double, minute: Double, second: Double, useUtc: Boolean, exact: Boolean,
        promise: Promise
    ) {
        promise.resolve(
            PushManager.scheduleLocalAt(
                templateJson, year.toInt(), month.toInt(), dayOfMonth.toInt(),
                hourOfDay.toInt(), minute.toInt(), second.toInt(), useUtc, exact
            )
        )
    }

    @ReactMethod
    fun canScheduleExactAlarms(promise: Promise) {
        promise.resolve(PushManager.canScheduleExactAlarms())
    }

    @ReactMethod
    fun requestExactAlarmPermission() {
        currentActivity?.let { PushManager.requestExactAlarmPermission(it) }
    }

    @ReactMethod fun fetchToken() = PushManager.fetchToken()
    @ReactMethod fun subscribeTopic(topic: String) = PushManager.subscribeTopic(topic)
    @ReactMethod fun unsubscribeTopic(topic: String) = PushManager.unsubscribeTopic(topic)
    @ReactMethod fun cancel(id: Double) = PushManager.cancel(id.toInt())
    @ReactMethod fun cancelAll() = PushManager.cancelAll()

    /**
     * Cold-start "get intent": if the app was launched by tapping a notification, resolves its
     * payload JSON (deep link + data); otherwise null. Read-only — consumes the launch intent
     * once without re-emitting `onNotificationOpened` (the JS side pulls it on boot).
     */
    @ReactMethod
    fun getLaunchNotification(promise: Promise) {
        val activity = currentActivity
        promise.resolve(if (activity != null) IntentDataReader.readLaunchIntent(activity) else null)
    }

    // Keep NativeEventEmitter happy on RN >= 0.65.
    @ReactMethod fun addListener(eventName: String) {}
    @ReactMethod fun removeListeners(count: Double) {}

    // ---- ActivityEventListener (warm-start notification taps) ----
    override fun onNewIntent(intent: Intent?) {
        // App was alive (backgrounded) and the user tapped a notification: emit onNotificationOpened.
        PushManager.consumeIntent(intent)
    }

    override fun onActivityResult(activity: Activity?, requestCode: Int, resultCode: Int, data: Intent?) {
        // Not used.
    }

    private fun emit(event: String, data: Any?) {
        reactContext
            .getJSModule(DeviceEventManagerModule.RCTDeviceEventEmitter::class.java)
            .emit(event, data)
    }

    // ---- PushListener (RN emitter dispatches to the JS thread) ----
    override fun onTokenReceived(token: String) = emit("onTokenReceived", token)
    override fun onTokenRefreshError(error: String) = emit("onTokenRefreshError", error)
    override fun onMessageReceivedForeground(messageJson: String) = emit("onMessageForeground", messageJson)
    override fun onNotificationOpened(dataJson: String) = emit("onNotificationOpened", dataJson)
    override fun onPermissionResult(granted: Boolean) = emit("onPermissionResult", granted)
    override fun onLocalNotificationScheduled(id: Int) = emit("onLocalScheduled", id)
    override fun onError(stage: String, message: String) = emit("onError", "$stage|$message")
}
