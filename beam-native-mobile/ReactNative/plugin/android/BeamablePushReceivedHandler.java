package com.beamable.rnsample;

import android.content.Context;
import android.util.Log;

import com.beamable.push.PushNotificationReceivedHandler;
import com.beamable.push.PushReceivedEvent;

/**
 * Sample receive-time push handler — demonstrates the Android-only native receive hook.
 *
 * It is instantiated by reflection from a background process (registered via the
 * AndroidManifest meta-data "com.beamable.push.notification_received_handler"), so it fires
 * even when the app is fully closed. There is NO React Native runtime running at that point
 * — which is why this must be native Java rather than a JS handler. The Expo config plugin
 * (plugins/withBeamableNotifications.js) copies this file into the generated
 * android/app/src/main/java/com/beamable/rnsample/ on every prebuild.
 *
 * Fires for:
 *   - LOCAL notifications (via NotificationActionReceiver — no Firebase needed), and
 *   - REMOTE data-only, high-priority FCM messages (requires google-services.json).
 * (A `notification`-block FCM message is auto-shown by the OS and only reaches the app on
 * tap, so use data-only messages to exercise the killed-app path.)
 *
 * Requirements: a public no-arg constructor (for reflection). Runs on a background thread.
 *
 * NOTE: this handler used to POST a demo Slack webhook to report delivery. That has been
 * removed — funnel analytics (Received/Opened/…) are now emitted natively by the Beamable
 * SDK via the Beamable analytics endpoint, so a game no longer wires up its own webhook.
 * Customize the body below to run your own per-game receive-time logic.
 */
public class BeamablePushReceivedHandler implements PushNotificationReceivedHandler {

    private static final String TAG = "BeamableRNHook";

    public BeamablePushReceivedHandler() {
    }

    @Override
    public void onNotificationReceived(Context context, PushReceivedEvent event) {
        // Example: log the received push. Replace with your own receive-time logic.
        Log.i(TAG, "Beamable push received (native handler)"
            + " messageId=" + event.getMessageId()
            + " deepLink=" + event.getDeepLink()
            + " wasForeground=" + event.getWasForeground()
            + " receivedAt=" + event.getReceivedTimeMillis()
            + " data=" + event.getDataJson());
    }
}
