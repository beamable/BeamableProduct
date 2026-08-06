package com.beamable.deeplink

import android.app.Activity
import android.content.Intent
import android.net.Uri
import org.junit.Assert.assertEquals
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.Robolectric
import org.robolectric.RobolectricTestRunner

/**
 * Covers [ActivityIntentObserver] cold-start dedupe: the launch intent is observed in
 * both `onActivityCreated` and `onActivityResumed` (and survives re-resumes), so the
 * same URL must be delivered only once until [ActivityIntentObserver.clear].
 */
@RunWith(RobolectricTestRunner::class)
class ActivityIntentObserverTest {

    private fun viewActivity(url: String): Activity {
        val intent = Intent(Intent.ACTION_VIEW, Uri.parse(url))
        return Robolectric.buildActivity(Activity::class.java, intent).get()
    }

    @Test
    fun deliversColdStartUrlOnlyOnce_acrossRepeatedLifecycleCallbacks() {
        val delivered = mutableListOf<Pair<String, Boolean>>()
        val observer = ActivityIntentObserver { url, isColdStart -> delivered += url to isColdStart }
        val activity = viewActivity("game://reward/42")

        observer.onActivityCreated(activity, null)
        observer.onActivityResumed(activity)
        observer.onActivityResumed(activity)

        assertEquals(listOf("game://reward/42" to true), delivered)
    }

    @Test
    fun clear_allowsSameUrlToBeDeliveredAgain() {
        val delivered = mutableListOf<String>()
        val observer = ActivityIntentObserver { url, _ -> delivered += url }
        val activity = viewActivity("game://reward/42")

        observer.onActivityCreated(activity, null)
        observer.clear()
        observer.onActivityResumed(activity)

        assertEquals(listOf("game://reward/42", "game://reward/42"), delivered)
    }

    @Test
    fun doesNotDeliver_whenIntentHasNoDeepLink() {
        val delivered = mutableListOf<String>()
        val observer = ActivityIntentObserver { url, _ -> delivered += url }
        val activity = Robolectric.buildActivity(Activity::class.java).get()

        observer.onActivityCreated(activity, null)
        observer.onActivityResumed(activity)

        assertEquals(emptyList<String>(), delivered)
    }
}
