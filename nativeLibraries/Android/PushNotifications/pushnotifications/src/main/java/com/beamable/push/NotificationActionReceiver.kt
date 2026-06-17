package com.beamable.push

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent

/**
 * Receives the [AlarmManager][android.app.AlarmManager] broadcast for a scheduled
 * local notification and posts it through the shared [NotificationBuilder].
 */
class NotificationActionReceiver : BroadcastReceiver() {

    override fun onReceive(context: Context, intent: Intent) {
        val json = intent.getStringExtra(LocalNotificationScheduler.EXTRA_TEMPLATE_JSON)
            ?: return
        try {
            val template = NotificationTemplate.fromJson(json)
            NotificationBuilder.show(context.applicationContext, template)
        } catch (t: Throwable) {
            PushManager.dispatchError("local_fire", t.message ?: t.toString())
        }
    }
}
