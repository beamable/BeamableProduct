package com.beamable.push

import org.json.JSONArray

/**
 * Payload-authored action buttons — the Android counterpart of the iOS NSE's `buttons` handling.
 *
 * The console's `actions` style sends the buttons it authored in the push itself, as `buttons`: a JSON
 * array of `{id, title, role}` (a JSON *string* on the wire, since the FCM data map is string→string).
 * Rendering them here is what lets a campaign author's own labels ("Claim" / "No thanks") reach the
 * device, instead of whatever button set the app happened to register under a category id.
 *
 * Android needs none of iOS's category machinery for this: [NotificationBuilder] constructs the
 * notification in-process at delivery time, so a button title is just a string passed to
 * `addAction`. There is no pre-registration and no race.
 *
 * Registered categories still win (see [NotificationBuilder.applyActions]) — an app that registered
 * its own set keeps its behavior unchanged.
 */
internal object ActionButtons {

    /** The `buttons` key as written by `PushRailService.WriteIntentData`. */
    const val KEY_BUTTONS = "buttons"

    /**
     * Built-in category id backing the `actions` style, mirroring iOS's `beam_actions`. An app may
     * register its own category under this id to override the built-in pair.
     */
    const val BUILT_IN_ACTIONS_CATEGORY = "beam_actions"

    /**
     * Buttons kept from a payload. The console authors exactly two (its Live Activity surface renders
     * only two), and both platforms must show the same pair, so the smaller number wins here even
     * though a notification could carry more.
     */
    const val MAX_BUTTONS = 2

    /**
     * Last-resort buttons for `style: "actions"` when the payload carries none and no category is
     * registered. Same labels and ids as the iOS built-in pair, so the two platforms read alike.
     */
    fun builtInActions(): List<NotificationActionSpec> = listOf(
        NotificationActionSpec(id = "open", title = "Open", foreground = true, destructive = false),
        NotificationActionSpec(id = "dismiss", title = "Dismiss", foreground = false, destructive = true),
    )

    /**
     * Parse the `buttons` wire value into action specs, sanitized and capped.
     *
     * Tolerant by design — a malformed value must cost the buttons, never the notification: anything
     * unparseable yields an empty list and the caller falls back. Entries with a blank `id` (nothing to
     * dispatch on tap) or blank `title` (an invisible button) are dropped, duplicate ids collapse
     * first-wins, and `role: "destructive"` maps onto [NotificationActionSpec.destructive].
     */
    fun parse(raw: String?): List<NotificationActionSpec> {
        if (raw.isNullOrBlank()) return emptyList()
        val array = try {
            JSONArray(raw.trim())
        } catch (_: Exception) {
            return emptyList()
        }

        val result = ArrayList<NotificationActionSpec>(MAX_BUTTONS)
        val seen = HashSet<String>()
        for (i in 0 until array.length()) {
            val obj = array.optJSONObject(i) ?: continue
            val id = obj.optString("id").trim()
            val title = obj.optString("title").trim()
            if (id.isEmpty() || title.isEmpty()) continue
            if (!seen.add(id)) continue
            val destructive = obj.optString("role").trim().equals("destructive", ignoreCase = true)
            result.add(
                NotificationActionSpec(
                    id = id,
                    title = title,
                    // Mirrors the iOS mapping: a destructive button is not a foreground open, every
                    // other button is (its tap has to route the deep link, which needs the app up).
                    foreground = !destructive,
                    destructive = destructive,
                )
            )
            if (result.size == MAX_BUTTONS) break
        }
        return result
    }
}
