package com.beamable.deeplink

import android.content.Intent

/**
 * Pure helper that pulls the deeplink URL out of an [Intent].
 *
 * A deeplink is an ACTION_VIEW intent carrying a data URI (configured by the
 * `<data android:scheme="...">` filter in the app manifest).
 */
object IntentDeepLinkExtractor {

    /**
     * @return the data URI as a string when [intent] is a VIEW intent with data,
     *         otherwise null.
     */
    fun extract(intent: Intent?): String? {
        if (intent == null) return null
        if (intent.action != Intent.ACTION_VIEW) return null
        val data = intent.data ?: return null
        return data.toString()
    }
}
