// Load SDK polyfills as early as possible.
import '../src/polyfills';

import { useEffect, useRef } from 'react';
import { Stack, useRouter } from 'expo-router';
import * as Notifications from 'expo-notifications';
import * as Linking from 'expo-linking';

import {
  addBeamableListener,
  configureClosedAppAnalytics,
  deepLinkFromNotification,
  getLaunchNotification,
  initBeamableNotifications,
  isBeamableNotificationsSupported,
  reportDelivery,
} from '../src/notifications/beamableNotifications';

export default function RootLayout() {
  const router = useRouter();
  const responseListener = useRef<Notifications.EventSubscription | null>(null);

  useEffect(() => {
    // Route a path embedded in a notification into the app.
    const routeFromData = (data: unknown) => {
      const path = (data as { path?: string } | undefined)?.path;
      if (typeof path === 'string' && path.length > 0) {
        router.push(path as never);
      }
    };

    // 1) App already running (foreground/background): user taps a notification.
    responseListener.current =
      Notifications.addNotificationResponseReceivedListener((response) => {
        routeFromData(response.notification.request.content.data);
      });

    // 2) App was launched cold by tapping a notification.
    Notifications.getLastNotificationResponseAsync().then((response) => {
      if (response) routeFromData(response.notification.request.content.data);
    });

    return () => responseListener.current?.remove();
  }, [router]);

  // ── Beamable Notifications (native iOS SDK) ────────────────────────────────
  // The same notification → deep-link routing, but via the native module. Its
  // payload carries a full URL deep link, which we open through the OS exactly
  // like a real server push would. No-op on Android.
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

    // Cold start: app launched by tapping a Beamable notification.
    getLaunchNotification().then((launch) => {
      if (launch) {
        reportDelivery('tapped (cold start)', launch);
        routeFromUrl(deepLinkFromNotification(launch));
      }
    });

    return () => {
      presentedSub.remove();
      tapSub.remove();
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
