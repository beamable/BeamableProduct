package com.beamable.push

import android.content.Context

/**
 * Interface contracts for the Beamable push library, consolidated into one file:
 * [PushListener], [PushNotificationReceivedHandler], and [EngineBridge].
 *
 * (The deeplink package keeps its own [com.beamable.deeplink.EngineBridge] — the two
 * are intentionally not merged across packages.)
 */

// ---------------------------------------------------------------------------
// PushListener
// ---------------------------------------------------------------------------

/**
 * Engine-agnostic callback surface for the push library.
 *
 * Implementations may be invoked from background threads (FCM callbacks run off
 * the main thread). Implementations must therefore be thread-safe or marshal to
 * their own main thread as needed. The bundled [com.beamable.push.unity.UnityNotificationsBridge]
 * forwards via UnityPlayer.UnitySendMessage, which is itself thread-safe.
 */
interface PushListener {
    /** A fresh FCM registration token is available. */
    fun onTokenReceived(token: String)

    /** Fetching/refreshing the FCM token failed. */
    fun onTokenRefreshError(error: String)

    /** A push message arrived while the app was in the foreground (JSON payload). */
    fun onMessageReceivedForeground(messageJson: String)

    /** The app was opened by tapping a notification; [dataJson] carries the payload. */
    fun onNotificationOpened(dataJson: String)

    /** Result of a POST_NOTIFICATIONS permission request. */
    fun onPermissionResult(granted: Boolean)

    /** A local notification was successfully scheduled; [id] is its notification id. */
    fun onLocalNotificationScheduled(id: Int)

    /** A recoverable error occurred at [stage] with a human-readable [message]. */
    fun onError(stage: String, message: String)
}

// ---------------------------------------------------------------------------
// PushNotificationReceivedHandler
// ---------------------------------------------------------------------------

/**
 * Receive-time hook, invoked by [PushFirebaseService] for EVERY incoming FCM message —
 * including while the app is backgrounded or fully killed.
 *
 * This is the only extension point that can run on receipt while the app is closed: it
 * executes in FCM's background process, with NO game-engine runtime (Unity/Unreal/RN)
 * initialized. Implementations must therefore be self-contained native code.
 *
 * IMPORTANT — FCM delivery constraints:
 *  - This fires while closed/backgrounded ONLY for **data-only** messages (no `notification`
 *    block) sent with high priority. A message carrying a `notification` block is displayed
 *    by the OS and does NOT invoke onMessageReceived until the user taps it.
 *  - A force-stopped (or aggressively OEM-killed) app receives nothing until reopened.
 *
 * Threading: called on FCM's background thread with a limited (~10s) execution budget. A
 * short blocking network call is acceptable here; for guaranteed delivery, enqueue WorkManager
 * from within your implementation.
 *
 * MULTIPLE HANDLERS (§1.1): the library supports N handlers, mirroring iOS's PluginRegistry.
 * Register via:
 *  1. AndroidManifest meta-data (required for the closed-app case — resolved by reflection).
 *     Declare one or more, using the shared key as a PREFIX (the manifest merger rejects
 *     duplicate exact names):
 *     <meta-data android:name="com.beamable.push.notification_received_handler"
 *                android:value="your.first.HandlerClass" />
 *     <meta-data android:name="com.beamable.push.notification_received_handler.1"
 *                android:value="your.second.HandlerClass" />
 *     Implementations registered this way MUST have a public no-arg constructor.
 *  2. Programmatically while the app is alive:
 *     [PushManager.addNotificationReceivedHandler].
 *
 * Manifest-declared and programmatic handlers are additive — both sets always participate.
 * Every registered handler is invoked for each event; one handler throwing must not block
 * the others (failures are isolated and routed to [PushManager.dispatchError]).
 */
interface PushNotificationReceivedHandler {
    fun onNotificationReceived(context: Context, event: PushReceivedEvent)
}

// ---------------------------------------------------------------------------
// EngineBridge
// ---------------------------------------------------------------------------

/**
 * Lowest-common-denominator bridge to a host game engine.
 *
 * Engine-specific adapters (Unity, Unreal, React Native) implement this to
 * forward a callback [method] name and a serialized [payload] string back to
 * managed/script code. The core library never depends on any engine type.
 */
interface EngineBridge {
    /**
     * Emit a callback to the host engine.
     *
     * @param method the callback/handler name the engine should dispatch to.
     * @param payload a string (often JSON) carrying the callback data.
     */
    fun emit(method: String, payload: String)
}
