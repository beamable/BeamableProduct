// Load SDK polyfills as early as possible.
import '../src/polyfills';

import { useEffect } from 'react';
import { Stack } from 'expo-router';
import * as Linking from 'expo-linking';

import {
  addBeamableDeepLinkListener,
  addBeamableListener,
  deepLinkFromNotification,
  getLaunchNotification,
  initBeamableNotifications,
  isBeamableNotificationsSupported,
} from '../src/notifications/beamableNotifications';

export default function RootLayout() {
  // ── Beamable Notifications (native iOS + Android SDK) ──────────────────────
  // The sole notification → deep-link path. A tapped notification carries a full
  // URL deep link, which we open through the OS exactly like a real server push
  // would. Covers local + remote, foreground/background/cold-start, on both
  // platforms (iOS via the Swift core, Android via the .aar's RN bridges).
  useEffect(() => {
    if (!isBeamableNotificationsSupported) return;

    initBeamableNotifications();

    const routeFromUrl = (url: string | null) => {
      if (url) Linking.openURL(url).catch(() => {});
    };

    // Foreground delivery — a notification arrives while the app is foregrounded.
    // (Funnel analytics for delivery now happen natively; nothing to report here.)
    const presentedSub = addBeamableListener('notificationPresented', () => {});

    // App already running: user taps a Beamable notification → route to its deep link.
    const tapSub = addBeamableListener('notificationOpened', (n) => {
      routeFromUrl(deepLinkFromNotification(n as never));
    });

    // Cold start: app launched by tapping a notification (local OR remote). The
    // native SDK claims the notification-center delegate during app launch (see
    // BMNLaunchInstaller), so the launch tap is captured and surfaced here.
    getLaunchNotification().then((launch) => {
      if (launch) {
        routeFromUrl(deepLinkFromNotification(launch));
      }
    });

    // Android-only: the native deeplink module captures URL-scheme VIEW intents. We just
    // log it here — expo-router already performs the actual navigation for VIEW intents, so
    // routing again would double-navigate. No-op on iOS (the listener is a stub there).
    const deepLinkSub = addBeamableDeepLinkListener((e) => {
      console.log(
        `[Beamable] native deep link captured: ${e.url} (coldStart=${e.isColdStart})`,
      );
    });

    return () => {
      presentedSub.remove();
      tapSub.remove();
      deepLinkSub.remove();
    };
  }, []);

  return (
    <Stack>
      <Stack.Screen name="index" options={{ title: 'Beam RN Sample' }} />
      <Stack.Screen name="sdk" options={{ title: 'SDK Explorer' }} />
      <Stack.Screen name="callbacks" options={{ title: 'Notification Callbacks' }} />
      <Stack.Screen
        name="details/[id]"
        options={{ title: 'Details (Deep Link)' }}
      />
    </Stack>
  );
}
