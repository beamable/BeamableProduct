// Load SDK polyfills as early as possible.
import '../src/polyfills';

import { useEffect } from 'react';
import { Stack } from 'expo-router';
import * as Linking from 'expo-linking';

import {
  addBeamableDeepLinkListener,
  addBeamableListener,
  configureClosedAppAnalytics,
  deepLinkFromNotification,
  getLaunchNotification,
  initBeamableNotifications,
  isBeamableNotificationsSupported,
  reportDelivery,
} from '../src/notifications/beamableNotifications';

export default function RootLayout() {
  // ── Beamable Notifications (native SDK) ────────────────────────────────────
  // This app uses the Beamable native notification SDK exclusively (no
  // expo-notifications): it owns the notification-center delegate and routes
  // deep links from its own payloads. Its payload carries the deep link, which
  // we open through the OS exactly like a real server push would.
  useEffect(() => {
    if (!isBeamableNotificationsSupported) return;

    initBeamableNotifications();

    // Point closed-app analytics at the configured endpoint (Discord webhook).
    // Writes the config into the App Group; the NSE POSTs to it on delivery.
    configureClosedAppAnalytics();

    const routeFromUrl = (url: string | null) => {
      if (url) Linking.openURL(url).catch(() => {});
    };

    // App-side delivery reporting (covers LOCAL notifications, which the NSE
    // can't). Fires the webhook in the only moments iOS runs our code:
    //  - presented: a notification arrives while the app is in the FOREGROUND
    //  - tapped: the user taps one (app was backgrounded/closed → launches now)
    const presentedSub = addBeamableListener('notificationPresented', (n) =>
      reportDelivery('delivered (foreground)', n),
    );

    // App already running: user taps a Beamable notification → route + report.
    const tapSub = addBeamableListener('notificationTapped', (n) => {
      reportDelivery('tapped', n);
      routeFromUrl(deepLinkFromNotification(n as never));
    });

    // Cold start: app launched by tapping a notification (local OR remote). The
    // native SDK claims the notification-center delegate during app launch (see
    // BMNLaunchInstaller), so the launch tap is captured and surfaced here.
    getLaunchNotification().then((launch) => {
      if (launch) {
        const url = deepLinkFromNotification(launch);
        reportDelivery('tapped (cold start)', launch);
        routeFromUrl(url);
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
