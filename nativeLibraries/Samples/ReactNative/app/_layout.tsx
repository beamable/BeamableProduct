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
  }, []);

  // ── Beamable Notifications → deep-link routing ────────────────────────────
  // A tapped notification carries a full URL deep link, which we open through the OS
  // exactly like a real server push would. The two P0 hooks own subscription lifecycle
  // and cold-start resolution — no manual addListener/getLaunchNotification effects, no
  // nativeSupported gate, no exhaustive-deps disables.

  // Warm start: user taps a Beamable notification while the app is running.
  BeamNotificationEvent('notificationOpened', (n) => {
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
