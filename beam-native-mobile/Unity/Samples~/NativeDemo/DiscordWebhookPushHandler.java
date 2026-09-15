package com.beamable.sample;

import android.content.Context;
import android.util.Log;

import com.beamable.push.PushNotificationReceivedHandler;
import com.beamable.push.PushReceivedEvent;

import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.charset.StandardCharsets;

/**
 * Receive-time push handler that posts a Discord webhook when a push arrives.
 *
 * It is instantiated by reflection from the FCM background process (registered via the
 * AndroidManifest meta-data "com.beamable.push.notification_received_handler"), so it fires
 * even when the app is fully closed. There is NO Unity engine running at that point — which is
 * why this must be native Java rather than a C# script. It still lives in the Unity project:
 * Unity compiles .java files under Assets/Plugins/Android/ into the app.
 *
 * Only fires for REMOTE, data-only FCM messages (requires google-services.json + a data-only,
 * high-priority message). Local AlarmManager notifications do not go through FCM, so they do
 * not trigger this handler.
 *
 * Requirements: a public no-arg constructor (for reflection) and the INTERNET permission
 * (already declared in AndroidManifest.xml). Runs on a background thread with a ~10s budget,
 * so a short blocking HTTP POST is fine.
 */
public class DiscordWebhookPushHandler implements PushNotificationReceivedHandler {

    private static final String TAG = "BeamableDiscordHook";

    // Test webhook. Treat as a secret-ish token; fine for a sample, rotate if it leaks.
    private static final String WEBHOOK_URL =
        "https://canary.discord.com/api/webhooks/1517215291623608532/nuiDOyAaW3l4Ysn1UzUUAigEdH6SqrtECNNg3RkotAuoEELWkvwDiWmec_3zPH-eke7G";

    public DiscordWebhookPushHandler() {
    }

    @Override
    public void onNotificationReceived(Context context, PushReceivedEvent event) {
        try {
            postToDiscord(buildContent(event));
        } catch (Throwable t) {
            Log.w(TAG, "Failed to post Discord webhook: " + t.getMessage());
        }
    }

    private String buildContent(PushReceivedEvent e) {
        return "**Unity - Android**: Beamable push received (native handler)\n"
            + "messageId: " + e.getMessageId() + "\n"
            + "deepLink: " + e.getDeepLink() + "\n"
            + "wasForeground: " + e.getWasForeground() + "\n"
            + "receivedAt: " + e.getReceivedTimeMillis() + "\n"
            + "data: " + e.getDataJson();
    }

    private void postToDiscord(String content) throws Exception {
        HttpURLConnection conn = (HttpURLConnection) new URL(WEBHOOK_URL).openConnection();
        try {
            conn.setRequestMethod("POST");
            conn.setConnectTimeout(8000);
            conn.setReadTimeout(8000);
            conn.setDoOutput(true);
            conn.setRequestProperty("Content-Type", "application/json; charset=utf-8");

            byte[] body = ("{\"content\":\"" + jsonEscape(content) + "\"}")
                .getBytes(StandardCharsets.UTF_8);
            try (OutputStream os = conn.getOutputStream()) {
                os.write(body);
            }

            int code = conn.getResponseCode();
            Log.i(TAG, "Discord webhook response: " + code);
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
