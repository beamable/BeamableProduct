package com.beamable.push

/**
 * Engine-agnostic callback surface for the push library.
 *
 * Implementations may be invoked from background threads (FCM callbacks run off
 * the main thread). Implementations must therefore be thread-safe or marshal to
 * their own main thread as needed. The bundled [com.beamable.push.unity.UnityNotificationsBridge]
 * forwards via UnityPlayer.UnitySendMessage, which is itself thread-safe.
 */
interface PushListener {
    /** A fresh FCM registration token is available. */
    fun onTokenReceived(token: String)

    /** Fetching/refreshing the FCM token failed. */
    fun onTokenRefreshError(error: String)

    /** A push message arrived while the app was in the foreground (JSON payload). */
    fun onMessageReceivedForeground(messageJson: String)

    /** The app was opened by tapping a notification; [dataJson] carries the payload. */
    fun onNotificationOpened(dataJson: String)

    /** Result of a POST_NOTIFICATIONS permission request. */
    fun onPermissionResult(granted: Boolean)

    /** A local notification was successfully scheduled; [id] is its notification id. */
    fun onLocalNotificationScheduled(id: Int)

    /** A recoverable error occurred at [stage] with a human-readable [message]. */
    fun onError(stage: String, message: String)
}
