package com.beamable.reactnative

import com.beamable.deeplink.react.ReactDeepLinkModule
import com.beamable.push.react.ReactPushModule
import com.facebook.react.ReactPackage
import com.facebook.react.bridge.NativeModule
import com.facebook.react.bridge.ReactApplicationContext
import com.facebook.react.uimanager.ViewManager

/**
 * Aggregator [ReactPackage] for the React Native `beamable-notifications-android` package.
 *
 * Android autolinking registers a single package per dependency, so this one exposes both
 * bridges shipped in the `.aar`: [ReactPushModule] ("BeamablePush") and [ReactDeepLinkModule]
 * ("BeamableDeeplink"). Autolink target (see react-native.config.js):
 * `com.beamable.reactnative.BeamableNotificationsPackage`.
 */
class BeamableNotificationsPackage : ReactPackage {
    override fun createNativeModules(reactContext: ReactApplicationContext): List<NativeModule> =
        listOf(
            ReactPushModule(reactContext),
            ReactDeepLinkModule(reactContext),
        )

    override fun createViewManagers(reactContext: ReactApplicationContext): List<ViewManager<*, *>> =
        emptyList()
}
