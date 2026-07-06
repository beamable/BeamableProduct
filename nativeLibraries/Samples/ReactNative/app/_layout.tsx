// Load SDK polyfills as early as possible (from the RN SDK adapter package).
import '@beamable/sdk-react-native/polyfills';

import { useEffect, useState } from 'react';
import { Stack } from 'expo-router';
import * as Linking from 'expo-linking';

import { BeamNotifications } from '../src/notifications/beamableNotifications';

export default function RootLayout() {
  // Static on iOS/Android; on web it flips to true once a Unity WebView host
  // reports a native-capable platform (see beamableNotifications.web.ts).
  const [nativeSupported, setNativeSupported] = useState(
    BeamNotifications.isSupported,
  );
  useEffect(() => {
    const sub = BeamNotifications.addSupportListener(setNativeSupported);
    return () => sub.remove();
  }, []);

  // ── Beamable Notifications (native iOS + Android SDK) ──────────────────────
  // The sole notification → deep-link path. A tapped notification carries a full
  // URL deep link, which we open through the OS exactly like a real server push
  // would. Covers local + remote, foreground/background/cold-start, on both
  // platforms (iOS via the Swift core, Android via the .aar's RN bridges).
  useEffect(() => {
    if (!nativeSupported) return;

    BeamNotifications.initialize();

    const routeFromUrl = (url: string | null) => {
      if (url) Linking.openURL(url).catch(() => {});
    };

    // Foreground delivery — a notification arrives while the app is foregrounded.
    // (Funnel analytics for delivery now happen natively; nothing to report here.)
    const presentedSub = BeamNotifications.addListener('notificationPresented', () => {});

    // App already running: user taps a Beamable notification → route to its deep link.
    const tapSub = BeamNotifications.addListener('notificationOpened', (n) => {
      routeFromUrl(BeamNotifications.deepLinkFromNotification(n));
    });

    // Cold start: app launched by tapping a notification (local OR remote). The
    // native SDK claims the notification-center delegate during app launch (see
    // BMNLaunchInstaller), so the launch tap is captured and surfaced here.
    BeamNotifications.getLaunchNotification().then((launch) => {
      if (launch) {
        routeFromUrl(BeamNotifications.deepLinkFromNotification(launch));
      }
    });

    // Android-only: the native deeplink module captures URL-scheme VIEW intents. We just
    // log it here — expo-router already performs the actual navigation for VIEW intents, so
    // routing again would double-navigate. No-op on iOS (the listener is a stub there).
    const deepLinkSub = BeamNotifications.addDeepLinkListener((e) => {
      console.log(
        `[Beamable] native deep link captured: ${e.url} (coldStart=${e.isColdStart})`,
      );
    });

    return () => {
      presentedSub.remove();
      tapSub.remove();
      deepLinkSub.remove();
    };
  }, [nativeSupported]);

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
