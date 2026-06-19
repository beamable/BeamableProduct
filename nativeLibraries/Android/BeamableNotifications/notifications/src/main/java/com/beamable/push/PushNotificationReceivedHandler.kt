package com.beamable.push

import android.content.Context

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
 * Registration (either mechanism):
 *  1. AndroidManifest meta-data (required for the closed-app case — resolved by reflection):
 *     <meta-data android:name="com.beamable.push.notification_received_handler"
 *                android:value="your.fully.Qualified.HandlerClass" />
 *     Implementations registered this way MUST have a public no-arg constructor.
 *  2. Programmatically while the app is alive: [PushManager.setNotificationReceivedHandler].
 */
interface PushNotificationReceivedHandler {
    fun onNotificationReceived(context: Context, event: PushReceivedEvent)
}
