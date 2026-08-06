package com.beamable.push

import android.app.Activity
import android.content.Intent
import org.json.JSONObject

/**
 * Reads notification payload data out of a launch / tap intent.
 *
 * Used by the host engine on resume/launch to detect "app opened from a
 * notification" and recover the deep link / data payload.
 */
object IntentDataReader {

    private const val MARKER_KEY = "beamable_notification"
    private const val PAYLOAD_JSON_KEY = "beamable_payload_json"

    // Kept in sync with NotificationBuilder.EXTRA_ACTION_ID: present when the app was opened by
    // tapping an action button (vs the notification body). Surfaced as "actionId" in the payload.
    private const val EXTRA_ACTION_ID = "beamable_action_id"

    /**
     * If [activity]'s current intent was produced by this library, returns its payload JSON
     * (and clears the marker so it is consumed only once). Returns null when the launch was
     * not from a notification. Cold-start path (the activity already holds the launch intent).
     */
    fun readLaunchIntent(activity: Activity): String? = readIntent(activity.intent)

    /**
     * If [intent] was produced by this library (marker == "1"), returns its payload JSON
     * (preferring the prebuilt JSON, otherwise assembled from the intent's string extras) and
     * clears the marker so it is consumed only once. Returns null when the intent is not a
     * notification tap. Warm-start path (e.g. `onNewIntent` hands a fresh intent that is not
     * yet the activity's current intent).
     */
    fun readIntent(intent: Intent?): String? {
        if (intent == null) return null
        val marker = intent.getStringExtra(MARKER_KEY)
        if (marker != "1") return null

        val payloadJson = intent.getStringExtra(PAYLOAD_JSON_KEY)
        var result = if (!payloadJson.isNullOrEmpty()) {
            payloadJson
        } else {
            buildJsonFromExtras(intent)
        }

        // If an action button (not the body) was tapped, merge its id in as "actionId" so the
        // engine/app learns which button fired. Body taps leave the extra absent → no actionId.
        val actionId = intent.getStringExtra(EXTRA_ACTION_ID)
        if (!actionId.isNullOrEmpty()) {
            result = withActionId(result, actionId)
        }

        // Clear the markers so a later resume does not re-consume the same intent.
        intent.removeExtra(MARKER_KEY)
        intent.removeExtra(PAYLOAD_JSON_KEY)
        intent.removeExtra(EXTRA_ACTION_ID)
        return result
    }

    /** Returns [json] with an "actionId" field set, tolerating a malformed/empty input. */
    private fun withActionId(json: String, actionId: String): String = try {
        JSONObject(json).put("actionId", actionId).toString()
    } catch (_: Throwable) {
        JSONObject().put("actionId", actionId).toString()
    }

    /** Assembles a JSON object from all string extras on [intent]. */
    private fun buildJsonFromExtras(intent: Intent): String {
        val obj = JSONObject()
        val extras = intent.extras ?: return obj.toString()
        for (key in extras.keySet()) {
            if (key == MARKER_KEY || key == PAYLOAD_JSON_KEY) continue
            val value = extras.get(key)
            if (value is String) obj.put(key, value)
        }
        return obj.toString()
    }
}
