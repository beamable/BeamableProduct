package com.beamable.deeplink

import android.content.Intent
import android.net.Uri
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner

/**
 * Mirrors the iOS deep-link extraction test (`testDeepLinkLiftedFromUserInfo`).
 *
 * Note: the iOS key-variant test (`deepLink`/`deeplink`/`deep_link`) has no Android
 * equivalent — Android uses the single key `"deeplink"` consistently, and the deeplink
 * here is the ACTION_VIEW intent's data URI, not a payload key.
 */
@RunWith(RobolectricTestRunner::class)
class IntentDeepLinkExtractorTest {

    @Test
    fun extract_returnsDataUri_forViewIntentWithData() {
        val intent = Intent(Intent.ACTION_VIEW, Uri.parse("game://reward/42"))
        assertEquals("game://reward/42", IntentDeepLinkExtractor.extract(intent))
    }

    @Test
    fun extract_returnsNull_forNullIntent() {
        assertNull(IntentDeepLinkExtractor.extract(null))
    }

    @Test
    fun extract_returnsNull_forNonViewAction() {
        val intent = Intent(Intent.ACTION_MAIN, Uri.parse("game://reward/42"))
        assertNull(IntentDeepLinkExtractor.extract(intent))
    }

    @Test
    fun extract_returnsNull_forViewIntentWithoutData() {
        val intent = Intent(Intent.ACTION_VIEW)
        assertNull(IntentDeepLinkExtractor.extract(intent))
    }
}
