package com.beamable.push

import android.app.Activity
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Build
import android.provider.Settings
import android.util.Log
import java.util.Calendar
import java.util.TimeZone
import java.util.concurrent.CopyOnWriteArrayList
import com.google.firebase.FirebaseApp
import com.google.firebase.messaging.FirebaseMessaging

/**
 * Public facade for the Beamable push library. Engine adapters and host apps
 * interact almost exclusively through this object.
 *
 * Threading: FCM callbacks invoke the dispatch* helpers from background threads.
 * The bundled Unity adapter forwards via UnityPlayer.UnitySendMessage, which is
 * thread-safe, so listener callbacks are invoked directly with no main-thread
 * marshaling. Custom [PushListener] implementations must be thread-safe.
 *
 * Remote (FCM) is OPTIONAL: the library works local-only when no Firebase config
 * (google-services.json) is present. See [remoteEnabled] / [initialize].
 */
object PushManager {

    private const val TAG = "BeamablePush"

    @Volatile
    var listener: PushListener? = null

    /** Set by the host engine to indicate whether the app is currently foregrounded. */
    @Volatile
    var isForeground: Boolean = false

    /**
     * Whether remote (FCM) features are active. True only when the caller opted in
     * (`enableRemote`) AND a Firebase config is actually present. When false the library
     * still does local notifications fully; remote calls become no-ops.
     */
    @Volatile
    var remoteEnabled: Boolean = false
        private set

    @Volatile
    private var appContext: Context? = null

    /**
     * Programmatically-registered receive-time handlers (§1.1). Note: these only help while the
     * app process is alive — when a push arrives with the app fully closed, the FCM process
     * starts fresh and this list is empty, so handlers are resolved from manifest meta-data
     * instead (see [resolveHandlers]). Multiple handlers are supported, mirroring iOS's
     * PluginRegistry.
     */
    private val programmaticHandlers = CopyOnWriteArrayList<PushNotificationReceivedHandler>()

    /**
     * Handler registered via the DEPRECATED legacy [setNotificationReceivedHandler]. Held in its
     * own slot (separate from [programmaticHandlers]) so a legacy set does not drop handlers that
     * were added via [addNotificationReceivedHandler], and vice-versa. When non-null it implies
     * [legacyOverride] is true (manifest handlers are suppressed — original "override the manifest"
     * semantics).
     */
    @Volatile
    private var legacyHandler: PushNotificationReceivedHandler? = null

    /**
     * True once the legacy [setNotificationReceivedHandler] has been used (even with null). Legacy
     * use SUPPRESSES manifest handlers; handlers added via [addNotificationReceivedHandler] are
     * additive and never suppress the manifest.
     */
    @Volatile
    private var legacyOverride = false

    // Cache of the manifest-declared handlers so we reflect/instantiate only once per process.
    @Volatile
    private var manifestHandlers: List<PushNotificationReceivedHandler> = emptyList()
    @Volatile
    private var manifestHandlersResolved = false

    /**
     * Cache of the COMBINED handler list (legacy + programmatic + manifest) so [resolveHandlers] —
     * called on every incoming notification — does not rebuild a LinkedHashSet per push. Null means
     * "not yet computed / invalidated"; recomputed lazily under [this]'s monitor. Invalidated
     * whenever the handler set changes (add/remove/legacy-set) or after manifest resolution.
     */
    @Volatile
    private var combinedHandlersCache: List<PushNotificationReceivedHandler>? = null

    /** Drops the combined-handler cache so the next [resolveHandlers] recomputes it. */
    private fun invalidateHandlerCache() {
        combinedHandlersCache = null
    }

    // Shared meta-data key used as a PREFIX: handlers are declared as this exact key plus
    // `.1`, `.2`, … because the manifest merger rejects duplicate exact <meta-data> names.
    private const val NOTIFICATION_RECEIVED_HANDLER_META =
        "com.beamable.push.notification_received_handler"

    // ---- Initialization -----------------------------------------------------

    /**
     * Initializes with an explicit [listener].
     *
     * @param enableRemote opt into FCM. Even when true, remote is only activated if a
     *   Firebase config is present (auto-detected); pass false to force local-only.
     */
    fun initialize(context: Context, listener: PushListener, enableRemote: Boolean = true) {
        this.appContext = context.applicationContext
        this.listener = listener
        this.remoteEnabled = enableRemote && isFirebaseAvailable(context)
        Log.i(TAG, "initialized (remoteEnabled=$remoteEnabled, requested=$enableRemote)")
    }

    /** True if a Firebase app/config is present in this process (google-services.json). */
    private fun isFirebaseAvailable(context: Context): Boolean {
        return try {
            FirebaseApp.getApps(context).isNotEmpty()
        } catch (t: Throwable) {
            false
        }
    }

    // Note: the public `listener` property already exposes a JVM `setListener(...)`
    // setter to Java/JNI/C# callers, so no explicit setter method is needed (an
    // explicit one would clash with the property's generated setter signature).

    /** Adds a receive-time handler programmatically (app-alive only). Duplicates are ignored. */
    fun addNotificationReceivedHandler(handler: PushNotificationReceivedHandler) {
        if (!programmaticHandlers.contains(handler)) {
            programmaticHandlers.add(handler)
            invalidateHandlerCache()
        }
    }

    /** Removes a previously-added programmatic receive-time handler. */
    fun removeNotificationReceivedHandler(handler: PushNotificationReceivedHandler) {
        if (programmaticHandlers.remove(handler)) invalidateHandlerCache()
    }

    /**
     * Back-compat: sets the single legacy receive-time handler (clear-and-set), restoring the
     * original "override the manifest" semantics — while a legacy handler is set, manifest-declared
     * handlers are SUPPRESSED (see [resolveHandlers]).
     *
     * Clear-and-set applies ONLY to the legacy slot: handlers added via
     * [addNotificationReceivedHandler] are kept in a separate list and are NEVER dropped by a later
     * legacy set call (and survive a legacy clear). Passing null clears the legacy slot, but the
     * override flag stays set for this process so manifest handlers remain suppressed once the
     * legacy API has been used at all.
     *
     * Prefer [addNotificationReceivedHandler] now that multiple additive handlers are supported.
     */
    @Deprecated(
        "Multiple handlers are now supported; use addNotificationReceivedHandler.",
        ReplaceWith("addNotificationReceivedHandler(handler)")
    )
    fun setNotificationReceivedHandler(handler: PushNotificationReceivedHandler?) {
        legacyOverride = true
        legacyHandler = handler
        invalidateHandlerCache()
    }

    /**
     * Resolves ALL receive-time handlers for the current process (§1.1), in priority order:
     * the legacy handler (if any) first, then the additive [addNotificationReceivedHandler] ones,
     * then — UNLESS the legacy API has been used ([legacyOverride]) — every class named by a
     * manifest meta-data (see [instantiateManifestHandlers]). The combined list is deduped.
     *
     * Precedence: use of the DEPRECATED [setNotificationReceivedHandler] restores the original
     * "override the manifest" behaviour and SUPPRESSES manifest handlers. Handlers added via
     * [addNotificationReceivedHandler] are additive and do NOT suppress the manifest.
     *
     * Uses the supplied [context] (the FCM service) rather than [appContext], which is null in a
     * freshly-spawned closed-app process.
     */
    internal fun resolveHandlers(context: Context): List<PushNotificationReceivedHandler> {
        if (!manifestHandlersResolved) {
            synchronized(this) {
                if (!manifestHandlersResolved) {
                    manifestHandlers = instantiateManifestHandlers(context)
                    manifestHandlersResolved = true
                    // Manifest set just changed; drop any stale combined cache built before it.
                    invalidateHandlerCache()
                }
            }
        }
        // Fast path: serve the cached combined list (set on this hot path, called per push).
        combinedHandlersCache?.let { return it }
        synchronized(this) {
            // Re-check under lock: another thread may have populated the cache meanwhile.
            combinedHandlersCache?.let { return it }
            // Legacy handler first (own slot), then additive handlers, deduped.
            val combined = LinkedHashSet<PushNotificationReceivedHandler>()
            legacyHandler?.let { combined.add(it) }
            combined.addAll(programmaticHandlers)
            // Manifest handlers are suppressed once the legacy override API has been used.
            if (!legacyOverride) combined.addAll(manifestHandlers)
            val list = combined.toList()
            combinedHandlersCache = list
            return list
        }
    }

    /** Resets all in-process handler/override state. Test seam only. */
    internal fun resetHandlersForTest() {
        programmaticHandlers.clear()
        legacyHandler = null
        legacyOverride = false
        manifestHandlers = emptyList()
        manifestHandlersResolved = false
        invalidateHandlerCache()
    }

    /**
     * Dispatches [event] to every resolved handler, isolating each handler's failure so one
     * throwing handler does not block the others (failures route to [dispatchError]).
     */
    internal fun dispatchNotificationReceived(context: Context, event: PushReceivedEvent) {
        for (handler in resolveHandlers(context)) {
            try {
                handler.onNotificationReceived(context, event)
            } catch (t: Throwable) {
                dispatchError("notification_received", t.message ?: t.toString())
            }
        }
    }

    private fun instantiateManifestHandlers(
        context: Context
    ): List<PushNotificationReceivedHandler> {
        return try {
            val ai = context.packageManager.getApplicationInfo(
                context.packageName,
                PackageManager.GET_META_DATA
            )
            val meta = ai.metaData ?: return emptyList()
            // Collect (index, className) for the exact base key (index -1, sorts first) and any
            // key whose suffix after "<BASE>." is a NON-NEGATIVE INTEGER (.1, .2, …). Keys with a
            // non-numeric suffix (e.g. ".enabled") are silently ignored — no dispatchError.
            val suffixPrefix = "$NOTIFICATION_RECEIVED_HANDLER_META."
            val entries = ArrayList<Pair<Int, String>>()
            val seen = HashSet<Int>() // dedup so the base key isn't processed twice
            for (key in meta.keySet()) {
                val index: Int = when {
                    key == NOTIFICATION_RECEIVED_HANDLER_META -> -1
                    key.startsWith(suffixPrefix) -> {
                        val suffix = key.substring(suffixPrefix.length)
                        suffix.toIntOrNull()?.takeIf { it >= 0 } ?: continue
                    }
                    else -> continue
                }
                if (!seen.add(index)) continue
                val className = meta.getString(key) ?: continue
                entries.add(index to className)
            }
            // Deterministic order: base key (-1) first, then ascending numeric index.
            entries.sortBy { it.first }
            val handlers = ArrayList<PushNotificationReceivedHandler>()
            for ((_, className) in entries) {
                try {
                    val instance = Class.forName(className)
                        .getDeclaredConstructor().newInstance()
                    (instance as? PushNotificationReceivedHandler)?.let { handlers.add(it) }
                } catch (t: Throwable) {
                    dispatchError(
                        "notification_received_handler_resolve",
                        "$className: ${t.message ?: t.toString()}"
                    )
                }
            }
            handlers
        } catch (t: Throwable) {
            dispatchError("notification_received_handler_resolve", t.message ?: t.toString())
            emptyList()
        }
    }

    // ---- Channels -----------------------------------------------------------

    /** Registers (creates) a notification channel from a [spec]. */
    fun registerChannel(spec: NotificationChannelSpec) {
        val ctx = appContext ?: return
        NotificationBuilder.ensureChannel(ctx, spec)
    }

    /**
     * Field overload of [registerChannel] for easy cross-language calls (engine adapters use this
     * — no JSON). [importance] uses the `NotificationManager.IMPORTANCE_*` constants (e.g. 4 = HIGH).
     */
    fun registerChannel(id: String, name: String, description: String, importance: Int) {
        registerChannel(NotificationChannelSpec(id, name, description, importance))
    }

    // ---- Permission ---------------------------------------------------------

    /** Requests the POST_NOTIFICATIONS permission (API 33+). */
    fun requestPermission(activity: Activity) {
        PermissionHelper.requestPermission(activity)
    }

    /** True if notifications are currently permitted. */
    fun hasPermission(context: Context): Boolean = PermissionHelper.hasPermission(context)

    // ---- Local notifications -----------------------------------------------

    /** Schedules a local notification after [delayMillis]; returns its id. */
    fun scheduleLocal(template: NotificationTemplate, delayMillis: Long): Int {
        val ctx = appContext
        if (ctx == null) {
            dispatchError("schedule_local", "PushManager not initialized")
            return 0
        }
        val id = LocalNotificationScheduler.schedule(ctx, template, delayMillis)
        dispatchLocalScheduled(id)
        return id
    }

    /** JSON overload of [scheduleLocal]. */
    fun scheduleLocal(templateJson: String, delayMillis: Long): Int {
        return try {
            scheduleLocal(NotificationTemplate.fromJson(templateJson), delayMillis)
        } catch (t: Throwable) {
            dispatchError("schedule_local", t.message ?: t.toString())
            0
        }
    }

    /**
     * Exact variant of [scheduleLocal] — uses an exact alarm so it fires precisely (subject to OEM
     * limits) instead of being batched. Requires the SCHEDULE_EXACT_ALARM permission on API 31+
     * (the app must declare it; API 33+ also needs a user grant — see [canScheduleExactAlarms] /
     * [requestExactAlarmPermission]); falls back to inexact with a `schedule_exact_denied` warning.
     */
    fun scheduleLocalExact(template: NotificationTemplate, delayMillis: Long): Int {
        val ctx = appContext
        if (ctx == null) {
            dispatchError("schedule_local", "PushManager not initialized")
            return 0
        }
        val id = LocalNotificationScheduler.schedule(ctx, template, delayMillis, exact = true)
        dispatchLocalScheduled(id)
        return id
    }

    /** JSON overload of [scheduleLocalExact]. */
    fun scheduleLocalExact(templateJson: String, delayMillis: Long): Int {
        return try {
            scheduleLocalExact(NotificationTemplate.fromJson(templateJson), delayMillis)
        } catch (t: Throwable) {
            dispatchError("schedule_local", t.message ?: t.toString())
            0
        }
    }

    /**
     * Schedules [template] at an absolute wall-clock time. The given fields are interpreted in
     * **UTC** when [useUtc] is true, otherwise in the **device's local** time zone, then converted
     * to an epoch instant. [month] is 1-12.
     *
     * @param exact use an exact alarm (see [scheduleLocalExact]); defaults to inexact via the JSON overload.
     */
    fun scheduleLocalAt(
        template: NotificationTemplate,
        year: Int, month: Int, dayOfMonth: Int, hourOfDay: Int, minute: Int, second: Int,
        useUtc: Boolean, exact: Boolean
    ): Int {
        val ctx = appContext
        if (ctx == null) {
            dispatchError("schedule_local", "PushManager not initialized")
            return 0
        }
        val tz = if (useUtc) TimeZone.getTimeZone("UTC") else TimeZone.getDefault()
        val cal = Calendar.getInstance(tz)
        cal.clear()
        cal.set(year, month - 1, dayOfMonth, hourOfDay, minute, second)
        val id = LocalNotificationScheduler.scheduleAt(ctx, template, cal.timeInMillis, exact)
        dispatchLocalScheduled(id)
        return id
    }

    /** JSON overload of [scheduleLocalAt]. */
    fun scheduleLocalAt(
        templateJson: String,
        year: Int, month: Int, dayOfMonth: Int, hourOfDay: Int, minute: Int, second: Int,
        useUtc: Boolean, exact: Boolean
    ): Int {
        return try {
            scheduleLocalAt(
                NotificationTemplate.fromJson(templateJson),
                year, month, dayOfMonth, hourOfDay, minute, second, useUtc, exact
            )
        } catch (t: Throwable) {
            dispatchError("schedule_local", t.message ?: t.toString())
            0
        }
    }

    /** True if exact alarms can currently be scheduled (always pre-API-31; permission-gated after). */
    fun canScheduleExactAlarms(): Boolean {
        val ctx = appContext ?: return false
        return LocalNotificationScheduler.canScheduleExact(ctx)
    }

    /** Opens the system settings screen where the user can grant exact-alarm permission (API 31+). */
    fun requestExactAlarmPermission(activity: Activity) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
            try {
                activity.startActivity(Intent(Settings.ACTION_REQUEST_SCHEDULE_EXACT_ALARM))
            } catch (t: Throwable) {
                dispatchError("request_exact_alarm", t.message ?: t.toString())
            }
        }
    }

    /** Cancels a scheduled/posted notification by id. */
    fun cancel(id: Int) {
        val ctx = appContext ?: return
        LocalNotificationScheduler.cancel(ctx, id)
    }

    /** Cancels all posted notifications. */
    fun cancelAll() {
        val ctx = appContext ?: return
        LocalNotificationScheduler.cancelAll(ctx)
    }

    // ---- FCM tokens / topics (no-op when remote disabled) -------------------

    /** Fetches the current FCM registration token asynchronously (remote only). */
    fun fetchToken() {
        if (!remoteEnabled) {
            Log.i(TAG, "fetchToken skipped: remote disabled (local-only)")
            return
        }
        FirebaseMessaging.getInstance().token.addOnCompleteListener { task ->
            if (task.isSuccessful) {
                dispatchToken(task.result ?: "")
            } else {
                val msg = task.exception?.message ?: "unknown token error"
                listener?.let { safe("token_error") { it.onTokenRefreshError(msg) } }
            }
        }
    }

    /** Subscribes to an FCM topic (remote only). */
    fun subscribeTopic(topic: String) {
        if (!remoteEnabled) {
            Log.i(TAG, "subscribeTopic skipped: remote disabled (local-only)")
            return
        }
        FirebaseMessaging.getInstance().subscribeToTopic(topic)
            .addOnCompleteListener { task ->
                if (!task.isSuccessful) {
                    dispatchError("subscribe_topic", task.exception?.message ?: topic)
                }
            }
    }

    /** Unsubscribes from an FCM topic (remote only). */
    fun unsubscribeTopic(topic: String) {
        if (!remoteEnabled) {
            Log.i(TAG, "unsubscribeTopic skipped: remote disabled (local-only)")
            return
        }
        FirebaseMessaging.getInstance().unsubscribeFromTopic(topic)
            .addOnCompleteListener { task ->
                if (!task.isSuccessful) {
                    dispatchError("unsubscribe_topic", task.exception?.message ?: topic)
                }
            }
    }

    // ---- Launch intent ------------------------------------------------------

    /**
     * Consumes the launch intent. Returns the notification payload JSON if the app
     * was opened from a notification (also dispatched via [onNotificationOpened]),
     * otherwise null.
     */
    fun consumeLaunchIntent(activity: Activity): String? {
        val data = IntentDataReader.readLaunchIntent(activity) ?: return null
        dispatchNotificationOpened(data)
        return data
    }

    /**
     * Warm-start variant of [consumeLaunchIntent]: consumes a freshly-delivered [intent]
     * (e.g. from `Activity.onNewIntent`, which does not update the activity's current intent)
     * and dispatches [onNotificationOpened] if it carries a notification payload. Returns the
     * payload JSON, or null when [intent] is not a notification tap.
     */
    fun consumeIntent(intent: Intent?): String? {
        val data = IntentDataReader.readIntent(intent) ?: return null
        dispatchNotificationOpened(data)
        return data
    }

    // ---- Auth credential writer (§4 / Decision Q5) --------------------------

    /**
     * Persists the player's auth credentials into the `beamable_notifications_auth` prefs that
     * [BeamableAnalytics] reads when firing the native funnel (the funnel is inert until these are
     * written). The native side is otherwise a pure reader, so the SDK must call this whenever the
     * player's token/scope changes.
     *
     * [authJson] is the canonical credential object:
     * `{ "accessToken": string, "refreshToken": string, "accessTokenExpiresAt": number(epoch ms),
     *    "cid": string, "pid": string, "host": string }`.
     * Keys are optional individually; present ones overwrite, absent ones are left untouched.
     * Malformed JSON routes to [dispatchError] and writes nothing (never crashes).
     */
    fun configureAuth(context: Context, authJson: String) {
        try {
            val obj = org.json.JSONObject(authJson)
            val prefs = context.applicationContext
                .getSharedPreferences(BeamableAnalytics.PREFS_NAME, Context.MODE_PRIVATE)
            prefs.edit().apply {
                if (obj.has("accessToken"))
                    putString(BeamableAnalytics.KEY_ACCESS_TOKEN, obj.optString("accessToken"))
                if (obj.has("refreshToken"))
                    putString(BeamableAnalytics.KEY_REFRESH_TOKEN, obj.optString("refreshToken"))
                if (obj.has("accessTokenExpiresAt"))
                    putLong(
                        BeamableAnalytics.KEY_ACCESS_TOKEN_EXPIRES_AT,
                        obj.optLong("accessTokenExpiresAt")
                    )
                if (obj.has("cid")) putString(BeamableAnalytics.KEY_CID, obj.optString("cid"))
                if (obj.has("pid")) putString(BeamableAnalytics.KEY_PID, obj.optString("pid"))
                if (obj.has("host")) putString(BeamableAnalytics.KEY_HOST, obj.optString("host"))
            }.apply()
        } catch (t: Throwable) {
            dispatchError("configure_auth", t.message ?: t.toString())
        }
    }

    /** Clears all persisted auth credentials (e.g. on sign-out). The funnel goes inert afterward. */
    fun clearAuth(context: Context) {
        try {
            context.applicationContext
                .getSharedPreferences(BeamableAnalytics.PREFS_NAME, Context.MODE_PRIVATE)
                .edit().clear().apply()
        } catch (t: Throwable) {
            dispatchError("clear_auth", t.message ?: t.toString())
        }
    }

    // ---- Internal dispatch helpers (all guarded) ----------------------------

    internal fun dispatchToken(token: String) {
        listener?.let { safe("token") { it.onTokenReceived(token) } }
    }

    internal fun dispatchForegroundMessage(json: String) {
        listener?.let { safe("foreground_message") { it.onMessageReceivedForeground(json) } }
    }

    internal fun dispatchNotificationOpened(json: String) {
        // Funnel: a notification tap is an "Opened" event (§4.5). Fired natively, gated on a
        // tracked campaign + scope/gamerTag inside trackFunnel.
        appContext?.let { ctx ->
            try {
                BeamableAnalytics.trackFunnel(
                    ctx, NotificationIntentData.fromJson(json), BeamableAnalytics.FunnelType.Opened
                )
            } catch (_: Throwable) { /* analytics is best-effort */ }
        }
        listener?.let { safe("notification_opened") { it.onNotificationOpened(json) } }
    }

    // ---- Offer / conversion funnel helpers (§4.7) ---------------------------

    /**
     * Emits a **Clicked** funnel event for an in-app offer click, attributed to the campaign that
     * arrived in the originating notification (§4.7). [intentDataJson] is the notification's
     * intent-data JSON (as delivered to the engine); [offerJson] is the single clicked offer.
     * No-op unless campaignId + nodeId + scope + gamerTag are present.
     */
    fun trackOfferClicked(intentDataJson: String, offerJson: String?) {
        trackOffer(intentDataJson, offerJson, BeamableAnalytics.FunnelType.Clicked)
    }

    /** Emits a **Converted** funnel event for an offer conversion (§4.7). See [trackOfferClicked]. */
    fun trackOfferConverted(intentDataJson: String, offerJson: String?) {
        trackOffer(intentDataJson, offerJson, BeamableAnalytics.FunnelType.Converted)
    }

    private fun trackOffer(
        intentDataJson: String,
        offerJson: String?,
        type: BeamableAnalytics.FunnelType
    ) {
        val ctx = appContext ?: return
        try {
            val intent = NotificationIntentData.fromJson(intentDataJson)
            val offer = offerJson?.takeIf { it.isNotEmpty() }?.let {
                NotificationOffer.fromJson(org.json.JSONObject(it))
            }
            BeamableAnalytics.trackFunnel(ctx, intent, type, offer)
        } catch (t: Throwable) {
            dispatchError("analytics_offer", t.message ?: t.toString())
        }
    }

    internal fun dispatchPermissionResult(granted: Boolean) {
        listener?.let { safe("permission_result") { it.onPermissionResult(granted) } }
    }

    internal fun dispatchLocalScheduled(id: Int) {
        listener?.let { safe("local_scheduled") { it.onLocalNotificationScheduled(id) } }
    }

    internal fun dispatchError(stage: String, message: String) {
        listener?.let { l ->
            try {
                l.onError(stage, message)
            } catch (_: Throwable) {
                // Never let listener errors propagate out of dispatch.
            }
        }
    }

    /** Runs [block], routing any thrown error to [onError] instead of crashing. */
    private inline fun safe(stage: String, block: () -> Unit) {
        try {
            block()
        } catch (t: Throwable) {
            dispatchError(stage, t.message ?: t.toString())
        }
    }
}
