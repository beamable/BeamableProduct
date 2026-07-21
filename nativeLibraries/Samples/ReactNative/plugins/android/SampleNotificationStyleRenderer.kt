package com.beamable.rnsample

import android.app.NotificationChannel
import android.app.NotificationManager
import android.content.Context
import android.graphics.Color
import android.os.Build
import android.os.SystemClock
import android.view.View
import android.widget.RemoteViews
import androidx.core.app.NotificationCompat
import androidx.core.app.NotificationManagerCompat
import com.beamable.push.LocalNotificationScheduler
import com.beamable.push.NotificationTemplate
import com.beamable.push.PushNotificationStyleRenderer
import com.beamable.push.PushReceivedEvent
import org.json.JSONObject

/**
 * Customer-side native renderer for the sample app's OWN custom notification styles.
 *
 * This demonstrates how a consuming app extends the shared `@beamable/notifications`
 * library with styles the shared lib does NOT ship. The shared lib keeps only a
 * bare-minimum set of built-in styles (default / bigPicture / bigText); the `animated`
 * and `countdown` styles used to live in the shared lib and have been REMOVED from it —
 * they are reproduced here to show the extension pattern.
 *
 * Discovery mirrors the shared receive-time handler exactly: the shared library resolves
 * this class by reflection from the AndroidManifest meta-data
 * `com.beamable.push.notification_style_renderer`, so it MUST keep a public no-arg
 * constructor. It runs on FCM's background thread for background/killed data-only pushes,
 * with NO React Native runtime available — which is why it must be native Kotlin.
 *
 * The Expo config plugin (plugins/withSampleNotificationStyles.js) copies this file into
 * the generated android/app/src/main/java/<package>/ on every prebuild, rewriting the
 * `package` declaration to `config.android.package`. Because the package is rewritten, this
 * file must NOT import a compile-time `R` class — it resolves all of its own resources by
 * name at runtime via [android.content.res.Resources.getIdentifier].
 */
class SampleNotificationStyleRenderer : PushNotificationStyleRenderer {

    override fun render(context: Context, event: PushReceivedEvent): Boolean {
        return try {
            val data = JSONObject(event.dataJson)
            when (data.optString("style")) {
                STYLE_ANIMATED -> renderAnimated(context, event, data)
                STYLE_COUNTDOWN -> renderCountdown(context, event, data)
                // Not a style we own — let the shared library (or another renderer) handle it.
                else -> false
            }
        } catch (_: Throwable) {
            // Any failure: fall back to the library's own default post.
            false
        }
    }

    // -----------------------------------------------------------------------
    // animated
    // -----------------------------------------------------------------------

    /**
     * "animated" preset: a big custom-layout notification whose colored panels auto-cycle via a
     * [android.widget.ViewFlipper]. Colors come from the `colors` data key (comma-separated hex)
     * or fall back to the default palette; the flip interval comes from `flipIntervalMs`
     * (default 900ms). Ported from the shared lib's NotificationBuilder.applyAnimatedStyle.
     */
    private fun renderAnimated(context: Context, event: PushReceivedEvent, data: JSONObject): Boolean {
        val pkg = context.packageName
        val res = context.resources

        val title = data.optString("title")
        val body = data.optString("body")

        val layoutId = res.getIdentifier("beam_notif_animated", "layout", pkg)
        if (layoutId == 0) return false
        val rv = RemoteViews(pkg, layoutId)

        val colors = parseColors(context, data.optStringOrNull("colors"))

        // Tint the panels that have a color; hide any leftover panels.
        for (index in 0 until 4) {
            val panelId = res.getIdentifier("panel$index", "id", pkg)
            if (panelId == 0) continue
            val color = colors.getOrNull(index)
            if (color != null) {
                rv.setInt(panelId, "setBackgroundColor", color)
            } else {
                rv.setViewVisibility(panelId, View.GONE)
            }
        }

        val titleId = res.getIdentifier("title", "id", pkg)
        val bodyId = res.getIdentifier("body", "id", pkg)
        if (titleId != 0) rv.setTextViewText(titleId, title)
        if (bodyId != 0) rv.setTextViewText(bodyId, body)

        val flipperId = res.getIdentifier("flipper", "id", pkg)
        val flipInterval = if (data.has("flipIntervalMs") && !data.isNull("flipIntervalMs"))
            data.optInt("flipIntervalMs") else DEFAULT_FLIP_INTERVAL_MS
        if (flipperId != 0) rv.setInt(flipperId, "setFlipInterval", flipInterval)

        ensureChannel(context)
        val builder = NotificationCompat.Builder(context, CHANNEL_ID)
            .setContentTitle(title)
            .setContentText(body)
            .setSmallIcon(context.applicationInfo.icon)
            .setAutoCancel(true)
            .setPriority(NotificationCompat.PRIORITY_HIGH)
            .setStyle(NotificationCompat.DecoratedCustomViewStyle())
            .setCustomContentView(rv)
            .setCustomBigContentView(rv)

        NotificationManagerCompat.from(context).notify(notificationId(event), builder.build())
        return true
    }

    /**
     * Parses [csv] (comma-separated hex colors) into a list of ARGB ints, skipping any token that
     * fails [Color.parseColor]. Falls back to the default palette (beam_notif_cycle_0..3) when [csv]
     * is blank or yields no valid color. Ported from NotificationBuilder.parseColors.
     */
    private fun parseColors(context: Context, csv: String?): List<Int> {
        val parsed = csv
            ?.split(',')
            ?.mapNotNull { token ->
                val t = token.trim()
                if (t.isEmpty()) null else try {
                    Color.parseColor(t)
                } catch (_: IllegalArgumentException) {
                    null
                }
            }
            ?: emptyList()
        if (parsed.isNotEmpty()) return parsed

        val res = context.resources
        val pkg = context.packageName
        return (0 until 4).mapNotNull { index ->
            val colorId = res.getIdentifier("beam_notif_cycle_$index", "color", pkg)
            if (colorId == 0) null else context.getColor(colorId)
        }
    }

    // -----------------------------------------------------------------------
    // countdown
    // -----------------------------------------------------------------------

    /**
     * "countdown" preset: an expiring-offer notification with a native, self-ticking chronometer
     * that counts DOWN to `expiresAtMs`. Ported from NotificationBuilder.applyCountdownStyle.
     * The "offer expired" swap is scheduled by reusing the shared [LocalNotificationScheduler],
     * which posts the replacement notification (same id) when the alarm fires.
     */
    private fun renderCountdown(context: Context, event: PushReceivedEvent, data: JSONObject): Boolean {
        val title = data.optString("title")
        val body = data.optString("body")
        val id = notificationId(event)

        val expiresAtMs = if (data.has("expiresAtMs") && !data.isNull("expiresAtMs")) {
            data.optLong("expiresAtMs")
        } else {
            System.currentTimeMillis() + data.optLong("expiresInSeconds") * 1000L
        }

        ensureChannel(context)

        // Custom layout with a big Chronometer counting DOWN to expiry. Chronometer's base uses the
        // SystemClock.elapsedRealtime() timebase (NOT wall-clock), so convert the wall-clock expiry.
        val res = context.resources
        val pkg = context.packageName
        val builder = NotificationCompat.Builder(context, CHANNEL_ID)
            .setContentTitle(title)
            .setContentText(body)
            .setSmallIcon(context.applicationInfo.icon)
            .setAutoCancel(true)
            .setPriority(NotificationCompat.PRIORITY_HIGH)
            .setOnlyAlertOnce(true)
            // Ongoing so the ticking offer isn't swiped away before it expires; the expired swap
            // (same id, autoCancel) replaces it.
            .setOngoing(true)

        val layoutId = res.getIdentifier("beam_notif_countdown", "layout", pkg)
        if (layoutId != 0) {
            val rv = RemoteViews(pkg, layoutId)
            val titleId = res.getIdentifier("cd_title", "id", pkg)
            val bodyId = res.getIdentifier("cd_body", "id", pkg)
            val chronoId = res.getIdentifier("cd_chrono", "id", pkg)
            if (titleId != 0) rv.setTextViewText(titleId, title)
            if (bodyId != 0) rv.setTextViewText(bodyId, body)
            if (chronoId != 0) {
                val base = SystemClock.elapsedRealtime() + (expiresAtMs - System.currentTimeMillis())
                rv.setChronometer(chronoId, base, null, true)
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
                    rv.setChronometerCountDown(chronoId, true)
                }
            }
            builder.setStyle(NotificationCompat.DecoratedCustomViewStyle())
            builder.setCustomContentView(rv)
            builder.setCustomBigContentView(rv)
        } else {
            // Fallback: header chronometer if the custom layout is missing.
            builder.setWhen(expiresAtMs).setShowWhen(true).setUsesChronometer(true)
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) builder.setChronometerCountDown(true)
        }

        NotificationManagerCompat.from(context).notify(id, builder.build())

        // Schedule the "expired" replacement (SAME id → replaces the ticking notification in place).
        // Use an EXACT alarm so it fires right at 0 (no lingering negative countdown); the app
        // declares USE_EXACT_ALARM/SCHEDULE_EXACT_ALARM (see withSampleNotificationStyles.js).
        val expired = NotificationTemplate(
            id = id,
            title = data.optStringOrNull("expiredTitle") ?: title,
            body = data.optStringOrNull("expiredBody") ?: "Offer expired",
            channelId = CHANNEL_ID,
            style = "default"
        )
        LocalNotificationScheduler.scheduleAt(context, expired, expiresAtMs, exact = true)
        return true
    }

    // -----------------------------------------------------------------------
    // helpers
    // -----------------------------------------------------------------------

    /** Ensures the shared default channel exists (API 26+). No-op below that. */
    private fun ensureChannel(context: Context) {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O) return
        val mgr = context.getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
        val channel = NotificationChannel(CHANNEL_ID, "Notifications", NotificationManager.IMPORTANCE_HIGH)
        mgr.createNotificationChannel(channel)
    }

    /** Derives a stable notification id from the message id, masked to a non-negative int. */
    private fun notificationId(event: PushReceivedEvent): Int =
        (event.messageId?.hashCode() ?: System.nanoTime().hashCode()) and Int.MAX_VALUE

    /** Returns the string value, or null when the key is missing/JSON null/empty. */
    private fun JSONObject.optStringOrNull(key: String): String? {
        if (!has(key) || isNull(key)) return null
        val value = optString(key, "")
        return value.ifEmpty { null }
    }

    companion object {
        private const val STYLE_ANIMATED = "animated"
        private const val STYLE_COUNTDOWN = "countdown"

        // Matches the shared library's default channel id.
        private const val CHANNEL_ID = "deeplink_channel"

        private const val DEFAULT_FLIP_INTERVAL_MS = 900
    }
}
