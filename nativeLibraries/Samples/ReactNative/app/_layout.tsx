// Load SDK polyfills as early as possible (from the Beamable Web SDK's RN build).
import '@beamable/sdk/react-native/polyfills';

import { useEffect } from 'react';
import { Stack } from 'expo-router';
import * as Linking from 'expo-linking';

import {
  BeamNotifications,
  BeamNotificationEvent,
  BeamLaunchNotification,
} from '@beamable/notifications-react-native';

export default function RootLayout() {
  // Initialize the native SDK once at app start (idempotent, safe no-op on web).
  useEffect(() => {
    BeamNotifications.initialize();

    // No `registerCategory` call is needed for campaign action buttons any more: the native SDK
    // renders the buttons the push itself carries (`buttons: [{id,title,role}]`, authored in the
    // Portal's Action Buttons style) on both platforms, and falls back to a built-in Open / Dismiss
    // pair for `style: "actions"` with no buttons. Tapping one still fires `notificationOpened` with
    // `actionId` set to the authored id, handled below.
    //
    // `registerCategory` remains available for APP-defined button sets — a registered category takes
    // precedence over the payload, which is the override path. Example, if you want it:
    //
    //   BeamNotifications.registerCategory({
    //     id: 'beam_actions',                          // overrides the SDK's built-in pair
    //     actions: [
    //       { id: 'accept', title: 'Accept', foreground: true },
    //       { id: 'decline', title: 'Decline', foreground: true },
    //     ],
    //   });
    //
    // Note it would then win over whatever the campaign author typed in the console, on both
    // platforms — which is why the sample leaves it off by default.
  }, []);

  // ── Beamable Notifications → deep-link routing ────────────────────────────
  // A tapped notification carries a full URL deep link, which we open through the OS
  // exactly like a real server push would. The two P0 hooks own subscription lifecycle
  // and cold-start resolution — no manual addListener/getLaunchNotification effects, no
  // nativeSupported gate, no exhaustive-deps disables.

  // Warm start: user taps a Beamable notification (body OR an action button) while running.
  BeamNotificationEvent('notificationOpened', (n) => {
    // `actionId` is set only when an action button was tapped (vs the body) — the app decides
    // what each button does. Here we just log it so the behavior is visible on-device.
    if (n.actionId) console.log(`[Beamable] action button tapped: ${n.actionId}`);
    const url = BeamNotifications.deepLinkFromNotification(n);
    if (url) Linking.openURL(url).catch(() => {});
  });

  // Cold start: app launched by tapping a notification (local OR remote).
  const launch = BeamLaunchNotification();
  useEffect(() => {
    if (!launch) return;
    const url = BeamNotifications.deepLinkFromNotification(launch);
    if (url) Linking.openURL(url).catch(() => {});
  }, [launch]);

  // Android-only: the native deeplink module captures URL-scheme VIEW intents. expo-router
  // already navigates for these, so we only log (routing again would double-navigate).
  // Inert stub on iOS / web.
  useEffect(() => {
    const sub = BeamNotifications.addDeepLinkListener((e) =>
      console.log(
        `[Beamable] native deep link captured: ${e.url} (coldStart=${e.isColdStart})`,
      ),
    );
    return () => sub.remove();
  }, []);

  return (
    <Stack>
      <Stack.Screen name="index" options={{ title: 'Beam RN Sample' }} />
      <Stack.Screen
        name="details/[id]"
        options={{ title: 'Details (Deep Link)' }}
      />
    </Stack>
  );
}
