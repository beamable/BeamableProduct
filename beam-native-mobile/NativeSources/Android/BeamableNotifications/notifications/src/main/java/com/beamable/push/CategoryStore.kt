package com.beamable.push

import android.content.Context
import org.json.JSONArray
import org.json.JSONObject

/**
 * A single interactive action button inside a [NotificationCategorySpec]. Mirrors the shared
 * `ActionSpec` TS/iOS contract (id/title/foreground/destructive); Android renders it as a
 * [androidx.core.app.NotificationCompat.Action] and, on tap, dispatches [id] back to the engine as
 * the notification's `actionId` (the app decides the behavior).
 *
 * @param foreground whether tapping opens the app (the only mode supported today — the tap always
 *   surfaces `actionId` via the normal open path). Kept for parity with the iOS contract.
 */
data class NotificationActionSpec(
    val id: String,
    val title: String,
    val foreground: Boolean = true,
    val destructive: Boolean = false,
) {
    fun toJson(): JSONObject = JSONObject().apply {
        put("id", id)
        put("title", title)
        put("foreground", foreground)
        put("destructive", destructive)
    }

    companion object {
        fun fromJson(o: JSONObject) = NotificationActionSpec(
            id = o.optString("id"),
            title = o.optString("title"),
            foreground = o.optBoolean("foreground", true),
            destructive = o.optBoolean("destructive", false),
        )
    }
}

/**
 * A named set of action buttons ("category"), mirroring the shared `CategorySpec` contract and iOS
 * `UNNotificationCategory`. Registered by id via [CategoryStore.register]; a push references it by
 * the `category` key so the library can render its buttons.
 */
data class NotificationCategorySpec(
    val id: String,
    val actions: List<NotificationActionSpec>,
) {
    fun toJson(): String = JSONObject().apply {
        put("id", id)
        put("actions", JSONArray().also { arr -> actions.forEach { arr.put(it.toJson()) } })
    }.toString()

    companion object {
        fun fromJson(json: String): NotificationCategorySpec {
            val o = JSONObject(json)
            val arr = o.optJSONArray("actions") ?: JSONArray()
            val actions = (0 until arr.length()).mapNotNull { i ->
                arr.optJSONObject(i)?.let { NotificationActionSpec.fromJson(it) }
            }
            return NotificationCategorySpec(o.optString("id"), actions)
        }
    }
}

/**
 * Persistent registry of notification action categories.
 *
 * Categories are PERSISTED to SharedPreferences (not just held in memory) because a data-only push
 * can arrive when the app is killed: [PushFirebaseService] then runs in a fresh process with no JS
 * state, so an in-memory registry would be empty and no buttons would render. Persisting at
 * registration time lets the killed-app path resolve a category and draw its buttons.
 */
object CategoryStore {
    private const val PREFS = "beamable_notification_categories"
    private const val KEY_PREFIX = "cat_"

    /** Registers (or overwrites) [spec] under its id. */
    fun register(context: Context, spec: NotificationCategorySpec) {
        context.applicationContext
            .getSharedPreferences(PREFS, Context.MODE_PRIVATE)
            .edit()
            .putString(KEY_PREFIX + spec.id, spec.toJson())
            .apply()
    }

    /** Resolves a previously-registered category, or null when [id] is blank/unknown/corrupt. */
    fun get(context: Context, id: String?): NotificationCategorySpec? {
        if (id.isNullOrEmpty()) return null
        val json = context.applicationContext
            .getSharedPreferences(PREFS, Context.MODE_PRIVATE)
            .getString(KEY_PREFIX + id, null) ?: return null
        return try {
            NotificationCategorySpec.fromJson(json)
        } catch (_: Throwable) {
            null
        }
    }
}
