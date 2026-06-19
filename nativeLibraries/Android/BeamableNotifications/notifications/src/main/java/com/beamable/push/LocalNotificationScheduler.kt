package com.beamable.push

import android.app.AlarmManager
import android.app.PendingIntent
import android.content.Context
import android.content.Intent
import android.os.Build

/**
 * Schedules local notifications via [AlarmManager].
 *
 * Defaults to inexact, doze-friendly alarms ([AlarmManager.setAndAllowWhileIdle]) which need **no**
 * permission. An opt-in `exact` mode uses [AlarmManager.setExactAndAllowWhileIdle], which on API 31+
 * requires the `SCHEDULE_EXACT_ALARM` permission (the consuming app must declare it and, on API 33+,
 * the user must grant it). When exact is requested but unavailable, scheduling **falls back to
 * inexact** and dispatches a `schedule_exact_denied` warning. The notification itself is posted by
 * [NotificationActionReceiver] when the alarm fires.
 */
object LocalNotificationScheduler {

    const val EXTRA_TEMPLATE_JSON = "template_json"

    /**
     * Schedules [template] to fire after [delayMillis] (relative to now).
     *
     * @param exact use an exact alarm (see class docs); defaults to inexact.
     * @return the notification id used (a stable random id is generated when [template].id is 0).
     */
    fun schedule(
        context: Context,
        template: NotificationTemplate,
        delayMillis: Long,
        exact: Boolean = false
    ): Int = scheduleAt(context, template, System.currentTimeMillis() + delayMillis, exact)

    /**
     * Schedules [template] to fire at the absolute [triggerAtMillis] (epoch millis, the same clock
     * as [System.currentTimeMillis]). Callers that need a local/UTC wall-clock convert to epoch
     * before calling (see `PushManager.scheduleLocalAt`).
     */
    fun scheduleAt(
        context: Context,
        template: NotificationTemplate,
        triggerAtMillis: Long,
        exact: Boolean = false
    ): Int {
        val id = if (template.id != 0) template.id else generateId()
        // Persist the resolved id into the template that gets delivered, so the
        // receiver posts (and can later cancel) under the same id.
        val resolved = template.copy(id = id)

        val pi = buildReceiverPendingIntent(context, resolved)
        val alarmManager = context.getSystemService(Context.ALARM_SERVICE) as AlarmManager

        if (exact && canScheduleExact(alarmManager)) {
            alarmManager.setExactAndAllowWhileIdle(AlarmManager.RTC_WAKEUP, triggerAtMillis, pi)
        } else {
            if (exact) {
                PushManager.dispatchError(
                    "schedule_exact_denied",
                    "SCHEDULE_EXACT_ALARM not granted; scheduled inexact instead"
                )
            }
            alarmManager.setAndAllowWhileIdle(AlarmManager.RTC_WAKEUP, triggerAtMillis, pi)
        }
        return id
    }

    /** True if exact alarms can currently be scheduled (always pre-API-31; permission-gated after). */
    fun canScheduleExact(context: Context): Boolean =
        canScheduleExact(context.getSystemService(Context.ALARM_SERVICE) as AlarmManager)

    private fun canScheduleExact(alarmManager: AlarmManager): Boolean =
        Build.VERSION.SDK_INT < Build.VERSION_CODES.S || alarmManager.canScheduleExactAlarms()

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
