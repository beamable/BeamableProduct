package com.beamable.push

import android.content.Context
import androidx.test.core.app.ApplicationProvider
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner

/**
 * Covers the CoreEvent JSON builder (§4.6): op/category/eventName shape, params, and the
 * single-offer selection rule.
 */
@RunWith(RobolectricTestRunner::class)
class BeamableAnalyticsTest {

    private fun trackedIntent(offersJson: String? = null) = NotificationIntentData(
        campaignId = "camp-1",
        nodeId = "node-7",
        gamerTag = "12345",
        accountId = "acc-9",
        cidPid = "CID.PID",
        offersJson = offersJson,
        deeplink = "game://x"
    )

    @Test
    fun buildCoreEvent_hasCoreShape() {
        val event = BeamableAnalytics.buildCoreEvent(
            trackedIntent(), BeamableAnalytics.FunnelType.Received, null
        )

        assertEquals("g.core", event.getString("op"))
        assertEquals("Received", event.getString("e"))
        assertEquals("notification_funnel", event.getString("c"))

        val p = event.getJSONObject("p")
        assertEquals("camp-1", p.getString("campaignId"))
        assertEquals("node-7", p.getString("nodeId"))
        assertEquals("12345", p.getString("gamerTag"))
        assertEquals("acc-9", p.getString("accountId"))
        assertEquals("CID.PID", p.getString("cidPid"))
        assertEquals("game://x", p.getString("deeplink"))
        assertEquals("Received", p.getString("funnelType"))
        // No offer present and none supplied → offerData omitted.
        assertFalse(p.has("offerData"))
    }

    @Test
    fun buildCoreEvent_explicitOfferWins() {
        val offer = NotificationOffer(itemId = "gem", rawValue = "5", customDataJson = null)
        val event = BeamableAnalytics.buildCoreEvent(
            trackedIntent(offersJson = """[{"itemId":"sword","value":"1"}]"""),
            BeamableAnalytics.FunnelType.Clicked,
            offer
        )
        val offerData = event.getJSONObject("p").getJSONObject("offerData")
        assertEquals("gem", offerData.getString("itemId"))
    }

    @Test
    fun buildCoreEvent_noOfferDataWhenNotExplicit() {
        // FIX 3: offerData is attached ONLY when an offer is explicitly passed. Received/Opened/Sent
        // (offer=null) must NOT fall back to the first carried offer (mis-attribution).
        for (type in listOf(
            BeamableAnalytics.FunnelType.Received,
            BeamableAnalytics.FunnelType.Opened,
            BeamableAnalytics.FunnelType.Sent
        )) {
            val event = BeamableAnalytics.buildCoreEvent(
                trackedIntent(offersJson = """[{"itemId":"sword","value":"1"},{"itemId":"shield","value":"2"}]"""),
                type,
                null
            )
            assertFalse(event.getJSONObject("p").has("offerData"))
        }
    }

    @Test
    fun buildBatch_isJsonArrayOfOne() {
        val batch = BeamableAnalytics.buildBatch(
            trackedIntent(), BeamableAnalytics.FunnelType.Opened, null
        )
        assertEquals(1, batch.length())
        assertEquals("Opened", batch.getJSONObject(0).getString("e"))
    }

    // ---- C1: persist-and-replay -------------------------------------------------

    private val context: Context get() = ApplicationProvider.getApplicationContext()

    @After
    fun clearPending() {
        BeamableAnalytics.drainPendingFunnel(context)
    }

    private fun pending(
        type: BeamableAnalytics.FunnelType = BeamableAnalytics.FunnelType.Received,
        offer: NotificationOffer? = null
    ) = BeamableAnalytics.PendingFunnel.from(trackedIntent(), type, offer)

    @Test
    fun appendThenDrain_roundTripsFields() {
        BeamableAnalytics.appendPendingFunnel(context, pending())
        val drained = BeamableAnalytics.drainPendingFunnel(context)

        assertEquals(1, drained.size)
        val e = drained.first()
        assertEquals(BeamableAnalytics.FunnelType.Received, e.funnelType)
        assertEquals("camp-1", e.campaignId)
        assertEquals("node-7", e.nodeId)
        assertEquals("12345", e.gamerTag)
        assertEquals("acc-9", e.accountId)
        assertEquals("CID.PID", e.cidPid)
        assertEquals("game://x", e.deeplink)
        // The persisted event rebuilds a usable CoreEvent body.
        val core = BeamableAnalytics.buildCoreEvent(e.toIntentData(), e.funnelType, e.offer)
        assertEquals("Received", core.getString("e"))
        assertEquals("camp-1", core.getJSONObject("p").getString("campaignId"))
    }

    @Test
    fun drain_clearsStore() {
        BeamableAnalytics.appendPendingFunnel(context, pending())
        assertEquals(1, BeamableAnalytics.drainPendingFunnel(context).size)
        // Second drain sees nothing — the store was cleared.
        assertTrue(BeamableAnalytics.drainPendingFunnel(context).isEmpty())
    }

    @Test
    fun append_dedupsByKey() {
        // Same funnelType|campaignId|nodeId|gamerTag|offerItemId → second append is ignored.
        BeamableAnalytics.appendPendingFunnel(context, pending())
        BeamableAnalytics.appendPendingFunnel(context, pending())
        assertEquals(1, BeamableAnalytics.loadPendingFunnel(context).size)

        // Different stage (Opened) is a distinct key → both kept.
        BeamableAnalytics.appendPendingFunnel(context, pending(BeamableAnalytics.FunnelType.Opened))
        assertEquals(2, BeamableAnalytics.loadPendingFunnel(context).size)
    }

    @Test
    fun append_dedupDistinguishesGamerTag() {
        // Two players' events on a shared device (offline account-switch) differ ONLY by
        // gamerTag — they must NOT dedup away each other; both are retained.
        val playerA = pending().copy(gamerTag = "11111")
        val playerB = pending().copy(gamerTag = "22222")
        assertNotEquals(playerA.dedupKey, playerB.dedupKey)

        BeamableAnalytics.appendPendingFunnel(context, playerA)
        BeamableAnalytics.appendPendingFunnel(context, playerB)
        assertEquals(2, BeamableAnalytics.loadPendingFunnel(context).size)
    }

    @Test
    fun append_dedupExcludesTimestamp() {
        val first = pending().copy(timestamp = 1L)
        val second = pending().copy(timestamp = 999L)
        BeamableAnalytics.appendPendingFunnel(context, first)
        BeamableAnalytics.appendPendingFunnel(context, second)
        // Same dedup key despite different timestamps → only one stored.
        assertEquals(1, BeamableAnalytics.loadPendingFunnel(context).size)
    }

    @Test
    fun append_capsAt200_trimmingOldest() {
        // Distinct keys via the offer itemId so none dedup away.
        for (i in 0 until 250) {
            BeamableAnalytics.appendPendingFunnel(
                context,
                pending(offer = NotificationOffer(itemId = "item-$i", rawValue = "1", customDataJson = null))
            )
        }
        val stored = BeamableAnalytics.loadPendingFunnel(context)
        assertEquals(200, stored.size)
        // Oldest (item-0 .. item-49) trimmed; item-49 gone, item-50 retained, item-249 newest.
        assertFalse(stored.any { it.offer?.itemId == "item-49" })
        assertTrue(stored.any { it.offer?.itemId == "item-50" })
        assertTrue(stored.any { it.offer?.itemId == "item-249" })
    }
}
