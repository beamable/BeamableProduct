package com.beamable.push

import android.content.Context
import org.json.JSONObject

/**
 * Engine-agnostic description of a single notification.
 *
 * Icon resources are carried as *names* (e.g. "icon_0") rather than ids so the
 * spec can travel as JSON across the engine boundary; they are resolved to ids
 * at build time via [resolveSmallIconId] / [resolveLargeIconId].
 *
 * @param id notification id; 0 means "assign one at schedule/show time".
 * @param title notification title.
 * @param body notification body text.
 * @param smallIconResName drawable/mipmap name for the small (status bar) icon.
 * @param largeIconResName drawable/mipmap name for the large icon.
 * @param channelId channel to post on.
 * @param dataPayload arbitrary key/value data carried into the launch intent.
 * @param deepLinkUrl convenience deep link; merged into the payload under "deeplink".
 */
data class NotificationTemplate(
    val id: Int = 0,
    val title: String,
    val body: String,
    val smallIconResName: String? = null,
    val largeIconResName: String? = null,
    val channelId: String,
    val dataPayload: Map<String, String> = emptyMap(),
    val deepLinkUrl: String? = null
) {

    /**
     * Returns [dataPayload] with [deepLinkUrl] merged in under the key "deeplink"
     * when present (an explicit "deeplink" already in the payload is preserved).
     */
    fun effectivePayload(): Map<String, String> {
        if (deepLinkUrl.isNullOrEmpty()) return dataPayload
        if (dataPayload.containsKey(KEY_DEEPLINK)) return dataPayload
        val merged = LinkedHashMap(dataPayload)
        merged[KEY_DEEPLINK] = deepLinkUrl
        return merged
    }

    /** Resolves [smallIconResName] to a resource id, or 0 if unset/unknown. */
    fun resolveSmallIconId(context: Context): Int = resolveResId(context, smallIconResName)

    /** Resolves [largeIconResName] to a resource id, or 0 if unset/unknown. */
    fun resolveLargeIconId(context: Context): Int = resolveResId(context, largeIconResName)

    /** Serializes this template to a JSON string (round-trips with [fromJson]). */
    fun toJson(): String {
        val obj = JSONObject()
        obj.put("id", id)
        obj.put("title", title)
        obj.put("body", body)
        obj.put("smallIconResName", smallIconResName ?: JSONObject.NULL)
        obj.put("largeIconResName", largeIconResName ?: JSONObject.NULL)
        obj.put("channelId", channelId)
        obj.put("deepLinkUrl", deepLinkUrl ?: JSONObject.NULL)
        val data = JSONObject()
        for ((k, v) in dataPayload) data.put(k, v)
        obj.put("dataPayload", data)
        return obj.toString()
    }

    companion object {
        const val KEY_DEEPLINK = "deeplink"

        /**
         * Builds a [NotificationTemplate] from a JSON string (see [toJson]).
         * Missing optional fields fall back to sensible defaults.
         */
        fun fromJson(json: String): NotificationTemplate {
            val obj = JSONObject(json)
            val data = LinkedHashMap<String, String>()
            obj.optJSONObject("dataPayload")?.let { d ->
                val keys = d.keys()
                while (keys.hasNext()) {
                    val key = keys.next()
                    data[key] = d.optString(key)
                }
            }
            return NotificationTemplate(
                id = obj.optInt("id", 0),
                title = obj.optString("title", ""),
                body = obj.optString("body", ""),
                smallIconResName = obj.optStringOrNull("smallIconResName"),
                largeIconResName = obj.optStringOrNull("largeIconResName"),
                channelId = obj.optString("channelId", ""),
                dataPayload = data,
                deepLinkUrl = obj.optStringOrNull("deepLinkUrl")
            )
        }

        private fun resolveResId(context: Context, name: String?): Int {
            if (name.isNullOrEmpty()) return 0
            val res = context.resources
            val pkg = context.packageName
            // Try the common resource types Unity exports icons into.
            var id = res.getIdentifier(name, "drawable", pkg)
            if (id == 0) id = res.getIdentifier(name, "mipmap", pkg)
            return id
        }

        /** Returns the string value, or null when the key is missing/JSON null. */
        private fun JSONObject.optStringOrNull(key: String): String? {
            if (!has(key) || isNull(key)) return null
            val value = optString(key, "")
            return value.ifEmpty { null }
        }
    }
}
