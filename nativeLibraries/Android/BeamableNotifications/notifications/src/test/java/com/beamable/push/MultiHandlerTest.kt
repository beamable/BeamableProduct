package com.beamable.push

import android.content.Context
import androidx.test.core.app.ApplicationProvider
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner

/**
 * Covers §1.1 multi-handler dispatch: every registered [PushNotificationReceivedHandler]
 * receives the event, a throwing handler does not block the others, and add/remove semantics
 * behave as specified.
 */
@RunWith(RobolectricTestRunner::class)
class MultiHandlerTest {

    private val context: Context get() = ApplicationProvider.getApplicationContext()

    private class RecordingHandler(val name: String, val throwOnReceive: Boolean = false) :
        PushNotificationReceivedHandler {
        val received = mutableListOf<String>()
        override fun onNotificationReceived(context: Context, event: PushReceivedEvent) {
            received += event.messageId ?: ""
            if (throwOnReceive) throw RuntimeException("boom from $name")
        }
    }

    private fun event() = PushReceivedEvent(
        messageId = "m1",
        dataJson = "{}",
        sentTimeMillis = 0,
        receivedTimeMillis = 0,
        wasForeground = false,
        deepLink = null
    )

    @After
    fun tearDown() {
        // Reset the shared singleton state between tests.
        PushManager.resetHandlersForTest()
    }

    @Test
    fun dispatchesToEveryRegisteredHandler() {
        val a = RecordingHandler("a")
        val b = RecordingHandler("b")
        PushManager.addNotificationReceivedHandler(a)
        PushManager.addNotificationReceivedHandler(b)

        PushManager.dispatchNotificationReceived(context, event())

        assertEquals(listOf("m1"), a.received)
        assertEquals(listOf("m1"), b.received)
    }

    @Test
    fun oneThrowingHandlerDoesNotBlockOthers() {
        val a = RecordingHandler("a", throwOnReceive = true)
        val b = RecordingHandler("b")
        PushManager.addNotificationReceivedHandler(a)
        PushManager.addNotificationReceivedHandler(b)

        PushManager.dispatchNotificationReceived(context, event())

        assertEquals(listOf("m1"), a.received)
        // b still ran despite a throwing.
        assertEquals(listOf("m1"), b.received)
    }

    @Test
    fun removeHandler_stopsDispatch() {
        val a = RecordingHandler("a")
        PushManager.addNotificationReceivedHandler(a)
        PushManager.removeNotificationReceivedHandler(a)

        PushManager.dispatchNotificationReceived(context, event())

        assertTrue(a.received.isEmpty())
    }

    @Test
    fun addHandler_ignoresDuplicates() {
        val a = RecordingHandler("a")
        PushManager.addNotificationReceivedHandler(a)
        PushManager.addNotificationReceivedHandler(a)

        PushManager.dispatchNotificationReceived(context, event())

        assertEquals(1, a.received.size)
    }

    @Test
    fun dispatchUsesProvidedStage_forThrowingHandler() {
        // TASK A: the dispatch loop routes a throwing handler's failure to the supplied stage.
        val errors = mutableListOf<Pair<String, String>>()
        val previous = PushManager.listener
        PushManager.listener = object : NoopListener() {
            override fun onError(stage: String, message: String) {
                errors += stage to message
            }
        }
        try {
            PushManager.addNotificationReceivedHandler(RecordingHandler("a", throwOnReceive = true))
            PushManager.dispatchNotificationReceived(
                context, event(), stage = "local_notification_received"
            )
            assertEquals(1, errors.size)
            assertEquals("local_notification_received", errors.first().first)
        } finally {
            PushManager.listener = previous
        }
    }

    // ---- Combined-handler cache invalidation (efficiency fix) ----------------
    // resolveHandlers() caches the combined list and only recomputes when the handler set
    // changes. These tests dispatch FIRST (priming the cache), then mutate the set, and assert
    // the next dispatch reflects the change — i.e. the cache was invalidated, not stale.

    @Test
    fun cacheInvalidates_whenHandlerAddedAfterFirstDispatch() {
        val a = RecordingHandler("a")
        PushManager.addNotificationReceivedHandler(a)
        // Prime the cache with just {a}.
        PushManager.dispatchNotificationReceived(context, event())
        assertEquals(1, a.received.size)

        // Add b AFTER the cache was built; the next dispatch must include it.
        val b = RecordingHandler("b")
        PushManager.addNotificationReceivedHandler(b)
        PushManager.dispatchNotificationReceived(context, event())

        assertEquals(2, a.received.size)
        assertEquals(1, b.received.size)
    }

    @Test
    fun cacheInvalidates_whenHandlerRemovedAfterFirstDispatch() {
        val a = RecordingHandler("a")
        val b = RecordingHandler("b")
        PushManager.addNotificationReceivedHandler(a)
        PushManager.addNotificationReceivedHandler(b)
        // Prime the cache with {a, b}.
        PushManager.dispatchNotificationReceived(context, event())
        assertEquals(1, a.received.size)
        assertEquals(1, b.received.size)

        // Remove b AFTER the cache was built; the next dispatch must NOT reach it.
        PushManager.removeNotificationReceivedHandler(b)
        PushManager.dispatchNotificationReceived(context, event())

        assertEquals(2, a.received.size)
        assertEquals(1, b.received.size) // unchanged — b no longer dispatched
    }

    @Test
    fun cachedResult_isStableWhenSetUnchanged() {
        // Repeated dispatches with no set change must hit every handler each time (cache reuse
        // must not drop or duplicate handlers).
        val a = RecordingHandler("a")
        val b = RecordingHandler("b")
        PushManager.addNotificationReceivedHandler(a)
        PushManager.addNotificationReceivedHandler(b)

        repeat(3) { PushManager.dispatchNotificationReceived(context, event()) }

        assertEquals(3, a.received.size)
        assertEquals(3, b.received.size)
    }

    /** No-op [PushListener] base so tests can override only the callbacks they care about. */
    private open class NoopListener : PushListener {
        override fun onTokenReceived(token: String) {}
        override fun onTokenRefreshError(error: String) {}
        override fun onMessageReceivedForeground(messageJson: String) {}
        override fun onNotificationOpened(dataJson: String) {}
        override fun onPermissionResult(granted: Boolean) {}
        override fun onLocalNotificationScheduled(id: Int) {}
        override fun onError(stage: String, message: String) {}
    }
}
