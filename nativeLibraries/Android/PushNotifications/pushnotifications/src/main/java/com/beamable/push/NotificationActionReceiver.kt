package com.beamable.push

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import org.json.JSONObject

/**
 * Receives the [AlarmManager][android.app.AlarmManager] broadcast for a scheduled local
 * notification, posts it through the shared [NotificationBuilder], and then fires the
 * receive-time [PushNotificationReceivedHandler] — so local notifications reach the same hook
 * as remote (FCM) ones.
 *
 * AlarmManager wakes the app process to deliver this broadcast, so the handler runs in
 * foreground, background, AND killed states. [PushReceivedEvent.wasForeground] (from
 * [PushManager.isForeground]) distinguishes app-open from app-closed receipt.
 *
 * The handler is invoked on a background thread via [goAsync] (BroadcastReceiver.onReceive runs
 * on the main thread, where blocking work — e.g. a network call — would throw
 * NetworkOnMainThreadException). This mirrors the FCM path, which is already off the main thread.
 */
class NotificationActionReceiver : BroadcastReceiver() {

    override fun onReceive(context: Context, intent: Intent) {
        val json = intent.getStringExtra(LocalNotificationScheduler.EXTRA_TEMPLATE_JSON)
            ?: return
        val appContext = context.applicationContext

        val template = try {
            NotificationTemplate.fromJson(json)
        } catch (t: Throwable) {
            PushManager.dispatchError("local_fire", t.message ?: t.toString())
            return
        }

        // Posting the notification is cheap and fine on the main thread.
        try {
            NotificationBuilder.show(appContext, template)
        } catch (t: Throwable) {
            PushManager.dispatchError("local_fire", t.message ?: t.toString())
        }

        val handler = PushManager.resolveNotificationReceivedHandler(appContext) ?: return

        // Run the handler off the main thread; goAsync() keeps the (possibly killed-app)
        // process alive for the ~10s background-work budget.
        val pending = goAsync()
        Thread {
            try {
                val now = System.currentTimeMillis()
                val event = PushReceivedEvent(
                    messageId = template.id.toString(),
                    dataJson = JSONObject(template.effectivePayload() as Map<*, *>).toString(),
                    sentTimeMillis = now,
                    receivedTimeMillis = now,
                    wasForeground = PushManager.isForeground,
                    deepLink = template.deepLinkUrl
                )
                handler.onNotificationReceived(appContext, event)
            } catch (t: Throwable) {
                PushManager.dispatchError("local_notification_received", t.message ?: t.toString())
            } finally {
                pending.finish()
            }
        }.start()
    }
}
