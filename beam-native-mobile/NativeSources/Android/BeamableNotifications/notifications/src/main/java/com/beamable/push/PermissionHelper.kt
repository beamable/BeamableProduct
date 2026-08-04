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
     * Note: Unity's activity does not forward onRequestPermissionsResult to this
     * library, so we cannot synchronously observe the dialog result. We fire the
     * system request and emit a best-effort [PushListener.onPermissionResult] based
     * on the current permission state. Callers needing the authoritative result
     * should re-query [hasPermission] after the dialog is dismissed.
     */
    fun requestPermission(activity: Activity, requestCode: Int = DEFAULT_REQUEST_CODE) {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.TIRAMISU) {
            PushManager.dispatchPermissionResult(true)
            return
        }
        if (hasPermission(activity)) {
            PushManager.dispatchPermissionResult(true)
            return
        }
        ActivityCompat.requestPermissions(
            activity,
            arrayOf(Manifest.permission.POST_NOTIFICATIONS),
            requestCode
        )
        // Best-effort: emit the current (likely still-not-granted) state.
        PushManager.dispatchPermissionResult(hasPermission(activity))
    }
}
