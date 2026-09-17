// Load SDK polyfills as early as possible (from the Beamable Web SDK's RN build).
import '@beamable/sdk/react-native/polyfills';

import { useEffect } from 'react';
import { Stack } from 'expo-router';

import { BeamNotifications } from '@beamable/notifications-react-native';

import AppProviders from '../src/state/AppProviders';

export default function RootLayout() {
  // Initialize the native SDK once at app start (idempotent, safe no-op on web).
  useEffect(() => {
    BeamNotifications.initialize();

    // No `registerCategory` call is needed for campaign action buttons any more: the native SDK
    // renders the buttons the push itself carries (`buttons: [{id,title,role}]`, authored in the
    // Portal's Action Buttons style) on both platforms, and falls back to a built-in Open / Dismiss
    // pair for `style: "actions"` with no buttons. Tapping one still fires `notificationOpened` with
    // `actionId` set to the authored id, handled in src/state/notificationContext.tsx.
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

  // Notification → deep-link routing, funnel coordinates, device registration and the two log
  // streams all live in the providers (src/state/). Beamable itself connects automatically on
  // mount — see BeamProvider.
  return (
    <AppProviders>
      <Stack>
        <Stack.Screen name="(tabs)" options={{ headerShown: false }} />
        <Stack.Screen name="details/[id]" options={{ title: 'Details (Deep Link)' }} />
      </Stack>
    </AppProviders>
  );
}
