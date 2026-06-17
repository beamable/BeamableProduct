package com.beamable.push.sample

import android.content.Context
import android.util.Log
import com.beamable.push.PushNotificationReceivedHandler
import com.beamable.push.PushReceivedEvent
import org.json.JSONObject
import java.net.HttpURLConnection
import java.net.URL

/**
 * Reference [PushNotificationReceivedHandler] showing the receive-time path.
 *
 * Registered (for this demo) via meta-data in the app manifest:
 *   <meta-data android:name="com.beamable.push.notification_received_handler"
 *              android:value="com.beamable.push.sample.DemoPushNotificationReceivedHandler" />
 *
 * It runs on FCM's background thread in EVERY app state (incl. killed) for data-only
 * messages. Replace [ENDPOINT] with your real endpoint (or swap the body for a Beamable
 * call). While [ENDPOINT] is left as the placeholder, it only logs — no network call.
 */
class DemoPushNotificationReceivedHandler : PushNotificationReceivedHandler {

    override fun onNotificationReceived(context: Context, event: PushReceivedEvent) {
        Log.i(
            TAG,
            "notification received: id=${event.messageId} foreground=${event.wasForeground} " +
                "deepLink=${event.deepLink} sent=${event.sentTimeMillis} " +
                "received=${event.receivedTimeMillis} data=${event.dataJson}"
        )

        if (ENDPOINT == PLACEHOLDER_ENDPOINT) {
            Log.i(TAG, "endpoint not configured; skipping network post")
            return
        }

        // Already on a background thread (FCM executor) — a short blocking POST is fine.
        // For guaranteed delivery under Doze, enqueue WorkManager here instead.
        try {
            postEvent(event)
        } catch (t: Throwable) {
            Log.w(TAG, "post failed: ${t.message}")
        }
    }

    private fun postEvent(event: PushReceivedEvent) {
        val body = JSONObject().apply {
            put("event", "notification_received")
            put("messageId", event.messageId ?: JSONObject.NULL)
            put("wasForeground", event.wasForeground)
            put("deepLink", event.deepLink ?: JSONObject.NULL)
            put("sentTimeMillis", event.sentTimeMillis)
            put("receivedTimeMillis", event.receivedTimeMillis)
            put("data", JSONObject(event.dataJson))
        }.toString()

        val conn = (URL(ENDPOINT).openConnection() as HttpURLConnection).apply {
            requestMethod = "POST"
            connectTimeout = 5000
            readTimeout = 5000
            doOutput = true
            setRequestProperty("Content-Type", "application/json")
        }
        try {
            conn.outputStream.use { it.write(body.toByteArray(Charsets.UTF_8)) }
            Log.i(TAG, "post -> HTTP ${conn.responseCode}")
        } finally {
            conn.disconnect()
        }
    }

    companion object {
        private const val TAG = "BeamablePushReceived"
        private const val PLACEHOLDER_ENDPOINT = "https://example.invalid/notifications"
        // TODO: replace with your real endpoint.
        private const val ENDPOINT = PLACEHOLDER_ENDPOINT
    }
}
