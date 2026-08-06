package com.beamable.push

import android.content.Context
import androidx.test.core.app.ApplicationProvider
import com.beamable.deeplink.DeepLinkNormalizer
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.Shadows.shadowOf

/**
 * Verifies the SDK completes a remote push's deep link into a full `<scheme>://…` URL before
 * it reaches the engine — covering the two incomplete shapes campaigns send in the wild:
 * a schemeless value under the canonical key, and the app scheme prepended to the key itself.
 */
@RunWith(RobolectricTestRunner::class)
class DeepLinkNormalizerTest {

    private val context: Context get() = ApplicationProvider.getApplicationContext()

    @After
    fun tearDown() {
        DeepLinkNormalizer.resetSchemeCacheForTest()
    }

    private fun setScheme(scheme: String) {
        val pm = shadowOf(context.packageManager)
        val ai = context.packageManager.getApplicationInfo(
            context.packageName,
            android.content.pm.PackageManager.GET_META_DATA
        )
        val bundle = ai.metaData ?: android.os.Bundle().also { ai.metaData = it }
        bundle.putString(DeepLinkNormalizer.META_DEEPLINK_SCHEME, scheme)
        pm.getInternalMutablePackageInfo(context.packageName).applicationInfo.metaData = bundle
        DeepLinkNormalizer.resetSchemeCacheForTest()
    }

    // ---- The two real campaign payloads from the wire -----------------------

    @Test
    fun schemelessValue_underCanonicalKey_isCompletedFromManifestScheme() {
        setScheme("beamrnsample")
        val data = mapOf(
            "campaignId" to "test2",
            "nodeId" to "test2",
            "title" to "Test Campaign",
            "deeplink" to "details/55"
        )
        val out = DeepLinkNormalizer.normalizeDataMap(context, data)
        assertEquals("beamrnsample://details/55", out["deeplink"])
    }

    @Test
    fun schemePrefixedKey_isFoldedIntoCanonicalKey_usingSchemeFromKey() {
        // No manifest scheme needed: the scheme rides on the key.
        val data = mapOf(
            "campaignId" to "test2",
            "nodeId" to "test2",
            "title" to "Test Campaign",
            "beamrnsample://deeplink" to "details/55"
        )
        val out = DeepLinkNormalizer.normalizeDataMap(context, data)
        assertEquals("beamrnsample://details/55", out["deeplink"])
        // The malformed key is collapsed away so only the canonical one remains.
        assertFalse(out.containsKey("beamrnsample://deeplink"))
    }

    // ---- Idempotency & graceful degradation ---------------------------------

    @Test
    fun alreadyFullUrl_isLeftUnchanged() {
        setScheme("beamrnsample")
        val data = mapOf("deeplink" to "beamrnsample://details/42")
        val out = DeepLinkNormalizer.normalizeDataMap(context, data)
        assertEquals("beamrnsample://details/42", out["deeplink"])
    }

    @Test
    fun schemeless_withNoSchemeAvailable_passesThroughUnchanged() {
        // No manifest scheme, canonical key → cannot complete; never worse than before.
        val data = mapOf("deeplink" to "details/55")
        val out = DeepLinkNormalizer.normalizeDataMap(context, data)
        assertEquals("details/55", out["deeplink"])
    }

    @Test
    fun noDeepLink_returnsMapUnchanged() {
        val data = mapOf("campaignId" to "test2", "nodeId" to "test2")
        val out = DeepLinkNormalizer.normalizeDataMap(context, data)
        assertEquals(data, out)
    }

    // ---- normalize() unit cases ---------------------------------------------

    @Test
    fun normalize_prependsScheme_strippingLeadingSlashes() {
        assertEquals("beamrnsample://details/55", DeepLinkNormalizer.normalize("/details/55", "beamrnsample"))
        assertEquals("beamrnsample://x", DeepLinkNormalizer.normalize("x", "beamrnsample"))
    }

    @Test
    fun normalize_leavesSchemedAndNullSchemeAlone() {
        assertEquals("https://x.com", DeepLinkNormalizer.normalize("https://x.com", "beamrnsample"))
        assertEquals("details/55", DeepLinkNormalizer.normalize("details/55", null))
    }
}
