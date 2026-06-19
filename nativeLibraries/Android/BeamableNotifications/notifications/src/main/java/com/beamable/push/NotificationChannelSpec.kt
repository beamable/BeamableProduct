package com.beamable.push

import android.app.NotificationManager

/**
 * Describes a notification channel (Android 8.0+ / API 26+).
 *
 * @param id stable channel id used when posting notifications.
 * @param name user-visible channel name (shown in system settings).
 * @param description user-visible channel description.
 * @param importance one of the [NotificationManager] IMPORTANCE_* constants.
 */
data class NotificationChannelSpec(
    val id: String,
    val name: String,
    val description: String,
    val importance: Int = NotificationManager.IMPORTANCE_HIGH
)
