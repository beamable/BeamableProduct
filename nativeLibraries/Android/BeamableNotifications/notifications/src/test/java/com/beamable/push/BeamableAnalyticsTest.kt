package com.beamable.push

import android.content.Context
import androidx.test.core.app.ApplicationProvider
import org.json.JSONArray
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
 * single-column `offerData` JSON-array + `campaignData` emission rules.
 */
@RunWith(RobolectricTestRunner::class)
class BeamableAnalyticsTest {

    private fun trackedIntent(
        offersJson: String? = null,
        campaignDataJson: String? = null
    ) = NotificationIntentData(
        campaignId = "camp-1",
        nodeId = "node-7",
        gamerTag = "12345",
        accountId = "acc-9",
        cidPid = "CID.PID",
        offersJson = offersJson,
        campaignDataJson = campaignDataJson,
        deeplink = "game://x"
    )

    @Test
    fun buildCoreEvent_hasCoreShape() {
        val event = BeamableAnalytics.buildCoreEvent(
            trackedIntent(), BeamableAnalytics.FunnelType.Received
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
        // No offers/campaignData on the intent → both omitted.
        assertFalse(p.has("offerData"))
        assertFalse(p.has("campaignData"))
    }

    @Test
    fun buildCoreEvent_emitsOfferDataArrayAndCampaignData() {
        val offers = """[{"itemId":"sword","value":"1"},{"itemId":"shield","value":2}]"""
        val campaignData = """{"season":"summer"}"""
        val event = BeamableAnalytics.buildCoreEvent(
            trackedIntent(offersJson = offers, campaignDataJson = campaignData),
            BeamableAnalytics.FunnelType.Received
        )
        val p = event.getJSONObject("p")
        // offerData is the verbatim wire array string — a single flat column.
        assertEquals(offers, p.getString("offerData"))
        val arr = JSONArray(p.getString("offerData"))
        assertEquals(2, arr.length())
        assertEquals("sword", arr.getJSONObject(0).getString("itemId"))
        assertEquals(campaignData, p.getString("campaignData"))
    }

    @Test
    fun buildParams_emitsKeysAlphabetically() {
        // All platforms emit the funnel params in alphabetical key order (iOS via .sortedKeys,
        // microservice via SortedDictionary) so the funnel JSON matches across the board.
        val p = BeamableAnalytics.buildCoreEvent(
            trackedIntent(offersJson = """[{"itemId":"x"}]""", campaignDataJson = """{"k":"v"}"""),
            BeamableAnalytics.FunnelType.Received
        ).getJSONObject("p")
        val keys = p.keys().asSequence().toList()
        assertEquals(keys.sorted(), keys)
        // Sanity: the expected sorted set is present.
        assertEquals(
            listOf("accountId", "campaignData", "campaignId", "cidPid", "deeplink", "funnelType", "gamerTag", "nodeId", "offerData"),
            keys,
        )
    }

    @Test
    fun stageEvents_carryAllCarriedOffers() {
        // Received/Opened/Sent now carry every offer the push held (built in PendingFunnel.from),
        // not blank — fixing the funnel inconsistency with the microservice "Sent" event.
        val offers = """[{"itemId":"sword","value":"1"},{"itemId":"shield","value":"2"}]"""
        for (type in listOf(
            BeamableAnalytics.FunnelType.Received,
            BeamableAnalytics.FunnelType.Opened,
            BeamableAnalytics.FunnelType.Sent
        )) {
            val e = BeamableAnalytics.PendingFunnel.from(
                trackedIntent(offersJson = offers), type, null
            )
            val p = BeamableAnalytics.buildCoreEvent(e.toIntentData(), e.funnelType)
                .getJSONObject("p")
            assertEquals(2, JSONArray(p.getString("offerData")).length())
        }
    }

    @Test
    fun clickedEvent_carriesSingleConcernedOfferAsOneElementArray() {
        val offer = NotificationOffer(itemId = "gem", rawValue = "5", customDataJson = """{"k":"v"}""")
        // Clicked passes an explicit single offer; even if the campaign carried others, only the
        // concerned one is attached (as a one-element array).
        val e = BeamableAnalytics.PendingFunnel.from(
            trackedIntent(offersJson = """[{"itemId":"sword","value":"1"}]"""),
            BeamableAnalytics.FunnelType.Clicked,
            offer
        )
        val p = BeamableAnalytics.buildCoreEvent(e.toIntentData(), e.funnelType).getJSONObject("p")
        val arr = JSONArray(p.getString("offerData"))
        assertEquals(1, arr.length())
        assertEquals("gem", arr.getJSONObject(0).getString("itemId"))
        // customData stays a STRINGIFIED JSON string (Athena-safe), not a nested object.
        val customDataStr = arr.getJSONObject(0).getString("customData")
        assertEquals("v", org.json.JSONObject(customDataStr).getString("k"))
    }

    @Test
    fun buildBatch_isJsonArrayOfOne() {
        val batch = BeamableAnalytics.buildBatch(
            trackedIntent(), BeamableAnalytics.FunnelType.Opened
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
        offer: NotificationOffer? = null,
        offersJson: String? = null,
        campaignDataJson: String? = null
    ) = BeamableAnalytics.PendingFunnel.from(
        trackedIntent(offersJson = offersJson, campaignDataJson = campaignDataJson), type, offer
    )

    @Test
    fun appendThenDrain_roundTripsFields() {
        val offers = """[{"itemId":"sword","value":"1"}]"""
        val campaignData = """{"season":"summer"}"""
        BeamableAnalytics.appendPendingFunnel(
            context, pending(offersJson = offers, campaignDataJson = campaignData)
        )
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
        // offers + campaignData survive the persist→replay round-trip (offers re-serialized
        // through toAnalyticsArray, so compare structurally rather than by exact string).
        assertEquals("sword", JSONArray(e.offersJson).getJSONObject(0).getString("itemId"))
        assertEquals(campaignData, e.campaignDataJson)
        // The persisted event rebuilds a usable CoreEvent body that carries both.
        val core = BeamableAnalytics.buildCoreEvent(e.toIntentData(), e.funnelType)
        assertEquals("Received", core.getString("e"))
        val p = core.getJSONObject("p")
        assertEquals("camp-1", p.getString("campaignId"))
        assertEquals("sword", JSONArray(p.getString("offerData")).getJSONObject(0).getString("itemId"))
        assertEquals(campaignData, p.getString("campaignData"))
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
        // Same funnelType|campaignId|nodeId|gamerTag|offersJson → second append is ignored.
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
        // Distinct keys via the offer itemId (→ distinct offersJson) so none dedup away.
        for (i in 0 until 250) {
            BeamableAnalytics.appendPendingFunnel(
                context,
                pending(offer = NotificationOffer(itemId = "item-$i", rawValue = "1", customDataJson = null))
            )
        }
        val stored = BeamableAnalytics.loadPendingFunnel(context)
        assertEquals(200, stored.size)
        // Oldest (item-0 .. item-49) trimmed; item-49 gone, item-50 retained, item-249 newest.
        assertFalse(stored.any { it.offersJson?.contains("\"item-49\"") == true })
        assertTrue(stored.any { it.offersJson?.contains("\"item-50\"") == true })
        assertTrue(stored.any { it.offersJson?.contains("\"item-249\"") == true })
    }
}
