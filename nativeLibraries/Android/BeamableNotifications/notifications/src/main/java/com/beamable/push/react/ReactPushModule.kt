package com.beamable.push.react

import android.app.Activity
import android.content.Intent
import com.beamable.push.BeamableAnalytics
import com.beamable.push.CategoryStore
import com.beamable.push.IntentDataReader
import com.beamable.push.NotificationCategorySpec
import com.beamable.push.NotificationIntentData
import com.beamable.push.PushListener
import com.beamable.push.PushManager
import com.facebook.react.bridge.ActivityEventListener
import com.facebook.react.bridge.Arguments
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

    /**
     * Registers a notification action category (a named set of buttons). [categoryJson] is a
     * serialized [NotificationCategorySpec] ({ id, actions:[{ id, title, foreground?, destructive? }] }).
     * Persisted so the killed-app FCM path can render the buttons (see [CategoryStore]).
     */
    @ReactMethod
    fun registerCategory(categoryJson: String) {
        try {
            CategoryStore.register(
                reactApplicationContext,
                NotificationCategorySpec.fromJson(categoryJson),
            )
        } catch (t: Throwable) {
            PushManager.dispatchError("register_category", t.message ?: t.toString())
        }
    }

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
     * Offer / conversion funnel tracking (§4.7). Emits a **Clicked** funnel event for an
     * in-app offer click, attributed to the originating notification's intent data.
     * [intentDataJson] is the notification's intent-data JSON; [offerJson] the single clicked
     * offer (nullable). No-op unless campaignId + nodeId + scope + gamerTag are present.
     */
    @ReactMethod
    fun trackOfferClicked(intentDataJson: String, offerJson: String?) =
        PushManager.trackOfferClicked(intentDataJson, offerJson)

    /** Emits a **Converted** funnel event for an offer conversion (§4.7). See [trackOfferClicked]. */
    @ReactMethod
    fun trackOfferConverted(intentDataJson: String, offerJson: String?) =
        PushManager.trackOfferConverted(intentDataJson, offerJson)

    /**
     * Persists the player's auth credentials so the native funnel can POST (see
     * [PushManager.configureAuth]). [authJson] is the canonical credential object.
     */
    @ReactMethod
    fun configureAuth(authJson: String) =
        PushManager.configureAuth(reactApplicationContext, authJson)

    /** Clears persisted auth credentials (see [PushManager.clearAuth]). */
    @ReactMethod
    fun clearAuth() = PushManager.clearAuth(reactApplicationContext)

    /**
     * Cold-start "get intent": if the app was launched by tapping a notification, resolves its
     * payload JSON (deep link + data); otherwise null. Read-only — consumes the launch intent
     * once without re-emitting `onNotificationOpened` (the JS side pulls it on boot).
     */
    @ReactMethod
    fun getLaunchNotification(promise: Promise) {
        val activity = currentActivity
        val payload = if (activity != null) IntentDataReader.readLaunchIntent(activity) else null
        // Cold-start opens were detected but never funnel-tracked (only warm-start fires Opened,
        // via onNewIntent -> consumeIntent). Fire the Opened funnel here too — analytics ONLY, not
        // a listener re-emit, since JS pulls the payload from this method's return value on boot.
        // trackFunnel is a no-op unless campaignId + nodeId + scope + gamerTag are present.
        if (payload != null) {
            try {
                BeamableAnalytics.trackFunnel(
                    reactContext,
                    NotificationIntentData.fromJson(payload),
                    BeamableAnalytics.FunnelType.Opened,
                )
            } catch (_: Throwable) { /* analytics is best-effort */ }
        }
        promise.resolve(payload)
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

    override fun onFunnelResult(funnelType: String, ok: Boolean, statusCode: Int, message: String) {
        android.util.Log.i("BeamablePush", "RN emit onFunnelResult: $funnelType ok=$ok code=$statusCode")
        val map = Arguments.createMap().apply {
            putString("funnelType", funnelType)
            putBoolean("ok", ok)
            putInt("statusCode", statusCode)
            putString("message", message)
        }
        emit("onFunnelResult", map)
    }
}
