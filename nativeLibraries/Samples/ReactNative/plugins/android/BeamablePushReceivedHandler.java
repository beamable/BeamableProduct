package com.beamable.rnsample;

import android.content.Context;
import android.util.Log;

import com.beamable.push.PushNotificationReceivedHandler;
import com.beamable.push.PushReceivedEvent;

import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.charset.StandardCharsets;

/**
 * Receive-time push handler that posts a Slack webhook when a push arrives — the
 * Android-only feature the React Native sample adds on top of iOS.
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
 * Requirements: a public no-arg constructor (for reflection) and the INTERNET permission
 * (default in a React Native app). Runs on a background thread with a ~10s budget, so a
 * short blocking HTTP POST is fine.
 */
public class BeamablePushReceivedHandler implements PushNotificationReceivedHandler {

    private static final String TAG = "BeamableRNHook";

    // Test Slack workflow webhook trigger. Treat as a secret-ish token; fine for a sample,
    // rotate if it leaks. The trigger expects a JSON body with a single "message" field.
    private static final String WEBHOOK_URL =
        "https://hooks.slack.com/triggers/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";

    public BeamablePushReceivedHandler() {
    }

    @Override
    public void onNotificationReceived(Context context, PushReceivedEvent event) {
        try {
            postToSlack(buildContent(event));
        } catch (Throwable t) {
            Log.w(TAG, "Failed to post Slack webhook: " + t.getMessage());
        }
    }

    private String buildContent(PushReceivedEvent e) {
        return "**RN - Android**: Beamable push received (native handler)\n"
            + "messageId: " + e.getMessageId() + "\n"
            + "deepLink: " + e.getDeepLink() + "\n"
            + "wasForeground: " + e.getWasForeground() + "\n"
            + "receivedAt: " + e.getReceivedTimeMillis() + "\n"
            + "data: " + e.getDataJson();
    }

    private void postToSlack(String message) throws Exception {
        HttpURLConnection conn = (HttpURLConnection) new URL(WEBHOOK_URL).openConnection();
        try {
            conn.setRequestMethod("POST");
            conn.setConnectTimeout(8000);
            conn.setReadTimeout(8000);
            conn.setDoOutput(true);
            conn.setRequestProperty("Content-Type", "application/json; charset=utf-8");

            byte[] body = ("{\"message\":\"" + jsonEscape(message) + "\"}")
                .getBytes(StandardCharsets.UTF_8);
            try (OutputStream os = conn.getOutputStream()) {
                os.write(body);
            }

            int code = conn.getResponseCode();
            Log.i(TAG, "Slack webhook response: " + code);
        } finally {
            conn.disconnect();
        }
    }

    private static String jsonEscape(String s) {
        if (s == null) {
            return "";
        }
        StringBuilder b = new StringBuilder(s.length() + 16);
        for (int i = 0; i < s.length(); i++) {
            char c = s.charAt(i);
            switch (c) {
                case '"':  b.append("\\\""); break;
                case '\\': b.append("\\\\"); break;
                case '\n': b.append("\\n");  break;
                case '\r': b.append("\\r");  break;
                case '\t': b.append("\\t");  break;
                default:
                    if (c < 0x20) {
                        b.append(String.format("\\u%04x", (int) c));
                    } else {
                        b.append(c);
                    }
            }
        }
        return b.toString();
    }
}
