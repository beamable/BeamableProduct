package com.beamable.push

import android.app.AlarmManager
import android.app.PendingIntent
import android.content.Context
import android.content.Intent

/**
 * Schedules local notifications via [AlarmManager].
 *
 * Uses inexact, doze-friendly alarms ([AlarmManager.setAndAllowWhileIdle]) so the
 * library never needs the SCHEDULE_EXACT_ALARM permission. The actual notification
 * is posted by [NotificationActionReceiver] when the alarm fires.
 */
object LocalNotificationScheduler {

    const val EXTRA_TEMPLATE_JSON = "template_json"

    /**
     * Schedules [template] to fire after [delayMillis].
     *
     * @return the notification id used (a stable random id is generated when
     *   [template].id is 0).
     */
    fun schedule(context: Context, template: NotificationTemplate, delayMillis: Long): Int {
        val id = if (template.id != 0) template.id else generateId()
        // Persist the resolved id into the template that gets delivered, so the
        // receiver posts (and can later cancel) under the same id.
        val resolved = template.copy(id = id)

        val pi = buildReceiverPendingIntent(context, resolved)
        val triggerAtMillis = System.currentTimeMillis() + delayMillis

        val alarmManager = context.getSystemService(Context.ALARM_SERVICE) as AlarmManager
        alarmManager.setAndAllowWhileIdle(AlarmManager.RTC_WAKEUP, triggerAtMillis, pi)
        return id
    }

    /** Cancels a previously scheduled alarm with the given [id]. */
    fun cancel(context: Context, id: Int) {
        val pi = buildCancelPendingIntent(context, id)
        if (pi != null) {
            val alarmManager = context.getSystemService(Context.ALARM_SERVICE) as AlarmManager
            alarmManager.cancel(pi)
            pi.cancel()
        }
        // Also dismiss it if it has already been posted.
        val mgr = context.getSystemService(Context.NOTIFICATION_SERVICE)
                as android.app.NotificationManager
        mgr.cancel(id)
    }

    /**
     * Cancels all posted notifications. Note: pending AlarmManager alarms cannot be
     * enumerated, so call [cancel] per-id to revoke not-yet-fired alarms.
     */
    fun cancelAll(context: Context) {
        val mgr = context.getSystemService(Context.NOTIFICATION_SERVICE)
                as android.app.NotificationManager
        mgr.cancelAll()
    }

    private fun buildReceiverPendingIntent(
        context: Context,
        template: NotificationTemplate
    ): PendingIntent {
        val intent = Intent(context, NotificationActionReceiver::class.java).apply {
            putExtra(EXTRA_TEMPLATE_JSON, template.toJson())
        }
        val flags = PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
        return PendingIntent.getBroadcast(context, template.id, intent, flags)
    }

    private fun buildCancelPendingIntent(context: Context, id: Int): PendingIntent? {
        val intent = Intent(context, NotificationActionReceiver::class.java)
        // NO_CREATE returns null if no matching PendingIntent exists.
        val flags = PendingIntent.FLAG_NO_CREATE or PendingIntent.FLAG_IMMUTABLE
        return PendingIntent.getBroadcast(context, id, intent, flags)
    }

    private fun generateId(): Int = (System.nanoTime() and Int.MAX_VALUE.toLong()).toInt()
}
