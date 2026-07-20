package com.beamable.push

import android.app.NotificationManager
import android.content.Context
import org.json.JSONArray
import org.json.JSONObject

/**
 * Engine-boundary data models for the Beamable push library.
 *
 * Consolidated into one file (Kotlin allows multiple top-level declarations) to keep the
 * cohesive notification models together: [PushReceivedEvent], [NotificationTemplate],
 * [NotificationChannelSpec], and the cross-platform [NotificationIntentData] schema (§3.3).
 */

// ---------------------------------------------------------------------------
// PushReceivedEvent
// ---------------------------------------------------------------------------

/**
 * Immutable snapshot of a push message at the moment it was received, passed to a
 * [PushNotificationReceivedHandler].
 *
 * @param messageId FCM message id, when present.
 * @param dataJson the full FCM data map serialized as a JSON object string.
 * @param sentTimeMillis when the message was sent (RemoteMessage.sentTime), epoch millis.
 * @param receivedTimeMillis when the device received it (System.currentTimeMillis()).
 * @param wasForeground whether the app was foregrounded when received (best-effort).
 * @param deepLink convenience copy of dataPayload["deeplink"] if present, else null.
 * @param intentData the full parsed campaign/funnel intent-data schema (§3.3), when present.
 */
data class PushReceivedEvent(
    val messageId: String?,
    val dataJson: String,
    val sentTimeMillis: Long,
    val receivedTimeMillis: Long,
    val wasForeground: Boolean,
    val deepLink: String?,
    val intentData: NotificationIntentData? = null
)

// ---------------------------------------------------------------------------
// NotificationChannelSpec
// ---------------------------------------------------------------------------

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

// ---------------------------------------------------------------------------
// NotificationTemplate
// ---------------------------------------------------------------------------

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
 * @param imageUrl rich-media image URL for the `bigPicture` style (downloaded at build time).
 * @param style built-in style preset: "default" | "bigPicture" | "bigText" (null = default).
 * @param badge app-icon badge count applied via setNumber (orthogonal to style); null leaves it unset.
 * @param category id of a registered [NotificationCategorySpec] whose action buttons to render
 *   (orthogonal to [style], like [badge]); null renders no buttons.
 */
data class NotificationTemplate(
    val id: Int = 0,
    val title: String,
    val body: String,
    val smallIconResName: String? = null,
    val largeIconResName: String? = null,
    val channelId: String,
    val dataPayload: Map<String, String> = emptyMap(),
    val deepLinkUrl: String? = null,
    val imageUrl: String? = null,
    val style: String? = null,
    val badge: Int? = null,
    val category: String? = null
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
        obj.put("imageUrl", imageUrl ?: JSONObject.NULL)
        obj.put("style", style ?: JSONObject.NULL)
        // 0 is a valid badge count, so serialize the number when set and JSON null when unset.
        obj.put("badge", badge ?: JSONObject.NULL)
        obj.put("category", category ?: JSONObject.NULL)
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
                deepLinkUrl = obj.optStringOrNull("deepLinkUrl"),
                imageUrl = obj.optStringOrNull("imageUrl"),
                style = obj.optStringOrNull("style"),
                // has()/optInt keeps 0 as a valid count (a missing/null key stays null).
                badge = if (obj.has("badge") && !obj.isNull("badge")) obj.optInt("badge") else null,
                category = obj.optStringOrNull("category")
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

// ---------------------------------------------------------------------------
// NotificationIntentData (cross-platform schema, §3.3)
// ---------------------------------------------------------------------------

/**
 * A single offer carried inside the notification intent data (§3.3 `offers[]`).
 *
 * `value` may be a string or a number on the wire; it is surfaced here as a String
 * ([rawValue]) since the bridge boundary is untyped. `customData` is a free-form object
 * carried as an opaque JSON string ([customDataJson]) and typed only at the SDK layer.
 */
data class NotificationOffer(
    val itemId: String?,
    val rawValue: String?,
    val customDataJson: String?
) {
    /** Serializes this offer back to a [JSONObject] (round-trips with [fromJson]). */
    fun toJson(): JSONObject {
        val obj = JSONObject()
        if (itemId != null) obj.put("itemId", itemId)
        if (rawValue != null) obj.put("value", rawValue)
        customDataJson?.let { raw ->
            // Re-embed the free-form object verbatim if it parses, else keep the string.
            val parsed = runCatching { JSONObject(raw) }.getOrNull()
            obj.put("customData", parsed ?: raw)
        }
        return obj
    }

    companion object {
        fun fromJson(obj: JSONObject): NotificationOffer {
            val value: String? = when {
                obj.isNull("value") || !obj.has("value") -> null
                else -> obj.get("value").toString()
            }
            val custom = obj.opt("customData")?.let { if (it == JSONObject.NULL) null else it.toString() }
            return NotificationOffer(
                itemId = obj.optString("itemId").ifEmpty { null },
                rawValue = value,
                customDataJson = custom
            )
        }
    }
}

/**
 * Canonical cross-platform notification intent-data schema (spec §3.3). Embedded inside the
 * FCM `data` map as a FLAT string→string map (Decision Q3 — nested objects are stringified).
 *
 * Scalars ([campaignId], [nodeId], [gamerTag], [accountId], [cidPid], [deeplink]) are plain
 * strings. [offersJson] and [campaignDataJson] arrive as JSON-encoded strings and are parsed
 * lazily via [offers] / [campaignData].
 *
 * Field names match the shared contract used by iOS and the engine SDKs verbatim.
 */
data class NotificationIntentData(
    val campaignId: String? = null,
    val nodeId: String? = null,
    val gamerTag: String? = null,
    val accountId: String? = null,
    val cidPid: String? = null,
    /** Raw JSON-encoded string of the `offers` array, or null. */
    val offersJson: String? = null,
    /** Raw JSON-encoded string of the free-form `campaignData` object, or null. */
    val campaignDataJson: String? = null,
    val deeplink: String? = null
) {

    /** Parses [offersJson] into a typed list (empty when absent/unparseable). */
    fun offers(): List<NotificationOffer> {
        val raw = offersJson ?: return emptyList()
        return try {
            val arr = JSONArray(raw)
            (0 until arr.length()).mapNotNull { i ->
                arr.optJSONObject(i)?.let { NotificationOffer.fromJson(it) }
            }
        } catch (_: Throwable) {
            emptyList()
        }
    }

    /** Parses [campaignDataJson] into a free-form map (empty when absent/unparseable). */
    fun campaignData(): Map<String, Any?> {
        val raw = campaignDataJson ?: return emptyMap()
        return try {
            val obj = JSONObject(raw)
            val out = LinkedHashMap<String, Any?>()
            val keys = obj.keys()
            while (keys.hasNext()) {
                val k = keys.next()
                out[k] = obj.opt(k)
            }
            out
        } catch (_: Throwable) {
            emptyMap()
        }
    }

    /** True when both campaignId and nodeId are present — i.e. part of a tracked campaign (§4.2). */
    fun isTrackedCampaign(): Boolean = !campaignId.isNullOrEmpty() && !nodeId.isNullOrEmpty()

    /** True when realm scope + player are present, so an authenticated POST is possible. */
    fun hasFunnelCredentials(): Boolean = !cidPid.isNullOrEmpty() && !gamerTag.isNullOrEmpty()

    companion object {
        const val KEY_CAMPAIGN_ID = "campaignId"
        const val KEY_NODE_ID = "nodeId"
        const val KEY_GAMER_TAG = "gamerTag"
        const val KEY_ACCOUNT_ID = "accountId"
        const val KEY_CID_PID = "cidPid"
        const val KEY_OFFERS = "offers"
        const val KEY_CAMPAIGN_DATA = "campaignData"
        const val KEY_DEEPLINK = "deeplink"

        /**
         * Builds [NotificationIntentData] from a flat string→string map (the FCM `data` map).
         * Nested objects ([KEY_OFFERS], [KEY_CAMPAIGN_DATA]) are kept as their raw JSON strings.
         */
        fun fromDataMap(data: Map<String, String?>): NotificationIntentData = NotificationIntentData(
            campaignId = data[KEY_CAMPAIGN_ID]?.ifEmpty { null },
            nodeId = data[KEY_NODE_ID]?.ifEmpty { null },
            gamerTag = data[KEY_GAMER_TAG]?.ifEmpty { null },
            accountId = data[KEY_ACCOUNT_ID]?.ifEmpty { null },
            cidPid = data[KEY_CID_PID]?.ifEmpty { null },
            offersJson = data[KEY_OFFERS]?.ifEmpty { null },
            campaignDataJson = data[KEY_CAMPAIGN_DATA]?.ifEmpty { null },
            deeplink = data[KEY_DEEPLINK]?.ifEmpty { null }
        )

        /** Builds [NotificationIntentData] from a JSON object string (e.g. PushReceivedEvent.dataJson). */
        fun fromJson(json: String?): NotificationIntentData {
            if (json.isNullOrEmpty()) return NotificationIntentData()
            return try {
                val obj = JSONObject(json)
                val map = LinkedHashMap<String, String?>()
                val keys = obj.keys()
                while (keys.hasNext()) {
                    val k = keys.next()
                    // Scalars come back as strings; nested objects/arrays as their JSON text.
                    map[k] = if (obj.isNull(k)) null else obj.get(k).toString()
                }
                fromDataMap(map)
            } catch (_: Throwable) {
                NotificationIntentData()
            }
        }
    }
}
