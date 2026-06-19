package com.beamable.deeplink

import android.app.Activity
import android.app.Application
import android.os.Bundle

/**
 * Lifecycle observer that detects cold-start deeplinks.
 *
 * It inspects each activity's intent in [onActivityCreated] and [onActivityResumed]
 * (resumed is a safety net in case the create pass ran before the listener was set,
 * or the intent was not yet populated) and reports any VIEW-intent URL it finds via
 * [callback] with `isColdStart = true`.
 *
 * IMPORTANT CAVEAT: [Application.ActivityLifecycleCallbacks] does NOT receive
 * `onNewIntent`. For a `singleTask` activity (our Unity host), warm-start deeplinks
 * arrive through `Activity.onNewIntent`, which is invisible here. Warm-starts must be
 * routed through [DeepLinkManager.handleNewIntent] by the engine instead. This observer
 * therefore guarantees cold-start coverage only.
 *
 * @param callback invoked with (url, isColdStart) when a fresh deeplink is found.
 */
class ActivityIntentObserver(
    private val callback: (url: String, isColdStart: Boolean) -> Unit
) : Application.ActivityLifecycleCallbacks {

    // Dedupe state: the launch intent is observed in both onActivityCreated and
    // onActivityResumed (and survives config-change re-resumes), so without this we
    // would deliver the same cold-start URL multiple times. We remember the last URL
    // we delivered and skip identical repeats. clear() resets it so the same URL can
    // legitimately be delivered again on a later, separate launch.
    private var lastDeliveredUrl: String? = null

    private fun maybeDeliver(activity: Activity) {
        val url = IntentDeepLinkExtractor.extract(activity.intent) ?: return

        // Skip if we already delivered this exact URL (dedupe window = until clear()).
        if (url == lastDeliveredUrl) return
        lastDeliveredUrl = url

        callback(url, true)
    }

    /** Reset dedupe so the same URL value can be delivered again on a future launch. */
    fun clear() {
        lastDeliveredUrl = null
    }

    override fun onActivityCreated(activity: Activity, savedInstanceState: Bundle?) {
        maybeDeliver(activity)
    }

    override fun onActivityResumed(activity: Activity) {
        maybeDeliver(activity)
    }

    // --- Remaining lifecycle callbacks are intentional no-ops. ---
    override fun onActivityStarted(activity: Activity) {}
    override fun onActivityPaused(activity: Activity) {}
    override fun onActivityStopped(activity: Activity) {}
    override fun onActivitySaveInstanceState(activity: Activity, outState: Bundle) {}
    override fun onActivityDestroyed(activity: Activity) {}
}
