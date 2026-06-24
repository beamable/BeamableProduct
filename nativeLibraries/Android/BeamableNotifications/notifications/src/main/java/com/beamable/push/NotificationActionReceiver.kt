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

        // Surface a C# OnNotificationReceived event (parity with iOS and the FCM foreground path),
        // independent of whether a manifest receive-handler is registered.
        try {
            val flat = JSONObject(template.effectivePayload() as Map<*, *>)
            flat.put("id", template.id.toString())
            flat.put("title", template.title)
            flat.put("body", template.body)
            PushManager.dispatchForegroundMessage(flat.toString())
        } catch (t: Throwable) {
            // best-effort; not fatal to delivery
        }

        val handlers = PushManager.resolveHandlers(appContext)
        val payload = template.effectivePayload()
        val intentData = NotificationIntentData.fromDataMap(payload)

        // Native funnel "Received" — local notifications are part of the same funnel as remote.
        try {
            BeamableAnalytics.trackFunnel(
                appContext, intentData, BeamableAnalytics.FunnelType.Received
            )
        } catch (_: Throwable) { /* best-effort */ }

        if (handlers.isEmpty()) return

        // Run the handlers off the main thread; goAsync() keeps the (possibly killed-app)
        // process alive for the ~10s background-work budget. Each handler's failure is isolated.
        val pending = goAsync()
        Thread {
            try {
                val now = System.currentTimeMillis()
                val event = PushReceivedEvent(
                    messageId = template.id.toString(),
                    dataJson = JSONObject(payload as Map<*, *>).toString(),
                    sentTimeMillis = now,
                    receivedTimeMillis = now,
                    wasForeground = PushManager.isForeground,
                    deepLink = template.deepLinkUrl,
                    intentData = intentData
                )
                for (handler in handlers) {
                    try {
                        handler.onNotificationReceived(appContext, event)
                    } catch (t: Throwable) {
                        PushManager.dispatchError(
                            "local_notification_received", t.message ?: t.toString()
                        )
                    }
                }
            } finally {
                pending.finish()
            }
        }.start()
    }
}
