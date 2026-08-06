package com.beamable.push

import android.app.NotificationManager
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner

/**
 * Light checks on the engine-boundary data classes, mirroring the trivial iOS model
 * assertions. Robolectric supplies the real `NotificationManager.IMPORTANCE_*` constants.
 */
@RunWith(RobolectricTestRunner::class)
class DataClassTest {

    @Test
    fun pushReceivedEvent_exposesAllFields() {
        val event = PushReceivedEvent(
            messageId = "promo-42",
            dataJson = """{"deeplink":"game://reward/42"}""",
            sentTimeMillis = 1000L,
            receivedTimeMillis = 2000L,
            wasForeground = false,
            deepLink = "game://reward/42"
        )

        assertEquals("promo-42", event.messageId)
        assertEquals(1000L, event.sentTimeMillis)
        assertEquals(2000L, event.receivedTimeMillis)
        assertEquals(false, event.wasForeground)
        assertEquals("game://reward/42", event.deepLink)
    }

    @Test
    fun pushReceivedEvent_allowsNullOptionalFields() {
        val event = PushReceivedEvent(
            messageId = null,
            dataJson = "{}",
            sentTimeMillis = 0L,
            receivedTimeMillis = 0L,
            wasForeground = true,
            deepLink = null
        )

        assertNull(event.messageId)
        assertNull(event.deepLink)
    }

    @Test
    fun notificationChannelSpec_defaultsToImportanceHigh() {
        val spec = NotificationChannelSpec(id = "c", name = "Channel", description = "desc")
        assertEquals(NotificationManager.IMPORTANCE_HIGH, spec.importance)
    }

    @Test
    fun notificationChannelSpec_honorsExplicitImportance() {
        val spec = NotificationChannelSpec(
            id = "c",
            name = "Channel",
            description = "desc",
            importance = NotificationManager.IMPORTANCE_LOW
        )
        assertEquals(NotificationManager.IMPORTANCE_LOW, spec.importance)
    }
}
