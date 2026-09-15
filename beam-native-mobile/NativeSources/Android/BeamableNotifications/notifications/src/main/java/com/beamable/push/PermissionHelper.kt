package com.beamable.push

import android.Manifest
import android.app.Activity
import android.content.Context
import android.content.pm.PackageManager
import android.os.Build
import androidx.core.app.ActivityCompat
import androidx.core.content.ContextCompat

/**
 * Helpers for the runtime POST_NOTIFICATIONS permission (Android 13 / API 33+).
 * On older platforms notification permission is implicit and always "granted".
 */
object PermissionHelper {

    const val DEFAULT_REQUEST_CODE = 6001

    /** True if notifications are permitted (always true below API 33). */
    fun hasPermission(context: Context): Boolean {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.TIRAMISU) return true
        return ContextCompat.checkSelfPermission(
            context,
            Manifest.permission.POST_NOTIFICATIONS
        ) == PackageManager.PERMISSION_GRANTED
    }

    /**
     * Requests POST_NOTIFICATIONS on API 33+. On older platforms it immediately
     * reports granted.
     *
     * Returns true when the system dialog was actually shown, meaning the authoritative result is
     * still pending. It returns false when the outcome was already known and dispatched (pre-API-33,
     * or already granted).
     *
     * [emitBestEffort] controls what happens once the dialog is shown. Hosts that cannot observe
     * `onRequestPermissionsResult` (Unity's activity does not forward it) leave it true and get an
     * immediate, best-effort [PushListener.onPermissionResult] based on the current state — which is
     * necessarily "not granted", since the user has not answered yet.
     *
     * Hosts that CAN observe the real callback must pass false and dispatch the result themselves;
     * otherwise the first request always reports denied and only a second request reports the truth.
     * See [com.beamable.push.react.ReactPushModule.requestPermission].
     */
    fun requestPermission(
        activity: Activity,
        requestCode: Int = DEFAULT_REQUEST_CODE,
        emitBestEffort: Boolean = true
    ): Boolean {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.TIRAMISU) {
            PushManager.dispatchPermissionResult(true)
            return false
        }
        if (hasPermission(activity)) {
            PushManager.dispatchPermissionResult(true)
            return false
        }
        ActivityCompat.requestPermissions(
            activity,
            arrayOf(Manifest.permission.POST_NOTIFICATIONS),
            requestCode
        )
        if (emitBestEffort) {
            // Best-effort: emit the current (still-not-granted) state.
            PushManager.dispatchPermissionResult(hasPermission(activity))
        }
        return true
    }
}
