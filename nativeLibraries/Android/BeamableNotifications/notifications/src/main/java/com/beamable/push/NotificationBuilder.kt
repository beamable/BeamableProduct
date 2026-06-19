package com.beamable.push

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.content.Context
import android.content.Intent
import android.os.Build
import androidx.core.app.NotificationCompat
import org.json.JSONObject

/**
 * The single source of truth for turning a [NotificationTemplate] into a
 * displayable [Notification] and its tap (content) [PendingIntent].
 *
 * Both the local-scheduler path and the remote (FCM) path funnel through here so
 * notification appearance and deep-link wiring stay identical.
 */
object NotificationBuilder {

    private const val MARKER_KEY = "beamable_notification"
    private const val PAYLOAD_JSON_KEY = "beamable_payload_json"
    private const val UNITY_ACTIVITY = "com.unity3d.player.UnityPlayerActivity"

    /** Creates [spec]'s channel on API 26+. No-op below that. */
    fun ensureChannel(context: Context, spec: NotificationChannelSpec) {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O) return
        val mgr = context.getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
        val channel = NotificationChannel(spec.id, spec.name, spec.importance).apply {
            description = spec.description
        }
        mgr.createNotificationChannel(channel)
    }

    /**
     * Convenience overload that creates a channel with a sensible default spec.
     * Used by the remote/default paths so notifications still display even when
     * the host app never explicitly registered a channel.
     */
    fun ensureChannel(context: Context, channelId: String) {
        ensureChannel(
            context,
            NotificationChannelSpec(
                id = channelId,
                name = "Notifications",
                description = "",
                importance = NotificationManager.IMPORTANCE_HIGH
            )
        )
    }

    /** Builds a [Notification] for [template], including its tap intent. */
    fun build(context: Context, template: NotificationTemplate): Notification {
        var smallIcon = template.resolveSmallIconId(context)
        if (smallIcon == 0) {
            // Fall back to the application icon so we never post with icon == 0,
            // which throws on some OEM ROMs.
            smallIcon = context.applicationInfo.icon
        }

        val builder = NotificationCompat.Builder(context, template.channelId)
            .setContentTitle(template.title)
            .setContentText(template.body)
            .setSmallIcon(smallIcon)
            .setAutoCancel(true)
            .setPriority(NotificationCompat.PRIORITY_HIGH)
            .setContentIntent(buildContentIntent(context, template))

        return builder.build()
    }

    /**
     * Builds the content [PendingIntent] that opens the host (Unity) activity and
     * carries the template payload so the engine can resolve the deep link on tap.
     */
    fun buildContentIntent(context: Context, template: NotificationTemplate): PendingIntent {
        val intent = resolveLaunchIntent(context)
        intent.flags = Intent.FLAG_ACTIVITY_SINGLE_TOP or Intent.FLAG_ACTIVITY_NEW_TASK

        val payload = template.effectivePayload()
        for ((key, value) in payload) {
            intent.putExtra(key, value)
        }

        // Markers the engine reads back via IntentDataReader.
        intent.putExtra(MARKER_KEY, "1")
        intent.putExtra(PAYLOAD_JSON_KEY, payloadToJson(payload))

        // Stable request code keyed on the notification id so updating an
        // existing notification reuses (and updates) the same PendingIntent.
        val requestCode = template.id

        // FLAG_IMMUTABLE is always safe here (minSdk 24) and required on API 31+.
        val flags = PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
        return PendingIntent.getActivity(context, requestCode, intent, flags)
    }

    /** ensureChannel + post the notification under [template].id. */
    fun show(context: Context, template: NotificationTemplate) {
        ensureChannel(context, template.channelId)
        val mgr = context.getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
        mgr.notify(template.id, build(context, template))
    }

    /**
     * Resolves the activity to open on tap. We target Unity's player activity by
     * name via Class.forName so this module has no hard dependency on Unity. If
     * that class is absent (Unreal / RN / tests), we fall back to the package's
     * default launch intent.
     */
    private fun resolveLaunchIntent(context: Context): Intent {
        try {
            val activityClass = Class.forName(UNITY_ACTIVITY)
            return Intent(context, activityClass)
        } catch (_: ClassNotFoundException) {
            // Not running under Unity.
        } catch (_: Throwable) {
            // Defensive: any reflection failure falls through to the launcher.
        }
        val launch = context.packageManager.getLaunchIntentForPackage(context.packageName)
        return launch ?: Intent()
    }

    private fun payloadToJson(payload: Map<String, String>): String {
        val obj = JSONObject()
        for ((k, v) in payload) obj.put(k, v)
        return obj.toString()
    }
}
