package com.beamable.deeplink

import android.app.Activity
import android.app.Application
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
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

// ---------------------------------------------------------------------------
// DeepLinkNormalizer (folded in from DeepLinkNormalizer.kt)
// ---------------------------------------------------------------------------

/**
 * Turns a remote push's deep-link value into a complete, openable URL **before** it is handed
 * to the engine, so every consumer (RN / Unity / Unreal) receives a ready-to-route deep link
 * rather than a schemeless fragment.
 *
 * Remote campaigns from the Beamable back-office often carry the deep link in one of two
 * incomplete shapes:
 *   1. a schemeless value under the canonical key — `{"deeplink": "details/55"}`; or
 *   2. the app scheme prepended to the *key* itself — `{"myapp://deeplink": "details/55"}`.
 * Both should resolve to the full `myapp://details/55`.
 *
 * The app scheme is resolved (in priority order) from:
 *   1. a scheme embedded in the deep-link key (shape 2 above — no app config needed); else
 *   2. the `com.beamable.push.deeplink_scheme` manifest `<meta-data>` the host app declares
 *      (Android can't introspect its own `<intent-filter>` schemes via PackageManager).
 * When neither yields a scheme, the value is passed through verbatim (current behavior) so a
 * misconfigured app is never worse off than before.
 */
object DeepLinkNormalizer {

    /** Canonical flat-map key for the deep link (matches `NotificationIntentData.KEY_DEEPLINK`). */
    const val KEY_DEEPLINK = "deeplink"

    /** Manifest `<meta-data>` key the host app sets to its deep-link URL scheme (e.g. "myapp"). */
    const val META_DEEPLINK_SCHEME = "com.beamable.push.deeplink_scheme"

    // "<scheme>://<rest>" where scheme follows RFC 3986 (letter, then letters/digits/+-.).
    private val SCHEME_PREFIX = Regex("^([a-zA-Z][a-zA-Z0-9+.-]*)://(.*)$")
    private val DEEPLINK_KEYS = setOf("deeplink", "deep_link")

    // Resolved once per process; the app scheme does not change at runtime.
    @Volatile
    private var cachedScheme: String? = null
    @Volatile
    private var schemeResolved = false

    /**
     * Returns a copy of [data] in which the canonical `deeplink` key holds a complete URL. When
     * there is no deep link, or it cannot be completed, [data] is returned unchanged.
     */
    fun normalizeDataMap(context: Context, data: Map<String, String>): Map<String, String> {
        // Find the deep-link value and any scheme hint carried on its key.
        var rawValue: String? = data[KEY_DEEPLINK]?.ifEmpty { null }
        var schemeFromKey: String? = null
        if (rawValue == null) {
            for ((key, value) in data) {
                if (value.isEmpty()) continue
                val match = SCHEME_PREFIX.matchEntire(key) ?: continue
                if (match.groupValues[2].lowercase() in DEEPLINK_KEYS) {
                    rawValue = value
                    schemeFromKey = match.groupValues[1]
                    break
                }
            }
        }
        if (rawValue == null) return data

        val scheme = schemeFromKey ?: appScheme(context)
        val normalized = normalize(rawValue, scheme)
        if (normalized == data[KEY_DEEPLINK]) return data

        val out = LinkedHashMap(data)
        // Drop any scheme-prefixed deeplink key so only the canonical one remains.
        out.keys.toList().forEach { key ->
            val match = SCHEME_PREFIX.matchEntire(key)
            if (match != null && match.groupValues[2].lowercase() in DEEPLINK_KEYS) out.remove(key)
        }
        out[KEY_DEEPLINK] = normalized
        return out
    }

    /**
     * Prepends [scheme] to a schemeless [value]; returns values that already carry a scheme (or
     * when [scheme] is null) unchanged. Intentionally scheme-only: mapping a bare id like "123"
     * to an app route (e.g. "details/123") is app-specific and stays in the consuming app.
     */
    fun normalize(value: String, scheme: String?): String {
        val trimmed = value.trim()
        if (trimmed.isEmpty()) return trimmed
        if (SCHEME_PREFIX.matches(trimmed)) return trimmed
        if (scheme.isNullOrEmpty()) return trimmed
        return "$scheme://${trimmed.trimStart('/')}"
    }

    /** Reads the host app's declared deep-link scheme from the manifest `<meta-data>` (cached). */
    private fun appScheme(context: Context): String? {
        if (schemeResolved) return cachedScheme
        cachedScheme = try {
            val ai = context.packageManager.getApplicationInfo(
                context.packageName, PackageManager.GET_META_DATA
            )
            ai.metaData?.getString(META_DEEPLINK_SCHEME)?.ifEmpty { null }
        } catch (_: Throwable) {
            null
        }
        schemeResolved = true
        return cachedScheme
    }

    /** Test seam: forget the cached app scheme so a test can re-resolve it from a fresh manifest. */
    internal fun resetSchemeCacheForTest() {
        cachedScheme = null
        schemeResolved = false
    }
}
