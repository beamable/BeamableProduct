package com.beamable.deeplink.react

import android.app.Activity
import android.app.Application
import android.content.Intent
import com.beamable.deeplink.DeepLinkListener
import com.beamable.deeplink.DeepLinkManager
import com.facebook.react.bridge.ActivityEventListener
import com.facebook.react.bridge.Arguments
import com.facebook.react.bridge.Promise
import com.facebook.react.bridge.ReactApplicationContext
import com.facebook.react.bridge.ReactContextBaseJavaModule
import com.facebook.react.bridge.ReactMethod
import com.facebook.react.modules.core.DeviceEventManagerModule

/**
 * React Native bridge for the deeplink core. Cold-start via [getInitialLink]; warm-start via an
 * [ActivityEventListener] that forwards `onNewIntent` to the core, emitting `onDeepLink` to JS.
 *
 * Compiled against a `compileOnly` `com.facebook.react` dependency.
 */
class ReactDeepLinkModule(
    private val reactContext: ReactApplicationContext
) : ReactContextBaseJavaModule(reactContext), DeepLinkListener, ActivityEventListener {

    init {
        reactContext.addActivityEventListener(this)
    }

    override fun getName() = "BeamableDeeplink"

    // Named initializeDeepLinks (not initialize) to avoid hiding the no-arg
    // ReactContextBaseJavaModule.initialize() lifecycle method.
    @ReactMethod
    fun initializeDeepLinks() {
        val app = reactContext.applicationContext as? Application ?: return
        DeepLinkManager.initialize(app, this)
    }

    @ReactMethod
    fun getInitialLink(promise: Promise) {
        promise.resolve(currentActivity?.let { DeepLinkManager.getInitialLink(it) })
    }

    @ReactMethod fun addListener(eventName: String) {}
    @ReactMethod fun removeListeners(count: Double) {}

    // ActivityEventListener — warm-start VIEW intents arrive here.
    override fun onNewIntent(intent: Intent) = DeepLinkManager.handleNewIntent(intent)
    override fun onActivityResult(activity: Activity?, requestCode: Int, resultCode: Int, data: Intent?) {}

    // DeepLinkListener
    override fun onDeepLink(url: String, isColdStart: Boolean) {
        val map = Arguments.createMap().apply {
            putString("url", url)
            putBoolean("isColdStart", isColdStart)
        }
        reactContext
            .getJSModule(DeviceEventManagerModule.RCTDeviceEventEmitter::class.java)
            .emit("onDeepLink", map)
    }
}
