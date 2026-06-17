package com.beamable.push.react

import com.beamable.push.PushListener
import com.beamable.push.PushManager
import com.facebook.react.bridge.Promise
import com.facebook.react.bridge.ReactApplicationContext
import com.facebook.react.bridge.ReactContextBaseJavaModule
import com.facebook.react.bridge.ReactMethod
import com.facebook.react.modules.core.DeviceEventManagerModule

/**
 * INBOUND + OUTBOUND React Native bridge. `@ReactMethod` functions are the inbound API the JS
 * layer calls; the implemented [PushListener] forwards results to JS via DeviceEventEmitter.
 *
 * Compiled against a `compileOnly` `com.facebook.react` dependency — react is provided by the
 * host RN app and is never present (or loaded) in a Unity/Unreal app.
 */
class ReactPushModule(
    private val reactContext: ReactApplicationContext
) : ReactContextBaseJavaModule(reactContext), PushListener {

    override fun getName() = "BeamablePush"

    @ReactMethod
    fun initialize(enableRemote: Boolean) {
        PushManager.initialize(reactContext.applicationContext, this, enableRemote)
    }

    @ReactMethod fun registerChannel(channelJson: String) = PushManager.registerChannel(channelJson)

    @ReactMethod
    fun requestPermission() {
        currentActivity?.let { PushManager.requestPermission(it) }
    }

    @ReactMethod
    fun scheduleLocal(templateJson: String, delayMillis: Double, promise: Promise) {
        promise.resolve(PushManager.scheduleLocal(templateJson, delayMillis.toLong()))
    }

    @ReactMethod fun fetchToken() = PushManager.fetchToken()
    @ReactMethod fun subscribeTopic(topic: String) = PushManager.subscribeTopic(topic)
    @ReactMethod fun unsubscribeTopic(topic: String) = PushManager.unsubscribeTopic(topic)
    @ReactMethod fun cancel(id: Double) = PushManager.cancel(id.toInt())
    @ReactMethod fun cancelAll() = PushManager.cancelAll()

    // Keep NativeEventEmitter happy on RN >= 0.65.
    @ReactMethod fun addListener(eventName: String) {}
    @ReactMethod fun removeListeners(count: Double) {}

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
