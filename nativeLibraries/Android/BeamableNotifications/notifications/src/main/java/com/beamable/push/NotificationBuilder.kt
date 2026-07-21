package com.beamable.push

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.content.Context
import android.content.Intent
import android.graphics.Bitmap
import android.graphics.BitmapFactory
import android.os.Build
import androidx.core.app.NotificationCompat
import org.json.JSONObject
import java.net.HttpURLConnection
import java.net.URL

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

    // Intent extra carrying the tapped action button's id (kept in sync with IntentDataReader,
    // which merges it into the payload JSON as "actionId"). Absent for a plain body tap.
    private const val EXTRA_ACTION_ID = "beamable_action_id"

    private const val UNITY_ACTIVITY = "com.unity3d.player.UnityPlayerActivity"

    // Built-in style presets the shared library renders. Custom styles (e.g. an app's own
    // `animated`/`countdown`) are handled by a PushNotificationStyleRenderer in the consuming app.
    private const val STYLE_BIG_PICTURE = "bigPicture"
    private const val STYLE_BIG_TEXT = "bigText"

    // Under the default preset, bodies longer than this auto-promote to BigTextStyle so the full
    // text is available on expand (the "sensible default" behavior).
    private const val DEFAULT_BIG_TEXT_THRESHOLD = 40

    /** Creates [spec]'s channel on API 26+. No-op below that. */
    fun ensureChannel(context: Context, spec: NotificationChannelSpec) {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O) return
        val mgr = context.getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
        val channel = NotificationChannel(spec.id, spec.name, spec.importance).apply {
            description = spec.description
            // Allow the launcher to show a badge (a dot on stock Android; OEM launchers may show
            // the numeric count set via setNumber). Best-effort — see the badge notes in the spec.
            setShowBadge(true)
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

        applyStyle(builder, template)

        // Badge is orthogonal to the chosen style: apply whenever a value is present. A numeric
        // app-icon badge on Android is launcher/OEM-dependent and often surfaces only as a
        // notification dot — this is intentionally best-effort with no new dependency.
        template.badge?.let { builder.setNumber(it) }

        // Action buttons are also orthogonal to style: render whenever the notification names a
        // registered category (mirrors iOS, where aps.category drives buttons independent of layout).
        applyActions(context, builder, template)

        return builder.build()
    }

    /**
     * Adds one action button per action of the template's registered [NotificationCategorySpec]
     * (looked up by [NotificationTemplate.category]). No-op when the notification names no category
     * or the category was never registered. Each button opens the app carrying its `actionId`, which
     * surfaces to the engine via the normal open path (see [buildActionIntent] / IntentDataReader).
     */
    private fun applyActions(
        context: Context,
        builder: NotificationCompat.Builder,
        template: NotificationTemplate
    ) {
        val category = CategoryStore.get(context, template.category) ?: return
        category.actions.forEachIndexed { index, action ->
            builder.addAction(0, action.title, buildActionIntent(context, template, action, index))
        }
    }

    /**
     * Applies the built-in style preset named by [NotificationTemplate.style]. Unknown/null styles
     * and any failure fall back to the plain default — a style is never allowed to fail the post.
     * Runs on a background thread (FCM / AlarmManager), so the synchronous image download for
     * [STYLE_BIG_PICTURE] is safe here.
     */
    private fun applyStyle(
        builder: NotificationCompat.Builder,
        template: NotificationTemplate
    ) {
        when (template.style) {
            STYLE_BIG_PICTURE -> {
                val bmp = downloadBitmap(template.imageUrl)
                if (bmp != null) {
                    builder.setLargeIcon(bmp)
                    builder.setStyle(
                        NotificationCompat.BigPictureStyle()
                            .bigPicture(bmp)
                            // Hide the large icon in the expanded state so the image isn't doubled.
                            .bigLargeIcon(null as Bitmap?)
                    )
                } else {
                    // Blank URL or download failure: fall back to default behavior.
                    applyDefaultStyle(builder, template)
                }
            }

            STYLE_BIG_TEXT ->
                builder.setStyle(NotificationCompat.BigTextStyle().bigText(template.body))

            else -> applyDefaultStyle(builder, template)
        }
    }

    /** Default preset: auto-promote to BigTextStyle when the body is long enough to be clipped. */
    private fun applyDefaultStyle(
        builder: NotificationCompat.Builder,
        template: NotificationTemplate
    ) {
        if (template.body.length > DEFAULT_BIG_TEXT_THRESHOLD) {
            builder.setStyle(NotificationCompat.BigTextStyle().bigText(template.body))
        }
    }

    /**
     * Downloads [url] into a [Bitmap], returning null on a blank URL or any failure (never throws).
     * Uses only the platform HttpURLConnection + BitmapFactory — no Glide/Coil/OkHttp. MUST be
     * called off the main thread; every caller (FCM onMessageReceived, AlarmManager receiver via
     * goAsync) already is.
     */
    private fun downloadBitmap(url: String?): Bitmap? {
        if (url.isNullOrBlank()) return null
        var connection: HttpURLConnection? = null
        return try {
            connection = (URL(url).openConnection() as HttpURLConnection).apply {
                connectTimeout = 10_000
                readTimeout = 10_000
                instanceFollowRedirects = true
                doInput = true
            }
            connection.inputStream.use { BitmapFactory.decodeStream(it) }
        } catch (_: Throwable) {
            null
        } finally {
            connection?.disconnect()
        }
    }

    /**
     * Builds the content [PendingIntent] that opens the host activity and carries the template
     * payload so the engine can resolve the deep link on tap (body tap — no `actionId`).
     */
    fun buildContentIntent(context: Context, template: NotificationTemplate): PendingIntent =
        // Stable request code keyed on the notification id so updating an existing notification
        // reuses (and updates) the same PendingIntent.
        buildOpenIntent(context, template, actionId = null, requestCode = template.id)

    /**
     * Builds the [PendingIntent] for one action button. It opens the app exactly like the content
     * intent, but additionally carries [action]'s id as `EXTRA_ACTION_ID` so the engine learns which
     * button was tapped. Uses `getActivity` (not a broadcast) so it complies with the Android 12+
     * notification-trampoline restriction on starting activities from a receiver.
     */
    private fun buildActionIntent(
        context: Context,
        template: NotificationTemplate,
        action: NotificationActionSpec,
        index: Int
    ): PendingIntent =
        buildOpenIntent(
            context, template, actionId = action.id,
            // Unique per (notification, action) so multiple buttons' PendingIntents don't collide
            // under FLAG_UPDATE_CURRENT (which would otherwise overwrite each other).
            requestCode = "${template.id}:${action.id}".hashCode()
        )

    /**
     * Shared builder for the "open the app" [PendingIntent] used by both the body tap and each
     * action button. When [actionId] is non-null it is attached so IntentDataReader can surface it.
     */
    private fun buildOpenIntent(
        context: Context,
        template: NotificationTemplate,
        actionId: String?,
        requestCode: Int
    ): PendingIntent {
        val intent = resolveLaunchIntent(context)
        intent.flags = Intent.FLAG_ACTIVITY_SINGLE_TOP or Intent.FLAG_ACTIVITY_NEW_TASK

        val payload = template.effectivePayload()
        for ((key, value) in payload) {
            intent.putExtra(key, value)
        }

        // Markers the engine reads back via IntentDataReader.
        intent.putExtra(MARKER_KEY, "1")
        intent.putExtra(PAYLOAD_JSON_KEY, payloadToJson(payload))
        if (actionId != null) intent.putExtra(EXTRA_ACTION_ID, actionId)

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
