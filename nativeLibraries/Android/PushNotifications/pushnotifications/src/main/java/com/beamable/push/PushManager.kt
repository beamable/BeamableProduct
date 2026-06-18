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
import com.beamable.push.unity.UnityPushBridge
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
     * Optional programmatically-set receive-time handler. Note: this only helps while the
     * app process is alive — when a push arrives with the app fully closed, the FCM process
     * starts fresh and this is null, so the handler is resolved from manifest meta-data
     * instead (see [resolveNotificationReceivedHandler]).
     */
    @Volatile
    var notificationReceivedHandler: PushNotificationReceivedHandler? = null
        private set

    // Cache of the manifest-declared handler so we reflect/instantiate only once per process.
    @Volatile
    private var manifestHandler: PushNotificationReceivedHandler? = null
    @Volatile
    private var manifestHandlerResolved = false

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

    /**
     * Unity convenience overload: routes callbacks to the GameObject named
     * [bridgeGameObjectName] via UnityPlayer.UnitySendMessage.
     */
    fun initialize(
        context: Context,
        bridgeGameObjectName: String,
        enableRemote: Boolean = true
    ) {
        initialize(context, UnityPushBridge(bridgeGameObjectName), enableRemote)
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

    /** Sets the receive-time handler programmatically (app-alive only). */
    fun setNotificationReceivedHandler(handler: PushNotificationReceivedHandler?) {
        this.notificationReceivedHandler = handler
    }

    /**
     * Resolves the receive-time handler for the current process: a programmatically-set
     * [notificationReceivedHandler] takes precedence; otherwise the class named by the
     * `com.beamable.push.notification_received_handler` manifest meta-data is instantiated by
     * reflection (no-arg constructor) and cached. Returns null if none is configured.
     *
     * Uses the supplied [context] (the FCM service) rather than [appContext], which is
     * null in a freshly-spawned closed-app process.
     */
    internal fun resolveNotificationReceivedHandler(
        context: Context
    ): PushNotificationReceivedHandler? {
        notificationReceivedHandler?.let { return it }
        if (manifestHandlerResolved) return manifestHandler

        synchronized(this) {
            if (manifestHandlerResolved) return manifestHandler
            manifestHandler = instantiateManifestHandler(context)
            manifestHandlerResolved = true
        }
        return manifestHandler
    }

    private fun instantiateManifestHandler(context: Context): PushNotificationReceivedHandler? {
        return try {
            val ai = context.packageManager.getApplicationInfo(
                context.packageName,
                PackageManager.GET_META_DATA
            )
            val className = ai.metaData?.getString(NOTIFICATION_RECEIVED_HANDLER_META)
                ?: return null
            val instance = Class.forName(className).getDeclaredConstructor().newInstance()
            instance as? PushNotificationReceivedHandler
        } catch (t: Throwable) {
            dispatchError("notification_received_handler_resolve", t.message ?: t.toString())
            null
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

    // ---- Internal dispatch helpers (all guarded) ----------------------------

    internal fun dispatchToken(token: String) {
        listener?.let { safe("token") { it.onTokenReceived(token) } }
    }

    internal fun dispatchForegroundMessage(json: String) {
        listener?.let { safe("foreground_message") { it.onMessageReceivedForeground(json) } }
    }

    internal fun dispatchNotificationOpened(json: String) {
        listener?.let { safe("notification_opened") { it.onNotificationOpened(json) } }
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
