package com.beamable.push

import org.json.JSONObject
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner

/**
 * Covers the cross-platform [NotificationIntentData] schema (§3.3): parsing a flat
 * string→string FCM data map, stringified nested objects (offers / campaignData),
 * and the funnel gating predicates (§4.2).
 */
@RunWith(RobolectricTestRunner::class)
class NotificationIntentDataTest {

    @Test
    fun fromDataMap_parsesScalarsAndKeepsNestedAsJsonStrings() {
        val data = mapOf(
            "campaignId" to "camp-1",
            "nodeId" to "node-7",
            "gamerTag" to "12345",
            "accountId" to "acc-9",
            "cidPid" to "CID.PID",
            "deeplink" to "game://reward/42",
            "offers" to """[{"itemId":"sword","value":"100","customData":{"rarity":"gold"}}]""",
            "campaignData" to """{"season":"spring"}"""
        )

        val intent = NotificationIntentData.fromDataMap(data)

        assertEquals("camp-1", intent.campaignId)
        assertEquals("node-7", intent.nodeId)
        assertEquals("12345", intent.gamerTag)
        assertEquals("acc-9", intent.accountId)
        assertEquals("CID.PID", intent.cidPid)
        assertEquals("game://reward/42", intent.deeplink)

        val offers = intent.offers()
        assertEquals(1, offers.size)
        assertEquals("sword", offers[0].itemId)
        assertEquals("100", offers[0].rawValue)
        assertEquals("gold", JSONObject(offers[0].customDataJson!!).getString("rarity"))

        assertEquals("spring", intent.campaignData()["season"])
    }

    @Test
    fun fromDataMap_treatsEmptyValuesAsNull() {
        val intent = NotificationIntentData.fromDataMap(
            mapOf("campaignId" to "", "nodeId" to null)
        )
        assertNull(intent.campaignId)
        assertNull(intent.nodeId)
        assertTrue(intent.offers().isEmpty())
        assertTrue(intent.campaignData().isEmpty())
    }

    @Test
    fun isTrackedCampaign_requiresBothCampaignAndNode() {
        assertTrue(
            NotificationIntentData(campaignId = "c", nodeId = "n").isTrackedCampaign()
        )
        assertFalse(NotificationIntentData(campaignId = "c").isTrackedCampaign())
        assertFalse(NotificationIntentData(nodeId = "n").isTrackedCampaign())
        assertFalse(NotificationIntentData().isTrackedCampaign())
    }

    @Test
    fun hasFunnelCredentials_requiresScopeAndGamerTag() {
        assertTrue(
            NotificationIntentData(cidPid = "c.p", gamerTag = "1").hasFunnelCredentials()
        )
        assertFalse(NotificationIntentData(cidPid = "c.p").hasFunnelCredentials())
        assertFalse(NotificationIntentData(gamerTag = "1").hasFunnelCredentials())
    }

    @Test
    fun fromJson_roundTripsFromDataJsonString() {
        val json = """
            {"campaignId":"c","nodeId":"n","gamerTag":"1","cidPid":"a.b",
             "offers":"[{\"itemId\":\"x\",\"value\":5}]","deeplink":"app://x"}
        """.trimIndent()

        val intent = NotificationIntentData.fromJson(json)

        assertEquals("c", intent.campaignId)
        assertEquals("n", intent.nodeId)
        assertEquals("app://x", intent.deeplink)
        // value arrived as a JSON number; surfaced as its string form.
        assertEquals("5", intent.offers().first().rawValue)
    }

    @Test
    fun offer_numericValue_surfacedAsString() {
        val offer = NotificationOffer.fromJson(JSONObject("""{"itemId":"x","value":42}"""))
        assertEquals("42", offer.rawValue)
    }
}
