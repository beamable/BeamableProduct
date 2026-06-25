package com.beamable.deeplink

import android.app.Activity
import android.app.Application
import android.content.Intent
import android.os.Bundle
import com.beamable.deeplink.unity.UnityDeepLinkBridge

/**
 * Public, engine-agnostic facade for native deeplink handling.
 *
 * NOTE: Unity also ships a C# class named `DeepLinkManager`. They are unrelated:
 * this is the native (Kotlin) manager; the C# one is the consumer.
 *
 * Responsibilities:
 *  - Cold-start: an [ActivityIntentObserver] inspects the launch intent and
 *    dispatches the deeplink (push API). [getInitialLink] is also available as a
 *    pull API for engines that prefer to query at their own timing.
 *  - Warm-start: the host activity is `singleTask`, so re-launch deeplinks arrive in
 *    `Activity.onNewIntent`, which lifecycle callbacks cannot see. The engine must
 *    forward those intents to [handleNewIntent].
 *
 * On Unity specifically, warm-start is primarily handled by Unity's own
 * `onNewIntent` -> `Application.deepLinkActivated` path. This native path guarantees
 * cold-start delivery; the dedupe in [ActivityIntentObserver] prevents a double fire
 * if both paths observe the same launch intent.
 */
object DeepLinkManager {

    @Volatile
    var listener: DeepLinkListener? = null

    private var observer: ActivityIntentObserver? = null

    @Volatile
    private var initialized = false

    /**
     * Initialize with an explicit listener (Unreal / React Native / custom).
     * Registers the cold-start observer exactly once.
     */
    @Synchronized
    fun initialize(application: Application, listener: DeepLinkListener) {
        this.listener = listener

        // Guard against double-registration (e.g. repeated initialize calls).
        if (initialized) return

        val obs = ActivityIntentObserver { url, isColdStart -> dispatch(url, isColdStart) }
        application.registerActivityLifecycleCallbacks(obs)
        observer = obs
        initialized = true
    }

    /**
     * Convenience overload for Unity: wires up a [UnityDeepLinkBridge] that forwards
     * deeplinks to the given GameObject via `UnityPlayer.UnitySendMessage`.
     */
    fun initialize(application: Application, bridgeGameObjectName: String) {
        initialize(application, UnityDeepLinkBridge(bridgeGameObjectName))
    }

    /**
     * Pull API for the cold-start URL: returns the launch intent's deeplink (if any)
     * WITHOUT dispatching to the listener. Useful for engines that query on startup.
     */
    fun getInitialLink(activity: Activity): String? {
        return IntentDeepLinkExtractor.extract(activity.intent)
    }

    /**
     * Warm-start hook. Engines that forward `Activity.onNewIntent` should call this.
     * Extracts the deeplink and, if present, dispatches it with `isColdStart = false`.
     */
    fun handleNewIntent(intent: Intent) {
        val url = IntentDeepLinkExtractor.extract(intent) ?: return
        dispatch(url, false)
    }

    /** Deliver a URL to the listener, guarded so a misbehaving listener can't crash us. */
    internal fun dispatch(url: String, isColdStart: Boolean) {
        try {
            listener?.onDeepLink(url, isColdStart)
        } catch (t: Throwable) {
            // Swallow: deeplink delivery must never crash the host process.
        }
    }

    /** Reset cold-start dedupe state so an identical URL can be delivered again. */
    fun clearConsumed() {
        observer?.clear()
    }
}

// ---------------------------------------------------------------------------
// IntentDeepLinkExtractor (folded in from IntentDeepLinkExtractor.kt)
// ---------------------------------------------------------------------------

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

// ---------------------------------------------------------------------------
// ActivityIntentObserver (folded in from ActivityIntentObserver.kt)
// ---------------------------------------------------------------------------

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
