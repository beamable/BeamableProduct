package com.beamable.push

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner

/**
 * Mirrors the iOS JSON round-trip and engine-JSON decode tests
 * (`testJSONValueRoundTrip`, `testLocalRequestDecodesFromEngineJSON`,
 * `testTemplateResolveDoesNotOverrideExplicit`).
 *
 * Uses Robolectric because [NotificationTemplate] serializes via `org.json.JSONObject`,
 * which is stubbed under plain JUnit.
 */
@RunWith(RobolectricTestRunner::class)
class NotificationTemplateTest {

    @Test
    fun toJson_thenFromJson_roundTripsAllFields() {
        val original = NotificationTemplate(
            id = 7,
            title = "Hi",
            body = "There",
            smallIconResName = "icon_0",
            largeIconResName = "icon_large",
            channelId = "default",
            dataPayload = linkedMapOf("k1" to "v1", "k2" to "v2"),
            deepLinkUrl = "game://reward/42"
        )

        val decoded = NotificationTemplate.fromJson(original.toJson())

        assertEquals(original.id, decoded.id)
        assertEquals(original.title, decoded.title)
        assertEquals(original.body, decoded.body)
        assertEquals(original.smallIconResName, decoded.smallIconResName)
        assertEquals(original.largeIconResName, decoded.largeIconResName)
        assertEquals(original.channelId, decoded.channelId)
        assertEquals(original.dataPayload, decoded.dataPayload)
        assertEquals(original.deepLinkUrl, decoded.deepLinkUrl)
    }

    @Test
    fun fromJson_appliesDefaultsForMissingOptionalFields() {
        // Mirrors testLocalRequestDecodesFromEngineJSON: a minimal engine JSON blob
        // decodes with sensible defaults for everything not specified.
        val json = """{"id":3,"title":"Hi","body":"There","channelId":"c"}"""

        val decoded = NotificationTemplate.fromJson(json)

        assertEquals(3, decoded.id)
        assertEquals("Hi", decoded.title)
        assertEquals("There", decoded.body)
        assertEquals("c", decoded.channelId)
        assertNull(decoded.smallIconResName)
        assertNull(decoded.largeIconResName)
        assertNull(decoded.deepLinkUrl)
        assertTrue(decoded.dataPayload.isEmpty())
    }

    @Test
    fun fromJson_treatsExplicitNullAndEmptyOptionalsAsNull() {
        val json = """
            {"id":0,"title":"","body":"","channelId":"",
             "smallIconResName":null,"largeIconResName":"","deepLinkUrl":null}
        """.trimIndent()

        val decoded = NotificationTemplate.fromJson(json)

        assertEquals(0, decoded.id)
        assertNull(decoded.smallIconResName)
        assertNull(decoded.largeIconResName)
        assertNull(decoded.deepLinkUrl)
    }

    @Test
    fun effectivePayload_mergesDeepLinkUnderDeeplinkKey() {
        val template = NotificationTemplate(
            title = "t",
            body = "b",
            channelId = "c",
            dataPayload = mapOf("foo" to "bar"),
            deepLinkUrl = "game://store"
        )

        val payload = template.effectivePayload()

        assertEquals("bar", payload["foo"])
        assertEquals("game://store", payload[NotificationTemplate.KEY_DEEPLINK])
    }

    @Test
    fun effectivePayload_doesNotOverrideExplicitDeeplink() {
        // Mirrors iOS "does not override explicit": an explicit "deeplink" in the
        // payload wins over the convenience deepLinkUrl.
        val template = NotificationTemplate(
            title = "t",
            body = "b",
            channelId = "c",
            dataPayload = mapOf(NotificationTemplate.KEY_DEEPLINK to "game://explicit"),
            deepLinkUrl = "game://convenience"
        )

        assertEquals("game://explicit", template.effectivePayload()[NotificationTemplate.KEY_DEEPLINK])
    }

    @Test
    fun effectivePayload_returnsOriginalWhenDeepLinkUrlNullOrEmpty() {
        val base = mapOf("foo" to "bar")

        val nullLink = NotificationTemplate(
            title = "t", body = "b", channelId = "c", dataPayload = base, deepLinkUrl = null
        )
        assertEquals(base, nullLink.effectivePayload())
        assertFalse(nullLink.effectivePayload().containsKey(NotificationTemplate.KEY_DEEPLINK))

        val emptyLink = NotificationTemplate(
            title = "t", body = "b", channelId = "c", dataPayload = base, deepLinkUrl = ""
        )
        assertEquals(base, emptyLink.effectivePayload())
    }
}
