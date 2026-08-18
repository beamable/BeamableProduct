package com.beamable.push

import android.content.Context
import androidx.test.core.app.ApplicationProvider
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.Shadows.shadowOf

/**
 * Covers FIX 2 (manifest meta-data suffix matching) and FIX 5 (auth-credential writer API).
 */
@RunWith(RobolectricTestRunner::class)
class PushManagerAuthAndManifestTest {

    private val context: Context get() = ApplicationProvider.getApplicationContext()

    @After
    fun tearDown() {
        PushManager.resetHandlersForTest()
        PushManager.clearAuth(context)
    }

    // ---- FIX 5: configureAuth / clearAuth -----------------------------------

    @Test
    fun configureAuth_writesPrefsUsingReaderKeys() {
        val json = """
            {
              "accessToken": "acc-tok",
              "refreshToken": "ref-tok",
              "accessTokenExpiresAt": 1234567890123,
              "cid": "CID",
              "pid": "PID",
              "host": "https://api.beamable.com"
            }
        """.trimIndent()

        PushManager.configureAuth(context, json)

        val prefs = context.getSharedPreferences(BeamableAnalytics.PREFS_NAME, Context.MODE_PRIVATE)
        assertEquals("acc-tok", prefs.getString(BeamableAnalytics.KEY_ACCESS_TOKEN, null))
        assertEquals("ref-tok", prefs.getString(BeamableAnalytics.KEY_REFRESH_TOKEN, null))
        assertEquals(1234567890123L, prefs.getLong(BeamableAnalytics.KEY_ACCESS_TOKEN_EXPIRES_AT, 0L))
        assertEquals("CID", prefs.getString(BeamableAnalytics.KEY_CID, null))
        assertEquals("PID", prefs.getString(BeamableAnalytics.KEY_PID, null))
        assertEquals("https://api.beamable.com", prefs.getString(BeamableAnalytics.KEY_HOST, null))
    }

    @Test
    fun clearAuth_removesPrefs() {
        PushManager.configureAuth(
            context,
            """{"accessToken":"a","cid":"CID","pid":"PID","host":"h"}"""
        )
        PushManager.clearAuth(context)

        val prefs = context.getSharedPreferences(BeamableAnalytics.PREFS_NAME, Context.MODE_PRIVATE)
        assertNull(prefs.getString(BeamableAnalytics.KEY_ACCESS_TOKEN, null))
        assertNull(prefs.getString(BeamableAnalytics.KEY_CID, null))
    }

    @Test
    fun configureAuth_malformedJson_doesNotCrashOrWrite() {
        PushManager.configureAuth(context, "not-json{")
        val prefs = context.getSharedPreferences(BeamableAnalytics.PREFS_NAME, Context.MODE_PRIVATE)
        assertNull(prefs.getString(BeamableAnalytics.KEY_ACCESS_TOKEN, null))
    }

    // ---- FIX 2: manifest meta-data suffix matching --------------------------

    private fun setMeta(vararg pairs: Pair<String, String>) {
        val pm = shadowOf(context.packageManager)
        val ai = context.packageManager.getApplicationInfo(
            context.packageName,
            android.content.pm.PackageManager.GET_META_DATA
        )
        val bundle = ai.metaData ?: android.os.Bundle().also { ai.metaData = it }
        for ((k, v) in pairs) bundle.putString(k, v)
        pm.getInternalMutablePackageInfo(context.packageName).applicationInfo.metaData = bundle
    }

    private fun dispatchAndCount(): Int {
        Probe.count = 0
        Probe2.count = 0
        PushManager.dispatchNotificationReceived(
            context,
            PushReceivedEvent("m", "{}", 0, 0, false, null)
        )
        return Probe.count + Probe2.count
    }

    @Test
    fun nonNumericSuffix_isIgnored() {
        // `.enabled` is a non-numeric suffix → must be ignored (no instantiation, no error).
        setMeta(
            "com.beamable.push.notification_received_handler.enabled" to "true"
        )
        assertEquals(0, dispatchAndCount())
    }

    @Test
    fun baseKeyAndNumericSuffix_areMatched() {
        setMeta(
            "com.beamable.push.notification_received_handler" to Probe::class.java.name,
            "com.beamable.push.notification_received_handler.1" to Probe2::class.java.name,
            "com.beamable.push.notification_received_handler.enabled" to "true"
        )
        assertEquals(2, dispatchAndCount())
    }
}

/** Public no-arg handler so reflection can instantiate it. */
class Probe : PushNotificationReceivedHandler {
    override fun onNotificationReceived(context: Context, event: PushReceivedEvent) { count++ }
    companion object { var count = 0 }
}

class Probe2 : PushNotificationReceivedHandler {
    override fun onNotificationReceived(context: Context, event: PushReceivedEvent) { count++ }
    companion object { var count = 0 }
}
