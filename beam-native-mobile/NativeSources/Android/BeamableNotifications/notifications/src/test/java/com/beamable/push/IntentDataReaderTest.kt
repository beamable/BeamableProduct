package com.beamable.push

import android.content.Intent
import org.json.JSONObject
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner

/**
 * Covers [IntentDataReader] — reading a notification tap payload back out of an Intent
 * and consuming it exactly once. Robolectric supplies real `Intent` / `Bundle` / `org.json`.
 */
@RunWith(RobolectricTestRunner::class)
class IntentDataReaderTest {

    private fun markedIntent() = Intent().putExtra("beamable_notification", "1")

    @Test
    fun readIntent_returnsPrebuiltPayloadJson() {
        val intent = markedIntent().putExtra("beamable_payload_json", """{"deeplink":"app://home"}""")

        assertEquals("""{"deeplink":"app://home"}""", IntentDataReader.readIntent(intent))
    }

    @Test
    fun readIntent_assemblesJsonFromStringExtras_whenNoPrebuiltJson() {
        val intent = markedIntent()
            .putExtra("deeplink", "app://home")
            .putExtra("title", "Hi")

        val obj = JSONObject(IntentDataReader.readIntent(intent)!!)

        assertEquals("app://home", obj.getString("deeplink"))
        assertEquals("Hi", obj.getString("title"))
        // The internal markers must not leak into the assembled payload.
        assertEquals(false, obj.has("beamable_notification"))
        assertEquals(false, obj.has("beamable_payload_json"))
    }

    @Test
    fun readIntent_returnsNull_whenMarkerAbsent() {
        val intent = Intent().putExtra("deeplink", "app://home")
        assertNull(IntentDataReader.readIntent(intent))
    }

    @Test
    fun readIntent_returnsNull_forNullIntent() {
        assertNull(IntentDataReader.readIntent(null))
    }

    @Test
    fun readIntent_consumesPayloadOnlyOnce() {
        val intent = markedIntent().putExtra("beamable_payload_json", """{"x":"1"}""")

        assertEquals("""{"x":"1"}""", IntentDataReader.readIntent(intent))
        // Second read sees the cleared marker → null (a later resume must not re-fire).
        assertNull(IntentDataReader.readIntent(intent))
    }
}
