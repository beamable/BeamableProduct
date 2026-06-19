package com.beamable.deeplink

import android.app.Activity
import android.app.Application
import android.content.Intent
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
