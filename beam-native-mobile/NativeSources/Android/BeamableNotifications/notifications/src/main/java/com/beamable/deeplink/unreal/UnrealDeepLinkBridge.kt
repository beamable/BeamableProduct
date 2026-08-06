package com.beamable.deeplink.unreal

import com.beamable.deeplink.DeepLinkListener

/**
 * OUTBOUND Unreal bridge: routes deeplinks to the UE plugin's C++ via a JNI `external` function
 * (`Java_com_beamable_deeplink_unreal_UnrealDeepLinkBridge_nativeOnDeepLink`, implemented in the
 * plugin's C++). No Unreal dependency at build time; only set as the listener under Unreal.
 */
object UnrealDeepLinkBridge : DeepLinkListener {

    private external fun nativeOnDeepLink(url: String, isColdStart: Boolean)

    override fun onDeepLink(url: String, isColdStart: Boolean) {
        try {
            nativeOnDeepLink(url, isColdStart)
        } catch (_: Throwable) {
            // No native impl bound (not running under Unreal) — ignore.
        }
    }
}
