package com.beamable.push

import android.app.Activity
import org.json.JSONObject

/**
 * Reads notification payload data out of the activity launch intent.
 *
 * Used by the host engine on resume/launch to detect "app opened from a
 * notification" and recover the deep link / data payload.
 */
object IntentDataReader {

    private const val MARKER_KEY = "beamable_notification"
    private const val PAYLOAD_JSON_KEY = "beamable_payload_json"

    /**
     * If [activity]'s intent was produced by this library (marker == "1"), returns
     * its payload JSON (preferring the prebuilt JSON, otherwise assembled from the
     * intent's string extras) and clears the marker so it is consumed only once.
     * Returns null when the launch was not from a notification.
     */
    fun readLaunchIntent(activity: Activity): String? {
        val intent = activity.intent ?: return null
        val marker = intent.getStringExtra(MARKER_KEY)
        if (marker != "1") return null

        val payloadJson = intent.getStringExtra(PAYLOAD_JSON_KEY)
        val result = if (!payloadJson.isNullOrEmpty()) {
            payloadJson
        } else {
            buildJsonFromExtras(activity)
        }

        // Clear the markers so a later resume does not re-consume the same intent.
        intent.removeExtra(MARKER_KEY)
        intent.removeExtra(PAYLOAD_JSON_KEY)
        return result
    }

    /** Assembles a JSON object from all string extras on the launch intent. */
    private fun buildJsonFromExtras(activity: Activity): String {
        val obj = JSONObject()
        val extras = activity.intent?.extras ?: return obj.toString()
        for (key in extras.keySet()) {
            if (key == MARKER_KEY || key == PAYLOAD_JSON_KEY) continue
            val value = extras.get(key)
            if (value is String) obj.put(key, value)
        }
        return obj.toString()
    }
}
