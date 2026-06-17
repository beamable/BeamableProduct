package com.beamable.push

/**
 * Immutable snapshot of a push message at the moment it was received, passed to a
 * [PushNotificationReceivedHandler].
 *
 * @param messageId FCM message id, when present.
 * @param dataJson the full FCM data map serialized as a JSON object string.
 * @param sentTimeMillis when the message was sent (RemoteMessage.sentTime), epoch millis.
 * @param receivedTimeMillis when the device received it (System.currentTimeMillis()).
 * @param wasForeground whether the app was foregrounded when received (best-effort).
 * @param deepLink convenience copy of dataPayload["deeplink"] if present, else null.
 */
data class PushReceivedEvent(
    val messageId: String?,
    val dataJson: String,
    val sentTimeMillis: Long,
    val receivedTimeMillis: Long,
    val wasForeground: Boolean,
    val deepLink: String?
)
