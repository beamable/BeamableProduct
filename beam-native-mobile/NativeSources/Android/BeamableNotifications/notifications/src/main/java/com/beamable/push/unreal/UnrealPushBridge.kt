package com.beamable.push.unreal

import com.beamable.push.PushListener

/**
 * OUTBOUND Unreal bridge: routes core [PushListener] callbacks to the UE plugin's C++ through
 * JNI `external` (native) functions. The matching implementations
 * (`Java_com_beamable_push_unreal_UnrealPushBridge_native*`) are provided by the Unreal plugin's
 * C++ (added later), which marshals onto the Game thread before broadcasting UE delegates.
 *
 * No Unreal dependency at build time — the `external` declarations compile standalone. This
 * object is only ever set as the listener from [UnrealPush.initialize] (which no-ops unless
 * running under Unreal), so the native methods are never invoked in a non-UE app.
 */
object UnrealPushBridge : PushListener {

    private external fun nativeOnToken(token: String)
    private external fun nativeOnTokenError(error: String)
    private external fun nativeOnMessageForeground(json: String)
    private external fun nativeOnNotificationOpened(json: String)
    private external fun nativeOnPermissionResult(granted: Boolean)
    private external fun nativeOnLocalScheduled(id: Int)
    private external fun nativeOnError(stage: String, message: String)

    override fun onTokenReceived(token: String) = safe { nativeOnToken(token) }
    override fun onTokenRefreshError(error: String) = safe { nativeOnTokenError(error) }
    override fun onMessageReceivedForeground(messageJson: String) = safe { nativeOnMessageForeground(messageJson) }
    override fun onNotificationOpened(dataJson: String) = safe { nativeOnNotificationOpened(dataJson) }
    override fun onPermissionResult(granted: Boolean) = safe { nativeOnPermissionResult(granted) }
    override fun onLocalNotificationScheduled(id: Int) = safe { nativeOnLocalScheduled(id) }
    override fun onError(stage: String, message: String) = safe { nativeOnError(stage, message) }

    private inline fun safe(block: () -> Unit) {
        try {
            block()
        } catch (_: Throwable) {
            // No native impl bound (not running under Unreal) — ignore.
        }
    }
}
