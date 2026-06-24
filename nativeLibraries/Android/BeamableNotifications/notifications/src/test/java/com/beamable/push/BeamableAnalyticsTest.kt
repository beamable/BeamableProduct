package com.beamable.push

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
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
}
