package com.beamable.push

import com.google.firebase.messaging.FirebaseMessagingService
import com.google.firebase.messaging.RemoteMessage
import org.json.JSONObject

/**
 * Firebase Cloud Messaging entry point.
 *
 * Forwards token refreshes and incoming messages to [PushManager]. All callbacks
 * here run off the main thread; [PushManager] dispatches are thread-safe.
 */
class PushFirebaseService : FirebaseMessagingService() {

    /** Called when a new FCM registration token is generated. */
    override fun onNewToken(token: String) {
        PushManager.dispatchToken(token)
    }

    /** Called for every incoming FCM message (data and/or notification payload). */
    override fun onMessageReceived(remoteMessage: RemoteMessage) {
        // Flatten the data map plus any notification title/body into one JSON blob.
        val json = buildMessageJson(remoteMessage)

        // Receive-time hook — fires in foreground, background, AND killed states
        // (data-only messages). Runs before any display/dispatch branching, and must never
        // throw out of here.
        invokeNotificationReceived(remoteMessage, json)

        if (PushManager.isForeground) {
            // App is visible: hand the raw message to the engine to decide how to
            // present it (e.g. in-game toast) rather than posting a system tray item.
            PushManager.dispatchForegroundMessage(json)
            return
        }

        // App is backgrounded. Note: FCM auto-displays messages that contain a
        // "notification" block; we explicitly display *data-only* messages here so
        // they still produce a tappable, deep-link-carrying notification.
        if (remoteMessage.notification == null) {
            displayDataMessage(remoteMessage)
        }
    }

    /** Resolves and invokes the receive-time handler, swallowing any failure. */
    private fun invokeNotificationReceived(remoteMessage: RemoteMessage, dataJson: String) {
        try {
            val handler =
                PushManager.resolveNotificationReceivedHandler(applicationContext) ?: return
            val event = PushReceivedEvent(
                messageId = remoteMessage.messageId,
                dataJson = dataJson,
                sentTimeMillis = remoteMessage.sentTime,
                receivedTimeMillis = System.currentTimeMillis(),
                wasForeground = PushManager.isForeground,
                deepLink = remoteMessage.data["deeplink"]
            )
            handler.onNotificationReceived(applicationContext, event)
        } catch (t: Throwable) {
            PushManager.dispatchError("notification_received", t.message ?: t.toString())
        }
    }

    /** Builds a JSON object from the data map and (optional) notification fields. */
    private fun buildMessageJson(remoteMessage: RemoteMessage): String {
        val obj = JSONObject()
        for ((k, v) in remoteMessage.data) obj.put(k, v)
        remoteMessage.notification?.let { n ->
            n.title?.let { obj.put("title", it) }
            n.body?.let { obj.put("body", it) }
        }
        return obj.toString()
    }

    /** Constructs a [NotificationTemplate] from the data payload and displays it. */
    private fun displayDataMessage(remoteMessage: RemoteMessage) {
        val data = remoteMessage.data
        val title = data["title"] ?: remoteMessage.notification?.title ?: ""
        val body = data["body"] ?: remoteMessage.notification?.body ?: ""
        val channelId = data["channelId"]
            ?: getString(R.string.beamable_default_channel)
        val deepLink = data["deeplink"]

        // Carry every data entry forward so the engine can read it on tap.
        val template = NotificationTemplate(
            id = 0,
            title = title,
            body = body,
            smallIconResName = data["smallIcon"],
            channelId = channelId,
            dataPayload = data,
            deepLinkUrl = deepLink
        ).let {
            // A 0 id would overwrite itself repeatedly; give data pushes a stable
            // per-message id derived from the FCM message id when available.
            if (it.id == 0) it.copy(id = stableId(remoteMessage)) else it
        }

        try {
            NotificationBuilder.show(applicationContext, template)
        } catch (t: Throwable) {
            PushManager.dispatchError("remote_display", t.message ?: t.toString())
        }
    }

    private fun stableId(remoteMessage: RemoteMessage): Int {
        val key = remoteMessage.messageId ?: System.nanoTime().toString()
        return key.hashCode() and Int.MAX_VALUE
    }
}
